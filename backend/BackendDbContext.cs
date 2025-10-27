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

            // กำหนด table name และ unique constraints
            modelBuilder.Entity<User>()
                .ToTable("users")
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<PingMetric>()
                .ToTable("ping_metrics");
        }
    }
}
