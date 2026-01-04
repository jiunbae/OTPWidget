using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OtpAuthenticator.Core.Models;
using OtpAuthenticator.Core.Services.Interfaces;

namespace OtpAuthenticator.App.Views;

/// <summary>
/// Manual account entry dialog
/// </summary>
public sealed partial class ManualAddDialog : ContentDialog
{
    private readonly IAccountRepository _accountRepository;

    /// <summary>
    /// Added account (available after dialog closes)
    /// </summary>
    public OtpAccount? AddedAccount { get; private set; }

    public ManualAddDialog()
    {
        this.InitializeComponent();

        _accountRepository = App.Services.GetRequiredService<IAccountRepository>();

        // Event handlers
        IssuerTextBox.TextChanged += OnTextChanged;
        SecretKeyTextBox.TextChanged += OnTextChanged;
        OtpTypeCombo.SelectionChanged += OnOtpTypeChanged;

        this.PrimaryButtonClick += OnPrimaryButtonClick;
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        ValidateForm();
    }

    private void ValidateForm()
    {
        bool hasIssuer = !string.IsNullOrWhiteSpace(IssuerTextBox.Text);
        bool hasSecret = !string.IsNullOrWhiteSpace(SecretKeyTextBox.Text);
        IsPrimaryButtonEnabled = hasIssuer && hasSecret;
    }

    private void OnOtpTypeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (OtpTypeCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            bool isTotp = tag == "totp";
            PeriodNumberBox.Header = isTotp ? "Period (seconds)" : "Counter";
            PeriodNumberBox.Value = isTotp ? 30 : 0;
            PeriodNumberBox.Minimum = isTotp ? 10 : 0;
            PeriodNumberBox.Maximum = isTotp ? 120 : 999999;
        }
    }

    private async void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();

        try
        {
            // Validate
            if (string.IsNullOrWhiteSpace(SecretKeyTextBox.Text))
            {
                StatusInfoBar.Severity = InfoBarSeverity.Error;
                StatusInfoBar.Message = "Secret Key is required.";
                args.Cancel = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(IssuerTextBox.Text))
            {
                StatusInfoBar.Severity = InfoBarSeverity.Error;
                StatusInfoBar.Message = "Service Name is required.";
                args.Cancel = true;
                return;
            }

            // OTP Type
            var otpType = OtpType.Totp;
            if (OtpTypeCombo.SelectedItem is ComboBoxItem typeItem && typeItem.Tag is string typeTag)
            {
                otpType = typeTag == "hotp" ? OtpType.Hotp : OtpType.Totp;
            }

            // Algorithm
            var algorithm = HashAlgorithmType.Sha1;
            if (AlgorithmCombo.SelectedItem is ComboBoxItem algoItem && algoItem.Tag is string algoTag)
            {
                algorithm = algoTag switch
                {
                    "SHA256" => HashAlgorithmType.Sha256,
                    "SHA512" => HashAlgorithmType.Sha512,
                    _ => HashAlgorithmType.Sha1
                };
            }

            // Digits
            int digits = 6;
            if (DigitsCombo.SelectedItem is ComboBoxItem digitsItem && digitsItem.Tag is string digitsTag)
            {
                digits = int.Parse(digitsTag);
            }

            // Normalize Secret Key
            string secretKey = SecretKeyTextBox.Text
                .Replace(" ", "")
                .Replace("-", "")
                .ToUpperInvariant();

            // Create account
            var account = new OtpAccount
            {
                Id = Guid.NewGuid(),
                Issuer = IssuerTextBox.Text.Trim(),
                AccountName = string.IsNullOrWhiteSpace(AccountNameTextBox.Text)
                    ? IssuerTextBox.Text.Trim()
                    : AccountNameTextBox.Text.Trim(),
                SecretKey = secretKey,
                Type = otpType,
                Algorithm = algorithm,
                Digits = digits,
                Period = otpType == OtpType.Totp ? (int)PeriodNumberBox.Value : 30,
                Counter = otpType == OtpType.Hotp ? (long)PeriodNumberBox.Value : 0,
                CreatedAt = DateTime.UtcNow
            };

            // Save
            await _accountRepository.AddAsync(account);
            AddedAccount = account;

            StatusInfoBar.Severity = InfoBarSeverity.Success;
            StatusInfoBar.Message = "Account added successfully!";
        }
        catch (Exception ex)
        {
            StatusInfoBar.Severity = InfoBarSeverity.Error;
            StatusInfoBar.Message = $"Failed to add account: {ex.Message}";
            args.Cancel = true;
        }
        finally
        {
            deferral.Complete();
        }
    }
}
