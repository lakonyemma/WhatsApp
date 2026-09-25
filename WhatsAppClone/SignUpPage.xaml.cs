using System;
using System.Linq;
using WhatsAppClone.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace WhatsAppClone
{
    public sealed partial class SignUpPage : Page
    {
        public SignUpPage()
        {
            InitializeComponent();
        }

        private async void SignUpButton_Click(object sender, RoutedEventArgs e)
        {
            string name = NameInput.Text.Trim();
            string phone = PhoneInput.Text.Trim();
            string email = EmailInput.Text.Trim();
            if (name.Length == 0 || phone.Length < 7 || phone.Length > 15 || !phone.All(char.IsDigit)
                || !IsValidEmail(email) || PasswordInput.Password.Length < 8)
            {
                Feedback.Text = "Enter a name, 7 to 15 phone digits, a valid email, and a password of at least 8 characters.";
                return;
            }
            if (PasswordInput.Password != ConfirmInput.Password)
            {
                Feedback.Text = "The passwords do not match.";
                return;
            }

            SignUpButton.IsEnabled = false;
            try
            {
                RegistrationResult result = await CredentialStore.RegisterAsync(name, phone, email, PasswordInput.Password);
                switch (result)
                {
                    case RegistrationResult.Success:
                        var dialog = new ContentDialog
                        {
                            Title = "Account created",
                            Content = "Your local account is ready. Log in to continue.",
                            CloseButtonText = "OK"
                        };
                        await dialog.ShowAsync();
                        Frame.Navigate(typeof(LoginPage));
                        Frame.BackStack.Clear();
                        break;
                    case RegistrationResult.DuplicateAccount:
                        Feedback.Text = "An account with this phone number or email already exists.";
                        break;
                    default:
                        Feedback.Text = "The local accounts file cannot be read or saved.";
                        break;
                }
            }
            finally { SignUpButton.IsEnabled = true; }
        }

        private static bool IsValidEmail(string email)
        {
            int at = email.IndexOf('@');
            return at > 0 && email.LastIndexOf('.') > at + 1 && !email.Contains(" ")
                   && !email.EndsWith(".", StringComparison.Ordinal);
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack) Frame.GoBack();
            else Frame.Navigate(typeof(LoginPage));
        }
    }
}
