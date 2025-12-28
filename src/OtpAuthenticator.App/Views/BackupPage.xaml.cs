using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OtpAuthenticator.Core.Services.Interfaces;
using Windows.Storage.Pickers;

namespace OtpAuthenticator.App.Views;

/// <summary>
/// 백업/복원 페이지
/// </summary>
public sealed partial class BackupPage : Page
{
    private readonly IBackupService _backupService;

    public BackupPage()
    {
        this.InitializeComponent();
        _backupService = App.Services.GetRequiredService<IBackupService>();

        ExportButton.Click += OnExportClick;
        ImportButton.Click += OnImportClick;
        ExportQrButton.Click += OnExportQrClick;
    }

    private async void OnExportClick(object sender, RoutedEventArgs e)
    {
        // 비밀번호 입력 다이얼로그
        var password = await ShowPasswordDialogAsync("Create Backup Password",
            "Enter a password to encrypt your backup:");

        if (string.IsNullOrEmpty(password))
            return;

        // 파일 저장 위치 선택
        var savePicker = new FileSavePicker();
        savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        savePicker.FileTypeChoices.Add("OTP Backup", new[] { ".otpbackup" });
        savePicker.SuggestedFileName = $"otp_backup_{DateTime.Now:yyyyMMdd}";

        // WinUI 3에서 picker 초기화
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Current.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

        var file = await savePicker.PickSaveFileAsync();
        if (file == null) return;

        try
        {
            await _backupService.ExportAsync(file.Path, password, IncludeSettingsToggle.IsOn);
            await ShowMessageAsync("Success", "Backup exported successfully.");
        }
        catch (Exception ex)
        {
            await ShowMessageAsync("Error", $"Failed to export backup: {ex.Message}");
        }
    }

    private async void OnImportClick(object sender, RoutedEventArgs e)
    {
        // 파일 선택
        var openPicker = new FileOpenPicker();
        openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        openPicker.FileTypeFilter.Add(".otpbackup");

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Current.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hwnd);

        var file = await openPicker.PickSingleFileAsync();
        if (file == null) return;

        // 비밀번호 입력
        var password = await ShowPasswordDialogAsync("Enter Backup Password",
            "Enter the password used to encrypt this backup:");

        if (string.IsNullOrEmpty(password))
            return;

        try
        {
            int count = await _backupService.ImportAsync(file.Path, password, RestoreSettingsToggle.IsOn);
            ImportInfoBar.Severity = InfoBarSeverity.Success;
            ImportInfoBar.Title = "Import Successful";
            ImportInfoBar.Message = $"{count} account(s) imported successfully.";
            ImportInfoBar.IsOpen = true;
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            ImportInfoBar.Severity = InfoBarSeverity.Error;
            ImportInfoBar.Title = "Invalid Password";
            ImportInfoBar.Message = "The password is incorrect or the backup file is corrupted.";
            ImportInfoBar.IsOpen = true;
        }
        catch (Exception ex)
        {
            ImportInfoBar.Severity = InfoBarSeverity.Error;
            ImportInfoBar.Title = "Import Failed";
            ImportInfoBar.Message = ex.Message;
            ImportInfoBar.IsOpen = true;
        }
    }

    private async void OnExportQrClick(object sender, RoutedEventArgs e)
    {
        // QR 코드 내보내기 (나중에 구현)
        await ShowMessageAsync("Coming Soon", "QR code export will be available in a future update.");
    }

    private async Task<string?> ShowPasswordDialogAsync(string title, string message)
    {
        var passwordBox = new PasswordBox { PlaceholderText = "Password" };

        var dialog = new ContentDialog
        {
            Title = title,
            Content = new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                    passwordBox
                }
            },
            PrimaryButtonText = "OK",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
            return passwordBox.Password;

        return null;
    }

    private async Task ShowMessageAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };

        await dialog.ShowAsync();
    }
}

/// <summary>
/// App.Current.MainWindow 확장
/// </summary>
public static class AppExtensions
{
    public static Window MainWindow { get; set; } = null!;
}
