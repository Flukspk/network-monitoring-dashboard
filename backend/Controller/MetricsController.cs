using Microsoft.AspNetCore.Mvc;
using Backend.Data;
using Backend.Models;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Text.Json;

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
               .GroupBy(p => new { p.Target, p.MetricType })
               .Select(g => g.OrderByDescending(p => p.Timestamp).FirstOrDefault())
               .ToList();
            return Ok(latest);
        }

        [HttpGet("summary")]
        public IActionResult GetSummary([FromQuery] int window = 200)
        {
            window = Math.Clamp(window, 20, 2000);

            List<(string Target, string MetricType)> keys;
            if (_activeTargets.Count > 0)
            {
                keys = _activeTargets
                    .Select(t =>
                    {
                        var parts = t.Split('|');
                        var target = parts[0];
                        var type = parts.Length > 1 ? parts[1] : "PING";
                        return (Target: target, MetricType: type);
                    })
                    .Distinct()
                    .ToList();
            }
            else
            {
                keys = _context.NetworkMetrics
                    .OrderByDescending(m => m.Timestamp)
                    .Take(500)
                    .Select(m => new { m.Target, m.MetricType })
                    .Distinct()
                    .AsEnumerable()
                    .Select(x => (x.Target, x.MetricType))
                    .ToList();
            }

            var now = DateTime.UtcNow;
            var summaries = new List<MonitorSummary>();

            foreach (var (target, metricType) in keys)
            {
                var metrics = _context.NetworkMetrics
                    .Where(m => m.Target == target && m.MetricType == metricType)
                    .OrderByDescending(m => m.Timestamp)
                    .Take(window)
                    .ToList();

                if (metrics.Count == 0) continue;

                var latest = metrics.OrderByDescending(m => m.Timestamp).First();
                var earliest = _context.NetworkMetrics
                    .Where(m => m.Target == target && m.MetricType == metricType)
                    .OrderBy(m => m.Timestamp)
                    .Select(m => m.Timestamp)
                    .FirstOrDefault();

                int? threshold = null;
                string? agentName = null;
                try
                {
                    var extra = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(latest.ExtraData ?? "{}");
                    if (extra != null)
                    {
                        if (extra.TryGetValue("Threshold", out var th) && th.ValueKind == JsonValueKind.Number && th.TryGetInt32(out var thVal))
                        {
                            threshold = thVal;
                        }
                        if (extra.TryGetValue("AgentName", out var an) && an.ValueKind == JsonValueKind.String)
                        {
                            agentName = an.GetString();
                        }
                    }
                }
                catch { }

                var valid = metrics
                    .Where(m => m.Value > 0)
                    .Select(m => m.Value)
                    .ToList();
                var avg = valid.Count > 0 ? valid.Average() : 0;

                var isFail = latest.Status != "Success" && latest.Status != "Pending";
                var durationSeconds = earliest == default ? 0 : (now - earliest).TotalSeconds;

                summaries.Add(new MonitorSummary
                {
                    Target = target,
                    MetricType = metricType,
                    AgentName = agentName,
                    Status = latest.Status,
                    LatestValue = latest.Value,
                    AvgValue = avg,
                    PacketLoss = latest.PacketLoss,
                    Threshold = threshold,
                    StartTime = earliest == default ? latest.Timestamp : earliest,
                    LastProbe = latest.Timestamp,
                    DurationSeconds = durationSeconds,
                    IsFail = isFail
                });
            }

            var result = new
            {
                total = summaries.Count,
                active = summaries.Count,
                fail = summaries.Count(s => s.IsFail),
                data = summaries.OrderByDescending(s => s.LastProbe).ToList()
            };

            return Ok(result);
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

    public class MonitorSummary
    {
        public string Target { get; set; } = string.Empty;
        public string MetricType { get; set; } = string.Empty;
        public string? AgentName { get; set; }
        public string Status { get; set; } = string.Empty;
        public double LatestValue { get; set; }
        public double AvgValue { get; set; }
        public float PacketLoss { get; set; }
        public int? Threshold { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime LastProbe { get; set; }
        public double DurationSeconds { get; set; }
        public bool IsFail { get; set; }
    }
}