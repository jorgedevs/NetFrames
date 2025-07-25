using Meadow.Devices;
using Meadow.Hardware;
using Meadow.Peripherals.Displays;
using NetFrames.EmbeddedClient.Contracts;

namespace NetFrames.EmbeddedClient.Hardware;

public class GalleryViewerProjectLabHardware : IGalleryViewerHardware
{
    private readonly IProjectLabHardware projectLab;

    public IPixelDisplay? Display => projectLab.Display;

    public RotationType DisplayRotation => RotationType._270Degrees;

    public IWiFiNetworkAdapter NetworkAdapter => projectLab.ComputeModule.NetworkAdapters.Primary<IWiFiNetworkAdapter>();

    public GalleryViewerProjectLabHardware(IProjectLabHardware projectLab)
    {
        this.projectLab = projectLab;
    }
}