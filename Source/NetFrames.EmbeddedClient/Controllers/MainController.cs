using Meadow;
using NetFrames.EmbeddedClient.Contracts;
using System.Threading.Tasks;

namespace NetFrames.EmbeddedClient.Controllers;

public class MainController
{
    private IGalleryViewerHardware hardware;

    private DisplayController displayController;

    public MainController() { }

    public Task Initialize(IGalleryViewerHardware hardware)
    {
        this.hardware = hardware;

        displayController = new DisplayController(
            this.hardware.Display,
            this.hardware.DisplayRotation);

        displayController.DrawSplashScreen();

        return Task.CompletedTask;
    }

    public async Task Run()
    {
        while (true)
        {
            Resolver.Log.Info($"Is Connected: {hardware.NetworkAdapter.IsConnected}");

            await Task.Delay(1000);
        }
    }
}