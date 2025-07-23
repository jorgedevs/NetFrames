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
app.MapGet("/images/{id}", (string id) =>
{
    var filePath = Path.Combine(imagesPath, $"{id}.jpg");
    if (!File.Exists(filePath))
        return Results.NotFound();

    return Results.File(filePath, "image/jpeg");
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

app.Run();