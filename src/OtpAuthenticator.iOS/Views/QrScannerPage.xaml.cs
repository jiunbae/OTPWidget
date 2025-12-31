using OtpAuthenticator.iOS.ViewModels;
using ZXing.Net.Maui;

namespace OtpAuthenticator.iOS.Views;

public partial class QrScannerPage : ContentPage
{
    private readonly QrScannerViewModel _viewModel;

    public QrScannerPage(QrScannerViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    private void OnBarcodesDetected(object sender, BarcodeDetectionEventArgs e)
    {
        var result = e.Results?.FirstOrDefault();
        if (result != null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _viewModel.ProcessBarcode(result.Value);
            });
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.StartScanning();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.StopScanning();
    }
}
