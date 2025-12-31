using OtpAuthenticator.macOS.ViewModels;

namespace OtpAuthenticator.macOS.Views;

public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
