using Microsoft.EntityFrameworkCore;
using Backend.Models;

namespace Backend.Data
{
    public class BackendDbContext : DbContext
    {
        public BackendDbContext(DbContextOptions<BackendDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<PingMetric> PingMetrics { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<User>()
        .ToTable("users")
        .HasIndex(u => u.Username)
        .IsUnique();

    modelBuilder.Entity<PingMetric>()
        .ToTable("ping_metrics");

    // Seed user data ด้วยค่าคงที่
    modelBuilder.Entity<User>().HasData(
        new User
        {
            UserId = 1,
            Username = "fluk",
            Password = "1234",
            RoleId = 1,
            CreatedAt = new DateTime(2025, 10, 28, 0, 0, 0, DateTimeKind.Utc) // static value
        }
    );
    }

    }
}
