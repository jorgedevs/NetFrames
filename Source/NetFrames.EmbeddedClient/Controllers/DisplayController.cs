using Meadow;
using Meadow.Foundation.Graphics;
using Meadow.Foundation.Graphics.Buffers;
using Meadow.Peripherals.Displays;
using SimpleJpegDecoder;
using System.IO;
using System.Reflection;

namespace NetFrames.EmbeddedClient.Controllers;

public class DisplayController
{
    MicroGraphics graphics;

    public DisplayController(
        IPixelDisplay? display,
        RotationType displayRotation)
    {
        graphics = new MicroGraphics(display)
        {
            Rotation = displayRotation
        };
    }

    public void DrawSplashScreen()
    {
        graphics.Clear(ColorConstants.SplashColor, false);
        DisplayJPG("splash.jpg");
    }

    void DisplayJPG(string filename)
    {
        var buffer = LoadJpeg(LoadResource(filename));
        graphics.DrawBuffer(0, 0, buffer);
        graphics.Show();
    }

    IPixelBuffer LoadJpeg(byte[] jpgData)
    {
        var decoder = new JpegDecoder();
        var jpg = decoder.DecodeJpeg(jpgData);

        return new BufferRgb888(decoder.Width, decoder.Height, jpg);
    }

    byte[] LoadResource(string filename)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"NetFrames.EmbeddedClient.Assets.{filename}";

        using (Stream stream = assembly.GetManifestResourceStream(resourceName))
        {
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}

public static class ColorConstants
{
    public static Color SplashColor = Color.FromHex("06416C");
    public static Color DayBackground = Color.FromHex("06BFCC");
    public static Color NightBackground = Color.FromHex("133B4F");
    public static Color DarkFont = Color.FromHex("06416C");
    public static Color LightFont = Color.FromHex("FFFFFF");
}