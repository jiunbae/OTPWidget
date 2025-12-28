using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using OtpAuthenticator.App.ViewModels;
using Windows.Graphics;

namespace OtpAuthenticator.App.Views;

/// <summary>
/// 시스템 트레이 팝업 윈도우
/// </summary>
public sealed partial class TrayPopupWindow : Window
{
    public TrayPopupViewModel ViewModel { get; }
    private AppWindow? _appWindow;

    public TrayPopupWindow()
    {
        this.InitializeComponent();

        ViewModel = App.Services.GetRequiredService<TrayPopupViewModel>();

        // 이벤트 연결
        ExpandButton.Click += OnExpandClick;
        ScanButton.Click += OnScanClick;
        SettingsButton.Click += OnSettingsClick;

        ViewModel.CloseRequested += (s, e) => this.Close();
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;

        // 창 설정
        SetupWindow();

        // 데이터 로드
        _ = ViewModel.LoadAccountsAsync();

        // 바인딩
        AccountListView.ItemsSource = ViewModel.Accounts;
    }

    private void SetupWindow()
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);

        if (_appWindow != null)
        {
            // 크기 설정
            _appWindow.Resize(new SizeInt32(340, 450));

            // 창 스타일 설정
            if (_appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsAlwaysOnTop = true;
                presenter.SetBorderAndTitleBar(false, false);
                presenter.IsResizable = false;
                presenter.IsMinimizable = false;
                presenter.IsMaximizable = false;
            }

            // 위치 설정 (작업 표시줄 근처)
            PositionNearTaskbar();
        }

        // 포커스 잃으면 닫기
        this.Activated += OnWindowActivated;
    }

    private void PositionNearTaskbar()
    {
        if (_appWindow == null) return;

        try
        {
            var displayArea = DisplayArea.GetFromWindowId(
                _appWindow.Id,
                DisplayAreaFallback.Primary);

            var workArea = displayArea.WorkArea;
            var windowSize = _appWindow.Size;

            // 오른쪽 하단에 위치 (작업 표시줄 위)
            int x = workArea.X + workArea.Width - windowSize.Width - 12;
            int y = workArea.Y + workArea.Height - windowSize.Height - 12;

            _appWindow.Move(new PointInt32(x, y));
        }
        catch
        {
            // 위치 설정 실패 시 무시
        }
    }

    private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            // 포커스 잃으면 닫기
            ViewModel.Cleanup();
            this.Close();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.IsEmpty))
        {
            EmptyState.Visibility = ViewModel.IsEmpty ? Visibility.Visible : Visibility.Collapsed;
            AccountListView.Visibility = ViewModel.IsEmpty ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    private void OnExpandClick(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenMainWindowCommand.Execute(null);
    }

    private void OnScanClick(object sender, RoutedEventArgs e)
    {
        ViewModel.ScanQrCommand.Execute(null);
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        ViewModel.OpenSettingsCommand.Execute(null);
    }

    private void OnItemPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        // 호버 효과
    }

    private void OnItemPointerExited(object sender, PointerRoutedEventArgs e)
    {
        // 호버 효과 제거
    }
}
