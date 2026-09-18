using FieldStack;
using FieldStack.Graphics;
using FieldStack.Graphics.Buffers;
using FieldStack.Graphics.MicroLayout;
using FieldStack.Peripherals.Displays;
using SixLabors.ImageSharp.PixelFormats;
using System.Reflection;
using ImageSharp = SixLabors.ImageSharp;

namespace NetFrames.YoshiPiClient.Controllers;

public class DisplayController
{
    private readonly double version;
    private readonly Color backgroundColor = Color.FromHex("06416D");
    private readonly Font12x16 font12x16 = new Font12x16();
    private readonly DisplayScreen displayScreen;

    private AbsoluteLayout splashLayout = default!;
    private Label status = default!;

    private AbsoluteLayout galleryLayout = default!;
    private Picture? picture;
    private Label counter = default!;

    public DisplayController(
        double version,
        IPixelDisplay display,
        RotationType displayRotation)
    {
        this.version = version;
        displayScreen = new DisplayScreen(display, displayRotation);
    }

    private byte[] LoadResource(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();

        using var stream = assembly.GetManifestResourceStream(fileName)
            ?? throw new FileNotFoundException($"embedded resource not found: {fileName}");

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    private IPixelBuffer LoadJpeg(byte[] jpgData)
    {
        using var decoded = ImageSharp.Image.Load<Rgb24>(jpgData);

        var pixelData = new byte[decoded.Width * decoded.Height * 3];
        decoded.CopyPixelDataTo(pixelData);

        return new BufferRgb888(decoded.Width, decoded.Height, pixelData);
    }

    public void LoadSplashScreen()
    {
        splashLayout = new AbsoluteLayout(displayScreen.Width, displayScreen.Height)
        {
            BackgroundColor = backgroundColor
        };

        var logo = Image.LoadFromResource("NetFrames.YoshiPiClient.Assets.img_logo.bmp");
        splashLayout.Controls.Add(new Picture(66, 19, logo.Width, logo.Height, logo));

        var versionLabel = new Label(0, 175, displayScreen.Width, font12x16.Height)
        {
            Text = $"Version {version:N1}",
            TextColor = Color.White,
            Font = font12x16,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        splashLayout.Controls.Add(versionLabel);

        status = new Label(0, 204, displayScreen.Width, font12x16.Height)
        {
            Text = "-",
            TextColor = Color.White,
            Font = font12x16,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        splashLayout.Controls.Add(status);

        displayScreen.Controls.Add(splashLayout);
    }

    public void LoadGalleryScreen()
    {
        galleryLayout = new AbsoluteLayout(displayScreen.Width, displayScreen.Height);

        displayScreen.Controls.Add(galleryLayout);
    }

    public void ShowGalleryScreen()
    {
        galleryLayout.IsVisible = true;
        splashLayout.IsVisible = false;
    }

    public void DisplaySampleImage()
    {
        var buffer = LoadJpeg(LoadResource("NetFrames.YoshiPiClient.Assets.img_sample.jpg"));
        var image = Image.LoadFromPixelData(buffer);
        var picture = new Picture(displayScreen.Width, displayScreen.Height, image);
        galleryLayout.Controls.Add(picture);
    }

    public void UpdateStatus(string text)
    {
        if (status.Text != text)
        {
            status.Text = text;
        }
    }

    public void DisplayImage(byte[] jpgData, int frameNumber)
    {
        var buffer = LoadJpeg(jpgData);
        var image = Image.LoadFromPixelData(buffer);

        Resolver.Log.Info(
            $"Image {frameNumber}: decoded {image.Width}x{image.Height} vs screen {displayScreen.Width}x{displayScreen.Height}");

        if (picture == null)
        {
            picture = new Picture(displayScreen.Width, displayScreen.Height, image);
            galleryLayout.Controls.Add(picture);

            counter = new Label(0, displayScreen.Height - font12x16.Height, frameNumber.ToString().Length * font12x16.Width + 2, font12x16.Height)
            {
                Text = frameNumber.ToString(),
                TextColor = Color.White,
                BackgroundColor = Color.Black,
                Font = font12x16,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            galleryLayout.Controls.Add(counter);
            counter.IsVisible = false;

            Resolver.Log.Info($"Image {frameNumber}: created Picture control, IsInvalid={picture.IsInvalid}, IsVisible={picture.IsVisible}");
        }
        else
        {
            picture.Image = image;
            counter.Text = frameNumber.ToString();
            if (counter.Width < frameNumber.ToString().Length * font12x16.Width + 2)
            {
                counter.Width = frameNumber.ToString().Length * font12x16.Width + 2;
            }

            Resolver.Log.Info($"Image {frameNumber}: updated Picture.Image, IsInvalid={picture.IsInvalid}, IsVisible={picture.IsVisible}");
        }
    }
}
