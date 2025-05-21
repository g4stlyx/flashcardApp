using System;

namespace flashcardApp.Models
{
    public class Friend
    {
        public int UserId1 { get; set; }
        public int UserId2 { get; set; }
        public DateTime CreatedAt { get; set; }

        // navigation
        public User User1 { get; set; } = null!;
        public User User2 { get; set; } = null!;
    }
}
