using System;
using System.Collections.Generic;

namespace flashcardApp.Models
{    public enum Visibility
    {
        Public = 0,   // 'public' in db
        Friends = 1,  // 'friends' in db
        Private = 2   // 'private' in db
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

        // Navigation
        public User User { get; set; } = null!;
        public ICollection<Flashcard> Flashcards { get; set; } = new List<Flashcard>();
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
        public ICollection<FavouriteSet> FavouritedBy { get; set; } = new List<FavouriteSet>();
        public ICollection<SetView> Views { get; set; } = new List<SetView>();

        // number of cards
        public int CardCount => Flashcards?.Count ?? 0;
    }
}
