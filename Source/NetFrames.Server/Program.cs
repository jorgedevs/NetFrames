using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

app.Urls.Add("http://0.0.0.0:5000");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseStaticFiles();

app.UseHttpsRedirection();

// In-memory image store (replace with persistent storage for production)
var imageStore = new Dictionary<string, byte[]>();

var imagesPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "images");
Directory.CreateDirectory(imagesPath);

// String simple endpoint
app.MapGet("/hello", () =>
{
    return "Hello from NetFrames API!";
});

// Return all image IDs in the store
app.MapGet("/images/list", () =>
{
    var files = Directory.GetFiles(imagesPath, "*.jpg")
        .Select(f => Path.GetFileNameWithoutExtension(f))
        .ToArray();

    return Results.Ok(files);
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

        if (image.Width != width && image.Height != height)
        {
            float screenAspect = (float)((float)width! / height!);
            float imageAspect = (float)image.Width / image.Height;

            if (screenAspect == imageAspect)
            {
                image.Mutate(x => x.Resize(width ?? 0, height ?? 0));
            }
            else
            {
                Rectangle cropRect = new Rectangle();

                if (screenAspect < imageAspect)
                {
                    image.Mutate(x => x.Resize(0, height.Value));
                    cropRect = new Rectangle((int)((image.Width - width) / 2), 0, (int)width, (int)height);
                }
                else
                {
                    image.Mutate(x => x.Resize(width.Value, 0));
                    cropRect = new Rectangle(0, (int)((image.Height - height) / 2), (int)width, (int)height);
                }

                Console.WriteLine($"Cropping and resizing to {width}x{height}");
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
    var fileName = $"{id}{Path.GetExtension(file.FileName)}";
    var filePath = Path.Combine(imagesPath, fileName);

    await using (var stream = File.Create(filePath))
    {
        await file.CopyToAsync(stream);
    }

    return Results.Ok(new { id = Path.GetFileNameWithoutExtension(fileName), fileName });
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

    return Results.Ok(new { message = $"Image {id} deleted." });
});
app.Run();