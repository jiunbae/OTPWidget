namespace OtpAuthenticator.Core.Models;

/// <summary>
/// 애플리케이션 설정
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Windows 시작 시 자동 실행
    /// </summary>
    public bool StartWithWindows { get; set; } = false;

    /// <summary>
    /// 시작 시 최소화 상태로 실행
    /// </summary>
    public bool StartMinimized { get; set; } = true;

    /// <summary>
    /// 닫기 버튼 클릭 시 트레이로 최소화
    /// </summary>
    public bool MinimizeToTray { get; set; } = true;

    /// <summary>
    /// OTP 클릭 시 자동으로 클립보드에 복사
    /// </summary>
    public bool AutoCopyToClipboard { get; set; } = true;

    /// <summary>
    /// 클립보드 자동 삭제 시간 (초)
    /// </summary>
    public int ClipboardClearSeconds { get; set; } = 30;

    /// <summary>
    /// 작업 표시줄에 표시
    /// </summary>
    public bool ShowInTaskbar { get; set; } = false;

    /// <summary>
    /// Windows 11 위젯 활성화
    /// </summary>
    public bool EnableWidgetProvider { get; set; } = true;

    /// <summary>
    /// 테마 (Light, Dark, System)
    /// </summary>
    public string Theme { get; set; } = "System";

    /// <summary>
    /// 언어 (ko-KR, en-US, System)
    /// </summary>
    public string Language { get; set; } = "System";

    /// <summary>
    /// 앱 실행 시 인증 필요 여부 (Windows Hello)
    /// </summary>
    public bool RequireAuthentication { get; set; } = false;

    /// <summary>
    /// 복사 후 알림 표시
    /// </summary>
    public bool ShowCopyNotification { get; set; } = true;

    /// <summary>
    /// 클라우드 동기화 설정
    /// </summary>
    public CloudSyncSettings CloudSync { get; set; } = new();

    /// <summary>
    /// 핫키 설정
    /// </summary>
    public HotkeySettings Hotkeys { get; set; } = new();
}

/// <summary>
/// 클라우드 동기화 설정
/// </summary>
public class CloudSyncSettings
{
    /// <summary>
    /// 동기화 활성화 여부
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// 클라우드 제공자
    /// </summary>
    public CloudProvider Provider { get; set; } = CloudProvider.None;

    /// <summary>
    /// 마지막 동기화 시간
    /// </summary>
    public DateTime? LastSyncTime { get; set; }

    /// <summary>
    /// 자동 동기화 활성화
    /// </summary>
    public bool AutoSync { get; set; } = true;

    /// <summary>
    /// 동기화 주기 (분)
    /// </summary>
    public int SyncIntervalMinutes { get; set; } = 15;
}

/// <summary>
/// 클라우드 제공자
/// </summary>
public enum CloudProvider
{
    None,
    OneDrive,
    GoogleDrive
}

/// <summary>
/// 핫키 설정
/// </summary>
public class HotkeySettings
{
    /// <summary>
    /// 팝업 표시 핫키
    /// </summary>
    public string ShowPopup { get; set; } = "Ctrl+Shift+O";

    /// <summary>
    /// 빠른 복사 핫키
    /// </summary>
    public string QuickCopy { get; set; } = "Ctrl+Shift+C";
}
