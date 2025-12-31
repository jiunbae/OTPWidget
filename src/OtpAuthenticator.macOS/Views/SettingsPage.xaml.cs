using OtpAuthenticator.macOS.ViewModels;

namespace OtpAuthenticator.macOS.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
