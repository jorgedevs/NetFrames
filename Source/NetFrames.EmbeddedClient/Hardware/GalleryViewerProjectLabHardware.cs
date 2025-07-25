using Meadow.Devices;
using Meadow.Peripherals.Displays;
using Meadow.Peripherals.Sensors.Buttons;
using NetFrames.EmbeddedClient.Contracts;

namespace NetFrames.EmbeddedClient.Hardware;

public class GalleryViewerProjectLabHardware : IGalleryViewerHardware
{
    private readonly IProjectLabHardware projectLab;

    public IButton? LeftButton => projectLab.LeftButton;

    public IButton? RightButton => projectLab.RightButton;

    public IPixelDisplay? Display => projectLab.Display;

    public RotationType DisplayRotation => RotationType._270Degrees;

    public GalleryViewerProjectLabHardware(IProjectLabHardware projectLab)
    {
        this.projectLab = projectLab;
    }
}
