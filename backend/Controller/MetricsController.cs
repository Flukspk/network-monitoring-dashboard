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

    // เช็ค null ก่อนใช้
    if (!string.IsNullOrEmpty(target))
    {
        query = query.Where(m => m.Target == target);
    }

    if (!string.IsNullOrEmpty(type))
    {
        query = query.Where(m => m.MetricType == type);
    }

    var data = query
        .OrderByDescending(m => m.Timestamp)
        .Take(20)
        .ToList();
        
    return Ok(data);
        }
    }
}