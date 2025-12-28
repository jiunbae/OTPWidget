using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OtpAuthenticator.Core.Models;
using OtpAuthenticator.Core.Services.Interfaces;

namespace OtpAuthenticator.App.ViewModels;

/// <summary>
/// 계정 편집/추가 ViewModel
/// </summary>
public partial class AccountEditViewModel : BaseViewModel
{
    private readonly IAccountRepository _accountRepository;
    private readonly IOtpService _otpService;

    private OtpAccount? _originalAccount;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private string _issuer = string.Empty;

    [ObservableProperty]
    private string _accountName = string.Empty;

    [ObservableProperty]
    private string _secretKey = string.Empty;

    [ObservableProperty]
    private OtpType _selectedType = OtpType.Totp;

    [ObservableProperty]
    private HashAlgorithmType _selectedAlgorithm = HashAlgorithmType.Sha1;

    [ObservableProperty]
    private int _digits = 6;

    [ObservableProperty]
    private int _period = 30;

    [ObservableProperty]
    private string? _notes;

    [ObservableProperty]
    private string? _validationError;

    [ObservableProperty]
    private bool _isValid;

    /// <summary>
    /// 저장 완료 이벤트
    /// </summary>
    public event EventHandler<OtpAccount>? Saved;

    /// <summary>
    /// 취소 이벤트
    /// </summary>
    public event EventHandler? Cancelled;

    public IReadOnlyList<OtpType> OtpTypes { get; } = Enum.GetValues<OtpType>();
    public IReadOnlyList<HashAlgorithmType> Algorithms { get; } = Enum.GetValues<HashAlgorithmType>();
    public IReadOnlyList<int> DigitOptions { get; } = new[] { 6, 8 };
    public IReadOnlyList<int> PeriodOptions { get; } = new[] { 15, 30, 60 };

    public AccountEditViewModel(
        IAccountRepository accountRepository,
        IOtpService otpService)
    {
        _accountRepository = accountRepository;
        _otpService = otpService;
    }

    /// <summary>
    /// 새 계정 추가 모드로 초기화
    /// </summary>
    public void InitializeForAdd()
    {
        IsEditMode = false;
        _originalAccount = null;

        Issuer = string.Empty;
        AccountName = string.Empty;
        SecretKey = string.Empty;
        SelectedType = OtpType.Totp;
        SelectedAlgorithm = HashAlgorithmType.Sha1;
        Digits = 6;
        Period = 30;
        Notes = null;

        Validate();
    }

    /// <summary>
    /// 기존 계정 편집 모드로 초기화
    /// </summary>
    public void InitializeForEdit(OtpAccount account)
    {
        IsEditMode = true;
        _originalAccount = account;

        Issuer = account.Issuer;
        AccountName = account.AccountName;
        SecretKey = account.SecretKey;
        SelectedType = account.Type;
        SelectedAlgorithm = account.Algorithm;
        Digits = account.Digits;
        Period = account.Period;
        Notes = account.Notes;

        Validate();
    }

    /// <summary>
    /// otpauth:// URI로 초기화
    /// </summary>
    public bool InitializeFromUri(string uri)
    {
        var account = _otpService.ParseOtpAuthUri(uri);
        if (account == null)
        {
            ValidationError = "Invalid OTP URI format";
            return false;
        }

        IsEditMode = false;
        _originalAccount = null;

        Issuer = account.Issuer;
        AccountName = account.AccountName;
        SecretKey = account.SecretKey;
        SelectedType = account.Type;
        SelectedAlgorithm = account.Algorithm;
        Digits = account.Digits;
        Period = account.Period;

        Validate();
        return true;
    }

    /// <summary>
    /// 유효성 검증
    /// </summary>
    private void Validate()
    {
        ValidationError = null;

        if (string.IsNullOrWhiteSpace(AccountName))
        {
            ValidationError = "Account name is required";
            IsValid = false;
            return;
        }

        if (string.IsNullOrWhiteSpace(SecretKey))
        {
            ValidationError = "Secret key is required";
            IsValid = false;
            return;
        }

        if (!_otpService.ValidateSecretKey(SecretKey))
        {
            ValidationError = "Invalid secret key format (must be Base32)";
            IsValid = false;
            return;
        }

        IsValid = true;
    }

    partial void OnAccountNameChanged(string value) => Validate();
    partial void OnSecretKeyChanged(string value) => Validate();

    /// <summary>
    /// 저장
    /// </summary>
    [RelayCommand(CanExecute = nameof(IsValid))]
    private async Task SaveAsync()
    {
        if (!IsValid) return;

        await ExecuteAsync(async () =>
        {
            OtpAccount account;

            if (IsEditMode && _originalAccount != null)
            {
                // 기존 계정 업데이트
                account = _originalAccount;
                account.Issuer = Issuer;
                account.AccountName = AccountName;
                account.SecretKey = SecretKey;
                account.Type = SelectedType;
                account.Algorithm = SelectedAlgorithm;
                account.Digits = Digits;
                account.Period = Period;
                account.Notes = Notes;

                await _accountRepository.UpdateAsync(account);
            }
            else
            {
                // 새 계정 추가
                account = new OtpAccount
                {
                    Issuer = Issuer,
                    AccountName = AccountName,
                    SecretKey = SecretKey,
                    Type = SelectedType,
                    Algorithm = SelectedAlgorithm,
                    Digits = Digits,
                    Period = Period,
                    Notes = Notes
                };

                account = await _accountRepository.AddAsync(account);
            }

            Saved?.Invoke(this, account);
        });
    }

    /// <summary>
    /// 취소
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 랜덤 비밀 키 생성
    /// </summary>
    [RelayCommand]
    private void GenerateRandomKey()
    {
        SecretKey = _otpService.GenerateSecretKey();
    }
}
