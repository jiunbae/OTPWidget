using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OtpAuthenticator.App.ViewModels;
using OtpAuthenticator.Core.Models;
using Windows.Storage.Pickers;

namespace OtpAuthenticator.App.Views;

/// <summary>
/// QR 코드 스캐너 다이얼로그
/// </summary>
public sealed partial class QrScannerDialog : ContentDialog
{
    public QrScannerViewModel ViewModel { get; }

    /// <summary>
    /// 추가된 계정 (다이얼로그가 닫힌 후 확인)
    /// </summary>
    public OtpAccount? AddedAccount { get; private set; }

    public QrScannerDialog()
    {
        this.InitializeComponent();

        ViewModel = App.Services.GetRequiredService<QrScannerViewModel>();
        ViewModel.Reset();

        // 이벤트 연결
        ScanScreenButton.Click += OnScanScreenClick;
        ScanAreaButton.Click += OnScanAreaClick;
        ImportImageButton.Click += OnImportImageClick;
        ParseUriButton.Click += OnParseUriClick;

        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        ViewModel.AccountAdded += OnAccountAdded;

        // 다이얼로그 버튼 이벤트
        this.PrimaryButtonClick += OnPrimaryButtonClick;
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ViewModel.StatusMessage):
                StatusInfoBar.Message = ViewModel.StatusMessage;
                break;

            case nameof(ViewModel.IsScanning):
                LoadingProgress.Visibility = ViewModel.IsScanning ? Visibility.Visible : Visibility.Collapsed;
                ScanScreenButton.IsEnabled = !ViewModel.IsScanning;
                ScanAreaButton.IsEnabled = !ViewModel.IsScanning;
                break;

            case nameof(ViewModel.HasScannedAccount):
                UpdateAccountPreview();
                IsPrimaryButtonEnabled = ViewModel.HasScannedAccount;
                break;

            case nameof(ViewModel.ScannedAccount):
                UpdateAccountPreview();
                break;
        }
    }

    private void UpdateAccountPreview()
    {
        if (ViewModel.ScannedAccount != null && ViewModel.HasScannedAccount)
        {
            AccountPreview.Visibility = Visibility.Visible;
            StatusInfoBar.Severity = InfoBarSeverity.Success;

            var account = ViewModel.ScannedAccount;
            AccountInitial.Text = account.Initial;
            AccountIssuer.Text = string.IsNullOrEmpty(account.Issuer) ? "Unknown" : account.Issuer;
            AccountName.Text = account.AccountName;
            AccountType.Text = account.Type.ToString().ToUpperInvariant();
            AccountAlgorithm.Text = $"{account.Algorithm} • {account.Digits} digits";
        }
        else
        {
            AccountPreview.Visibility = Visibility.Collapsed;
            StatusInfoBar.Severity = InfoBarSeverity.Informational;
        }
    }

    private async void OnScanScreenClick(object sender, RoutedEventArgs e)
    {
        // 다이얼로그를 잠시 숨기고 스캔
        this.Hide();
        await Task.Delay(500); // 다이얼로그가 완전히 닫힐 때까지 대기

        await ViewModel.ScanScreenCommand.ExecuteAsync(null);

        // 다시 표시
        await this.ShowAsync();
    }

    private async void OnScanAreaClick(object sender, RoutedEventArgs e)
    {
        this.Hide();
        await Task.Delay(300);

        await ViewModel.ScanWithPickerCommand.ExecuteAsync(null);

        await this.ShowAsync();
    }

    private async void OnImportImageClick(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
        picker.FileTypeFilter.Add(".bmp");
        picker.FileTypeFilter.Add(".gif");
        picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;

        // WinUI 3에서는 윈도우 핸들이 필요
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            ViewModel.ScanFromFileCommand.Execute(file.Path);
        }
    }

    private void OnParseUriClick(object sender, RoutedEventArgs e)
    {
        ViewModel.ManualUri = ManualUriTextBox.Text;
        ViewModel.ParseManualUriCommand.Execute(null);
    }

    private async void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // 저장 처리를 위해 deferral 사용
        var deferral = args.GetDeferral();

        try
        {
            await ViewModel.SaveAccountCommand.ExecuteAsync(null);
        }
        finally
        {
            deferral.Complete();
        }
    }

    private void OnAccountAdded(object? sender, OtpAccount account)
    {
        AddedAccount = account;
    }
}
