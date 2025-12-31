using OtpAuthenticator.Core.Services.Interfaces;
using CoreGraphics;
using Foundation;

#if MACCATALYST
using AppKit;
#else
using UIKit;
#endif

namespace OtpAuthenticator.Core.Apple.Services;

/// <summary>
/// Apple 플랫폼 화면 캡처 서비스
/// Note: iOS에서는 보안 제한으로 전체 화면 캡처가 제한됩니다.
/// </summary>
public class ScreenCaptureService : IScreenCaptureService
{
    /// <summary>
    /// macOS: 사용자에게 화면 선택 요청 (ScreenCaptureKit 사용 권장)
    /// iOS: 지원되지 않음
    /// </summary>
    public Task<CaptureResult?> CaptureWithPickerAsync()
    {
#if MACCATALYST
        return CaptureMainScreenAsync();
#else
        // iOS에서는 앱 외부 화면 캡처가 제한됨
        return Task.FromResult<CaptureResult?>(null);
#endif
    }

    /// <summary>
    /// 모든 화면 캡처 (macOS only)
    /// </summary>
    public async Task<IReadOnlyList<CaptureResult>> CaptureAllScreensAsync()
    {
        var results = new List<CaptureResult>();

#if MACCATALYST
        var screens = NSScreen.Screens;
        foreach (var screen in screens)
        {
            var result = await CaptureScreenAsync(screen);
            if (result != null && result.IsSuccess)
            {
                results.Add(result);
            }
        }
#endif

        return results;
    }

    /// <summary>
    /// 특정 영역 캡처
    /// </summary>
    public Task<CaptureResult?> CaptureRegionAsync(int x, int y, int width, int height)
    {
#if MACCATALYST
        return Task.Run(() =>
        {
            try
            {
                var rect = new CGRect(x, y, width, height);

                // CGWindowListCreateImage를 사용하여 캡처
                using var imageRef = CGImage.ScreenImage((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);

                if (imageRef == null)
                    return null;

                return ConvertCGImageToResult(imageRef, width, height);
            }
            catch
            {
                return null;
            }
        });
#else
        return Task.FromResult<CaptureResult?>(null);
#endif
    }

#if MACCATALYST
    private Task<CaptureResult?> CaptureMainScreenAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                var mainScreen = NSScreen.MainScreen;
                if (mainScreen == null)
                    return null;

                var frame = mainScreen.Frame;
                var width = (int)frame.Width;
                var height = (int)frame.Height;

                using var imageRef = CGImage.ScreenImage(0, 0, width, height);

                if (imageRef == null)
                    return null;

                return ConvertCGImageToResult(imageRef, width, height);
            }
            catch
            {
                return null;
            }
        });
    }

    private Task<CaptureResult?> CaptureScreenAsync(NSScreen screen)
    {
        return Task.Run(() =>
        {
            try
            {
                var frame = screen.Frame;
                var x = (int)frame.X;
                var y = (int)frame.Y;
                var width = (int)frame.Width;
                var height = (int)frame.Height;

                using var imageRef = CGImage.ScreenImage(x, y, width, height);

                if (imageRef == null)
                    return null;

                return ConvertCGImageToResult(imageRef, width, height);
            }
            catch
            {
                return null;
            }
        });
    }
#endif

    private CaptureResult? ConvertCGImageToResult(CGImage image, int width, int height)
    {
        try
        {
            var bytesPerPixel = 4;
            var bytesPerRow = bytesPerPixel * width;
            var totalBytes = bytesPerRow * height;

            var pixelData = new byte[totalBytes];

            using var colorSpace = CGColorSpace.CreateDeviceRGB();
            using var context = new CGBitmapContext(
                pixelData,
                width,
                height,
                8,
                bytesPerRow,
                colorSpace,
                CGImageAlphaInfo.PremultipliedFirst | (CGImageAlphaInfo)((int)CGBitmapFlags.ByteOrder32Little));

            context.DrawImage(new CGRect(0, 0, width, height), image);

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
}
