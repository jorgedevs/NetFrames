using System.Text.Json;
using SixLabors.ImageSharp;
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
        .Select(id => new { id, enabled = !disabledIds.Contains(id), folderId = FolderOf(id) })
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
    using var image = await Image.LoadAsync(filePath);

    return Results.Ok(new
    {
        id,
        extension = ".jpg",
        uploadedAt = fileInfo.CreationTimeUtc,
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
        using var image = await Image.LoadAsync(filePath);
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

        using var image = await Image.LoadAsync(filePath);

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

    var id = Guid.NewGuid().ToString();
    var fileName = $"{id}.jpg";
    var filePath = Path.Combine(imagesPath, fileName);

    await using (var stream = File.Create(filePath))
    {
        await file.CopyToAsync(stream);
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
                coverId = ids.FirstOrDefault()
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
record FolderRequest(string? Name);
record MoveRequest(string[]? ImageIds, string? FolderId);

class FolderData
{
    public List<Folder> Folders { get; set; } = new();
    public Dictionary<string, string> Assignments { get; set; } = new();
}