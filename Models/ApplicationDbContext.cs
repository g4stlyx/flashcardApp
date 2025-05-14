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

            // Map entities to existing tables with snake_case naming
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<FlashcardSet>().ToTable("flashcard_sets");
            modelBuilder.Entity<Flashcard>().ToTable("flashcards");
            modelBuilder.Entity<Tag>().ToTable("tags");
            modelBuilder.Entity<FavouriteSet>().ToTable("favourite_sets");
            modelBuilder.Entity<FriendRequest>().ToTable("friend_requests");
            modelBuilder.Entity<Friend>().ToTable("friends");
            modelBuilder.Entity<SetView>().ToTable("set_views");

            // Configure composite keys
            modelBuilder.Entity<FavouriteSet>()
                .HasKey(fs => new { fs.UserId, fs.SetId });

            modelBuilder.Entity<Friend>()
                .HasKey(f => new { f.UserId1, f.UserId2 });

            // Column name mappings for User
            modelBuilder.Entity<User>()
                .Property(u => u.Username).HasColumnName("username");
            modelBuilder.Entity<User>()
                .Property(u => u.Email).HasColumnName("email");
            modelBuilder.Entity<User>()
                .Property(u => u.PasswordHash).HasColumnName("password_hash");
            modelBuilder.Entity<User>()
                .Property(u => u.Salt).HasColumnName("salt");
            modelBuilder.Entity<User>()
                .Property(u => u.Bio).HasColumnName("bio");
            modelBuilder.Entity<User>()
                .Property(u => u.FirstName).HasColumnName("first_name");
            modelBuilder.Entity<User>()
                .Property(u => u.LastName).HasColumnName("last_name");
            modelBuilder.Entity<User>()
                .Property(u => u.PhoneNumber).HasColumnName("phone_number");
            modelBuilder.Entity<User>()
                .Property(u => u.ProfilePictureUrl).HasColumnName("profile_picture_url");
            modelBuilder.Entity<User>()
                .Property(u => u.IsAdmin).HasColumnName("is_admin");
            modelBuilder.Entity<User>()
                .Property(u => u.CreatedAt).HasColumnName("created_at");
            modelBuilder.Entity<User>()
                .Property(u => u.UpdatedAt).HasColumnName("updated_at");

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
