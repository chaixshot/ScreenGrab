using ScreenGrab.Extensions;
using ScreenGrab.Models;
using System.Drawing;
using System.Windows;
using System.Windows.Threading;
using WpfScreenHelper;
using Point = System.Windows.Point;

namespace ScreenGrab;

public abstract class ScreenGrabber
{
    public static bool IsCapturing { get; private set; }

    public static Action<Bitmap, bool, Point, Point>? OnCaptured { get; set; }

    private static TaskCompletionSource<Bitmap?>? _captureTaskCompletionSource;
    private static TaskCompletionSource<ScreenCaptureResult?>? _captureWithRegionTaskCompletionSource;

    public static void Capture(bool isAuxiliary = false)
    {
        if (IsCapturing) return;

        IsCapturing = true;

        var captureTargets = Screen.AllScreens.ToList().CreateCaptureTargets();
        var allScreenGrab = CreateScreenGrabViews(captureTargets, _ =>
            new ScreenGrabView(OnCaptured, isAuxiliary)
            {
                OnGrabClose = () => IsCapturing = false
            });

        ShowScreenGrabViews(captureTargets, allScreenGrab);
    }

    /// <summary>
    /// 同步方式捕获屏幕截图，类似 Window.ShowDialog() 的阻塞式调用
    /// </summary>
    /// <param name="isAuxiliary">是否显示辅助线</param>
    /// <returns>返回捕获的 Bitmap，如果用户取消则返回 null</returns>
    public static Tuple<Bitmap?, bool, Point, Point> CaptureDialog(bool isAuxiliary = false)
    {
        if (IsCapturing)
            return null;

        Bitmap? result = null;
        var frame = new DispatcherFrame();
        bool isRightClick = false;
        Point startPoint = new();
        Point endPoint = new();

        if (IsCapturing)
            return Tuple.Create(result, false, new Point(), new Point());

        IsCapturing = true;

        var captureTargets = Screen.AllScreens.ToList().CreateCaptureTargets();
        var allScreenGrab = CreateScreenGrabViews(captureTargets, _ =>
            new ScreenGrabView((Bitmap bitmap, bool rightClick, Point stPoint, Point edPoint) =>
            {
                // 截图成功时保存结果并退出消息循环
                result = bitmap;
                isRightClick = rightClick;
                startPoint = stPoint;
                endPoint = edPoint;
                frame.Continue = false;
            }, isAuxiliary)
            {
                OnGrabClose = () =>
                {
                    IsCapturing = false;
                    // 关闭时退出消息循环
                    frame.Continue = false;
                }
            });

        ShowScreenGrabViews(captureTargets, allScreenGrab);

        // 阻塞等待用户完成截图或取消
        Dispatcher.PushFrame(frame);

        return Tuple.Create(result, isRightClick, startPoint, endPoint);
    }

    /// <summary>
    /// 异步方式捕获屏幕截图，类似 Dialog 的使用方式
    /// </summary>
    /// <param name="isAuxiliary">是否显示辅助线</param>
    /// <returns>返回捕获的 Bitmap，如果用户取消则返回 null</returns>
    public static Task<Bitmap?> CaptureAsync(bool isAuxiliary = false)
    {
        if (IsCapturing)
            return Task.FromResult<Bitmap?>(null);

        _captureTaskCompletionSource = new TaskCompletionSource<Bitmap?>();

        IsCapturing = true;

        var captureTargets = Screen.AllScreens.ToList().CreateCaptureTargets();
        var allScreenGrab = CreateScreenGrabViews(captureTargets, _ =>
            new ScreenGrabView((Bitmap bitmap, bool rightClick, Point stPoint, Point edPoint) =>
            {
                // 截图成功时完成任务
                _captureTaskCompletionSource?.TrySetResult(bitmap);
            }, isAuxiliary)
            {
                OnGrabClose = () =>
                {
                    IsCapturing = false;
                },
                OnCancel = () =>
                {
                    _captureTaskCompletionSource?.TrySetResult(null);
                }
            });

        ShowScreenGrabViews(captureTargets, allScreenGrab);

        return _captureTaskCompletionSource.Task;
    }

    /// <summary>
    /// 异步方式捕获屏幕截图，返回截图位图与选区物理坐标。
    /// </summary>
    /// <param name="isAuxiliary">是否显示辅助线</param>
    /// <param name="padImage">
    /// 是否对过小截图进行 padding 扩展（默认 <c>true</c>，保持历史行为）。
    /// 当调用方需要"位图尺寸 == 选区尺寸"（如贴回选区原位置）时应传 <c>false</c>，
    /// 否则 &lt;64px 的截图会被扩展为更大画布，导致原始内容被缩小。
    /// </param>
    /// <returns>返回截图结果（含选区物理坐标），如果用户取消则返回 null</returns>
    public static Task<ScreenCaptureResult?> CaptureWithRegionAsync(bool isAuxiliary = false, bool padImage = true)
    {
        if (IsCapturing)
            return Task.FromResult<ScreenCaptureResult?>(null);

        _captureWithRegionTaskCompletionSource = new TaskCompletionSource<ScreenCaptureResult?>();

        IsCapturing = true;

        var captureTargets = Screen.AllScreens.ToList().CreateCaptureTargets();
        var allScreenGrab = CreateScreenGrabViews(captureTargets, _ =>
            new ScreenGrabView(result =>
            {
                // 截图成功时完成任务
                _captureWithRegionTaskCompletionSource?.TrySetResult(result);
            }, isAuxiliary, padImage: padImage)
            {
                OnGrabClose = () =>
                {
                    IsCapturing = false;
                },
                OnCancel = () =>
                {
                    _captureWithRegionTaskCompletionSource?.TrySetResult(null);
                }
            });

        ShowScreenGrabViews(captureTargets, allScreenGrab);

        return _captureWithRegionTaskCompletionSource.Task;
    }

    private static List<ScreenGrabView> CreateScreenGrabViews(
        IReadOnlyList<ScreenCaptureTarget> captureTargets,
        Func<ScreenCaptureTarget, ScreenGrabView> createView)
    {
        var allScreenGrab = Application.Current.Windows.OfType<ScreenGrabView>().ToList();
        for (var screenIndex = allScreenGrab.Count; screenIndex < captureTargets.Count; screenIndex++)
        {
            allScreenGrab.Add(createView(captureTargets[screenIndex]));
        }

        return allScreenGrab;
    }

    private static void ShowScreenGrabViews(
        IReadOnlyList<ScreenCaptureTarget> captureTargets,
        IReadOnlyList<ScreenGrabView> allScreenGrab)
    {
        for (var i = 0; i < captureTargets.Count && i < allScreenGrab.Count; i++)
        {
            var screenGrab = allScreenGrab[i];
            screenGrab.SetCaptureTarget(captureTargets[i]);

            screenGrab.Show();
            screenGrab.ApplyTargetBounds();
            screenGrab.Activate();
        }
    }
}
