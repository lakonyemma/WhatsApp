using System;
using WhatsAppClone.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace WhatsAppClone
{
    public sealed partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string identifier = IdentifierInput.Text.Trim();
            if (identifier.Length == 0 || PasswordInput.Password.Length == 0)
            {
                Feedback.Text = "Enter your email or phone number and password.";
                return;
            }

            LoginButton.IsEnabled = false;
            try
            {
                LoginResult result = await CredentialStore.LoginAsync(identifier, PasswordInput.Password);
                switch (result)
                {
                    case LoginResult.Success:
                        Frame.Navigate(typeof(MainPage));
                        Frame.BackStack.Clear();
                        break;
                    case LoginResult.MissingFile:
                        Feedback.Text = "No accounts file exists. Create an account first.";
                        break;
                    case LoginResult.UnreadableFile:
                        Feedback.Text = "The local accounts file is unreadable. Check your app data and try again.";
                        break;
                    default:
                        Feedback.Text = "Incorrect email, phone number, or password.";
                        break;
                }
            }
            finally { LoginButton.IsEnabled = true; }
        }

        private void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(ForgotPasswordPage));
        }

        private void CreateAccount_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SignUpPage));
        }
    }
}
