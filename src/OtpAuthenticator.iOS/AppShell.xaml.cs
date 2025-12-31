namespace OtpAuthenticator.iOS;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(Views.AccountEditPage), typeof(Views.AccountEditPage));
        Routing.RegisterRoute(nameof(Views.QrScannerPage), typeof(Views.QrScannerPage));
    }
}
