using Microsoft.Extensions.DependencyInjection;
using OtpAuthenticator.Core.CloudSync;
using OtpAuthenticator.Core.Services;
using OtpAuthenticator.Core.Services.Interfaces;

namespace OtpAuthenticator.Core.Extensions;

/// <summary>
/// DI 확장 메서드
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Core 서비스 등록
    /// </summary>
    public static IServiceCollection AddOtpAuthenticatorCore(this IServiceCollection services)
    {
        // Singleton 서비스
        services.AddSingleton<ISecureStorageService, SecureStorageService>();
        services.AddSingleton<IEncryptionService, EncryptionService>();
        services.AddSingleton<IOtpService, OtpService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IAccountRepository, AccountRepository>();
        services.AddSingleton<IClipboardService, ClipboardService>();

        // QR 코드 & 화면 캡처 서비스
        services.AddSingleton<IQrCodeService, QrCodeService>();
        services.AddSingleton<IScreenCaptureService, ScreenCaptureService>();

        // 클라우드 동기화 서비스
        services.AddSingleton<ICloudSyncService, SyncManager>();

        // Transient 서비스
        services.AddTransient<IBackupService, BackupService>();

        return services;
    }
}
