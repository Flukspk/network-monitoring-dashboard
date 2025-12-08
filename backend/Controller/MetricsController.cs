using Microsoft.AspNetCore.Mvc;
using Backend.Data;
using Backend.Models;
using System.Threading.Tasks;
using System;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MetricsController : ControllerBase
    {
        private readonly BackendDbContext _context;
        private readonly ILogger<MetricsController> _logger;

        public MetricsController(BackendDbContext context, ILogger<MetricsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> PostMetric([FromBody] NetworkMetric metric)
        {
            // 🔍 LOG VALIDATION ERRORS
            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                _logger.LogError($"❌ BAD REQUEST: {errors}");
                return BadRequest(new { error = "Invalid Model", details = errors });
            }

            if (metric == null)
            {
                _logger.LogError("❌ Metric object is NULL");
                return BadRequest("Metric is null");
            }

            if (metric.Timestamp == default)
            {
                metric.Timestamp = DateTime.UtcNow;
            }

            try
            {
                _context.NetworkMetrics.Add(metric);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Data saved" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ DATABASE ERROR: {ex.Message}");
                return StatusCode(500, "Database error");
            }
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

            // เช็ค null ก่อนใช้ และ trim whitespace
            if (!string.IsNullOrEmpty(target))
            {
                var trimmedTarget = target.Trim();
                // หมายเหตุ: การใช้ .Trim() ใน LINQ อาจมีปัญหากับบาง Database Provider 
                // แต่ถ้าใช้ PostgreSQL (Npgsql) ปกติจะรองรับครับ
                query = query.Where(m => m.Target.Trim() == trimmedTarget);
            }

            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(m => m.MetricType == type);
            }

            var data = query
                .OrderByDescending(m => m.Timestamp)
                .Take(20)
                .ToList();

            _logger.LogInformation($"Filtered metrics: target={target}, type={type}, count={data.Count}");
            return Ok(data);
        }

        // ✅ เพิ่ม API นี้: ให้ Agent มาดึงรายชื่อ Target จากประวัติเดิม
        [HttpGet("targets")]
        public IActionResult GetActiveTargets()
        {
            // ไปกวาดดูใน Database ย้อนหลัง 24 ชม. ว่ามี Target ไหนถูกยิงบ้าง
            var targets = _context.NetworkMetrics
                .Where(m => m.Timestamp > DateTime.UtcNow.AddHours(-24)) // เอาแค่ที่ Active ใน 24 ชม.
                .Select(m => new { m.Target, m.MetricType }) // เลือกมาแค่ชื่อกับประเภท
                .Distinct() // ตัดตัวซ้ำทิ้ง
                .ToList();

            return Ok(targets);
        }
    }
}