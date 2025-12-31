namespace OtpAuthenticator.macOS;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(Views.AccountEditPage), typeof(Views.AccountEditPage));
    }
}
