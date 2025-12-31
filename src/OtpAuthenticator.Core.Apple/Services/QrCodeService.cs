using OtpAuthenticator.Core.Models;
using OtpAuthenticator.Core.Services.Interfaces;
using CoreImage;
using CoreGraphics;
using Foundation;

#if MACCATALYST
using AppKit;
#else
using UIKit;
#endif

namespace OtpAuthenticator.Core.Apple.Services;

/// <summary>
/// Apple 플랫폼 QR 코드 서비스 (CoreImage 사용)
/// </summary>
public class QrCodeService : IQrCodeService
{
    private readonly IOtpService _otpService;
    private readonly CIDetector _qrDetector;

    public QrCodeService(IOtpService otpService)
    {
        _otpService = otpService;

        _qrDetector = CIDetector.CreateQRDetector(
            context: null,
            options: new CIDetectorOptions
            {
                Accuracy = CIDetectorAccuracy.High
            });
    }

    public string? DecodeFromImage(byte[] imageData, int width, int height)
    {
        try
        {
            using var ciImage = CreateCIImageFromBgra(imageData, width, height);
            if (ciImage == null)
                return null;

            var features = _qrDetector.FeaturesInImage(ciImage);

            foreach (var feature in features)
            {
                if (feature is CIQRCodeFeature qrFeature)
                {
                    return qrFeature.MessageString;
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public OtpAccount? DecodeOtpAccountFromImage(byte[] imageData, int width, int height)
    {
        var text = DecodeFromImage(imageData, width, height);
        if (string.IsNullOrEmpty(text))
            return null;

        return _otpService.ParseOtpAuthUri(text);
    }

    public byte[] GenerateQrCode(OtpAccount account, int size = 256)
    {
        var uri = _otpService.GenerateOtpAuthUri(account);
        return GenerateQrCode(uri, size);
    }

    public byte[] GenerateQrCode(string text, int size = 256)
    {
        try
        {
            // CIQRCodeGenerator를 사용하여 QR 코드 생성
            var data = NSData.FromString(text, NSStringEncoding.UTF8);

            var qrFilter = CIFilter.FromName("CIQRCodeGenerator");
            if (qrFilter == null)
                return Array.Empty<byte>();

            qrFilter.SetValueForKey(data, new NSString("inputMessage"));
            qrFilter.SetValueForKey(new NSString("H"), new NSString("inputCorrectionLevel"));

            var outputImage = qrFilter.OutputImage;
            if (outputImage == null)
                return Array.Empty<byte>();

            // 크기 조정
            var scaleX = size / outputImage.Extent.Width;
            var scaleY = size / outputImage.Extent.Height;

            var transform = CGAffineTransform.MakeScale(scaleX, scaleY);
            var scaledImage = outputImage.ImageByApplyingTransform(transform);

            // PNG로 변환
            using var context = new CIContext();
            using var cgImage = context.CreateCGImage(scaledImage, scaledImage.Extent);

            if (cgImage == null)
                return Array.Empty<byte>();

#if MACCATALYST
            using var nsImage = new NSImage(cgImage, new CGSize(size, size));
            using var tiffData = nsImage.AsTiff();
            if (tiffData == null)
                return Array.Empty<byte>();

            using var bitmapRep = new NSBitmapImageRep(tiffData);
            using var pngData = bitmapRep.RepresentationUsingTypeProperties(NSBitmapImageFileType.Png);

            if (pngData == null)
                return Array.Empty<byte>();

            return pngData.ToArray();
#else
            using var uiImage = new UIImage(cgImage);
            using var pngData = uiImage.AsPNG();

            if (pngData == null)
                return Array.Empty<byte>();

            return pngData.ToArray();
#endif
        }
        catch
        {
            return Array.Empty<byte>();
        }
    }

    private CIImage? CreateCIImageFromBgra(byte[] bgraData, int width, int height)
    {
        if (bgraData.Length != width * height * 4)
            return null;

        try
        {
            using var data = NSData.FromArray(bgraData);
            using var colorSpace = CGColorSpace.CreateDeviceRGB();

            var bitmapInfo = CGBitmapFlags.ByteOrder32Little | (CGBitmapFlags)CGImageAlphaInfo.PremultipliedFirst;

            using var provider = new CGDataProvider(data);
            using var cgImage = new CGImage(
                width,
                height,
                8,
                32,
                width * 4,
                colorSpace,
                bitmapInfo,
                provider,
                null,
                false,
                CGColorRenderingIntent.Default);

            return new CIImage(cgImage);
        }
        catch
        {
            return null;
        }
    }
}
