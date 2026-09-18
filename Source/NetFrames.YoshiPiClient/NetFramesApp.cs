using FieldStack;
using FieldStack.Logging;
using FieldStack.Peripherals.Displays;
using FieldStack.Units;
using NetFrames.YoshiPiClient.Controllers;

namespace NetFrames.YoshiPiClient;

public class NetFramesApp : App<RaspberryPi>
{
    public const double VERSION = 2.7;

    private const int DefaultSpiMegahertz = 62;

    private YoshiPi hardware = default!;
    private MainController mainController = default!;

    public override Task Initialize()
    {
        Resolver.Log.LogLevel = LogLevel.Information;
        Resolver.Log.Info($"NETFRAMES {VERSION}");

        var baseUrl = BaseUrlFromArgs();
        var megahertz = SpiMegahertzFromArgs();

        hardware = YoshiPi.Create(
            Device,
            new Frequency(megahertz, Frequency.UnitType.Megahertz),
            ColorMode.Format16bppRgb565,
            invertColor: true);

        Resolver.Log.Info($"display {hardware.Display.Width}x{hardware.Display.Height}, SPI {megahertz}MHz");

        mainController = new MainController(VERSION);
        mainController.Initialize(Device.NetworkAdapters, hardware.Display, RotationType._270Degrees, baseUrl);

        return Task.CompletedTask;
    }

    public override Task Run()
    {
        return mainController.Run(CancellationToken);
    }

    public override Task OnShutdown()
    {
        hardware?.Dispose();
        return Task.CompletedTask;
    }

    private static string BaseUrlFromArgs()
    {
        var args = Environment.GetCommandLineArgs();
        var index = Array.FindIndex(args, a => a.Equals("--base-url", StringComparison.OrdinalIgnoreCase));

        if (index >= 0 && index + 1 < args.Length)
        {
            return args[index + 1];
        }

        throw new ArgumentException(
            "A NetFrames.Server base URL is required. Pass it with --base-url <url>, e.g. --base-url http://192.168.1.73:5000");
    }

    private static int SpiMegahertzFromArgs()
    {
        var args = Environment.GetCommandLineArgs();
        var index = Array.FindIndex(args, a => a.Equals("--mhz", StringComparison.OrdinalIgnoreCase));

        return index >= 0 && index + 1 < args.Length && int.TryParse(args[index + 1], out var parsed)
            ? parsed
            : DefaultSpiMegahertz;
    }
}
