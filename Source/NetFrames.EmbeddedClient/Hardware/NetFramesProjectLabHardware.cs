using Meadow.Devices;
using Meadow.Hardware;
using Meadow.Peripherals.Displays;
using NetFrames.EmbeddedClient.Contracts;

namespace NetFrames.EmbeddedClient.Hardware;

public class NetFramesProjectLabHardware : INetFramesHardware
{
    private readonly IProjectLabHardware projectLab;

    public IPixelDisplay? Display => (IRotatableDisplay?)projectLab.Display;

    public RotationType DisplayRotation => RotationType._270Degrees;

    public IWiFiNetworkAdapter NetworkAdapter => projectLab.ComputeModule.NetworkAdapters.Primary<IWiFiNetworkAdapter>();

    public NetFramesProjectLabHardware(IProjectLabHardware projectLab)
    {
        this.projectLab = projectLab;
    }
}