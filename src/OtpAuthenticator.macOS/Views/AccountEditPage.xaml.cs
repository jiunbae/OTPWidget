using OtpAuthenticator.macOS.ViewModels;

namespace OtpAuthenticator.macOS.Views;

public partial class AccountEditPage : ContentPage
{
    public AccountEditPage(AccountEditViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
