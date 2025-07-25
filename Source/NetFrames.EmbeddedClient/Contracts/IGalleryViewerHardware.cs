using Meadow.Hardware;
using Meadow.Peripherals.Displays;

namespace NetFrames.EmbeddedClient.Contracts;

public interface IGalleryViewerHardware
{
    IPixelDisplay? Display { get; }

    RotationType DisplayRotation { get; }

    IWiFiNetworkAdapter NetworkAdapter { get; }
}