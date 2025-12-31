using OtpAuthenticator.macOS.ViewModels;

namespace OtpAuthenticator.macOS.Views;

public partial class AccountListPage : ContentPage
{
    private readonly AccountListViewModel _viewModel;

    public AccountListPage(AccountListViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.OnAppearing();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.OnDisappearing();
    }
}
