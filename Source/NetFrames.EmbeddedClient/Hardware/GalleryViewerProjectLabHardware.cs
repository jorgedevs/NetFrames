﻿using Meadow.Devices;
using Meadow.Peripherals.Displays;
using Meadow.Peripherals.Sensors.Buttons;
using NetFrames.EmbeddedClient.Contracts;

namespace NetFrames.EmbeddedClient.Hardware;

public class GalleryViewerProjectLabHardware : IGalleryViewerHardware
{
    private readonly IProjectLabHardware projLab;

    public IButton? LeftButton => projLab.LeftButton;

    public IButton? RightButton => projLab.RightButton;

    public IPixelDisplay? Display => projLab.Display;

    public RotationType DisplayRotation => RotationType._270Degrees;

    public GalleryViewerProjectLabHardware(IProjectLabHardware projLab)
    {
        this.projLab = projLab;
    }
}
