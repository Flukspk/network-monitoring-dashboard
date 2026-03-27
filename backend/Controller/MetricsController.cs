using Microsoft.AspNetCore.Mvc;
using Backend.Data;
using Backend.Models;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MetricsController : ControllerBase
    {
        private readonly BackendDbContext _context;
        private readonly ILogger<MetricsController> _logger;

        // จำรายชื่อเป้าหมายที่กำลังทำงาน (อยู่ใน RAM)
        private static HashSet<string> _activeTargets = new HashSet<string>();

        public MetricsController(BackendDbContext context, ILogger<MetricsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ✅ เปลี่ยนชื่อ Class รับค่าเป็น MonitorRequest (เพื่อหนี Error ชื่อซ้ำ)
        [HttpPost("start")]
        public IActionResult StartMonitoring([FromBody] MonitorRequest req)
        {
            if (string.IsNullOrEmpty(req.Target)) return BadRequest();
            
            var key = $"{req.Target}|{req.Type}";
            _activeTargets.Add(key);
            
            _logger.LogInformation($"🟢 Started monitoring: {key}");
            return Ok(new { message = "Started", current = _activeTargets });
        }

        [HttpPost("stop")]
        public IActionResult StopMonitoring([FromBody] MonitorRequest req)
        {
            var key = $"{req.Target}|{req.Type}";
            _activeTargets.Remove(key); 
            _activeTargets.Remove(req.Target); 

            _logger.LogInformation($"🔴 Stopped monitoring: {key}");
            return Ok(new { message = "Stopped", current = _activeTargets });
        }

        [HttpGet("targets")]
        public IActionResult GetActiveTargets()
        {
            var targets = _activeTargets.Select(t => {
                var parts = t.Split('|');
                return new { 
                    Target = parts[0], 
                    MetricType = parts.Length > 1 ? parts[1] : "PING" 
                };
            }).ToList();

            return Ok(targets);
        }

        [HttpPost]
        public async Task<IActionResult> PostMetric([FromBody] NetworkMetric metric) 
        {
            if (metric == null) return BadRequest("Metric is null");
            if (metric.Timestamp == default) metric.Timestamp = DateTime.UtcNow;
            
            _context.NetworkMetrics.Add(metric); 
            await _context.SaveChangesAsync();
            return Ok(new { message = "Data saved" });
        }

        [HttpGet("latest")]
        public IActionResult GetLatest()
        {
             var latest = _context.NetworkMetrics
                .GroupBy(p => p.Target)
                .Select(g => g.OrderByDescending(p => p.Timestamp).FirstOrDefault())
                .ToList();
            return Ok(latest);
        }

        [HttpGet("filter")]
        public IActionResult GetFilteredMetrics([FromQuery] string? target, [FromQuery] string? type)
        {
            var query = _context.NetworkMetrics.AsQueryable();
            if (!string.IsNullOrEmpty(target)) query = query.Where(m => m.Target == target);
            if (!string.IsNullOrEmpty(type)) query = query.Where(m => m.MetricType == type);

            var data = query.OrderByDescending(m => m.Timestamp).Take(500).ToList();
            return Ok(data);
        }
    }

    // ✅ ชื่อคลาสใหม่ (MonitorRequest) ไม่ซ้ำใครแน่นอน
    public class MonitorRequest
    {
        public string Target { get; set; }
        public string Type { get; set; }
    }
}