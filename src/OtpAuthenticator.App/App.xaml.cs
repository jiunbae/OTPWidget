using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
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
    private Window? _mainWindow;
    private TaskbarIcon? _trayIcon;

    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// 메인 윈도우 접근용 속성
    /// </summary>
    public new Window? MainWindow => _mainWindow;

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
        _mainWindow = new MainWindow();

        // 시스템 트레이 초기화
        InitializeTrayIcon();

        // 설정에 따라 시작 모드 결정
        if (settingsService.Settings.StartMinimized)
        {
            // 최소화 상태로 시작 (트레이만 표시)
            _mainWindow.Hide();
        }
        else
        {
            _mainWindow.Activate();
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
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "OTP Authenticator"
        };

        // 좌클릭: 팝업 표시
        _trayIcon.LeftClickCommand = new RelayCommand(ShowTrayPopup);

        // 더블클릭: 메인 창 표시
        _trayIcon.DoubleClickCommand = new RelayCommand(ShowMainWindow);

        // 컨텍스트 메뉴 설정
        _trayIcon.ContextMenuMode = ContextMenuMode.SecondWindow;
    }

    private void ShowTrayPopup()
    {
        var popup = new TrayPopupWindow();
        popup.Activate();
    }

    private void ShowMainWindow()
    {
        if (_mainWindow != null)
        {
            _mainWindow.Show();
            _mainWindow.Activate();
        }
    }

    public void HideToTray()
    {
        _mainWindow?.Hide();
    }

    public void Exit()
    {
        _trayIcon?.Dispose();
        _mainWindow?.Close();
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
