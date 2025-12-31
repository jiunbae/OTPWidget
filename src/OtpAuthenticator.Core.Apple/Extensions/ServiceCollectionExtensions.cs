using Microsoft.Extensions.DependencyInjection;
using OtpAuthenticator.Core.Services.Interfaces;
using OtpAuthenticator.Core.Apple.Services;

namespace OtpAuthenticator.Core.Apple.Extensions;

/// <summary>
/// Apple 플랫폼 서비스 DI 확장
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Apple 플랫폼 전용 서비스 등록
    /// </summary>
    public static IServiceCollection AddApplePlatformServices(this IServiceCollection services)
    {
        services.AddSingleton<ISecureStorageService, KeychainStorageService>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<IScreenCaptureService, ScreenCaptureService>();
        services.AddSingleton<IQrCodeService, QrCodeService>();

        return services;
    }
}
