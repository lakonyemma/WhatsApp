using System;
using WhatsAppClone.Services;
using Windows.Security.Cryptography;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace WhatsAppClone
{
    public sealed partial class ForgotPasswordPage : Page
    {
        private static DateTimeOffset _cooldownUntilUtc = DateTimeOffset.MinValue;
        private readonly DispatcherTimer _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };

        public ForgotPasswordPage()
        {
            InitializeComponent();
            _timer.Tick += Timer_Tick;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            UpdateCooldown();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _timer.Stop();
            base.OnNavigatedFrom(e);
        }

        private async void SendCodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (DateTimeOffset.UtcNow < _cooldownUntilUtc) return;
            string email = EmailInput.Text.Trim();
            if (email.Length == 0 || email.IndexOf('@') < 1)
            {
                Feedback.Text = "Enter the email address for your account.";
                return;
            }

            SendCodeButton.IsEnabled = false;
            EmailLookupResult result = await CredentialStore.FindEmailAsync(email);
            if (result != EmailLookupResult.Found)
            {
                Feedback.Text = result == EmailLookupResult.MissingFile ? "No accounts file exists. Create an account first."
                              : result == EmailLookupResult.UnreadableFile ? "The local accounts file is unreadable."
                              : "No account uses this email address.";
                UpdateCooldown();
                return;
            }

            byte[] bytes;
            CryptographicBuffer.CopyToByteArray(CryptographicBuffer.GenerateRandom(4), out bytes);
            int code = 100000 + (int)(BitConverter.ToUInt32(bytes, 0) % 900000);
            CodeOutput.Text = "Recovery code: " + code;
            Feedback.Text = "Demo code shown on this page. No email has been sent.";
            _cooldownUntilUtc = DateTimeOffset.UtcNow.AddSeconds(30);
            UpdateCooldown();
        }

        private void Timer_Tick(object sender, object e) => UpdateCooldown();

        private void UpdateCooldown()
        {
            int seconds = (int)Math.Ceiling((_cooldownUntilUtc - DateTimeOffset.UtcNow).TotalSeconds);
            if (seconds > 0)
            {
                SendCodeButton.IsEnabled = false;
                CooldownOutput.Text = "You can request another code in " + seconds + " seconds.";
                _timer.Start();
            }
            else
            {
                SendCodeButton.IsEnabled = true;
                CooldownOutput.Text = "";
                _timer.Stop();
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack) Frame.GoBack();
            else Frame.Navigate(typeof(LoginPage));
        }
    }
}
