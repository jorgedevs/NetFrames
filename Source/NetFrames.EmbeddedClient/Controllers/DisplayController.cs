using Meadow.Foundation.Graphics;
using Meadow.Foundation.Graphics.Buffers;
using Meadow.Foundation.Graphics.MicroLayout;
using Meadow.Peripherals.Displays;
using SimpleJpegDecoder;

namespace NetFrames.EmbeddedClient.Controllers;

public class DisplayController
{
    private DisplayScreen displayScreen;
    private AbsoluteLayout splashLayout;
    private AbsoluteLayout galleryLayout;
    private Picture picture;

    public DisplayController(
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

        displayScreen.Controls.Add(galleryLayout);
    }

    public void DisplayImage(byte[] jpgData)
    {
        var buffer = LoadJpeg(jpgData);
        var image = Image.LoadFromPixelData(buffer);

        if (picture == null)
        {
            picture = new Picture(displayScreen.Width, displayScreen.Height, image);
            galleryLayout.Controls.Add(picture);
        }
        else
        {
            picture.Image = image;
            displayScreen.Invalidate();
        }
    }

    private IPixelBuffer LoadJpeg(byte[] jpgData)
    {
        var decoder = new JpegDecoder();
        var jpg = decoder.DecodeJpeg(jpgData);

        return new BufferRgb888(decoder.Width, decoder.Height, jpg);
    }
}
