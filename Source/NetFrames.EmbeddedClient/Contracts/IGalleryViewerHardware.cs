using Meadow.Peripherals.Displays;
using Meadow.Peripherals.Sensors.Buttons;

namespace NetFrames.EmbeddedClient.Contracts;

public interface IGalleryViewerHardware
{
    IButton? LeftButton { get; }

    IButton? RightButton { get; }

    IPixelDisplay? Display { get; }

    RotationType DisplayRotation { get; }
}
