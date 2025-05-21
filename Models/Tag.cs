namespace flashcardApp.Models
{
    public class Tag
    {
        public int Id { get; set; }
        public int SetId { get; set; }
        public string TagName { get; set; } = null!;

        // navigation
        public FlashcardSet FlashcardSet { get; set; } = null!;
    }
}
