using Microsoft.AspNetCore.Mvc;
using Backend.Data;   // แก้ b เป็น B ใหญ่ (ให้ตรงกับ namespace ในไฟล์ BackendDbContext.cs)
using Backend.Models; // ตรวจสอบว่าในไฟล์ PingMetric.cs ใช้ namespace นี้จริงไหม
using System.Threading.Tasks;
using System;
using System.Linq;    // เพิ่มบรรทัดนี้ เพื่อให้ใช้ GroupBy, OrderBy ได้

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MetricsController : ControllerBase
    {
        private readonly BackendDbContext _context;

        public MetricsController(BackendDbContext context)
        {
            _context = context;
        }

        // Agent จะยิงมาที่นี่
        [HttpPost]
        public async Task<IActionResult> PostMetric([FromBody] PingMetric metric)
        {
            if (metric == null) return BadRequest();

            metric.Timestamp = DateTime.UtcNow;
            _context.PingMetrics.Add(metric);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Data saved" });
        }

        // Frontend ดึงข้อมูลจากที่นี่
        [HttpGet("latest")]
        public IActionResult GetLatest()
        {
             var latest = _context.PingMetrics
                .GroupBy(p => p.Target)
                .Select(g => g.OrderByDescending(p => p.Timestamp).FirstOrDefault())
                .ToList();
            return Ok(latest);
        }
    }
}