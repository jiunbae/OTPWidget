using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OtpAuthenticator.App.ViewModels;
using OtpAuthenticator.Core.Models;
using OtpAuthenticator.Core.Services.Interfaces;

namespace OtpAuthenticator.App.Views;

/// <summary>
/// 메인 윈도우
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }
    private readonly IAccountRepository _accountRepository;
    private int _folderInsertIndex = -1;

    public MainWindow()
    {
        this.InitializeComponent();

        ViewModel = App.Services.GetRequiredService<MainViewModel>();
        _accountRepository = App.Services.GetRequiredService<IAccountRepository>();

        // 네비게이션 설정
        NavigationViewControl.SelectionChanged += OnNavigationSelectionChanged;
        AddFolderNavItem.Tapped += OnAddFolderTapped;

        // 초기 페이지 로드
        ContentFrame.Navigate(typeof(AccountListPage));

        // ViewModel 초기화
        _ = ViewModel.InitializeAsync();

        // 폴더 로드
        _ = LoadFoldersAsync();

        // 편집 패널 바인딩
        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.IsAccountEditVisible))
            {
                EditOverlay.Visibility = ViewModel.IsAccountEditVisible
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        };

        // 창 설정
        SetupWindow();
    }

    private async Task LoadFoldersAsync()
    {
        var folders = await _accountRepository.GetAllFoldersAsync();

        // Find the index after "Folders" header
        for (int i = 0; i < NavigationViewControl.MenuItems.Count; i++)
        {
            if (NavigationViewControl.MenuItems[i] is NavigationViewItemHeader header &&
                header.Content?.ToString() == "Folders")
            {
                _folderInsertIndex = i + 1;
                break;
            }
        }

        // Add folder items
        foreach (var folder in folders)
        {
            AddFolderNavigationItem(folder);
        }
    }

    private void AddFolderNavigationItem(OtpFolder folder)
    {
        var navItem = new NavigationViewItem
        {
            Content = folder.Name,
            Tag = $"folder:{folder.Id}",
            Icon = new FontIcon { Glyph = folder.Icon }
        };

        // Parse color and apply
        if (!string.IsNullOrEmpty(folder.Color))
        {
            try
            {
                var color = ParseHexColor(folder.Color);
                ((FontIcon)navItem.Icon).Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(color);
            }
            catch { }
        }

        if (_folderInsertIndex >= 0)
        {
            NavigationViewControl.MenuItems.Insert(_folderInsertIndex, navItem);
            _folderInsertIndex++;
        }
    }

    private static Windows.UI.Color ParseHexColor(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
        {
            return Windows.UI.Color.FromArgb(255,
                byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber),
                byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber));
        }
        return Windows.UI.Color.FromArgb(255, 0, 120, 212); // Default blue
    }

    private async void OnAddFolderTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "New Folder",
            PrimaryButtonText = "Create",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.Content.XamlRoot
        };

        var input = new TextBox
        {
            PlaceholderText = "Folder name",
            Margin = new Thickness(0, 16, 0, 0)
        };
        dialog.Content = input;

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(input.Text))
        {
            var folder = new OtpFolder
            {
                Name = input.Text,
                Icon = "\uE8B7", // Folder icon
                Color = "#0078D4"
            };

            await _accountRepository.AddFolderAsync(folder);
            AddFolderNavigationItem(folder);
        }
    }

    private void SetupWindow()
    {
        // 창 크기 설정
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

        appWindow.Resize(new Windows.Graphics.SizeInt32(900, 650));
        appWindow.Title = "OTP Authenticator";

        // 아이콘 설정 (나중에)
        // appWindow.SetIcon("Assets/Logo/app.ico");
    }

    private void OnNavigationSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
            return;
        }

        var selectedItem = args.SelectedItem as NavigationViewItem;
        if (selectedItem == null) return;

        var tag = selectedItem.Tag?.ToString();

        if (string.IsNullOrEmpty(tag))
            return;

        // Handle different navigation types
        if (tag.StartsWith("folder:"))
        {
            // Navigate to folder view
            var folderId = Guid.Parse(tag.Substring(7));
            ContentFrame.Navigate(typeof(AccountListPage), new AccountListNavigationArgs { FolderId = folderId });
        }
        else
        {
            var pageType = tag switch
            {
                "accounts" => typeof(AccountListPage),
                "favorites" => typeof(AccountListPage),
                "uncategorized" => typeof(AccountListPage),
                "backup" => typeof(BackupPage),
                _ => typeof(AccountListPage)
            };

            object? parameter = tag switch
            {
                "favorites" => new AccountListNavigationArgs { ShowFavoritesOnly = true },
                "uncategorized" => new AccountListNavigationArgs { ShowUncategorizedOnly = true },
                _ => null
            };

            ContentFrame.Navigate(pageType, parameter);
        }
    }

    /// <summary>
    /// 창 닫기 처리
    /// </summary>
    private void OnWindowClosing(object sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
    {
        // 설정에 따라 트레이로 최소화
        var settingsService = App.Services.GetRequiredService<Core.Services.Interfaces.ISettingsService>();
        if (settingsService.Settings.MinimizeToTray)
        {
            args.Cancel = true;
            this.Hide();
        }
    }

    public void Show()
    {
        this.Activate();
    }

    public void Hide()
    {
        // WinUI 3에서는 직접 숨기기 어려움, 최소화로 대체
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        PInvoke.User32.ShowWindow(hwnd, PInvoke.User32.ShowWindowCommand.SW_HIDE);
    }
}

/// <summary>
/// AccountListPage 네비게이션 인자
/// </summary>
public class AccountListNavigationArgs
{
    public Guid? FolderId { get; set; }
    public bool ShowFavoritesOnly { get; set; }
    public bool ShowUncategorizedOnly { get; set; }
}

/// <summary>
/// P/Invoke 헬퍼
/// </summary>
internal static partial class PInvoke
{
    internal static class User32
    {
        public enum ShowWindowCommand
        {
            SW_HIDE = 0,
            SW_SHOW = 5,
            SW_MINIMIZE = 6,
            SW_RESTORE = 9
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, ShowWindowCommand nCmdShow);
    }
}
