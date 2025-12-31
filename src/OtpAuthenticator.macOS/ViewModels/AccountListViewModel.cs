using System.Collections.ObjectModel;
using System.Timers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OtpAuthenticator.Core.Models;
using OtpAuthenticator.Core.Services.Interfaces;

namespace OtpAuthenticator.macOS.ViewModels;

public partial class AccountListViewModel : BaseViewModel
{
    private readonly IAccountRepository _accountRepository;
    private readonly IOtpService _otpService;
    private readonly IClipboardService _clipboardService;
    private readonly System.Timers.Timer _refreshTimer;

    [ObservableProperty]
    private ObservableCollection<AccountItemViewModel> _accounts = new();

    [ObservableProperty]
    private AccountItemViewModel? _selectedAccount;

    [ObservableProperty]
    private int _remainingSeconds;

    [ObservableProperty]
    private double _progress;

    public AccountListViewModel(
        IAccountRepository accountRepository,
        IOtpService otpService,
        IClipboardService clipboardService)
    {
        _accountRepository = accountRepository;
        _otpService = otpService;
        _clipboardService = clipboardService;

        Title = "Accounts";

        _refreshTimer = new System.Timers.Timer(1000);
        _refreshTimer.Elapsed += OnTimerElapsed;
        _refreshTimer.Start();
    }

    [RelayCommand]
    private async Task LoadAccountsAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            var accounts = await _accountRepository.GetAllAsync();

            Accounts.Clear();
            foreach (var account in accounts.OrderBy(a => a.SortOrder))
            {
                var vm = new AccountItemViewModel(account, _otpService);
                Accounts.Add(vm);
            }

            UpdateCodes();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddAccountAsync()
    {
        await Shell.Current.GoToAsync(nameof(Views.AccountEditPage));
    }

    [RelayCommand]
    private async Task EditAccountAsync(AccountItemViewModel? account)
    {
        if (account == null) return;

        await Shell.Current.GoToAsync($"{nameof(Views.AccountEditPage)}?id={account.Id}");
    }

    [RelayCommand]
    private async Task DeleteAccountAsync(AccountItemViewModel? account)
    {
        if (account == null) return;

        var confirm = await Shell.Current.DisplayAlert(
            "Delete Account",
            $"Are you sure you want to delete '{account.DisplayName}'?",
            "Delete",
            "Cancel");

        if (confirm)
        {
            await _accountRepository.DeleteAsync(account.Id);
            Accounts.Remove(account);
        }
    }

    [RelayCommand]
    private async Task CopyCodeAsync(AccountItemViewModel? account)
    {
        if (account == null) return;

        await _clipboardService.CopyAsync(account.CurrentCode, 30);

        // Update last used time
        var otpAccount = await _accountRepository.GetByIdAsync(account.Id);
        if (otpAccount != null)
        {
            otpAccount.LastUsedAt = DateTime.UtcNow;
            await _accountRepository.UpdateAsync(otpAccount);
        }
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateCodes();
        });
    }

    private void UpdateCodes()
    {
        RemainingSeconds = _otpService.GetRemainingSeconds();
        Progress = RemainingSeconds / 30.0;

        foreach (var account in Accounts)
        {
            account.RefreshCode();
        }
    }

    public void OnAppearing()
    {
        _refreshTimer.Start();
        _ = LoadAccountsAsync();
    }

    public void OnDisappearing()
    {
        _refreshTimer.Stop();
    }
}

public partial class AccountItemViewModel : ObservableObject
{
    private readonly OtpAccount _account;
    private readonly IOtpService _otpService;

    public string Id => _account.Id;
    public string Issuer => _account.Issuer;
    public string AccountName => _account.AccountName;
    public string DisplayName => _account.DisplayName;
    public string Initial => _account.Initial;
    public string Color => _account.Color;

    [ObservableProperty]
    private string _currentCode = string.Empty;

    public AccountItemViewModel(OtpAccount account, IOtpService otpService)
    {
        _account = account;
        _otpService = otpService;
        RefreshCode();
    }

    public void RefreshCode()
    {
        try
        {
            CurrentCode = _otpService.GenerateCode(_account);
        }
        catch
        {
            CurrentCode = "------";
        }
    }
}
