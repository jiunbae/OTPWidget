using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using OtpAuthenticator.App.ViewModels;

namespace OtpAuthenticator.App.Views;

/// <summary>
/// 계정 목록 페이지
/// </summary>
public sealed partial class AccountListPage : Page
{
    public AccountListViewModel ViewModel { get; }

    public AccountListPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<AccountListViewModel>();

        // 이벤트 연결
        AddButton.Click += OnAddButtonClick;
        ScanQrButton.Click += OnScanQrButtonClick;

        // ViewModel 상태 바인딩
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // Handle navigation parameters
        if (e.Parameter is AccountListNavigationArgs args)
        {
            ViewModel.FilterFolderId = args.FolderId;
            ViewModel.ShowFavoritesOnly = args.ShowFavoritesOnly;
            ViewModel.ShowUncategorizedOnly = args.ShowUncategorizedOnly;
        }
        else
        {
            // Reset filters for "All Accounts"
            ViewModel.FilterFolderId = null;
            ViewModel.ShowFavoritesOnly = false;
            ViewModel.ShowUncategorizedOnly = false;
        }

        await ViewModel.LoadAccountsAsync();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        // 리소스 정리하지 않음 (다시 돌아올 수 있으므로)
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ViewModel.IsEmpty):
                EmptyState.Visibility = ViewModel.IsEmpty ? Visibility.Visible : Visibility.Collapsed;
                AccountListView.Visibility = ViewModel.IsEmpty ? Visibility.Collapsed : Visibility.Visible;
                break;

            case nameof(ViewModel.IsLoading):
                LoadingRing.IsActive = ViewModel.IsLoading;
                break;
        }
    }

    private void OnAddButtonClick(object sender, RoutedEventArgs e)
    {
        ViewModel.AddAccountCommand.Execute(null);
    }

    private void OnScanQrButtonClick(object sender, RoutedEventArgs e)
    {
        // QR 스캔 기능 (나중에 구현)
        // TODO: QR 스캔 창 열기
    }
}
