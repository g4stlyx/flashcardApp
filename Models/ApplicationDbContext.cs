using Microsoft.EntityFrameworkCore;

namespace flashcardApp.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<FlashcardSet> FlashcardSets { get; set; } = null!;
        public DbSet<Flashcard> Flashcards { get; set; } = null!;
        public DbSet<Tag> Tags { get; set; } = null!;
        public DbSet<FavouriteSet> FavouriteSets { get; set; } = null!;
        public DbSet<FriendRequest> FriendRequests { get; set; } = null!;
        public DbSet<Friend> Friends { get; set; } = null!;
        public DbSet<SetView> SetViews { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure composite keys
            modelBuilder.Entity<FavouriteSet>()
                .HasKey(fs => new { fs.UserId, fs.SetId });

            modelBuilder.Entity<Friend>()
                .HasKey(f => new { f.UserId1, f.UserId2 });

            // Configure relationships
            modelBuilder.Entity<FavouriteSet>()
                .HasOne(fs => fs.User)
                .WithMany(u => u.FavouriteSets)
                .HasForeignKey(fs => fs.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FavouriteSet>()
                .HasOne(fs => fs.FlashcardSet)
                .WithMany(s => s.FavouritedBy)
                .HasForeignKey(fs => fs.SetId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Friend>()
                .HasOne(f => f.User1)
                .WithMany()
                .HasForeignKey(f => f.UserId1)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Friend>()
                .HasOne(f => f.User2)
                .WithMany()
                .HasForeignKey(f => f.UserId2)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FriendRequest>()
                .HasOne(fr => fr.Sender)
                .WithMany(u => u.SentFriendRequests)
                .HasForeignKey(fr => fr.SenderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FriendRequest>()
                .HasOne(fr => fr.Receiver)
                .WithMany(u => u.ReceivedFriendRequests)
                .HasForeignKey(fr => fr.ReceiverId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure property mappings (to match SQL column names)
            modelBuilder.Entity<Tag>()
                .Property(t => t.TagName)
                .HasColumnName("tag");
        }
    }
}
