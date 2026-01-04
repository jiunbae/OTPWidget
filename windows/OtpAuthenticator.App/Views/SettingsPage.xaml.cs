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
        ThemeCombo.SelectionChanged += OnThemeChanged;
        StartWithWindowsToggle.Toggled += (s, e) => ViewModel.StartWithWindows = StartWithWindowsToggle.IsOn;
        StartMinimizedToggle.Toggled += (s, e) => ViewModel.StartMinimized = StartMinimizedToggle.IsOn;
        MinimizeToTrayToggle.Toggled += (s, e) => ViewModel.MinimizeToTray = MinimizeToTrayToggle.IsOn;
        EnableWidgetToggle.Toggled += (s, e) => ViewModel.EnableWidgetProvider = EnableWidgetToggle.IsOn;
        RequireAuthToggle.Toggled += (s, e) => ViewModel.RequireAuthentication = RequireAuthToggle.IsOn;
        ClipboardClearCombo.SelectionChanged += OnClipboardClearChanged;
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
        EnableWidgetToggle.IsOn = ViewModel.EnableWidgetProvider;
        RequireAuthToggle.IsOn = ViewModel.RequireAuthentication;

        // ComboBox 선택
        SelectComboBoxItem(ClipboardClearCombo, ViewModel.ClipboardClearSeconds.ToString());
        SelectComboBoxItem(ThemeCombo, ViewModel.SelectedTheme);
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

    private void OnThemeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeCombo.SelectedItem is ComboBoxItem item && item.Tag is string theme)
        {
            ViewModel.SelectedTheme = theme;
        }
    }

    private void OnClipboardClearChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ClipboardClearCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            if (int.TryParse(tag, out int seconds))
            {
                ViewModel.ClipboardClearSeconds = seconds;
            }
        }
    }
}
