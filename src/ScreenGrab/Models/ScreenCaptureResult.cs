using System.Drawing;

namespace ScreenGrab.Models;

/// <summary>
/// 截图结果，包含截图位图与选区在虚拟屏幕中的物理像素坐标。
/// </summary>
public sealed class ScreenCaptureResult(Bitmap bitmap, Rectangle region)
{
    /// <summary>截图位图。</summary>
    public Bitmap Bitmap { get; } = bitmap;

    /// <summary>
    /// 截图选区在虚拟屏幕中的物理像素坐标。
    /// 多显示器场景下坐标可能为负值（选区在非主显示器左侧/上方）。
    /// </summary>
    public Rectangle Region { get; } = region;
}
