using Meadow;
using Meadow.Foundation.Graphics.MicroLayout;
using Meadow.Peripherals.Displays;

namespace NetFrames.EmbeddedClient;

public class DisplayController
{
    private Color foregroundColor = Color.White;
    private Color atmosphericColor = Color.White;
    private Color motionColor = Color.FromHex("23ABE3");
    private Color buttonColor = Color.FromHex("EF7D3B");

    private readonly DisplayScreen displayScreen;

    public DisplayController(IPixelDisplay display)
    {
        displayScreen = new DisplayScreen(display, RotationType._270Degrees)
        {
            BackgroundColor = Color.FromHex("0B3749")
        };
    }
}