using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Processing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

// Uncomment for receiving requests from other devices on the network
app.Urls.Add("http://0.0.0.0:5233");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseStaticFiles();

// Only ever decode JPEGs. Photos are stored as .jpg, and keeping the other decoders (TIFF, etc.)
// out of reach avoids ImageSharp 3.x advisories such as GHSA-wmxv-xphr-5c9g (BigTIFF decoder hang).
var jpegOnly = new DecoderOptions { Configuration = new Configuration(new JpegConfigurationModule()) };
const long MaxUploadBytes = 20 * 1024 * 1024;

// In-memory image store (replace with persistent storage for production)
var imageStore = new Dictionary<string, byte[]>();

var imagesPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "images");
Directory.CreateDirectory(imagesPath);

// Disabled images tracking
var disabledPath = Path.Combine(imagesPath, "disabled.json");
var disabledIds = new HashSet<string>();
if (File.Exists(disabledPath))
{
    disabledIds = JsonSerializer.Deserialize<HashSet<string>>(File.ReadAllText(disabledPath)) ?? new();
}
void SaveDisabled() => File.WriteAllText(disabledPath, JsonSerializer.Serialize(disabledIds));

// Folders: metadata only, image files never move. An image belongs to at most one folder.
var foldersPath = Path.Combine(imagesPath, "folders.json");
var foldersLock = new object();
var folderData = new FolderData();
if (File.Exists(foldersPath))
{
    folderData = JsonSerializer.Deserialize<FolderData>(File.ReadAllText(foldersPath)) ?? new();
}
void SaveFolders() => File.WriteAllText(foldersPath, JsonSerializer.Serialize(folderData));

// Date a photo was taken, from EXIF when present, otherwise the file's date. Only the image
// header is read, and results are cached since a photo's content never changes for its id.
var photoDates = new ConcurrentDictionary<string, PhotoDate>();
string[] exifDateFormats = ["yyyy:MM:dd HH:mm:ss", "yyyy-MM-dd HH:mm:ss", "yyyy:MM:dd HH:mm"];

PhotoDate GetPhotoDate(string id) => photoDates.GetOrAdd(id, key =>
{
    var path = Path.Combine(imagesPath, $"{key}.jpg");
    try
    {
        var exif = Image.Identify(jpegOnly, path).Metadata.ExifProfile;
        if (exif is not null)
        {
            foreach (var tag in new[] { ExifTag.DateTimeOriginal, ExifTag.DateTimeDigitized, ExifTag.DateTime })
            {
                if (exif.TryGetValue(tag, out var value)
                    && DateTime.TryParseExact(value.Value?.Trim('\0', ' '), exifDateFormats,
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var taken))
                {
                    return new PhotoDate(taken, true);
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Could not read EXIF for {key}: {ex.Message}");
    }

    // Camera EXIF times are wall-clock times, so keep the fallback in the same form.
    return new PhotoDate(DateTime.SpecifyKind(File.GetLastWriteTime(path), DateTimeKind.Unspecified), false);
});

string? FolderOf(string imageId)
{
    lock (foldersLock)
    {
        return folderData.Assignments.TryGetValue(imageId, out var folderId) ? folderId : null;
    }
}

// String simple endpoint
app.MapGet("/hello", () =>
{
    return "Hello from NetFrames API!";
});

// Return enabled image IDs (for embedded client)
app.MapGet("/images/list", () =>
{
    var files = Directory.GetFiles(imagesPath, "*.jpg")
        .Select(f => Path.GetFileNameWithoutExtension(f))
        .Where(id => !disabledIds.Contains(id))
        .ToArray();

    return Results.Ok(files);
});

// Return all images with enabled/disabled status (for WebPortal)
app.MapGet("/images/list/all", () =>
{
    var files = Directory.GetFiles(imagesPath, "*.jpg")
        .Select(f => Path.GetFileNameWithoutExtension(f))
        .Select(id => new { id, date = GetPhotoDate(id) })
        .OrderByDescending(x => x.date.TakenAt)
        .Select(x => new
        {
            id = x.id,
            enabled = !disabledIds.Contains(x.id),
            folderId = FolderOf(x.id),
            takenAt = x.date.TakenAt,
            hasTakenDate = x.date.FromExif
        })
        .ToArray();

    return Results.Ok(files);
});

// Toggle image visibility
app.MapPost("/images/{id}/toggle", (string id) =>
{
    var filePath = Path.Combine(imagesPath, $"{id}.jpg");
    if (!File.Exists(filePath))
        return Results.NotFound();

    bool enabled;
    if (disabledIds.Contains(id))
    {
        disabledIds.Remove(id);
        enabled = true;
    }
    else
    {
        disabledIds.Add(id);
        enabled = false;
    }
    SaveDisabled();

    return Results.Ok(new { id, enabled, folderId = FolderOf(id) });
});

// Get image metadata
app.MapGet("/images/{id}/info", async (string id) =>
{
    var filePath = Path.Combine(imagesPath, $"{id}.jpg");
    if (!File.Exists(filePath))
        return Results.NotFound();

    var fileInfo = new FileInfo(filePath);
    var image = await Image.IdentifyAsync(jpegOnly, filePath);
    var date = GetPhotoDate(id);

    return Results.Ok(new
    {
        id,
        extension = ".jpg",
        uploadedAt = fileInfo.CreationTimeUtc,
        takenAt = date.TakenAt,
        hasTakenDate = date.FromExif,
        width = image.Width,
        height = image.Height
    });
});

// Thumbnail endpoint (generated once, cached on disk). Image ids are GUIDs and never change,
// so responses can be cached by the browser for a long time.
var thumbsPath = Path.Combine(app.Environment.ContentRootPath, "thumbs");
Directory.CreateDirectory(thumbsPath);
const int ThumbWidth = 400;
const int ThumbHeight = 300;

app.MapGet("/images/{id}/thumb", async (string id, HttpContext http) =>
{
    var filePath = Path.Combine(imagesPath, $"{id}.jpg");
    if (!File.Exists(filePath))
        return Results.NotFound();

    var thumbPath = Path.Combine(thumbsPath, $"{id}.jpg");
    if (!File.Exists(thumbPath))
    {
        using var image = await Image.LoadAsync(jpegOnly, filePath);
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(ThumbWidth, ThumbHeight),
            Mode = ResizeMode.Crop
        }));
        var tmpPath = thumbPath + ".tmp";
        await image.SaveAsJpegAsync(tmpPath);
        File.Move(tmpPath, thumbPath, overwrite: true);
    }

    http.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
    return Results.File(thumbPath, "image/jpeg");
});

// Get image endpoint (serve from disk)
app.MapGet("/images/{id}", async (string id, int? width, int? height, HttpContext http) =>
{
    http.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
    var filePath = Path.Combine(imagesPath, $"{id}.jpg");
    Console.WriteLine($"Requested image id: {id}");
    Console.WriteLine($"File path: {filePath}");
    Console.WriteLine($"Width: {width}, Height: {height}");

    if (!File.Exists(filePath))
    {
        Console.WriteLine("File not found.");
        return Results.NotFound();
    }

    try
    {
        if (width is null && height is null)
        {
            Console.WriteLine("Returning original image.");
            return Results.File(filePath, "image/jpeg");
        }

        using var image = await Image.LoadAsync(jpegOnly, filePath);

        if (width is null || height is null)
        {
            // Only one dimension was requested: resize preserving aspect ratio.
            image.Mutate(x => x.Resize(width ?? 0, height ?? 0));
        }
        else if (image.Width != width.Value || image.Height != height.Value)
        {
            int w = width.Value;
            int h = height.Value;

            float screenAspect = (float)w / h;
            float imageAspect = (float)image.Width / image.Height;

            if (screenAspect == imageAspect)
            {
                image.Mutate(x => x.Resize(w, h));
            }
            else
            {
                Rectangle cropRect;

                if (screenAspect < imageAspect)
                {
                    image.Mutate(x => x.Resize(0, h));
                    cropRect = new Rectangle((image.Width - w) / 2, 0, w, h);
                }
                else
                {
                    image.Mutate(x => x.Resize(w, 0));
                    cropRect = new Rectangle(0, (image.Height - h) / 2, w, h);
                }

                Console.WriteLine($"Cropping and resizing to {w}x{h}");
                image.Mutate(x => x.Crop(cropRect));
            }
        }

        var ms = new MemoryStream();
        await image.SaveAsJpegAsync(ms);
        ms.Position = 0;
        Console.WriteLine("Returning processed image.");
        return Results.File(ms, "image/jpeg");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Exception: {ex}");
        return Results.Problem("Internal Server Error");
    }
});

// Upload image endpoint (save to disk)
app.MapPost("/images/upload", async (HttpRequest request) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest("Form content type required.");

    var form = await request.ReadFormAsync();
    var file = form.Files["image"];
    if (file == null || file.Length == 0)
        return Results.BadRequest("No image uploaded.");
    if (file.Length > MaxUploadBytes)
        return Results.BadRequest("Image exceeds the maximum size of 20 MB.");

    // Accept only files that really are JPEGs, whatever their name or content type says.
    using var data = new MemoryStream();
    await file.CopyToAsync(data);
    try
    {
        data.Position = 0;
        await Image.IdentifyAsync(jpegOnly, data);
    }
    catch (Exception ex) when (ex is UnknownImageFormatException or InvalidImageContentException)
    {
        return Results.BadRequest("Only JPEG images are supported.");
    }

    var id = Guid.NewGuid().ToString();
    var fileName = $"{id}.jpg";
    var filePath = Path.Combine(imagesPath, fileName);

    data.Position = 0;
    await using (var stream = File.Create(filePath))
    {
        await data.CopyToAsync(stream);
    }

    return Results.Ok(new { id, fileName });
})
.WithName("UploadImage");

// Delete image endpoint (remove from disk)
app.MapDelete("/images/{id}", (string id) =>
{
    var filePath = Path.Combine(imagesPath, $"{id}.jpg");
    if (!File.Exists(filePath))
    {
        return Results.NotFound();
    }

    File.Delete(filePath);
    var thumbFile = Path.Combine(thumbsPath, $"{id}.jpg");
    if (File.Exists(thumbFile)) File.Delete(thumbFile);
    photoDates.TryRemove(id, out _);
    if (disabledIds.Remove(id)) SaveDisabled();
    lock (foldersLock)
    {
        if (folderData.Assignments.Remove(id)) SaveFolders();
    }

    return Results.Ok(new { message = $"Image {id} deleted." });
});

// List folders with image counts and a cover image
app.MapGet("/folders", () =>
{
    var existing = Directory.GetFiles(imagesPath, "*.jpg")
        .Select(f => Path.GetFileNameWithoutExtension(f))
        .ToHashSet();

    lock (foldersLock)
    {
        var result = folderData.Folders.Select(f =>
        {
            var ids = folderData.Assignments
                .Where(a => a.Value == f.Id && existing.Contains(a.Key))
                .Select(a => a.Key)
                .ToList();
            return new
            {
                id = f.Id,
                name = f.Name,
                count = ids.Count,
                enabledCount = ids.Count(i => !disabledIds.Contains(i)),
                coverId = ids.OrderByDescending(i => GetPhotoDate(i).TakenAt).FirstOrDefault()
            };
        });
        return Results.Ok(result.ToArray());
    }
});

app.MapPost("/folders", (FolderRequest request) =>
{
    var name = request.Name?.Trim();
    if (string.IsNullOrEmpty(name))
        return Results.BadRequest("Folder name is required.");

    lock (foldersLock)
    {
        if (folderData.Folders.Any(f => f.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return Results.Conflict("A folder with that name already exists.");

        var folder = new Folder(Guid.NewGuid().ToString(), name);
        folderData.Folders.Add(folder);
        SaveFolders();
        return Results.Ok(new { id = folder.Id, name = folder.Name, count = 0, enabledCount = 0, coverId = (string?)null });
    }
});

app.MapPut("/folders/{id}", (string id, FolderRequest request) =>
{
    var name = request.Name?.Trim();
    if (string.IsNullOrEmpty(name))
        return Results.BadRequest("Folder name is required.");

    lock (foldersLock)
    {
        var index = folderData.Folders.FindIndex(f => f.Id == id);
        if (index < 0)
            return Results.NotFound();
        if (folderData.Folders.Any(f => f.Id != id && f.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            return Results.Conflict("A folder with that name already exists.");

        folderData.Folders[index] = folderData.Folders[index] with { Name = name };
        SaveFolders();
        return Results.Ok(new { id, name });
    }
});

// Deleting a folder only returns its images to the top level; photos are never deleted.
app.MapDelete("/folders/{id}", (string id) =>
{
    lock (foldersLock)
    {
        if (folderData.Folders.RemoveAll(f => f.Id == id) == 0)
            return Results.NotFound();

        foreach (var key in folderData.Assignments.Where(a => a.Value == id).Select(a => a.Key).ToList())
            folderData.Assignments.Remove(key);
        SaveFolders();
        return Results.Ok(new { message = $"Folder {id} deleted." });
    }
});

// Move images into a folder (or back to the top level when folderId is null)
app.MapPost("/images/move", (MoveRequest request) =>
{
    lock (foldersLock)
    {
        if (request.FolderId is not null && !folderData.Folders.Any(f => f.Id == request.FolderId))
            return Results.NotFound("Folder not found.");

        foreach (var imageId in request.ImageIds ?? [])
        {
            if (!File.Exists(Path.Combine(imagesPath, $"{imageId}.jpg")))
                continue;

            if (request.FolderId is null)
                folderData.Assignments.Remove(imageId);
            else
                folderData.Assignments[imageId] = request.FolderId;
        }
        SaveFolders();
        return Results.Ok();
    }
});

app.Run();

record Folder(string Id, string Name);
record PhotoDate(DateTime TakenAt, bool FromExif);
record FolderRequest(string? Name);
record MoveRequest(string[]? ImageIds, string? FolderId);

class FolderData
{
    public List<Folder> Folders { get; set; } = new();
    public Dictionary<string, string> Assignments { get; set; } = new();
}