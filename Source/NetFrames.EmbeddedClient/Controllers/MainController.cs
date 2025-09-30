using Meadow;
using Meadow.Logging;
using Meadow.Update;
using NetFrames.EmbeddedClient.Commands;
using NetFrames.EmbeddedClient.Contracts;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NetFrames.EmbeddedClient.Controllers;

public class MainController
{
    private int counter;
    private bool isUpdating;
    private Random random;

    private List<string> images = new List<string>();
    private List<string> imagesShown = new List<string>();

    private INetFramesHardware hardware;

    private DisplayController displayController;
    private RestClientController restClientController;

    public MainController() { }

    public Task Initialize(INetFramesHardware hardware)
    {
        this.hardware = hardware;

        var cloudLogger = new CloudLogger();
        Resolver.Log.AddProvider(cloudLogger);
        Resolver.Services.Add(cloudLogger);

        counter = 0;
        random = new Random();
        images = new List<string>();

        displayController = new DisplayController(
            this.hardware.Display,
            this.hardware.DisplayRotation);
        displayController.LoadSplashScreen();
        Thread.Sleep(5000);
        displayController.LoadGalleryScreen();

        restClientController = new RestClientController();

        var updateService = Resolver.UpdateService;
        updateService.StateChanged += OnStateChanged;
        updateService.UpdateAvailable += OnUpdateAvailable;
        updateService.UpdateRetrieved += OnUpdateRetrieved;
        updateService.RetrieveProgress += OnRetrieveProgress;

        Resolver.CommandService.Subscribe<ResetCommand>(command =>
        {
            Resolver.Log.Info($"Forcing reset");
            Resolver.Device?.PlatformOS.Reset();
        });

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
        isUpdating = true;
        hardware?.RgbPwmLed?.StartBlink(Color.Magenta);
        displayController.ShowSplashScreen();
        displayController.UpdateStatus("Update available!");
    }

    private void OnUpdateRetrieved(IUpdateService updateService, UpdateInfo info, CancellationTokenSource cancel)
    {
        hardware?.RgbPwmLed?.StartBlink(Color.Cyan);
        displayController.UpdateStatus("Update retrieved!");
    }

    private void OnRetrieveProgress(IUpdateService updateService, UpdateInfo info, CancellationTokenSource cancel)
    {
        displayController.UpdateStatus("Downloading update...");
        short percentage = (short)((double)info.DownloadProgress / info.FileSize * 100);
        displayController.UpdateDownloadProgress(percentage);
    }

    private void OnCloudStateChanged(object sender, CloudConnectionState e)
    {
        displayController.UpdateStatus($"{FormatStatusMessage(e)}");
    }

    private async Task GetImagesAsync()
    {
        hardware?.RgbPwmLed?.StartBlink(Color.Red);

        await restClientController.GetImageFilenamesAsync()
            .ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    var allImages = task.Result;

                    Resolver.Log.Info($"Fetched {allImages.Count} images.");

                    foreach (var imageShown in imagesShown)
                    {
                        allImages.Remove(imageShown);
                    }

                    if (allImages.Count == 0)
                    {
                        Resolver.Log.Info("All images have been shown. Resetting shown images list.");
                        imagesShown.Clear();
                        allImages = task.Result;
                    }

                    images = allImages;
                }
                else
                {
                    Resolver.Log.Error("Failed to fetch image filenames.");
                }
            });

        hardware?.RgbPwmLed?.StartBlink(Color.Green);
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
        while (!isUpdating)
        {
            // Show a sample image for testing purposes
            //displayController.DisplaySampleImage();
            //await Task.Delay(TimeSpan.FromMinutes(1));

            if (hardware.NetworkAdapter.IsConnected)
            {
                if (images.Count == 0)
                {
                    Resolver.Log.Info("Network is connected. Fetching images...");
                    await GetImagesAsync();
                    await Task.Delay(TimeSpan.FromSeconds(5)); // Attempt to prevent ESP32 panic
                }

                if (images.Count > 0)
                {
                    string imageId = images[random.Next(images.Count)];
                    var imageData = await restClientController.GetImageAsync(imageId);

                    if (imageData.Length > 0)
                    {
                        counter++;
                        imagesShown.Add(imageId);
                        displayController.DisplayImage(imageData, counter);

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