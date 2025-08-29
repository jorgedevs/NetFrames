using Meadow;
using Meadow.Foundation.Graphics;
using Meadow.Foundation.Graphics.Buffers;
using Meadow.Foundation.Graphics.MicroLayout;
using Meadow.Peripherals.Displays;
using SimpleJpegDecoder;
using System;
using System.Threading;

namespace NetFrames.EmbeddedClient.Controllers;

public class DisplayController
{
    private readonly Color backgroundColor = Color.FromHex("06416D");
    private readonly Font12x16 font12x16 = new Font12x16();
    private readonly DisplayScreen displayScreen;

    private AbsoluteLayout splashLayout;
    private ProgressBar progressBar;
    private Label progressValue;
    private Label version;
    private Label status;

    private AbsoluteLayout galleryLayout;
    private Picture picture;

    public DisplayController(
        IPixelDisplay? display,
        RotationType displayRotation)
    {
        displayScreen = new DisplayScreen(display, displayRotation);
    }

    public void UpdateStatus(string text)
    {
        status.Text = text;
    }

    public void ShowSplashScreen()
    {
        splashLayout = new AbsoluteLayout(displayScreen.Width, displayScreen.Height)
        {
            BackgroundColor = backgroundColor
        };

        var logo = Image.LoadFromResource("NetFrames.EmbeddedClient.Assets.img_logo.bmp");
        splashLayout.Controls.Add(new Picture(66, 19, logo.Width, logo.Height, logo));

        version = new Label(0, 175, displayScreen.Width, font12x16.Height)
        {
            Text = $"Version {MeadowApp.VERSION:N1}",
            TextColor = Color.White,
            Font = font12x16,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        splashLayout.Controls.Add(version);

        progressBar = new ProgressBar(66, 172, 188, 21)
        {
            BackgroundColor = Color.Black,
            ValueColor = Color.FromHex("0B3749"),
            BorderColor = Color.FromHex("0B3749"),
        };
        splashLayout.Controls.Add(progressBar);
        progressBar.IsVisible = false;

        progressValue = new Label(66, 175, 188, 16)
        {
            Text = "0%",
            TextColor = Color.White,
            Font = font12x16,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        splashLayout.Controls.Add(progressValue);
        progressValue.IsVisible = false;

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

    public void UpdateDownloadProgress(int progress)
    {
        if (!progressBar.IsVisible)
        {
            progressBar.IsVisible = true;
            progressValue.IsVisible = true;
        }

        progressBar.Value = progress;
        progressValue.Text = $"{progress}%";

        if (progress == 100)
        {
            UpdateStatus("Download Complete");

            Thread.Sleep(TimeSpan.FromSeconds(3));

            UpdateStatus(string.Empty);
            progressBar.IsVisible = false;
            progressValue.IsVisible = false;
        }
    }

    private IPixelBuffer LoadJpeg(byte[] jpgData)
    {
        var decoder = new JpegDecoder();
        var jpg = decoder.DecodeJpeg(jpgData);

        return new BufferRgb888(decoder.Width, decoder.Height, jpg);
    }
}
