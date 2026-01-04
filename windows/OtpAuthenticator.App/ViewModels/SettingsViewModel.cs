using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OtpAuthenticator.Core.Models;
using OtpAuthenticator.Core.Services.Interfaces;

namespace OtpAuthenticator.App.ViewModels;

/// <summary>
/// 설정 ViewModel
/// </summary>
public partial class SettingsViewModel : BaseViewModel
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private bool _startWithWindows;

    [ObservableProperty]
    private bool _startMinimized;

    [ObservableProperty]
    private bool _minimizeToTray;

    [ObservableProperty]
    private bool _autoCopyToClipboard;

    [ObservableProperty]
    private int _clipboardClearSeconds;

    [ObservableProperty]
    private bool _enableWidgetProvider;

    [ObservableProperty]
    private string _selectedTheme = "System";

    [ObservableProperty]
    private bool _requireAuthentication;

    [ObservableProperty]
    private bool _cloudSyncEnabled;

    [ObservableProperty]
    private CloudProvider _selectedCloudProvider;

    [ObservableProperty]
    private bool _autoSync;

    [ObservableProperty]
    private int _syncIntervalMinutes;

    public IReadOnlyList<string> ThemeOptions { get; } = new[] { "System", "Light", "Dark" };
    public IReadOnlyList<int> ClipboardClearOptions { get; } = new[] { 0, 15, 30, 60, 120 };
    public IReadOnlyList<int> SyncIntervalOptions { get; } = new[] { 5, 15, 30, 60 };
    public IReadOnlyList<CloudProvider> CloudProviders { get; } = Enum.GetValues<CloudProvider>();

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    /// <summary>
    /// 설정 로드
    /// </summary>
    [RelayCommand]
    public async Task LoadSettingsAsync()
    {
        await _settingsService.LoadAsync();
        var settings = _settingsService.Settings;

        StartWithWindows = settings.StartWithWindows;
        StartMinimized = settings.StartMinimized;
        MinimizeToTray = settings.MinimizeToTray;
        AutoCopyToClipboard = settings.AutoCopyToClipboard;
        ClipboardClearSeconds = settings.ClipboardClearSeconds;
        EnableWidgetProvider = settings.EnableWidgetProvider;
        SelectedTheme = settings.Theme;
        RequireAuthentication = settings.RequireAuthentication;
        CloudSyncEnabled = settings.CloudSync.Enabled;
        SelectedCloudProvider = settings.CloudSync.Provider;
        AutoSync = settings.CloudSync.AutoSync;
        SyncIntervalMinutes = settings.CloudSync.SyncIntervalMinutes;
    }

    /// <summary>
    /// 설정 저장
    /// </summary>
    [RelayCommand]
    public async Task SaveSettingsAsync()
    {
        await ExecuteAsync(async () =>
        {
            var settings = _settingsService.Settings;

            settings.StartWithWindows = StartWithWindows;
            settings.StartMinimized = StartMinimized;
            settings.MinimizeToTray = MinimizeToTray;
            settings.AutoCopyToClipboard = AutoCopyToClipboard;
            settings.ClipboardClearSeconds = ClipboardClearSeconds;
            settings.EnableWidgetProvider = EnableWidgetProvider;
            settings.Theme = SelectedTheme;
            settings.RequireAuthentication = RequireAuthentication;
            settings.CloudSync.Enabled = CloudSyncEnabled;
            settings.CloudSync.Provider = SelectedCloudProvider;
            settings.CloudSync.AutoSync = AutoSync;
            settings.CloudSync.SyncIntervalMinutes = SyncIntervalMinutes;

            await _settingsService.SaveAsync();

            // Windows 시작 프로그램 등록/해제
            await UpdateStartupAsync();
        });
    }

    /// <summary>
    /// 설정 초기화
    /// </summary>
    [RelayCommand]
    public async Task ResetSettingsAsync()
    {
        await ExecuteAsync(async () =>
        {
            await _settingsService.ResetAsync();
            await LoadSettingsAsync();
        });
    }

    /// <summary>
    /// Windows 시작 프로그램 등록/해제
    /// </summary>
    private async Task UpdateStartupAsync()
    {
        // StartupTask API 사용
        try
        {
            var startupTask = await Windows.ApplicationModel.StartupTask.GetAsync("OtpAuthStartup");

            if (StartWithWindows)
            {
                await startupTask.RequestEnableAsync();
            }
            else
            {
                startupTask.Disable();
            }
        }
        catch
        {
            // 시작 프로그램 설정 실패 (무시)
        }
    }

    // 값 변경 시 자동 저장
    partial void OnStartWithWindowsChanged(bool value) => _ = SaveSettingsAsync();
    partial void OnStartMinimizedChanged(bool value) => _ = SaveSettingsAsync();
    partial void OnMinimizeToTrayChanged(bool value) => _ = SaveSettingsAsync();
    partial void OnAutoCopyToClipboardChanged(bool value) => _ = SaveSettingsAsync();
    partial void OnClipboardClearSecondsChanged(int value) => _ = SaveSettingsAsync();
    partial void OnSelectedThemeChanged(string value)
    {
        ApplyTheme(value);
        _ = SaveSettingsAsync();
    }

    /// <summary>
    /// 테마 적용
    /// </summary>
    public static void ApplyTheme(string theme)
    {
        if (App.MainWindow?.Content is Microsoft.UI.Xaml.FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme switch
            {
                "Light" => Microsoft.UI.Xaml.ElementTheme.Light,
                "Dark" => Microsoft.UI.Xaml.ElementTheme.Dark,
                _ => Microsoft.UI.Xaml.ElementTheme.Default
            };
        }
    }
}
