namespace OtpAuthenticator.Core.CloudSync;

/// <summary>
/// 클라우드 프로바이더 인터페이스
/// </summary>
public interface ICloudProvider
{
    /// <summary>
    /// 프로바이더 이름
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// 인증 여부 확인
    /// </summary>
    Task<bool> IsAuthenticatedAsync();

    /// <summary>
    /// 인증 수행
    /// </summary>
    Task<bool> AuthenticateAsync();

    /// <summary>
    /// 로그아웃
    /// </summary>
    Task SignOutAsync();

    /// <summary>
    /// 파일 업로드
    /// </summary>
    /// <param name="fileName">파일 이름</param>
    /// <param name="data">파일 데이터</param>
    /// <returns>업로드된 파일 정보</returns>
    Task<CloudFile?> UploadAsync(string fileName, byte[] data);

    /// <summary>
    /// 파일 다운로드
    /// </summary>
    /// <param name="fileId">파일 ID</param>
    /// <returns>파일 데이터</returns>
    Task<byte[]?> DownloadAsync(string fileId);

    /// <summary>
    /// 파일 정보 조회
    /// </summary>
    /// <param name="fileName">파일 이름</param>
    /// <returns>파일 정보 또는 null</returns>
    Task<CloudFile?> GetFileInfoAsync(string fileName);

    /// <summary>
    /// 파일 삭제
    /// </summary>
    /// <param name="fileId">파일 ID</param>
    Task<bool> DeleteAsync(string fileId);

    /// <summary>
    /// 파일 마지막 수정 시간 조회
    /// </summary>
    /// <param name="fileName">파일 이름</param>
    /// <returns>마지막 수정 시간 또는 null</returns>
    Task<DateTime?> GetLastModifiedAsync(string fileName);
}

/// <summary>
/// 클라우드 파일 정보
/// </summary>
public class CloudFile
{
    /// <summary>
    /// 파일 ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 파일 이름
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 마지막 수정 시간
    /// </summary>
    public DateTime ModifiedTime { get; set; }

    /// <summary>
    /// 파일 크기 (바이트)
    /// </summary>
    public long Size { get; set; }
}
