using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OtpAuthenticator.Core.Services.Interfaces;
using ZXing.Net.Maui;

namespace OtpAuthenticator.iOS.ViewModels;

public partial class QrScannerViewModel : ObservableObject
{
    private readonly IOtpService _otpService;
    private readonly IAccountRepository _accountRepository;
    private bool _hasProcessed;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private BarcodeReaderOptions _scannerOptions;

    public QrScannerViewModel(IOtpService otpService, IAccountRepository accountRepository)
    {
        _otpService = otpService;
        _accountRepository = accountRepository;

        ScannerOptions = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.TwoDimensional,
            AutoRotate = true,
            Multiple = false
        };
    }

    public void StartScanning()
    {
        _hasProcessed = false;
        IsScanning = true;
    }

    public void StopScanning()
    {
        IsScanning = false;
    }

    public async void ProcessBarcode(string value)
    {
        if (_hasProcessed || string.IsNullOrEmpty(value))
            return;

        _hasProcessed = true;
        IsScanning = false;

        var account = _otpService.ParseOtpAuthUri(value);
        if (account != null)
        {
            await _accountRepository.AddAsync(account);

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Shell.Current.DisplayAlert("Success", $"Account '{account.DisplayName}' added!", "OK");
                await Shell.Current.GoToAsync("..");
            });
        }
        else
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Shell.Current.DisplayAlert("Error", "Invalid QR code format", "OK");
                _hasProcessed = false;
                IsScanning = true;
            });
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
