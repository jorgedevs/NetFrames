using Meadow;
using Meadow.Devices;
using NetFrames.EmbeddedClient.Controllers;
using NetFrames.EmbeddedClient.Hardware;
using System.Threading.Tasks;

namespace NetFrames.EmbeddedClient;

//public class MeadowApp : ProjectLabCoreComputeApp
public class MeadowApp : App<F7FeatherV2>
{
    public static double VERSION { get; set; } = 1.6;

    private MainController? mainController;

    public override Task Initialize()
    {
        Resolver.Log.Info("Initialize...");

        //var hardware = new NetFramesProjectLabHardware(Hardware);
        var hardware = new NetFramesF7FeatherHardware(Device);

        mainController = new MainController();
        mainController?.Initialize(hardware);

        return Task.CompletedTask;
    }

    public override Task Run()
    {
        Resolver.Log.Info("Run...");

        mainController?.Run();

        return Task.CompletedTask;
    }
}
