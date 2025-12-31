using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using OtpAuthenticator.Core.Apple.Extensions;
using OtpAuthenticator.Core.Extensions;
using OtpAuthenticator.macOS.ViewModels;
using OtpAuthenticator.macOS.Views;

namespace OtpAuthenticator.macOS;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register Core Services
        builder.Services.AddCoreServices();

        // Register Apple Platform Services
        builder.Services.AddApplePlatformServices();

        // Register ViewModels
        builder.Services.AddTransient<MainViewModel>();
        builder.Services.AddTransient<AccountListViewModel>();
        builder.Services.AddTransient<AccountEditViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

        // Register Views
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<AccountListPage>();
        builder.Services.AddTransient<AccountEditPage>();
        builder.Services.AddTransient<SettingsPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
