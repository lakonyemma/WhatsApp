using System;

namespace WhatsAppClone.Models
{
    public sealed class Contact
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string ProfileImageUrl { get; set; }
        public string Status { get; set; }
    }
}
