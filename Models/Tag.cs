namespace flashcardApp.Models
{
    public class Tag
    {
        public int Id { get; set; }
        public int SetId { get; set; }
        public string TagName { get; set; } = null!;

        // Navigation property
        public FlashcardSet FlashcardSet { get; set; } = null!;
    }
}
