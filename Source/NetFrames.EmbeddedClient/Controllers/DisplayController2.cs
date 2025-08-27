using Meadow.Foundation.Graphics;
using Meadow.Foundation.Graphics.Buffers;
using Meadow.Foundation.Graphics.MicroLayout;
using Meadow.Peripherals.Displays;
using SimpleJpegDecoder;
using System.IO;
using System.Reflection;

namespace NetFrames.EmbeddedClient.Controllers;

internal class DisplayController2
{
    private DisplayScreen displayScreen;

    private AbsoluteLayout splashLayout;
    private AbsoluteLayout galleryLayout;

    private Picture picture;

    public DisplayController2(
        IPixelDisplay? display,
        RotationType displayRotation)
    {
        displayScreen = new DisplayScreen(display, displayRotation);
    }

    public void ShowSplashScreen()
    {
        splashLayout = new AbsoluteLayout(displayScreen.Width, displayScreen.Height);

        var image = Image.LoadFromResource("NetFrames.EmbeddedClient.Assets.img_splash.bmp");
        splashLayout.Controls.Add(new Picture(displayScreen.Width, displayScreen.Height, image));

        displayScreen.Controls.Add(splashLayout);
    }

    public void ShowGalleryScreen()
    {
        galleryLayout = new AbsoluteLayout(displayScreen.Width, displayScreen.Height);

        var buffer = LoadJpeg(LoadResource("images0.jpg"));
        var image = Image.LoadFromPixelData(buffer);
        picture = new Picture(displayScreen.Width, displayScreen.Height, image);
        galleryLayout.Controls.Add(picture);

        displayScreen.Controls.Add(galleryLayout);
    }

    public void DisplayImage(byte[] jpgData)
    {
        if (picture == null)
        {

        }
        else
        {

        }

        var buffer = LoadJpeg(jpgData);
        var image = Image.LoadFromPixelData(buffer);
        picture.Image = image;
        displayScreen.Invalidate();
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
