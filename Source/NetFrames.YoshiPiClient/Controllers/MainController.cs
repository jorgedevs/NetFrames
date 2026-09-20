using FieldStack;
using FieldStack.Hardware;
using FieldStack.Peripherals.Displays;

namespace NetFrames.YoshiPiClient.Controllers;

public class MainController
{
    private readonly double version;

    private int counter;
    private Random random = default!;

    private List<string> images = new();
    private readonly List<string> imagesShown = new();

    private INetworkAdapterCollection networkAdapters = default!;
    private IPixelDisplay display = default!;

    private DisplayController displayController = default!;
    private RestClientController restClientController = default!;

    public MainController(double version)
    {
        this.version = version;
    }

    public void Initialize(
        INetworkAdapterCollection networkAdapters,
        IPixelDisplay display,
        RotationType displayRotation,
        string baseUrl)
    {
        this.networkAdapters = networkAdapters;
        this.display = display;

        counter = 0;
        random = new Random();
        images = new List<string>();

        displayController = new DisplayController(version, display, displayRotation);
        displayController.LoadSplashScreen();
        Thread.Sleep(5000);
        displayController.LoadGalleryScreen();
        displayController.ShowGalleryScreen();

        restClientController = new RestClientController(baseUrl);
    }

    private async Task GetImagesAsync()
    {
        var allImages = await restClientController.GetImageFilenamesAsync();

        Resolver.Log.Info($"Fetched {allImages.Count} images.");

        foreach (var imageShown in imagesShown)
        {
            allImages.Remove(imageShown);
        }

        if (allImages.Count == 0)
        {
            Resolver.Log.Info("All images have been shown. Resetting shown images list.");
            imagesShown.Clear();
            allImages = await restClientController.GetImageFilenamesAsync();
        }

        images = allImages;
    }

    public async Task Run(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (networkAdapters.Any(a => a.IsConnected))
                {
                    if (images.Count == 0)
                    {
                        Resolver.Log.Info("Network is connected. Fetching images...");
                        displayController.UpdateStatus("Fetching images...");
                    }

                    // Refresh every cycle so newly uploaded, enabled or disabled images are picked up.
                    await GetImagesAsync();

                    if (images.Count == 0)
                    {
                        Resolver.Log.Info("No images available. Retrying...");
                        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                    }
                    else
                    {
                        displayController.UpdateStatus(string.Empty);

                        var imageId = images[random.Next(images.Count)];
                        var imageData = await restClientController.GetImageAsync(imageId, display.Width, display.Height);

                        if (imageData.Length > 0)
                        {
                            counter++;
                            imagesShown.Add(imageId);
                            displayController.DisplayImage(imageData, counter);

                            Resolver.Log.Info($"Displayed image {counter}: {imageId}");
                            await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                        }
                        else
                        {
                            Resolver.Log.Error("Failed to fetch image data.");
                            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                        }
                    }
                }
                else
                {
                    Resolver.Log.Info("Network is not connected. Retrying...");
                    displayController.UpdateStatus("Network disconnected");
                    await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Resolver.Log.Error($"Error displaying image: {ex.GetType().Name}: {ex.Message}");

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
