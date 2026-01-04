using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OtpAuthenticator.App.ViewModels;
using OtpAuthenticator.App.Views;
using OtpAuthenticator.Core.Extensions;
using OtpAuthenticator.Core.Windows.Extensions;
using OtpAuthenticator.Core.Services.Interfaces;
using H.NotifyIcon;

namespace OtpAuthenticator.App;

/// <summary>
/// 애플리케이션 엔트리포인트
/// </summary>
public partial class App : Application
{
    private TaskbarIcon? _trayIcon;

    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// 메인 윈도우 접근용 정적 속성
    /// </summary>
    public static Window? MainWindow { get; private set; }

    public App()
    {
        this.InitializeComponent();

        // DI 컨테이너 설정
        Services = ConfigureServices();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        // 설정 로드
        var settingsService = Services.GetRequiredService<ISettingsService>();
        await settingsService.LoadAsync();

        // 메인 윈도우 생성
        MainWindow = new MainWindow();

        // 저장된 테마 적용
        SettingsViewModel.ApplyTheme(settingsService.Settings.Theme);

        // 시스템 트레이 초기화
        InitializeTrayIcon();

        // 설정에 따라 시작 모드 결정
        if (settingsService.Settings.StartMinimized)
        {
            // 최소화 상태로 시작 (트레이만 표시)
            MainWindow.Hide();
        }
        else
        {
            MainWindow.Activate();
        }
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Core 서비스 등록
        services.AddCoreServices();
        services.AddWindowsPlatformServices();
        services.AddAccountRepository();

        // ViewModels 등록
        services.AddTransient<MainViewModel>();
        services.AddTransient<AccountListViewModel>();
        services.AddTransient<AccountEditViewModel>();
        services.AddTransient<TrayPopupViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<QrScannerViewModel>();

        return services.BuildServiceProvider();
    }

    private void InitializeTrayIcon()
    {
        try
        {
            _trayIcon = new TaskbarIcon
            {
                ToolTipText = "OTP Authenticator",
                IconSource = new H.NotifyIcon.GeneratedIconSource
                {
                    Text = "OTP",
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White),
                    Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DodgerBlue)
                }
            };
        }
        catch (Exception ex)
        {
            // 트레이 아이콘 초기화 실패 시 로그
            var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OtpAuthenticator", "app.log");
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.AppendAllText(logPath, $"[{DateTime.Now}] TrayIcon Error: {ex}\n");
            return;
        }

        // 좌클릭: 팝업 표시
        _trayIcon.LeftClickCommand = new RelayCommand(ShowTrayPopup);

        // 더블클릭: 메인 창 표시
        _trayIcon.DoubleClickCommand = new RelayCommand(ShowMainWindow);

        // 우클릭 컨텍스트 메뉴 설정
        _trayIcon.ContextMenuMode = ContextMenuMode.SecondWindow;
        _trayIcon.ContextFlyout = CreateContextMenu();

        // 트레이 아이콘 강제 생성
        _trayIcon.ForceCreate();
    }

    private MenuFlyout CreateContextMenu()
    {
        var menu = new MenuFlyout();

        // 메인 창 열기
        var openItem = new MenuFlyoutItem
        {
            Text = "Open OTP Authenticator",
            Icon = new FontIcon { Glyph = "\uE8A7" }
        };
        openItem.Click += (s, e) => ShowMainWindow();
        menu.Items.Add(openItem);

        // 설정
        var settingsItem = new MenuFlyoutItem
        {
            Text = "Settings",
            Icon = new FontIcon { Glyph = "\uE713" }
        };
        settingsItem.Click += (s, e) => OpenSettings();
        menu.Items.Add(settingsItem);

        // 구분선
        menu.Items.Add(new MenuFlyoutSeparator());

        // 종료
        var exitItem = new MenuFlyoutItem
        {
            Text = "Exit",
            Icon = new FontIcon { Glyph = "\uE7E8" }
        };
        exitItem.Click += (s, e) => Exit();
        menu.Items.Add(exitItem);

        return menu;
    }

    private void OpenSettings()
    {
        ShowMainWindow();
        // 설정 페이지로 이동
        if (MainWindow is MainWindow mainWin)
        {
            mainWin.NavigateToSettings();
        }
    }

    private void ShowTrayPopup()
    {
        var popup = new TrayPopupWindow();
        popup.Activate();
    }

    private void ShowMainWindow()
    {
        if (MainWindow != null)
        {
            MainWindow.Show();
            MainWindow.Activate();
        }
    }

    public void HideToTray()
    {
        MainWindow?.Hide();
    }

    public new void Exit()
    {
        _trayIcon?.Dispose();
        MainWindow?.Close();
        Environment.Exit(0);
    }
}

/// <summary>
/// 간단한 RelayCommand 구현
/// </summary>
public class RelayCommand : System.Windows.Input.ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
