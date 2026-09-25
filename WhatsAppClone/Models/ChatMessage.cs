using System;
using Windows.UI.Xaml.Media.Imaging;

namespace WhatsAppClone.Models
{
    public sealed class ChatMessage
    {
        public string Text { get; set; }
        public string MediaName { get; set; }
        public BitmapImage MediaSource { get; set; }
        public bool SentByMe { get; set; }
        public DateTimeOffset SentAt { get; set; } = DateTimeOffset.Now;
    }
}
