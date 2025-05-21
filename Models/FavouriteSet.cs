using System;

namespace flashcardApp.Models
{
    public class FavouriteSet
    {
        public int UserId { get; set; }
        public int SetId { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation
        public User User { get; set; } = null!;
        public FlashcardSet FlashcardSet { get; set; } = null!;
    }
}
