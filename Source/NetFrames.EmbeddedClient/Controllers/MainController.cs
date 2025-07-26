using Meadow;
using NetFrames.EmbeddedClient.Contracts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetFrames.EmbeddedClient.Controllers;

public class MainController
{
    private List<string> imageFilenames;

    private IGalleryViewerHardware hardware;

    private DisplayController displayController;
    private RestClientController restClientController;

    public MainController() { }

    public async Task Initialize(IGalleryViewerHardware hardware)
    {
        this.hardware = hardware;

        imageFilenames = new List<string>();

        displayController = new DisplayController(
            this.hardware.Display,
            this.hardware.DisplayRotation);
        displayController.DrawSplashScreen();

        restClientController = new RestClientController();
    }

    private async Task GetImagesAsync()
    {
        await restClientController.GetImageFilenamesAsync()
            .ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    imageFilenames = task.Result;
                    Resolver.Log.Info($"Fetched {imageFilenames.Count} images.");
                }
                else
                {
                    Resolver.Log.Error("Failed to fetch image filenames.");
                }
            });
    }

    public async Task Run()
    {
        while (true)
        {
            if (hardware.NetworkAdapter.IsConnected)
            {
                Resolver.Log.Info("Network is connected. Fetching images...");
                await GetImagesAsync();
                var imageData = await restClientController.GetImageAsync(imageFilenames[0]);
                if (imageData.Length > 0)
                {
                    displayController.DisplayImage(imageData);
                }
                else
                {
                    Resolver.Log.Error("Failed to fetch image data.");
                }
            }
            else
            {
                Resolver.Log.Info("Network is not connected. Retrying...");
            }

            Resolver.Log.Info($"Is Connected: {hardware.NetworkAdapter.IsConnected}");

            await Task.Delay(10000);
        }
    }
}