using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using Backend.Data;   // ✅ เพิ่มการเชื่อมต่อ Database
using Backend.Models; // ✅ เพิ่ม Model ใหม่
using System.Text.Json; // ✅ เพิ่มสำหรับทำ JSON ExtraData

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/agent")] 
    public class AgentController : ControllerBase
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly BackendDbContext _context; // ตัวจัดการ Database

        // Inject DbContext เข้ามาใน Constructor
        public AgentController(BackendDbContext context)
        {
            _context = context;
        }

        [HttpPost("run")]
        public async Task<IActionResult> RunManualTest([FromBody] ManualTestRequest request)
        {
            if (string.IsNullOrEmpty(request.Target)) return BadRequest("Target is required");

            var sw = Stopwatch.StartNew();
            
            // ตัวแปรสำหรับเก็บลง Database
            string status = "Success";
            float value = 0; // ใช้เก็บ Latency หรือ Response Time
            float packetLoss = 0;
            int? statusCode = null;
            string metricType = "PING"; // ค่าเริ่มต้น
            string message = "Manual test executed";

            try
            {
                // 🌐 กรณีเป็น HTTP (เว็บ)
                if (request.Target.StartsWith("http"))
                {
                    metricType = "HTTP";
                    var response = await _httpClient.GetAsync(request.Target);
                    sw.Stop();

                    status = response.IsSuccessStatusCode ? "Success" : "Investigate";
                    value = sw.ElapsedMilliseconds; // เก็บ Response Time
                    statusCode = (int)response.StatusCode;
                    message = response.ReasonPhrase ?? "HTTP Request Finished";
                    packetLoss = response.IsSuccessStatusCode ? 0 : 1;
                }
                // 📡 กรณีเป็น Ping (IP หรือ Domain)
                else
                {
                    metricType = "PING";
                    using var ping = new Ping();
                    var reply = await ping.SendPingAsync(request.Target);
                    sw.Stop();

                    if (reply.Status == IPStatus.Success)
                    {
                        status = "Success";
                        value = reply.RoundtripTime; // เก็บ Latency
                        packetLoss = 0;
                        message = "Ping Reply Received";
                    }
                    else
                    {
                        status = "Investigate";
                        value = 0;
                        packetLoss = 1; // Loss 100%
                        message = reply.Status.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                status = "Investigate"; 
                message = ex.Message;
                packetLoss = 1;
                value = 0;
            }

            // --- 💾 บันทึกลงตาราง NetworkMetrics ---
            var metric = new NetworkMetric
            {
                Target = request.Target,
                MetricType = metricType,
                Value = value,
                PacketLoss = packetLoss,
                StatusCode = statusCode,
                Status = status,
                // เก็บข้อมูลเสริมลง JSON (รวมถึง AgentId ที่เลือกจากหน้าเว็บ)
                ExtraData = JsonSerializer.Serialize(new 
                { 
                    Source = "Manual Run (Web Console)",
                    SelectedAgent = request.AgentId,
                    Message = message
                }),
                Timestamp = DateTime.UtcNow
            };

            _context.NetworkMetrics.Add(metric);
            await _context.SaveChangesAsync();
            // ----------------------------------------

            // ส่งผลลัพธ์กลับไปโชว์ที่หน้าเว็บทันที
            return Ok(new 
            {
                target = request.Target,
                message = message,
                status = status,
                timestamp = metric.Timestamp,
                latency = value + " ms"
            });
        }
    }

    public class ManualTestRequest
    {
        public string AgentId { get; set; }
        public string Target { get; set; }
    }
}