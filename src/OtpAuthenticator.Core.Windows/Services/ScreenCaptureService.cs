using System.Runtime.InteropServices;
using OtpAuthenticator.Core.Services.Interfaces;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Microsoft.Graphics.Canvas;

namespace OtpAuthenticator.Core.Windows.Services;

/// <summary>
/// Windows 화면 캡처 서비스 (Windows.Graphics.Capture API)
/// </summary>
public class ScreenCaptureService : IScreenCaptureService
{
    public async Task<CaptureResult?> CaptureWithPickerAsync()
    {
        var picker = new GraphicsCapturePicker();

        var hwnd = GetForegroundWindow();
        if (hwnd != IntPtr.Zero)
        {
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
        }

        var item = await picker.PickSingleItemAsync();
        if (item == null)
            return null;

        return await CaptureItemAsync(item);
    }

    public async Task<IReadOnlyList<CaptureResult>> CaptureAllScreensAsync()
    {
        var results = new List<CaptureResult>();
        var monitors = GetAllMonitors();

        foreach (var monitor in monitors)
        {
            var result = await CaptureRegionAsync(
                monitor.Left,
                monitor.Top,
                monitor.Width,
                monitor.Height);

            if (result != null && result.IsSuccess)
            {
                results.Add(result);
            }
        }

        return results;
    }

    public Task<CaptureResult?> CaptureRegionAsync(int x, int y, int width, int height)
    {
        return Task.Run(() =>
        {
            try
            {
                using var bitmap = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using var graphics = System.Drawing.Graphics.FromImage(bitmap);

                graphics.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(width, height));

                var bmpData = bitmap.LockBits(
                    new System.Drawing.Rectangle(0, 0, width, height),
                    System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                var pixelData = new byte[width * height * 4];
                Marshal.Copy(bmpData.Scan0, pixelData, 0, pixelData.Length);
                bitmap.UnlockBits(bmpData);

                return new CaptureResult
                {
                    PixelData = pixelData,
                    Width = width,
                    Height = height
                };
            }
            catch
            {
                return null;
            }
        });
    }

    private async Task<CaptureResult?> CaptureItemAsync(GraphicsCaptureItem item)
    {
        try
        {
            var device = CanvasDevice.GetSharedDevice();

            using var framePool = Direct3D11CaptureFramePool.Create(
                device,
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                1,
                item.Size);

            using var session = framePool.CreateCaptureSession(item);

            var tcs = new TaskCompletionSource<CaptureResult?>();

            framePool.FrameArrived += (s, a) =>
            {
                using var frame = s.TryGetNextFrame();
                if (frame != null)
                {
                    var result = ProcessFrame(frame, device);
                    tcs.TrySetResult(result);
                }
                else
                {
                    tcs.TrySetResult(null);
                }
            };

            session.StartCapture();

            var timeoutTask = Task.Delay(3000);
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            session.Dispose();

            if (completedTask == timeoutTask)
                return null;

            return await tcs.Task;
        }
        catch
        {
            return null;
        }
    }

    private CaptureResult? ProcessFrame(Direct3D11CaptureFrame frame, CanvasDevice device)
    {
        try
        {
            using var canvasBitmap = CanvasBitmap.CreateFromDirect3D11Surface(device, frame.Surface);

            var width = (int)canvasBitmap.SizeInPixels.Width;
            var height = (int)canvasBitmap.SizeInPixels.Height;

            var pixelData = canvasBitmap.GetPixelBytes();

            return new CaptureResult
            {
                PixelData = pixelData,
                Width = width,
                Height = height
            };
        }
        catch
        {
            return null;
        }
    }

    private List<MonitorInfo> GetAllMonitors()
    {
        var monitors = new List<MonitorInfo>();

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (hMonitor, hdcMonitor, lprcMonitor, dwData) =>
        {
            var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (GetMonitorInfo(hMonitor, ref info))
            {
                monitors.Add(new MonitorInfo
                {
                    Left = info.rcMonitor.Left,
                    Top = info.rcMonitor.Top,
                    Width = info.rcMonitor.Right - info.rcMonitor.Left,
                    Height = info.rcMonitor.Bottom - info.rcMonitor.Top
                });
            }
            return true;
        }, IntPtr.Zero);

        return monitors;
    }

    #region P/Invoke

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, IntPtr lprcMonitor, IntPtr dwData);

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private class MonitorInfo
    {
        public int Left { get; set; }
        public int Top { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    #endregion
}
