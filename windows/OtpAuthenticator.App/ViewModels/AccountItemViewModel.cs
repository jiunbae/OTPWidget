using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using OtpAuthenticator.Core.Models;
using OtpAuthenticator.Core.Services.Interfaces;

namespace OtpAuthenticator.App.ViewModels;

/// <summary>
/// 계정 아이템 ViewModel (OTP 코드 표시용)
/// </summary>
public partial class AccountItemViewModel : ObservableObject, IDisposable
{
    private readonly IOtpService _otpService;
    private readonly DispatcherQueueTimer _timer;
    private readonly DispatcherQueue _dispatcherQueue;
    private bool _disposed;

    public OtpAccount Account { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedCode))]
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

        // UI 스레드의 DispatcherQueue 가져오기
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        // DispatcherQueueTimer 사용 (UI 스레드에서 실행됨)
        _timer = _dispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += OnTimerTick;

        // 초기 코드 생성
        UpdateCode();

        // TOTP인 경우 타이머 시작
        if (account.Type == OtpType.Totp)
        {
            _timer.Start();
        }
    }

    private void OnTimerTick(DispatcherQueueTimer sender, object args)
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
    public async void ShowCopiedIndicator()
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
        _timer.Tick -= OnTimerTick;
    }
}
