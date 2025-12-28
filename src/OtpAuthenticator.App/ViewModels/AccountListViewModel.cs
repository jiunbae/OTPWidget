using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OtpAuthenticator.Core.Models;
using OtpAuthenticator.Core.Services.Interfaces;

namespace OtpAuthenticator.App.ViewModels;

/// <summary>
/// 계정 목록 ViewModel
/// </summary>
public partial class AccountListViewModel : BaseViewModel
{
    private readonly IAccountRepository _accountRepository;
    private readonly IOtpService _otpService;
    private readonly IClipboardService _clipboardService;
    private readonly ISettingsService _settingsService;

    public ObservableCollection<AccountItemViewModel> Accounts { get; } = new();

    [ObservableProperty]
    private AccountItemViewModel? _selectedAccount;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isEmpty = true;

    /// <summary>
    /// 계정 편집 요청 이벤트
    /// </summary>
    public event EventHandler<OtpAccount>? EditRequested;

    /// <summary>
    /// 계정 추가 요청 이벤트
    /// </summary>
    public event EventHandler? AddRequested;

    public AccountListViewModel(
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
    /// 계정 목록 로드
    /// </summary>
    [RelayCommand]
    public async Task LoadAccountsAsync()
    {
        await ExecuteAsync(async () =>
        {
            // 기존 ViewModel 정리
            foreach (var vm in Accounts)
            {
                vm.CopyRequested -= OnCopyRequested;
                vm.Dispose();
            }
            Accounts.Clear();

            // 계정 로드
            var accounts = await _accountRepository.GetAllAsync();

            foreach (var account in accounts)
            {
                var vm = new AccountItemViewModel(account, _otpService);
                vm.CopyRequested += OnCopyRequested;
                Accounts.Add(vm);
            }

            IsEmpty = Accounts.Count == 0;
        });
    }

    /// <summary>
    /// 계정 추가
    /// </summary>
    [RelayCommand]
    private void AddAccount()
    {
        AddRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 계정 편집
    /// </summary>
    [RelayCommand]
    private void EditAccount(AccountItemViewModel? vm)
    {
        if (vm != null)
        {
            EditRequested?.Invoke(this, vm.Account);
        }
    }

    /// <summary>
    /// 계정 삭제
    /// </summary>
    [RelayCommand]
    private async Task DeleteAccountAsync(AccountItemViewModel? vm)
    {
        if (vm == null) return;

        await ExecuteAsync(async () =>
        {
            await _accountRepository.DeleteAsync(vm.Account.Id);

            vm.CopyRequested -= OnCopyRequested;
            vm.Dispose();
            Accounts.Remove(vm);

            IsEmpty = Accounts.Count == 0;
        });
    }

    /// <summary>
    /// 즐겨찾기 토글
    /// </summary>
    [RelayCommand]
    private async Task ToggleFavoriteAsync(AccountItemViewModel? vm)
    {
        if (vm == null) return;

        await ExecuteAsync(async () =>
        {
            vm.Account.IsFavorite = !vm.Account.IsFavorite;
            await _accountRepository.UpdateAsync(vm.Account);
        });
    }

    /// <summary>
    /// 검색
    /// </summary>
    partial void OnSearchQueryChanged(string value)
    {
        // TODO: 필터링 구현
    }

    /// <summary>
    /// 코드 복사 처리
    /// </summary>
    private async void OnCopyRequested(object? sender, string code)
    {
        var settings = _settingsService.Settings;
        await _clipboardService.CopyAsync(code, settings.ClipboardClearSeconds);
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
