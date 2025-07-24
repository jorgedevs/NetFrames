using Meadow;
using Meadow.Devices;
using NetFrames.EmbeddedClient.Controllers;
using NetFrames.EmbeddedClient.Hardware;
using System.Threading.Tasks;

namespace NetFrames.EmbeddedClient;

public class MeadowApp : ProjectLabCoreComputeApp
{
    private MainController? mainController;

    public override Task Initialize()
    {
        Resolver.Log.Info("Initialize...");

        var hardware = new GalleryViewerProjectLabHardware(Hardware);

        mainController = new MainController();
        mainController.Initialize(hardware);

        return Task.CompletedTask;
    }

    public override Task Run()
    {
        Resolver.Log.Info("Run...");

        return Task.CompletedTask;
    }
}