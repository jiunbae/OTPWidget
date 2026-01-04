using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OtpAuthenticator.Core.Services.Interfaces;

namespace OtpAuthenticator.App.ViewModels;

/// <summary>
/// 시스템 트레이 팝업 ViewModel
/// </summary>
public partial class TrayPopupViewModel : BaseViewModel
{
    private readonly IAccountRepository _accountRepository;
    private readonly IOtpService _otpService;
    private readonly IClipboardService _clipboardService;
    private readonly ISettingsService _settingsService;

    public ObservableCollection<AccountItemViewModel> Accounts { get; } = new();

    [ObservableProperty]
    private bool _isEmpty = true;

    /// <summary>
    /// 메인 창 열기 요청
    /// </summary>
    public event EventHandler? OpenMainWindowRequested;

    /// <summary>
    /// 설정 열기 요청
    /// </summary>
    public event EventHandler? OpenSettingsRequested;

    /// <summary>
    /// QR 스캔 요청
    /// </summary>
    public event EventHandler? ScanQrRequested;

    /// <summary>
    /// 팝업 닫기 요청
    /// </summary>
    public event EventHandler? CloseRequested;

    public TrayPopupViewModel(
        IAccountRepository accountRepository,
        IOtpService otpService,
        IClipboardService clipboardService,
        ISettingsService settingsService)
    {
        _accountRepository = accountRepository;
        _otpService = otpService;
        _clipboardService = clipboardService;
        _settingsService = settingsService;
    }

    /// <summary>
    /// 계정 로드 (즐겨찾기 우선)
    /// </summary>
    [RelayCommand]
    public async Task LoadAccountsAsync()
    {
        await ExecuteAsync(async () =>
        {
            // 기존 정리
            foreach (var vm in Accounts)
            {
                vm.CopyRequested -= OnCopyRequested;
                vm.Dispose();
            }
            Accounts.Clear();

            // 즐겨찾기 먼저, 그 다음 최근 사용 순
            var allAccounts = await _accountRepository.GetAllAsync();
            var sortedAccounts = allAccounts
                .OrderByDescending(a => a.IsFavorite)
                .ThenByDescending(a => a.LastUsedAt)
                .Take(10); // 팝업에는 최대 10개만 표시

            foreach (var account in sortedAccounts)
            {
                var vm = new AccountItemViewModel(account, _otpService);
                vm.CopyRequested += OnCopyRequested;
                Accounts.Add(vm);
            }

            IsEmpty = Accounts.Count == 0;
        });
    }

    /// <summary>
    /// 코드 복사 및 팝업 닫기
    /// </summary>
    [RelayCommand]
    private async Task CopyAndCloseAsync(AccountItemViewModel? vm)
    {
        if (vm == null) return;

        var settings = _settingsService.Settings;
        await _clipboardService.CopyAsync(vm.CurrentCode, settings.ClipboardClearSeconds);

        // 마지막 사용 시간 업데이트
        vm.Account.LastUsedAt = DateTime.UtcNow;
        await _accountRepository.UpdateAsync(vm.Account);

        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 메인 창 열기
    /// </summary>
    [RelayCommand]
    private void OpenMainWindow()
    {
        OpenMainWindowRequested?.Invoke(this, EventArgs.Empty);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 설정 열기
    /// </summary>
    [RelayCommand]
    private void OpenSettings()
    {
        OpenSettingsRequested?.Invoke(this, EventArgs.Empty);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// QR 스캔
    /// </summary>
    [RelayCommand]
    private void ScanQr()
    {
        ScanQrRequested?.Invoke(this, EventArgs.Empty);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private async void OnCopyRequested(object? sender, string code)
    {
        var settings = _settingsService.Settings;
        await _clipboardService.CopyAsync(code, settings.ClipboardClearSeconds);

        if (sender is AccountItemViewModel vm)
        {
            vm.Account.LastUsedAt = DateTime.UtcNow;
            await _accountRepository.UpdateAsync(vm.Account);
        }
    }

    /// <summary>
    /// 리소스 정리
    /// </summary>
    public void Cleanup()
    {
        foreach (var vm in Accounts)
        {
            vm.CopyRequested -= OnCopyRequested;
            vm.Dispose();
        }
        Accounts.Clear();
    }
}
