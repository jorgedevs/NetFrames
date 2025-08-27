using Meadow;
using Meadow.Foundation.Displays;
using Meadow.Hardware;
using Meadow.Peripherals.Displays;
using Meadow.Units;
using NetFrames.EmbeddedClient.Contracts;

namespace NetFrames.EmbeddedClient.Hardware;

public class NetFramesF7FeatherHardware : INetFramesHardware
{
    private readonly IF7FeatherMeadowDevice featherF7;

    public IPixelDisplay? Display { get; }

    public RotationType DisplayRotation => RotationType._270Degrees;

    public IWiFiNetworkAdapter NetworkAdapter => featherF7.NetworkAdapters.Primary<IWiFiNetworkAdapter>();

    public NetFramesF7FeatherHardware(IF7FeatherMeadowDevice featherF7)
    {
        this.featherF7 = featherF7;

        var config = new SpiClockConfiguration(new Frequency(48000, Frequency.UnitType.Kilohertz), SpiClockConfiguration.Mode.Mode3);
        var spiBus = featherF7.CreateSpiBus(featherF7.Pins.SCK, featherF7.Pins.MOSI, featherF7.Pins.MISO, config);

        Display = new Ili9341
        (
            spiBus: spiBus,
            chipSelectPin: featherF7.Pins.D02,
            dcPin: featherF7.Pins.D01,
            resetPin: featherF7.Pins.D00,
            width: 240, height: 320,
            ColorMode.Format12bppRgb444
        );
    }
}