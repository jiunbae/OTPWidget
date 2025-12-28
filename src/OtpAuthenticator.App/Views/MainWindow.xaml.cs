using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OtpAuthenticator.App.ViewModels;

namespace OtpAuthenticator.App.Views;

/// <summary>
/// 메인 윈도우
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        this.InitializeComponent();

        ViewModel = App.Services.GetRequiredService<MainViewModel>();

        // 네비게이션 설정
        NavigationViewControl.SelectionChanged += OnNavigationSelectionChanged;

        // 초기 페이지 로드
        ContentFrame.Navigate(typeof(AccountListPage));

        // ViewModel 초기화
        _ = ViewModel.InitializeAsync();

        // 편집 패널 바인딩
        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.IsAccountEditVisible))
            {
                EditOverlay.Visibility = ViewModel.IsAccountEditVisible
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        };

        // 창 설정
        SetupWindow();
    }

    private void SetupWindow()
    {
        // 창 크기 설정
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

        appWindow.Resize(new Windows.Graphics.SizeInt32(900, 650));
        appWindow.Title = "OTP Authenticator";

        // 아이콘 설정 (나중에)
        // appWindow.SetIcon("Assets/Logo/app.ico");
    }

    private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }

        var selectedItem = args.SelectedItem as NavigationViewItem;
        if (selectedItem == null) return;

        var tag = selectedItem.Tag?.ToString();

        var pageType = tag switch
        {
            "accounts" => typeof(AccountListPage),
            "backup" => typeof(BackupPage),
            _ => typeof(AccountListPage)
        };

        ContentFrame.Navigate(pageType);
    }

    /// <summary>
    /// 창 닫기 처리
    /// </summary>
    private void OnWindowClosing(object sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
    {
        // 설정에 따라 트레이로 최소화
        var settingsService = App.Services.GetRequiredService<Core.Services.Interfaces.ISettingsService>();
        if (settingsService.Settings.MinimizeToTray)
        {
            args.Cancel = true;
            this.Hide();
        }
    }

    public void Show()
    {
        this.Activate();
    }

    public void Hide()
    {
        // WinUI 3에서는 직접 숨기기 어려움, 최소화로 대체
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        PInvoke.User32.ShowWindow(hwnd, PInvoke.User32.ShowWindowCommand.SW_HIDE);
    }
}

/// <summary>
/// P/Invoke 헬퍼
/// </summary>
internal static partial class PInvoke
{
    internal static class User32
    {
        public enum ShowWindowCommand
        {
            SW_HIDE = 0,
            SW_SHOW = 5,
            SW_MINIMIZE = 6,
            SW_RESTORE = 9
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, ShowWindowCommand nCmdShow);
    }
}
