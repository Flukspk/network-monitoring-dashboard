using Microsoft.EntityFrameworkCore;
using Backend.Models;

namespace Backend.Data
{
    public class BackendDbContext : DbContext
    {
        public BackendDbContext(DbContextOptions<BackendDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        

        public DbSet<NetworkMetric> NetworkMetrics { get; set; }

        public DbSet<Agent> Agents { get; set; }
        public DbSet<Notification> Notifications { get; set; }
    }
}