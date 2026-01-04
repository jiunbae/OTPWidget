using Microsoft.Extensions.DependencyInjection;
using OtpAuthenticator.Core.Services.Interfaces;
using OtpAuthenticator.Core.Windows.Services;

namespace OtpAuthenticator.Core.Windows.Extensions;

/// <summary>
/// Windows 플랫폼 서비스 DI 확장
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Windows 플랫폼 전용 서비스 등록
    /// </summary>
    public static IServiceCollection AddWindowsPlatformServices(this IServiceCollection services)
    {
        services.AddSingleton<ISecureStorageService, SecureStorageService>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<IScreenCaptureService, ScreenCaptureService>();
        services.AddSingleton<IQrCodeService, QrCodeService>();

        return services;
    }
}
