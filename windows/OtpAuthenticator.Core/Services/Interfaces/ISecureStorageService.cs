namespace OtpAuthenticator.Core.Services.Interfaces;

/// <summary>
/// 보안 저장소 서비스 인터페이스
/// </summary>
public interface ISecureStorageService
{
    /// <summary>
    /// 비밀 값 저장 (PasswordVault)
    /// </summary>
    /// <param name="key">키</param>
    /// <param name="value">값</param>
    void StoreSecret(string key, string value);

    /// <summary>
    /// 비밀 값 조회
    /// </summary>
    /// <param name="key">키</param>
    /// <returns>값 또는 null</returns>
    string? RetrieveSecret(string key);

    /// <summary>
    /// 비밀 값 삭제
    /// </summary>
    /// <param name="key">키</param>
    void RemoveSecret(string key);

    /// <summary>
    /// 암호화된 데이터 저장 (DPAPI)
    /// </summary>
    /// <typeparam name="T">데이터 타입</typeparam>
    /// <param name="filename">파일명</param>
    /// <param name="data">데이터</param>
    Task SaveEncryptedDataAsync<T>(string filename, T data);

    /// <summary>
    /// 암호화된 데이터 로드
    /// </summary>
    /// <typeparam name="T">데이터 타입</typeparam>
    /// <param name="filename">파일명</param>
    /// <returns>데이터 또는 default</returns>
    Task<T?> LoadEncryptedDataAsync<T>(string filename);

    /// <summary>
    /// 암호화된 데이터 파일 삭제
    /// </summary>
    /// <param name="filename">파일명</param>
    Task DeleteEncryptedDataAsync(string filename);

    /// <summary>
    /// 데이터 디렉토리 경로
    /// </summary>
    string DataDirectory { get; }
}
