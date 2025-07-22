using Meadow;
using Meadow.Devices;
using Meadow.Foundation;
using System;
using System.Threading.Tasks;

namespace NetFrames.EmbeddedClient;

public class MeadowApp : ProjectLabCoreComputeApp
{
    private DisplayController? displayController;

    public override Task Initialize()
    {
        Resolver.Log.LogLevel = Meadow.Logging.LogLevel.Trace;

        Resolver.Log.Info($"Running on ProjectLab Hardware {Hardware.RevisionString}");

        if (Hardware.RgbLed is { } rgbLed)
        {
            rgbLed.SetColor(Color.Blue);
        }

        if (Hardware.Display is { } display)
        {
            Resolver.Log.Trace("Creating DisplayController");
            displayController = new DisplayController(display);
            Resolver.Log.Trace("DisplayController up");
        }

        Resolver.Log.Info("Initialization complete");

        return base.Initialize();
    }

    public override async Task Run()
    {
        Resolver.Log.Info("Run...");

        if (Hardware?.RgbLed is { } rgbLed)
        {
            Resolver.Log.Info("starting blink");
            _ = rgbLed.StartBlink(
                WildernessLabsColors.PearGreen,
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(2000),
                0.5f);
        }
    }
}