using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OtpAuthenticator.Core.Models;
using OtpAuthenticator.Core.Services.Interfaces;

namespace OtpAuthenticator.macOS.ViewModels;

public partial class SettingsViewModel : BaseViewModel
{
    private readonly ISettingsService _settingsService;
    private readonly IBackupService _backupService;

    [ObservableProperty]
    private bool _copyOnClick = true;

    [ObservableProperty]
    private int _autoClearSeconds = 30;

    [ObservableProperty]
    private bool _showInMenuBar = true;

    [ObservableProperty]
    private string _selectedLanguage = "en-US";

    [ObservableProperty]
    private string? _statusMessage;

    public List<int> AutoClearOptions => new() { 0, 15, 30, 60, 120 };
    public List<string> LanguageOptions => new() { "en-US", "ko-KR" };

    public SettingsViewModel(ISettingsService settingsService, IBackupService backupService)
    {
        _settingsService = settingsService;
        _backupService = backupService;

        Title = "Settings";
        _ = LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        var settings = await _settingsService.GetSettingsAsync();
        if (settings != null)
        {
            CopyOnClick = settings.CopyOnClick;
            AutoClearSeconds = settings.AutoClearSeconds;
            ShowInMenuBar = settings.ShowInMenuBar;
            SelectedLanguage = settings.Language;
        }
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        var settings = new AppSettings
        {
            CopyOnClick = CopyOnClick,
            AutoClearSeconds = AutoClearSeconds,
            ShowInMenuBar = ShowInMenuBar,
            Language = SelectedLanguage
        };

        await _settingsService.SaveSettingsAsync(settings);
        StatusMessage = "Settings saved";
    }

    [RelayCommand]
    private async Task ExportBackupAsync()
    {
        try
        {
            IsBusy = true;

            var password = await Shell.Current.DisplayPromptAsync(
                "Backup Password",
                "Enter a password to protect your backup:",
                "OK",
                "Cancel",
                placeholder: "Password",
                maxLength: 100);

            if (string.IsNullOrEmpty(password))
                return;

            var backupData = await _backupService.CreateBackupAsync(password);

            // Save to file using file picker
            // For now, save to Documents folder
            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var fileName = $"otp_backup_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var filePath = Path.Combine(documentsPath, fileName);

            await File.WriteAllTextAsync(filePath, backupData);

            StatusMessage = $"Backup saved to {fileName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Backup failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ImportBackupAsync()
    {
        try
        {
            IsBusy = true;

            // TODO: Implement file picker for macOS
            await Shell.Current.DisplayAlert("Import", "File picker will be implemented.", "OK");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
