using Meadow;
using Meadow.Devices;
using NetFrames.EmbeddedClient.Controllers;
using NetFrames.EmbeddedClient.Hardware;
using System.Threading.Tasks;

namespace NetFrames.EmbeddedClient;

/*

OTA instructions:

1. Bump VERSION value below

2. Open a Terminal (VS2022 - View -> Terminal) and create an mpak file: 

    meadow cloud package create --name <filename>.mpak

3. Upload the mpak file to Meadow.Cloud:

    meadow cloud package upload bin\Release\netstandard2.1\mpak\<filename>.mpak

4. Publish the mpak uploaded to roll out an OTA Update:

    Go to Meadow.Cloud (https://www.meadowcloud.co/) -> Packages, click Publish on the .mpak uploaded

*/

//public class MeadowApp : ProjectLabCoreComputeApp
public class MeadowApp : App<F7FeatherV2>
{
    public static double VERSION { get; set; } = 2.1;

    private MainController? mainController;

    public override Task Initialize()
    {
        Resolver.Log.Info("Initialize...");

        Settings.TryGetValue("Settings.BASE_URL", out RestClientController.BASE_URL);

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
