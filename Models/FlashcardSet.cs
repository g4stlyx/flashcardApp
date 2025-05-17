using System;
using System.Collections.Generic;

namespace flashcardApp.Models
{    public enum Visibility
    {
        Public = 0,   // Maps to 'public' in the database
        Friends = 1,  // Maps to 'friends' in the database
        Private = 2   // Maps to 'private' in the database
    }

    public class FlashcardSet
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public Visibility Visibility { get; set; } = Visibility.Public;
        public string? CoverImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
        public ICollection<Flashcard> Flashcards { get; set; } = new List<Flashcard>();
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
        public ICollection<FavouriteSet> FavouritedBy { get; set; } = new List<FavouriteSet>();
        public ICollection<SetView> Views { get; set; } = new List<SetView>();

        // Computed property to get the number of cards
        public int CardCount => Flashcards?.Count ?? 0;
    }
}
