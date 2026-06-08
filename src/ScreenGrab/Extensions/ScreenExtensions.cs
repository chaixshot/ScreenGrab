using System.Drawing;
using System.Windows;
using System.Windows.Media;
using ScreenGrab.Models;
using WpfScreenHelper;

namespace ScreenGrab.Extensions;

public static class ScreenExtensions
{
    public static Rect ScaledBounds(this Screen displayInfo)
    {
        Rect displayRect = displayInfo.Bounds;
        var scaleFraction = displayInfo.ScaleFactor;

        // Scale size and position
        Rect scaledBounds = new(
            displayRect.X / scaleFraction,
            displayRect.Y / scaleFraction,
            displayRect.Width / scaleFraction,
            displayRect.Height / scaleFraction);
        return scaledBounds;
    }

    /// <summary>
    /// 在窗口显示之前预先截取所有屏幕的图像
    /// </summary>
    public static Dictionary<Screen, ImageSource> PreCaptureAllScreens(this IEnumerable<Screen> screens)
    {
        var result = new Dictionary<Screen, ImageSource>();
        foreach (var screen in screens)
        {
            var imageSource = screen.CaptureScreenImage();
            if (imageSource != null)
            {
                result[screen] = imageSource;
            }
        }
        return result;
    }

    internal static List<ScreenCaptureTarget> CreateCaptureTargets(this IEnumerable<Screen> screens)
    {
        var result = new List<ScreenCaptureTarget>();
        foreach (var screen in screens)
        {
            result.Add(new ScreenCaptureTarget(
                screen,
                screen.Bounds,
                screen.ScaledBounds(),
                screen.CaptureScreenImage()));
        }

        return result;
    }

    internal static ImageSource? CaptureScreenImage(this Screen screen)
    {
        return CaptureScreenBoundsImage(screen.Bounds);
    }

    internal static ImageSource? CaptureScreenImage(this ScreenCaptureTarget target)
    {
        return CaptureScreenBoundsImage(target.PhysicalBounds);
    }

    private static ImageSource? CaptureScreenBoundsImage(Rect bounds)
    {
        var width = Math.Max(1, (int)Math.Round(bounds.Width));
        var height = Math.Max(1, (int)Math.Round(bounds.Height));

        using var bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.CopyFromScreen((int)Math.Round(bounds.Left), (int)Math.Round(bounds.Top), 0, 0, bmp.Size,
            CopyPixelOperation.SourceCopy);
        return bmp.ToImageSource();
    }
}
