using System.Windows;
using System.Windows.Media;
using WpfScreenHelper;

namespace ScreenGrab.Models;

internal sealed class ScreenCaptureTarget
{
    public ScreenCaptureTarget(Screen screen, Rect physicalBounds, Rect wpfBounds, ImageSource? preCapture)
    {
        Screen = screen;
        PhysicalBounds = physicalBounds;
        WpfBounds = wpfBounds;
        PreCapture = preCapture;
    }

    public Screen Screen { get; }

    public Rect PhysicalBounds { get; }

    public Rect WpfBounds { get; }

    public ImageSource? PreCapture { get; }
}
