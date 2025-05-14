using System;

namespace flashcardApp.Models
{
    public class SetView
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public int SetId { get; set; }
        public string? ViewerIpHash { get; set; }
        public DateTime ViewedAt { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public FlashcardSet FlashcardSet { get; set; } = null!;
    }
}
