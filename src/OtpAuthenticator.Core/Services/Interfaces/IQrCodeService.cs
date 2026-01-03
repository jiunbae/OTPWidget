using OtpAuthenticator.Core.Models;

namespace OtpAuthenticator.Core.Services.Interfaces;

/// <summary>
/// QR 코드 서비스 인터페이스
/// </summary>
public interface IQrCodeService
{
    /// <summary>
    /// 이미지 바이트에서 QR 코드 디코딩
    /// </summary>
    /// <param name="imageData">이미지 데이터</param>
    /// <param name="width">이미지 너비</param>
    /// <param name="height">이미지 높이</param>
    /// <returns>디코딩된 텍스트 또는 null</returns>
    string? DecodeFromImage(byte[] imageData, int width, int height);

    /// <summary>
    /// 이미지에서 OTP 계정 파싱
    /// </summary>
    /// <param name="imageData">이미지 데이터</param>
    /// <param name="width">이미지 너비</param>
    /// <param name="height">이미지 높이</param>
    /// <returns>파싱된 OTP 계정 또는 null</returns>
    OtpAccount? DecodeOtpAccountFromImage(byte[] imageData, int width, int height);

    /// <summary>
    /// OTP 계정을 QR 코드 이미지로 생성
    /// </summary>
    /// <param name="account">OTP 계정</param>
    /// <param name="size">이미지 크기 (정사각형)</param>
    /// <returns>PNG 이미지 바이트</returns>
    byte[] GenerateQrCode(OtpAccount account, int size = 256);

    /// <summary>
    /// 텍스트를 QR 코드 이미지로 생성
    /// </summary>
    /// <param name="text">인코딩할 텍스트</param>
    /// <param name="size">이미지 크기</param>
    /// <returns>PNG 이미지 바이트</returns>
    byte[] GenerateQrCode(string text, int size = 256);

    /// <summary>
    /// 이미지 파일에서 QR 코드 디코딩
    /// </summary>
    /// <param name="filePath">이미지 파일 경로</param>
    /// <returns>디코딩된 텍스트 또는 null</returns>
    string? DecodeFromFile(string filePath);

    /// <summary>
    /// 이미지 파일에서 OTP 계정 파싱
    /// </summary>
    /// <param name="filePath">이미지 파일 경로</param>
    /// <returns>파싱된 OTP 계정 또는 null</returns>
    OtpAccount? DecodeOtpAccountFromFile(string filePath);
}
