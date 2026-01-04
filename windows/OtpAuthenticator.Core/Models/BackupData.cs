namespace OtpAuthenticator.Core.Models;

/// <summary>
/// 백업 데이터 (평문)
/// </summary>
public class BackupData
{
    /// <summary>
    /// 백업 형식 버전
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// 백업 생성 시간
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 백업 생성 장치명
    /// </summary>
    public string DeviceName { get; set; } = Environment.MachineName;

    /// <summary>
    /// OTP 계정 목록
    /// </summary>
    public List<OtpAccount> Accounts { get; set; } = new();

    /// <summary>
    /// 앱 설정 (선택)
    /// </summary>
    public AppSettings? Settings { get; set; }

    /// <summary>
    /// 데이터 체크섬 (SHA256)
    /// </summary>
    public string Checksum { get; set; } = string.Empty;
}

/// <summary>
/// 암호화된 백업 데이터
/// </summary>
public class EncryptedBackup
{
    /// <summary>
    /// 암호화 형식 버전
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// PBKDF2 솔트 (Base64)
    /// </summary>
    public string Salt { get; set; } = string.Empty;

    /// <summary>
    /// AES IV (Base64)
    /// </summary>
    public string IV { get; set; } = string.Empty;

    /// <summary>
    /// 암호화된 데이터 (Base64)
    /// </summary>
    public string Data { get; set; } = string.Empty;

    /// <summary>
    /// HMAC-SHA256 무결성 검증값 (Base64)
    /// </summary>
    public string Hmac { get; set; } = string.Empty;
}

/// <summary>
/// 동기화 상태
/// </summary>
public class SyncState
{
    /// <summary>
    /// 동기화 상태
    /// </summary>
    public SyncStatus Status { get; set; } = SyncStatus.Idle;

    /// <summary>
    /// 마지막 동기화 시간
    /// </summary>
    public DateTime? LastSyncTime { get; set; }

    /// <summary>
    /// 오류 메시지
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 진행률 (0-100)
    /// </summary>
    public int Progress { get; set; }
}

/// <summary>
/// 동기화 상태
/// </summary>
public enum SyncStatus
{
    Idle,
    Syncing,
    Completed,
    Error
}

/// <summary>
/// 동기화 결과
/// </summary>
public enum SyncResult
{
    Success,
    ProviderNotFound,
    AuthenticationFailed,
    NetworkError,
    ConflictDetected,
    Error
}
