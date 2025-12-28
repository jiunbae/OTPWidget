using OtpAuthenticator.Core.Models;
using OtpAuthenticator.Core.Services.Interfaces;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;

namespace OtpAuthenticator.Core.Services;

/// <summary>
/// QR 코드 서비스 (ZXing.NET 사용)
/// </summary>
public class QrCodeService : IQrCodeService
{
    private readonly IOtpService _otpService;
    private readonly BarcodeReader<System.Drawing.Bitmap> _barcodeReader;
    private readonly BarcodeWriter<System.Drawing.Bitmap> _barcodeWriter;

    public QrCodeService(IOtpService otpService)
    {
        _otpService = otpService;

        // 바코드 리더 설정
        _barcodeReader = new BarcodeReader<System.Drawing.Bitmap>
        {
            AutoRotate = true,
            Options = new DecodingOptions
            {
                TryHarder = true,
                TryInverted = true,
                PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE }
            }
        };

        // 바코드 라이터 설정
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

    /// <summary>
    /// 이미지에서 QR 코드 디코딩
    /// </summary>
    public string? DecodeFromImage(byte[] imageData, int width, int height)
    {
        try
        {
            // BGRA 바이트 배열을 Bitmap으로 변환
            using var bitmap = CreateBitmapFromBgra(imageData, width, height);
            if (bitmap == null) return null;

            var result = _barcodeReader.Decode(bitmap);
            return result?.Text;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 이미지에서 OTP 계정 디코딩
    /// </summary>
    public OtpAccount? DecodeOtpAccountFromImage(byte[] imageData, int width, int height)
    {
        var text = DecodeFromImage(imageData, width, height);
        if (string.IsNullOrEmpty(text)) return null;

        return _otpService.ParseOtpAuthUri(text);
    }

    /// <summary>
    /// OTP 계정을 QR 코드로 생성
    /// </summary>
    public byte[] GenerateQrCode(OtpAccount account, int size = 256)
    {
        var uri = _otpService.GenerateOtpAuthUri(account);
        return GenerateQrCode(uri, size);
    }

    /// <summary>
    /// 텍스트를 QR 코드로 생성
    /// </summary>
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

    /// <summary>
    /// BGRA 바이트 배열을 Bitmap으로 변환
    /// </summary>
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
