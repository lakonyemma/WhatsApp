using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WhatsAppClone.Models;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace WhatsAppClone
{
    public sealed partial class MainPage : Page
    {
        private readonly ObservableCollection<Contact> allContacts = new ObservableCollection<Contact>();
        private readonly Dictionary<Guid, ObservableCollection<ChatMessage>> _chats =
            new Dictionary<Guid, ObservableCollection<ChatMessage>>();
        private readonly Dictionary<Guid, int> _pendingReplies = new Dictionary<Guid, int>();
        private readonly Dictionary<Guid, int> _chatGenerations = new Dictionary<Guid, int>();
        private Contact _selectedContact;

        public MainPage()
        {
            InitializeComponent();
            allContacts.Add(new Contact { Name = "Alex Morgan", Status = "Online", ProfileImageUrl = "ms-appx:///Assets/Square44x44Logo.png" });
            allContacts.Add(new Contact { Name = "Jordan Lee", Status = "Available", ProfileImageUrl = "ms-appx:///Assets/Square44x44Logo.png" });
            allContacts.Add(new Contact { Name = "Sam Rivera", Status = "Busy", ProfileImageUrl = "ms-appx:///Assets/Square44x44Logo.png" });

            ContactsList.ItemsSource = allContacts;
            ThemeToggle.IsOn = string.Equals(ApplicationData.Current.LocalSettings.Values["theme"] as string,
                "dark", StringComparison.Ordinal);
            RequestedTheme = ThemeToggle.IsOn ? ElementTheme.Dark : ElementTheme.Light;
            ContactsList.SelectedIndex = 0;
        }

        private ObservableCollection<ChatMessage> Conversation(Contact contact)
        {
            if (!_chats.TryGetValue(contact.Id, out ObservableCollection<ChatMessage> messages))
            {
                messages = new ObservableCollection<ChatMessage>();
                _chats.Add(contact.Id, messages);
            }
            return messages;
        }

        private void ContactsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedContact = ContactsList.SelectedItem as Contact;
            RenderMessages();
        }

        private void RenderMessages()
        {
            MessagesStack.Children.Clear();
            bool active = _selectedContact != null;
            MessageInput.IsEnabled = SendButton.IsEnabled = AttachButton.IsEnabled = EmojiButton.IsEnabled = active;
            ContactName.Text = active ? _selectedContact.Name : "Select a contact";
            UpdateTypingStatus();

            if (!active)
            {
                ClearChatButton.IsEnabled = false;
                return;
            }

            ObservableCollection<ChatMessage> messages = Conversation(_selectedContact);
            ClearChatButton.IsEnabled = messages.Count > 0;
            if (messages.Count == 0)
            {
                MessagesStack.Children.Add(new TextBlock
                {
                    Text = "Start a conversation with " + _selectedContact.Name + ".",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Opacity = 0.7,
                    Margin = new Thickness(0, 28, 0, 0)
                });
            }
            foreach (ChatMessage message in messages) MessagesStack.Children.Add(CreateMessageBubble(message));

            MessagesScroller.UpdateLayout();
            MessagesScroller.ChangeView(null, MessagesScroller.ScrollableHeight, null);
        }

        private Border CreateMessageBubble(ChatMessage message)
        {
            Color color = message.SentByMe ? Color.FromArgb(255, 220, 248, 198)
                : ThemeToggle.IsOn ? Color.FromArgb(255, 51, 61, 65) : Color.FromArgb(255, 236, 240, 241);
            var body = new StackPanel();
            if (message.MediaSource != null)
            {
                body.Children.Add(new Image
                {
                    Source = message.MediaSource,
                    MaxWidth = 360,
                    MaxHeight = 250,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Left
                });
                body.Children.Add(new TextBlock
                {
                    Text = message.MediaName,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 7, 0, 0),
                    Foreground = message.SentByMe ? new SolidColorBrush(Colors.Black) : null
                });
            }
            else
            {
                body.Children.Add(new TextBlock
                {
                    Text = message.Text,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = message.SentByMe ? new SolidColorBrush(Colors.Black) : null
                });
            }
            body.Children.Add(new TextBlock
            {
                Text = message.SentAt.ToString("HH:mm"),
                FontSize = 11,
                Opacity = 0.6,
                HorizontalAlignment = HorizontalAlignment.Right,
                Foreground = message.SentByMe ? new SolidColorBrush(Colors.Black) : null,
                Margin = new Thickness(0, 5, 0, 0)
            });

            return new Border
            {
                Background = new SolidColorBrush(color),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12),
                Margin = new Thickness(message.SentByMe ? 60 : 0, 0, message.SentByMe ? 0 : 60, 12),
                HorizontalAlignment = message.SentByMe ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                MaxWidth = 420,
                Child = body
            };
        }

        private async void AddContact_Click(object sender, RoutedEventArgs e)
        {
            var name = new TextBox { Header = "Name", PlaceholderText = "Contact name" };
            var imageUrl = new TextBox { Header = "Profile image URL", PlaceholderText = "https://example.com/photo.png" };
            var status = new TextBox { Header = "Status", PlaceholderText = "Available" };
            var error = new TextBlock { Foreground = new SolidColorBrush(Colors.Red), TextWrapping = TextWrapping.Wrap };
            var fields = new StackPanel();
            fields.Children.Add(name);
            fields.Children.Add(imageUrl);
            fields.Children.Add(status);
            fields.Children.Add(error);

            var dialog = new ContentDialog
            {
                Title = "Add Contact",
                Content = fields,
                PrimaryButtonText = "Add Contact",
                CloseButtonText = "Cancel",
                RequestedTheme = RequestedTheme
            };
            dialog.PrimaryButtonClick += (s, args) =>
            {
                string newName = name.Text.Trim();
                string newUrl = imageUrl.Text.Trim();
                string newStatus = status.Text.Trim();
                if (newName.Length == 0 || newStatus.Length == 0 || newUrl.Length == 0)
                {
                    error.Text = "Enter a name, image URL, and status.";
                    args.Cancel = true;
                }
                else if (!Uri.TryCreate(newUrl, UriKind.Absolute, out Uri uri)
                         || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
                {
                    error.Text = "Enter a complete HTTP or HTTPS image URL.";
                    args.Cancel = true;
                }
                else if (allContacts.Any(c => string.Equals(c.Name, newName, StringComparison.OrdinalIgnoreCase)))
                {
                    error.Text = "A contact with this name already exists.";
                    args.Cancel = true;
                }
                else
                {
                    var contact = new Contact { Name = newName, Status = newStatus, ProfileImageUrl = newUrl };
                    allContacts.Add(contact);
                    ContactsList.SelectedItem = contact;
                }
            };
            await dialog.ShowAsync();
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendMessageAsync();
        }

        private async Task SendMessageAsync()
        {
            Contact contact = _selectedContact;
            string text = MessageInput.Text.Trim();
            if (contact == null || text.Length == 0) return;

            ObservableCollection<ChatMessage> conversation = Conversation(contact);
            conversation.Add(new ChatMessage { Text = text, SentByMe = true });
            MessageInput.Text = "";
            RenderMessages();

            int generation = _chatGenerations.TryGetValue(contact.Id, out int current) ? current : 0;
            _pendingReplies[contact.Id] = (_pendingReplies.TryGetValue(contact.Id, out int pending) ? pending : 0) + 1;
            UpdateTypingStatus();
            await Task.Delay(TimeSpan.FromSeconds(2));

            // Clearing a conversation cancels replies scheduled for its old messages.
            if ((_chatGenerations.TryGetValue(contact.Id, out current) ? current : 0) != generation) return;
            _pendingReplies[contact.Id]--;
            conversation.Add(new ChatMessage { Text = "Thanks for your message! 😊", SentByMe = false });
            if (_selectedContact?.Id == contact.Id) RenderMessages();
        }

        private void UpdateTypingStatus()
        {
            if (_selectedContact == null)
            {
                TypingStatus.Text = "";
                return;
            }
            bool typing = _pendingReplies.TryGetValue(_selectedContact.Id, out int pending) && pending > 0;
            TypingStatus.Text = typing ? _selectedContact.Name + " is typing..." : _selectedContact.Status;
        }

        private void Emoji_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is MenuFlyoutItem item)) return;
            int position = MessageInput.SelectionStart;
            string value = MessageInput.Text;
            MessageInput.Text = value.Remove(position, MessageInput.SelectionLength).Insert(position, item.Text);
            MessageInput.SelectionStart = position + item.Text.Length;
            MessageInput.Focus(FocusState.Programmatic);
        }

        private async void MessageInput_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                e.Handled = true;
                await SendMessageAsync();
            }
        }

        private async void AttachMedia_Click(object sender, RoutedEventArgs e)
        {
            Contact contact = _selectedContact;
            if (contact == null) return;

            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".bmp");
            StorageFile file = await picker.PickSingleFileAsync();
            if (file == null) return;

            try
            {
                if ((await file.GetBasicPropertiesAsync()).Size > 10 * 1024 * 1024)
                {
                    await ShowMessageAsync("Choose an image smaller than 10 MB.");
                    return;
                }
                var image = new BitmapImage { DecodePixelWidth = 600 };
                using (var stream = await file.OpenAsync(FileAccessMode.Read))
                {
                    await image.SetSourceAsync(stream);
                }
                Conversation(contact).Add(new ChatMessage
                {
                    MediaName = file.Name,
                    MediaSource = image,
                    SentByMe = true
                });
                if (_selectedContact?.Id == contact.Id) RenderMessages();
            }
            catch (Exception)
            {
                await ShowMessageAsync("The selected image could not be opened.");
            }
        }

        private async void ClearChat_Click(object sender, RoutedEventArgs e)
        {
            Contact contact = _selectedContact;
            if (contact == null || Conversation(contact).Count == 0) return;
            var dialog = new ContentDialog
            {
                Title = "Clear Chat",
                Content = "Remove all messages with " + contact.Name + "?",
                PrimaryButtonText = "Clear",
                CloseButtonText = "Cancel",
                RequestedTheme = RequestedTheme
            };
            if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;

            _chatGenerations[contact.Id] = (_chatGenerations.TryGetValue(contact.Id, out int current) ? current : 0) + 1;
            _pendingReplies.Remove(contact.Id);
            Conversation(contact).Clear();
            if (_selectedContact?.Id == contact.Id) RenderMessages();
        }

        private void ThemeToggle_Toggled(object sender, RoutedEventArgs e)
        {
            RequestedTheme = ThemeToggle.IsOn ? ElementTheme.Dark : ElementTheme.Light;
            ApplicationData.Current.LocalSettings.Values["theme"] = ThemeToggle.IsOn ? "dark" : "light";
            if (MessagesStack != null) RenderMessages();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(LoginPage));
            Frame.BackStack.Clear();
        }

        private static async Task ShowMessageAsync(string message)
        {
            await new ContentDialog { Title = "Media", Content = message, CloseButtonText = "OK" }.ShowAsync();
        }
    }
}
