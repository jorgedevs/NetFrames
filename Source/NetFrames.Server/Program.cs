var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// In-memory image store (replace with persistent storage for production)
var imageStore = new Dictionary<string, byte[]>();

// String simple endpoint
app.MapGet("/hello", () => "Hello from NetFrames API!");

// Upload image endpoint
app.MapPost("/images/upload", async (HttpRequest request) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest("Form content type required.");

    var form = await request.ReadFormAsync();
    var file = form.Files["image"];
    if (file == null || file.Length == 0)
        return Results.BadRequest("No image uploaded.");

    using var ms = new MemoryStream();
    await file.CopyToAsync(ms);
    var imageBytes = ms.ToArray();
    var id = Guid.NewGuid().ToString();

    imageStore[id] = imageBytes;

    return Results.Ok(new { id });
})
.WithName("UploadImage");

// Get image endpoint
app.MapGet("/images/{id}", (string id) =>
{
    if (!imageStore.TryGetValue(id, out var imageBytes))
        return Results.NotFound();

    return Results.File(imageBytes, "image/jpeg");
})
.WithName("GetImage");

app.Run();