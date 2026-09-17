using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

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
        .Select(id => new { id, enabled = !disabledIds.Contains(id) })
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

    return Results.Ok(new { id, enabled });
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

// Get image endpoint (serve from disk)
app.MapGet("/images/{id}", async (string id, int? width, int? height) =>
{
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
    if (disabledIds.Remove(id)) SaveDisabled();

    return Results.Ok(new { message = $"Image {id} deleted." });
});
app.Run();