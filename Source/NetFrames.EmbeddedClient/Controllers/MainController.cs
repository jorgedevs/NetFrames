using Meadow;
using Meadow.Update;
using NetFrames.EmbeddedClient.Contracts;
using System;
using System.Collections.Generic;
using System.Threading;
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
        displayController.ShowSplashScreen();
        Thread.Sleep(5000);
        displayController.ShowGalleryScreen();

        restClientController = new RestClientController();

        var updateService = Resolver.UpdateService;
        updateService.StateChanged += OnStateChanged;
        updateService.UpdateAvailable += OnUpdateAvailable;
        updateService.UpdateRetrieved += OnUpdateRetrieved;
        updateService.RetrieveProgress += OnRetrieveProgress;

        var cloudService = Resolver.MeadowCloudService;
        cloudService.ConnectionStateChanged += OnCloudStateChanged;

        return Task.CompletedTask;
    }

    private void OnStateChanged(object sender, UpdateState e)
    {
        displayController.UpdateStatus($"{FormatStatusMessage(e)}");
    }

    private void OnUpdateAvailable(IUpdateService updateService, UpdateInfo info, CancellationTokenSource cancel)
    {
        _ = hardware.RgbPwmLed.StartBlink(Color.Magenta);
        displayController.UpdateStatus("Update available!");
    }

    private void OnUpdateRetrieved(IUpdateService updateService, UpdateInfo info, CancellationTokenSource cancel)
    {
        _ = hardware.RgbPwmLed.StartBlink(Color.Cyan);
        displayController.UpdateStatus("Update retrieved!");
    }

    private void OnRetrieveProgress(IUpdateService updateService, UpdateInfo info, CancellationTokenSource cancel)
    {
        short percentage = (short)(((double)info.DownloadProgress / info.FileSize) * 100);
        displayController.UpdateDownloadProgress(percentage);
    }

    private void OnCloudStateChanged(object sender, CloudConnectionState e)
    {
        displayController.UpdateStatus($"{FormatStatusMessage(e)}");
    }

    private async Task GetImagesAsync()
    {
        _ = hardware.RgbPwmLed.StartBlink(Color.Red);

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

        _ = hardware.RgbPwmLed.StartBlink(Color.Green);
    }

    private string FormatStatusMessage(UpdateState state)
    {
        string message = string.Empty;

        switch (state)
        {
            case UpdateState.Dead: message = "Failed"; break;
            case UpdateState.Disconnected: message = "Disconnected"; break;
            case UpdateState.Connected: message = "Connected!"; break;
            case UpdateState.DownloadingFile: message = "Downloading File..."; break;
            case UpdateState.UpdateInProgress: message = "Update In Progress..."; break;
        }

        return message;
    }

    private string FormatStatusMessage(CloudConnectionState state)
    {
        string message = string.Empty;

        switch (state)
        {
            case CloudConnectionState.Disconnected: message = "Disconnected"; break;
            case CloudConnectionState.Authenticating: message = "Authenticating..."; break;
            case CloudConnectionState.Connecting: message = "Connecting..."; break;
            case CloudConnectionState.Connected: message = "Connected!"; break;
        }

        return message;
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