using Microsoft.Extensions.DependencyInjection;
using OtpAuthenticator.Core.Services;
using OtpAuthenticator.Core.Services.Interfaces;

namespace OtpAuthenticator.Core.Extensions;

/// <summary>
/// DI 확장 메서드 (플랫폼 독립적)
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Core 서비스 등록 (플랫폼 독립적인 서비스만)
    /// 플랫폼별 서비스는 각 플랫폼 확장에서 등록:
    /// - Windows: OtpAuthenticator.Core.Windows.Extensions.AddWindowsPlatformServices()
    /// - Apple: OtpAuthenticator.Core.Apple.Extensions.AddApplePlatformServices()
    /// </summary>
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        // 플랫폼 독립적 서비스
        services.AddSingleton<IOtpService, OtpService>();
        services.AddSingleton<IEncryptionService, EncryptionService>();
        services.AddTransient<IBackupService, BackupService>();

        return services;
    }

    /// <summary>
    /// 플랫폼별 저장소 서비스를 사용하는 리포지토리 등록
    /// ISecureStorageService가 먼저 등록되어 있어야 함
    /// </summary>
    public static IServiceCollection AddAccountRepository(this IServiceCollection services)
    {
        services.AddSingleton<IAccountRepository, AccountRepository>();
        services.AddSingleton<ISettingsService, SettingsService>();

        return services;
    }

    // Cloud Sync is temporarily disabled - requires Azure AD / Google Cloud setup
    // #if NET8_0_WINDOWS
    //     public static IServiceCollection AddCloudSyncServices(this IServiceCollection services)
    //     {
    //         services.AddSingleton<ICloudSyncService, CloudSync.SyncManager>();
    //         return services;
    //     }
    // #endif
}
