using Meadow.Hardware;
using Meadow.Peripherals.Displays;
using Meadow.Peripherals.Leds;

namespace NetFrames.EmbeddedClient.Contracts;

public interface INetFramesHardware
{
    IRgbPwmLed? RgbPwmLed { get; }

    IPixelDisplay? Display { get; }

    RotationType DisplayRotation { get; }

    IWiFiNetworkAdapter NetworkAdapter { get; }
}