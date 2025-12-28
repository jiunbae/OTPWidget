using OtpAuthenticator.Core.Models;

namespace OtpAuthenticator.Core.Services.Interfaces;

/// <summary>
/// OTP 코드 생성 및 계정 관리 서비스
/// </summary>
public interface IOtpService
{
    /// <summary>
    /// OTP 코드 생성
    /// </summary>
    /// <param name="account">OTP 계정</param>
    /// <returns>생성된 OTP 코드</returns>
    string GenerateCode(OtpAccount account);

    /// <summary>
    /// TOTP 남은 시간 계산 (초)
    /// </summary>
    /// <param name="period">주기 (초)</param>
    /// <returns>남은 시간 (초)</returns>
    int GetRemainingSeconds(int period = 30);

    /// <summary>
    /// HOTP 카운터 증가
    /// </summary>
    /// <param name="account">HOTP 계정</param>
    void IncrementCounter(OtpAccount account);

    /// <summary>
    /// otpauth:// URI 파싱
    /// </summary>
    /// <param name="uri">otpauth URI</param>
    /// <returns>파싱된 OTP 계정 또는 null</returns>
    OtpAccount? ParseOtpAuthUri(string uri);

    /// <summary>
    /// otpauth:// URI 생성
    /// </summary>
    /// <param name="account">OTP 계정</param>
    /// <returns>otpauth URI</returns>
    string GenerateOtpAuthUri(OtpAccount account);

    /// <summary>
    /// 비밀 키 유효성 검증
    /// </summary>
    /// <param name="secretKey">Base32 인코딩된 비밀 키</param>
    /// <returns>유효 여부</returns>
    bool ValidateSecretKey(string secretKey);

    /// <summary>
    /// 랜덤 비밀 키 생성
    /// </summary>
    /// <param name="length">키 길이 (바이트)</param>
    /// <returns>Base32 인코딩된 비밀 키</returns>
    string GenerateSecretKey(int length = 20);
}
