using System;

namespace flashcardApp.Models
{
    public class Flashcard
    {
        public int Id { get; set; }
        public int SetId { get; set; }
        public string Term { get; set; } = null!;
        public string Definition { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public string? ExampleSentence { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation
        public FlashcardSet FlashcardSet { get; set; } = null!;
    }
}
