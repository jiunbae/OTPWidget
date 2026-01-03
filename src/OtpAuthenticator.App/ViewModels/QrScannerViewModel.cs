using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OtpAuthenticator.Core.Models;
using OtpAuthenticator.Core.Services.Interfaces;

namespace OtpAuthenticator.App.ViewModels;

/// <summary>
/// QR 코드 스캐너 ViewModel
/// </summary>
public partial class QrScannerViewModel : BaseViewModel
{
    private readonly IQrCodeService _qrCodeService;
    private readonly IScreenCaptureService _screenCaptureService;
    private readonly IAccountRepository _accountRepository;

    [ObservableProperty]
    private string _statusMessage = "Click 'Scan Screen' to capture QR code from your screen";

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private OtpAccount? _scannedAccount;

    [ObservableProperty]
    private bool _hasScannedAccount;

    [ObservableProperty]
    private string? _manualUri;

    /// <summary>
    /// 계정 추가 완료 이벤트
    /// </summary>
    public event EventHandler<OtpAccount>? AccountAdded;

    /// <summary>
    /// 닫기 요청 이벤트
    /// </summary>
    public event EventHandler? CloseRequested;

    public QrScannerViewModel(
        IQrCodeService qrCodeService,
        IScreenCaptureService screenCaptureService,
        IAccountRepository accountRepository)
    {
        _qrCodeService = qrCodeService;
        _screenCaptureService = screenCaptureService;
        _accountRepository = accountRepository;
    }

    /// <summary>
    /// 전체 화면에서 QR 코드 스캔
    /// </summary>
    [RelayCommand]
    private async Task ScanScreenAsync()
    {
        if (IsScanning) return;

        await ExecuteAsync(async () =>
        {
            IsScanning = true;
            StatusMessage = "Scanning screen for QR codes...";
            ScannedAccount = null;
            HasScannedAccount = false;

            // 모든 화면 캡처
            var captures = await _screenCaptureService.CaptureAllScreensAsync();

            foreach (var capture in captures)
            {
                if (!capture.IsSuccess) continue;

                // QR 코드 디코딩 시도
                var account = _qrCodeService.DecodeOtpAccountFromImage(
                    capture.PixelData,
                    capture.Width,
                    capture.Height);

                if (account != null)
                {
                    ScannedAccount = account;
                    HasScannedAccount = true;
                    StatusMessage = $"Found: {account.DisplayName}";
                    return;
                }
            }

            StatusMessage = "No QR code found on screen. Try selecting a specific area.";
        });

        IsScanning = false;
    }

    /// <summary>
    /// 영역 선택하여 스캔
    /// </summary>
    [RelayCommand]
    private async Task ScanWithPickerAsync()
    {
        if (IsScanning) return;

        await ExecuteAsync(async () =>
        {
            IsScanning = true;
            StatusMessage = "Select a window or screen area...";
            ScannedAccount = null;
            HasScannedAccount = false;

            var capture = await _screenCaptureService.CaptureWithPickerAsync();

            if (capture == null || !capture.IsSuccess)
            {
                StatusMessage = "Capture cancelled or failed";
                return;
            }

            var account = _qrCodeService.DecodeOtpAccountFromImage(
                capture.PixelData,
                capture.Width,
                capture.Height);

            if (account != null)
            {
                ScannedAccount = account;
                HasScannedAccount = true;
                StatusMessage = $"Found: {account.DisplayName}";
            }
            else
            {
                StatusMessage = "No QR code found in selected area";
            }
        });

        IsScanning = false;
    }

    /// <summary>
    /// 파일에서 QR 코드 스캔
    /// </summary>
    [RelayCommand]
    private void ScanFromFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return;

        StatusMessage = "Scanning QR code from image file...";
        ScannedAccount = null;
        HasScannedAccount = false;

        var account = _qrCodeService.DecodeOtpAccountFromFile(filePath);

        if (account != null)
        {
            ScannedAccount = account;
            HasScannedAccount = true;
            StatusMessage = $"Found: {account.DisplayName}";
        }
        else
        {
            StatusMessage = "No QR code found in the image file";
        }
    }

    /// <summary>
    /// 수동 URI 입력으로 추가
    /// </summary>
    [RelayCommand]
    private void ParseManualUri()
    {
        if (string.IsNullOrWhiteSpace(ManualUri))
        {
            StatusMessage = "Please enter an otpauth:// URI";
            return;
        }

        // IOtpService를 사용하여 URI 파싱
        var otpService = App.Services.GetService(typeof(IOtpService)) as IOtpService;
        if (otpService == null) return;

        var account = otpService.ParseOtpAuthUri(ManualUri);

        if (account != null)
        {
            ScannedAccount = account;
            HasScannedAccount = true;
            StatusMessage = $"Parsed: {account.DisplayName}";
        }
        else
        {
            StatusMessage = "Invalid otpauth:// URI format";
            HasScannedAccount = false;
        }
    }

    /// <summary>
    /// 스캔된 계정 저장
    /// </summary>
    [RelayCommand]
    private async Task SaveAccountAsync()
    {
        if (ScannedAccount == null) return;

        await ExecuteAsync(async () =>
        {
            var savedAccount = await _accountRepository.AddAsync(ScannedAccount);
            AccountAdded?.Invoke(this, savedAccount);
            StatusMessage = "Account added successfully!";

            // 잠시 후 닫기
            await Task.Delay(1000);
            CloseRequested?.Invoke(this, EventArgs.Empty);
        });
    }

    /// <summary>
    /// 취소
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 초기화
    /// </summary>
    public void Reset()
    {
        StatusMessage = "Click 'Scan Screen' to capture QR code from your screen";
        ScannedAccount = null;
        HasScannedAccount = false;
        ManualUri = null;
        IsScanning = false;
    }
}
