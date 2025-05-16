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
                
            // Configure column names for FavouriteSet
            modelBuilder.Entity<FavouriteSet>()
                .Property(fs => fs.UserId).HasColumnName("user_id");
            modelBuilder.Entity<FavouriteSet>()
                .Property(fs => fs.SetId).HasColumnName("set_id");
            modelBuilder.Entity<FavouriteSet>()
                .Property(fs => fs.CreatedAt).HasColumnName("created_at");

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
                .OnDelete(DeleteBehavior.Cascade);            // Configure property mappings (to match SQL column names)
            modelBuilder.Entity<Tag>()
                .Property(t => t.TagName)
                .HasColumnName("tag");
                
            // Column name mappings for FlashcardSet
            modelBuilder.Entity<FlashcardSet>()
                .Property(fs => fs.UserId).HasColumnName("user_id");
            modelBuilder.Entity<FlashcardSet>()
                .Property(fs => fs.Title).HasColumnName("title");            modelBuilder.Entity<FlashcardSet>()
                .Property(fs => fs.Description).HasColumnName("description");
            modelBuilder.Entity<FlashcardSet>()
                .Property(fs => fs.CoverImageUrl).HasColumnName("cover_image_url");
            modelBuilder.Entity<FlashcardSet>()
                .Property(fs => fs.CreatedAt).HasColumnName("created_at");
            modelBuilder.Entity<FlashcardSet>()
                .Property(fs => fs.UpdatedAt).HasColumnName("updated_at");
                  // Column name mappings for Flashcard
            modelBuilder.Entity<Flashcard>()
                .Property(f => f.SetId).HasColumnName("set_id");
            modelBuilder.Entity<Flashcard>()
                .Property(f => f.Term).HasColumnName("term");
            modelBuilder.Entity<Flashcard>()
                .Property(f => f.Definition).HasColumnName("definition");
            modelBuilder.Entity<Flashcard>()
                .Property(f => f.ImageUrl).HasColumnName("image_url");
            modelBuilder.Entity<Flashcard>()
                .Property(f => f.ExampleSentence).HasColumnName("example_sentence");
            modelBuilder.Entity<Flashcard>()
                .Property(f => f.CreatedAt).HasColumnName("created_at");
            modelBuilder.Entity<Flashcard>()
                .Property(f => f.UpdatedAt).HasColumnName("updated_at");
                
            // Add explicit relationship mapping for Flashcard to FlashcardSet
            modelBuilder.Entity<Flashcard>()
                .HasOne(f => f.FlashcardSet)
                .WithMany(fs => fs.Flashcards)
                .HasForeignKey(f => f.SetId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Column name mappings for SetView
            modelBuilder.Entity<SetView>()
                .Property(sv => sv.UserId).HasColumnName("user_id");
            modelBuilder.Entity<SetView>()
                .Property(sv => sv.SetId).HasColumnName("set_id");
            modelBuilder.Entity<SetView>()
                .Property(sv => sv.ViewerIpHash).HasColumnName("viewer_ip_hash");
            modelBuilder.Entity<SetView>()
                .Property(sv => sv.ViewedAt).HasColumnName("viewed_at");            // Configure the Visibility enum to string conversion for MySQL ENUM
            modelBuilder.Entity<FlashcardSet>()
                .Property(fs => fs.Visibility)
                .HasConversion(
                    // Convert enum to specific string values expected by the database
                    v => v.ToString().ToLower(),  // enum to string
                    v => (Visibility)Enum.Parse(typeof(Visibility), v, true) // string to enum
                )
                .HasColumnName("visibility");            // Configure string conversions for other enums used in entities
            modelBuilder.Entity<FriendRequest>()
                .Property(fr => fr.Status)
                .HasConversion(
                    // Convert enum to specific string values expected by the database
                    s => s.ToString().ToLower(),  // enum to string
                    s => (FriendRequestStatus)Enum.Parse(typeof(FriendRequestStatus), s, true) // string to enum
                )
                .HasColumnName("status");

            // Add explicit relationship mapping for Tag to FlashcardSet
            modelBuilder.Entity<Tag>()
                .HasOne(t => t.FlashcardSet)
                .WithMany(fs => fs.Tags)
                .HasForeignKey(t => t.SetId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure property mappings for Tag
            modelBuilder.Entity<Tag>()
                .Property(t => t.SetId).HasColumnName("set_id");
                
            // Add explicit relationship mapping for SetView to FlashcardSet
            modelBuilder.Entity<SetView>()
                .HasOne(sv => sv.FlashcardSet)
                .WithMany(fs => fs.Views)
                .HasForeignKey(sv => sv.SetId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
