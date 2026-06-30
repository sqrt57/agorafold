using AgoraFold.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgoraFold.Core;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Listing> Listings => Set<Listing>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Listing>()
            .Property(l => l.Price)
            .HasColumnType("numeric(18,2)");
    }
}
