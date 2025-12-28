using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OtpAuthenticator.Core.Models;
using OtpAuthenticator.Core.Services.Interfaces;

namespace OtpAuthenticator.App.ViewModels;

/// <summary>
/// 계정 아이템 ViewModel (OTP 코드 표시용)
/// </summary>
public partial class AccountItemViewModel : ObservableObject, IDisposable
{
    private readonly IOtpService _otpService;
    private readonly System.Timers.Timer _timer;
    private bool _disposed;

    public OtpAccount Account { get; }

    [ObservableProperty]
    private string _currentCode = "------";

    [ObservableProperty]
    private int _remainingSeconds;

    [ObservableProperty]
    private double _progress = 1.0;

    [ObservableProperty]
    private bool _isCopied;

    public string Issuer => Account.Issuer;
    public string AccountName => Account.AccountName;
    public string Initial => Account.Initial;
    public bool IsFavorite => Account.IsFavorite;
    public string? Color => Account.Color;

    /// <summary>
    /// 복사 요청 이벤트
    /// </summary>
    public event EventHandler<string>? CopyRequested;

    public AccountItemViewModel(OtpAccount account, IOtpService otpService)
    {
        Account = account;
        _otpService = otpService;

        // 타이머 설정 (1초마다 업데이트)
        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += OnTimerElapsed;

        // 초기 코드 생성
        UpdateCode();

        // TOTP인 경우 타이머 시작
        if (account.Type == OtpType.Totp)
        {
            _timer.Start();
        }
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        UpdateCode();
    }

    private void UpdateCode()
    {
        try
        {
            CurrentCode = _otpService.GenerateCode(Account);
            RemainingSeconds = _otpService.GetRemainingSeconds(Account.Period);
            Progress = (double)RemainingSeconds / Account.Period;
        }
        catch
        {
            CurrentCode = "Error";
        }
    }

    /// <summary>
    /// 코드 복사
    /// </summary>
    [RelayCommand]
    private void CopyCode()
    {
        CopyRequested?.Invoke(this, CurrentCode);
        ShowCopiedIndicator();
    }

    /// <summary>
    /// HOTP 카운터 증가 및 새 코드 생성
    /// </summary>
    [RelayCommand]
    private void GenerateNextCode()
    {
        if (Account.Type == OtpType.Hotp)
        {
            _otpService.IncrementCounter(Account);
            UpdateCode();
        }
    }

    /// <summary>
    /// 복사 표시 (잠깐 표시 후 숨김)
    /// </summary>
    private async void ShowCopiedIndicator()
    {
        IsCopied = true;
        await Task.Delay(2000);
        IsCopied = false;
    }

    /// <summary>
    /// 코드 포맷팅 (3자리씩 분리)
    /// </summary>
    public string FormattedCode
    {
        get
        {
            if (CurrentCode.Length == 6)
                return $"{CurrentCode[..3]} {CurrentCode[3..]}";
            if (CurrentCode.Length == 8)
                return $"{CurrentCode[..4]} {CurrentCode[4..]}";
            return CurrentCode;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _timer.Stop();
        _timer.Elapsed -= OnTimerElapsed;
        _timer.Dispose();
    }
}
