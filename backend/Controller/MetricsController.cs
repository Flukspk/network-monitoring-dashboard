using Microsoft.AspNetCore.Mvc;
using Backend.Data;
using Backend.Models;
using System.Threading.Tasks;
using System;
using System.Linq;

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

        [HttpPost]
        // ❌ แก้ตรงนี้: จาก PingMetric เป็น NetworkMetric
        public async Task<IActionResult> PostMetric([FromBody] NetworkMetric metric) 
        {
            if (metric == null) return BadRequest();

            metric.Timestamp = DateTime.UtcNow;
            
            // ❌ แก้ตรงนี้: จาก _context.PingMetrics เป็น _context.NetworkMetrics
            _context.NetworkMetrics.Add(metric); 
            await _context.SaveChangesAsync();

            return Ok(new { message = "Data saved" });
        }

        [HttpGet("latest")]
        public IActionResult GetLatest()
        {
             // ❌ แก้ตรงนี้: จาก _context.PingMetrics เป็น _context.NetworkMetrics
             var latest = _context.NetworkMetrics
                .GroupBy(p => p.Target)
                .Select(g => g.OrderByDescending(p => p.Timestamp).FirstOrDefault())
                .ToList();
            return Ok(latest);
        }
    }
}