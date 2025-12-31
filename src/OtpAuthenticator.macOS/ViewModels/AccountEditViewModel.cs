using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OtpAuthenticator.Core.Models;
using OtpAuthenticator.Core.Services.Interfaces;

namespace OtpAuthenticator.macOS.ViewModels;

[QueryProperty(nameof(AccountId), "id")]
public partial class AccountEditViewModel : BaseViewModel
{
    private readonly IAccountRepository _accountRepository;
    private readonly IOtpService _otpService;
    private readonly IQrCodeService _qrCodeService;

    [ObservableProperty]
    private string? _accountId;

    [ObservableProperty]
    private string _issuer = string.Empty;

    [ObservableProperty]
    private string _accountName = string.Empty;

    [ObservableProperty]
    private string _secretKey = string.Empty;

    [ObservableProperty]
    private OtpType _otpType = OtpType.Totp;

    [ObservableProperty]
    private HashAlgorithmType _algorithm = HashAlgorithmType.Sha1;

    [ObservableProperty]
    private int _digits = 6;

    [ObservableProperty]
    private int _period = 30;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string? _errorMessage;

    public List<OtpType> OtpTypes => Enum.GetValues<OtpType>().ToList();
    public List<HashAlgorithmType> Algorithms => Enum.GetValues<HashAlgorithmType>().ToList();
    public List<int> DigitOptions => new() { 6, 8 };
    public List<int> PeriodOptions => new() { 30, 60 };

    public AccountEditViewModel(
        IAccountRepository accountRepository,
        IOtpService otpService,
        IQrCodeService qrCodeService)
    {
        _accountRepository = accountRepository;
        _otpService = otpService;
        _qrCodeService = qrCodeService;

        Title = "Add Account";
    }

    partial void OnAccountIdChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            IsEditing = true;
            Title = "Edit Account";
            _ = LoadAccountAsync(value);
        }
    }

    private async Task LoadAccountAsync(string id)
    {
        var account = await _accountRepository.GetByIdAsync(id);
        if (account != null)
        {
            Issuer = account.Issuer;
            AccountName = account.AccountName;
            SecretKey = account.SecretKey;
            OtpType = account.Type;
            Algorithm = account.Algorithm;
            Digits = account.Digits;
            Period = account.Period;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(Issuer))
        {
            ErrorMessage = "Issuer is required";
            return;
        }

        if (string.IsNullOrWhiteSpace(SecretKey))
        {
            ErrorMessage = "Secret key is required";
            return;
        }

        if (!_otpService.ValidateSecretKey(SecretKey))
        {
            ErrorMessage = "Invalid secret key format";
            return;
        }

        try
        {
            IsBusy = true;

            if (IsEditing && !string.IsNullOrEmpty(AccountId))
            {
                var existing = await _accountRepository.GetByIdAsync(AccountId);
                if (existing != null)
                {
                    existing.Issuer = Issuer;
                    existing.AccountName = AccountName;
                    existing.SecretKey = SecretKey;
                    existing.Type = OtpType;
                    existing.Algorithm = Algorithm;
                    existing.Digits = Digits;
                    existing.Period = Period;

                    await _accountRepository.UpdateAsync(existing);
                }
            }
            else
            {
                var account = new OtpAccount
                {
                    Issuer = Issuer,
                    AccountName = AccountName,
                    SecretKey = SecretKey,
                    Type = OtpType,
                    Algorithm = Algorithm,
                    Digits = Digits,
                    Period = Period
                };

                await _accountRepository.AddAsync(account);
            }

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task ScanQrCodeAsync()
    {
        // TODO: Implement QR code scanning using camera or screen capture
        await Shell.Current.DisplayAlert("QR Scan", "QR code scanning will be implemented.", "OK");
    }

    [RelayCommand]
    private void ParseUri(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
            return;

        var account = _otpService.ParseOtpAuthUri(uri);
        if (account != null)
        {
            Issuer = account.Issuer;
            AccountName = account.AccountName;
            SecretKey = account.SecretKey;
            OtpType = account.Type;
            Algorithm = account.Algorithm;
            Digits = account.Digits;
            Period = account.Period;
        }
        else
        {
            ErrorMessage = "Invalid OTP URI format";
        }
    }
}
