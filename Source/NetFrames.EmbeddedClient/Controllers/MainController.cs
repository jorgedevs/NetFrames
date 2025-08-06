using Meadow;
using NetFrames.EmbeddedClient.Contracts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetFrames.EmbeddedClient.Controllers;

public class MainController
{
    private int counter;
    private Random random;
    private List<string> imageFilenames;

    private INetFramesHardware hardware;

    private DisplayController displayController;
    private RestClientController restClientController;

    public MainController() { }

    public Task Initialize(INetFramesHardware hardware)
    {
        this.hardware = hardware;

        counter = 0;
        random = new Random();
        imageFilenames = new List<string>();

        displayController = new DisplayController(
            this.hardware.Display,
            this.hardware.DisplayRotation);
        displayController.DrawSplashScreen();

        restClientController = new RestClientController();

        return Task.CompletedTask;
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
                if (imageFilenames.Count == 0)
                {
                    Resolver.Log.Info("Network is connected. Fetching images...");
                    await GetImagesAsync();
                    await Task.Delay(TimeSpan.FromSeconds(5)); // Attempt to prevent ESP32 panic
                }

                if (imageFilenames.Count > 0)
                {
                    var imageData = await restClientController.GetImageAsync(imageFilenames[random.Next(imageFilenames.Count)]);
                    if (imageData.Length > 0)
                    {
                        displayController.DisplayImage(imageData);
                        counter++;
                        Resolver.Log.Info($"Endpoint counter {counter}");

                        await Task.Delay(TimeSpan.FromMinutes(1));
                    }
                    else
                    {
                        Resolver.Log.Error("Failed to fetch image data.");

                        await Task.Delay(TimeSpan.FromSeconds(10));
                    }
                }
            }
            else
            {
                Resolver.Log.Info("Network is not connected. Retrying...");

                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }
    }
}