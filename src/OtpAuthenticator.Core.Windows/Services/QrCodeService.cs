using OtpAuthenticator.Core.Models;
using OtpAuthenticator.Core.Services.Interfaces;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;

namespace OtpAuthenticator.Core.Windows.Services;

/// <summary>
/// Windows QR 코드 서비스 (ZXing.NET Windows)
/// </summary>
public class QrCodeService : IQrCodeService
{
    private readonly IOtpService _otpService;
    private readonly BarcodeReaderGeneric _barcodeReader;
    private readonly BarcodeWriter<System.Drawing.Bitmap> _barcodeWriter;

    public QrCodeService(IOtpService otpService)
    {
        _otpService = otpService;

        _barcodeReader = new BarcodeReaderGeneric
        {
            AutoRotate = true,
            Options = new DecodingOptions
            {
                TryHarder = true,
                TryInverted = true,
                PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE }
            }
        };

        _barcodeWriter = new BarcodeWriter<System.Drawing.Bitmap>
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new EncodingOptions
            {
                Width = 256,
                Height = 256,
                Margin = 1,
                PureBarcode = false
            },
            Renderer = new BitmapRenderer()
        };
    }

    public string? DecodeFromImage(byte[] imageData, int width, int height)
    {
        try
        {
            using var bitmap = CreateBitmapFromBgra(imageData, width, height);
            if (bitmap == null) return null;

            // BitmapLuminanceSource를 사용하여 디코딩
            var luminanceSource = new BitmapLuminanceSource(bitmap);
            var result = _barcodeReader.Decode(luminanceSource);
            return result?.Text;
        }
        catch
        {
            return null;
        }
    }

    public OtpAccount? DecodeOtpAccountFromImage(byte[] imageData, int width, int height)
    {
        var text = DecodeFromImage(imageData, width, height);
        if (string.IsNullOrEmpty(text)) return null;

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
            _barcodeWriter.Options.Width = size;
            _barcodeWriter.Options.Height = size;

            using var bitmap = _barcodeWriter.Write(text);
            using var ms = new MemoryStream();

            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return ms.ToArray();
        }
        catch
        {
            return Array.Empty<byte>();
        }
    }

    public string? DecodeFromFile(string filePath)
    {
        try
        {
            using var bitmap = new System.Drawing.Bitmap(filePath);
            var luminanceSource = new BitmapLuminanceSource(bitmap);
            var result = _barcodeReader.Decode(luminanceSource);
            return result?.Text;
        }
        catch
        {
            return null;
        }
    }

    public OtpAccount? DecodeOtpAccountFromFile(string filePath)
    {
        var text = DecodeFromFile(filePath);
        if (string.IsNullOrEmpty(text)) return null;

        return _otpService.ParseOtpAuthUri(text);
    }

    private static System.Drawing.Bitmap? CreateBitmapFromBgra(byte[] bgraData, int width, int height)
    {
        if (bgraData.Length != width * height * 4)
            return null;

        try
        {
            var bitmap = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            var bmpData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, width, height),
                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            System.Runtime.InteropServices.Marshal.Copy(bgraData, 0, bmpData.Scan0, bgraData.Length);
            bitmap.UnlockBits(bmpData);

            return bitmap;
        }
        catch
        {
            return null;
        }
    }
}
