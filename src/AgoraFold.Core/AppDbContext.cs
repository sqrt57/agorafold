using AgoraFold.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AgoraFold.Core;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<ListingImage> ListingImages => Set<ListingImage>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Listing>()
            .Property(l => l.Price)
            .HasColumnType("numeric(18,2)");

        modelBuilder.Entity<Listing>()
            .HasOne(l => l.Owner)
            .WithMany(u => u.Listings)
            .HasForeignKey(l => l.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ListingImage>()
            .HasOne(i => i.Listing)
            .WithMany(l => l.Images)
            .HasForeignKey(i => i.ListingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Conversation>()
            .HasOne(c => c.Listing)
            .WithMany(l => l.Conversations)
            .HasForeignKey(c => c.ListingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Conversation>()
            .HasOne(c => c.Participant)
            .WithMany()
            .HasForeignKey(c => c.ParticipantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Conversation)
            .WithMany(c => c.Messages)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Electronics" },
            new Category { Id = 2, Name = "Vehicles" },
            new Category { Id = 3, Name = "Furniture" },
            new Category { Id = 4, Name = "Clothing & Accessories" },
            new Category { Id = 5, Name = "Home & Garden" },
            new Category { Id = 6, Name = "Free Stuff" },
            new Category { Id = 7, Name = "Other" });
    }
}
