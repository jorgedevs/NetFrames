using FieldStack;
using FieldStack.Drivers.Displays;
using FieldStack.Drivers.ICs.IOExpanders;
using FieldStack.Hardware;
using FieldStack.Peripherals.Displays;
using FieldStack.Units;

namespace NetFrames.YoshiPiClient;

public sealed class YoshiPi : IDisposable
{
    private readonly IDigitalOutputPort backlight;

    public Ili9341 Display { get; }

    private YoshiPi(Ili9341 display, IDigitalOutputPort backlight)
    {
        Display = display;
        this.backlight = backlight;
    }

    public static YoshiPi Create(RaspberryPi device, Frequency spiSpeed, ColorMode colorMode, bool invertColor)
    {
        var expander = new Mcp23008(
            device.CreateI2cBus(1),
            address: (byte)Mcp23008.Addresses.Default,
            resetPort: device.Pins.GPIO17.CreateDigitalOutputPort(false));

        var display = new Ili9341(
            device.CreateSpiBus(0, spiSpeed),
            chipSelectPort: device.Pins.GPIO4.CreateDigitalOutputPort(true),
            dataCommandPort: device.Pins.GPIO23.CreateDigitalOutputPort(),
            resetPort: device.Pins.GPIO24.CreateDigitalOutputPort(),
            width: 240,
            height: 320,
            colorMode: colorMode);

        display.InvertDisplayColor(invertColor);

        var backlight = expander.Pins.GP4.CreateDigitalOutputPort(true);
        backlight.State = true;

        return new YoshiPi(display, backlight);
    }

    public void Dispose()
    {
        backlight.State = false;
        backlight.Dispose();
        Display.Dispose();
    }
}
