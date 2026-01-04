using OtpAuthenticator.Core.Models;

namespace OtpAuthenticator.Core.Services.Interfaces;

/// <summary>
/// 암호화 서비스 인터페이스
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// AES-256으로 데이터 암호화
    /// </summary>
    /// <param name="plainText">평문</param>
    /// <param name="password">비밀번호</param>
    /// <returns>암호화된 백업 데이터</returns>
    EncryptedBackup Encrypt(string plainText, string password);

    /// <summary>
    /// 암호화된 데이터 복호화
    /// </summary>
    /// <param name="encrypted">암호화된 백업 데이터</param>
    /// <param name="password">비밀번호</param>
    /// <returns>복호화된 평문</returns>
    /// <exception cref="System.Security.Cryptography.CryptographicException">
    /// 비밀번호가 틀리거나 데이터 무결성 검증 실패 시
    /// </exception>
    string Decrypt(EncryptedBackup encrypted, string password);

    /// <summary>
    /// SHA256 해시 계산
    /// </summary>
    /// <param name="input">입력 문자열</param>
    /// <returns>해시값 (Hex)</returns>
    string ComputeSha256Hash(string input);
}
