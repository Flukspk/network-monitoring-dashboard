using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Models;

namespace Backend.Data // หรือ namespace ที่คุณใช้อยู่
{
    public class BackendDbContext : DbContext
    {
        public BackendDbContext(DbContextOptions<BackendDbContext> options) : base(options) { }

        // --- ส่วนที่หายไป: ต้องเพิ่มบรรทัดนี้ครับ ---
        public DbSet<PingMetric> PingMetrics { get; set; } 
        // ----------------------------------------
        
        // ถ้ามีตาราง User ด้วย ก็ต้องมีบรรทัดนี้
        public DbSet<User> Users { get; set; } 
    }
}