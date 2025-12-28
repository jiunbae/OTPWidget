using OtpAuthenticator.Core.Models;

namespace OtpAuthenticator.Core.Services.Interfaces;

/// <summary>
/// 백업/복원 서비스 인터페이스
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// 암호화된 백업 파일 생성
    /// </summary>
    /// <param name="filePath">저장 경로</param>
    /// <param name="password">암호화 비밀번호</param>
    /// <param name="includeSettings">설정 포함 여부</param>
    Task ExportAsync(string filePath, string password, bool includeSettings = true);

    /// <summary>
    /// 암호화된 백업 파일에서 복원
    /// </summary>
    /// <param name="filePath">파일 경로</param>
    /// <param name="password">복호화 비밀번호</param>
    /// <param name="restoreSettings">설정 복원 여부</param>
    /// <returns>복원된 계정 수</returns>
    Task<int> ImportAsync(string filePath, string password, bool restoreSettings = false);

    /// <summary>
    /// 백업 파일 미리보기 (암호화된 상태로)
    /// </summary>
    /// <param name="filePath">파일 경로</param>
    /// <returns>백업 메타데이터</returns>
    Task<BackupMetadata?> PreviewBackupAsync(string filePath);

    /// <summary>
    /// 평문 JSON으로 내보내기 (테스트/디버그용)
    /// </summary>
    Task ExportPlainJsonAsync(string filePath, bool includeSettings = true);

    /// <summary>
    /// 평문 JSON에서 가져오기
    /// </summary>
    Task<int> ImportPlainJsonAsync(string filePath, bool restoreSettings = false);
}

/// <summary>
/// 백업 메타데이터 (미리보기용)
/// </summary>
public class BackupMetadata
{
    public string Version { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public int AccountCount { get; set; }
    public bool HasSettings { get; set; }
}
