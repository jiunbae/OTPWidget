using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using OtpAuthenticator.App.ViewModels;

namespace OtpAuthenticator.App.Views;

/// <summary>
/// 설정 페이지
/// </summary>
public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel { get; }

    public SettingsPage()
    {
        this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<SettingsViewModel>();

        // 이벤트 연결
        ResetButton.Click += async (s, e) => await ViewModel.ResetSettingsCommand.ExecuteAsync(null);
        CloudSyncToggle.Toggled += OnCloudSyncToggled;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ViewModel.LoadSettingsAsync();
        UpdateUI();
    }

    private void UpdateUI()
    {
        StartWithWindowsToggle.IsOn = ViewModel.StartWithWindows;
        StartMinimizedToggle.IsOn = ViewModel.StartMinimized;
        MinimizeToTrayToggle.IsOn = ViewModel.MinimizeToTray;
        AutoCopyToggle.IsOn = ViewModel.AutoCopyToClipboard;
        EnableWidgetToggle.IsOn = ViewModel.EnableWidgetProvider;
        RequireAuthToggle.IsOn = ViewModel.RequireAuthentication;
        CloudSyncToggle.IsOn = ViewModel.CloudSyncEnabled;

        // ComboBox 선택
        SelectComboBoxItem(ClipboardClearCombo, ViewModel.ClipboardClearSeconds.ToString());
        SelectComboBoxItem(ThemeCombo, ViewModel.SelectedTheme);
        SelectComboBoxItem(CloudProviderCombo, ViewModel.SelectedCloudProvider.ToString());

        // Cloud sync 관련 컨트롤 활성화/비활성화
        CloudProviderCombo.IsEnabled = ViewModel.CloudSyncEnabled;
        SyncNowButton.IsEnabled = ViewModel.CloudSyncEnabled;
    }

    private void SelectComboBoxItem(ComboBox comboBox, string tag)
    {
        foreach (ComboBoxItem item in comboBox.Items)
        {
            if (item.Tag?.ToString() == tag)
            {
                comboBox.SelectedItem = item;
                return;
            }
        }
    }

    private void OnCloudSyncToggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        CloudProviderCombo.IsEnabled = CloudSyncToggle.IsOn;
        SyncNowButton.IsEnabled = CloudSyncToggle.IsOn;
    }
}
