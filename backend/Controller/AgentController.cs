using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Backend.Data;   // ✅ เพิ่มการเชื่อมต่อ Database
using Backend.Models; // ✅ เพิ่ม Model ใหม่
using System.Text.Json; // ✅ เพิ่มสำหรับทำ JSON ExtraData

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/agent")] 
    public class AgentController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly BackendDbContext _context; // ตัวจัดการ Database

        // Inject DbContext เข้ามาใน Constructor
        public AgentController(BackendDbContext context)
        {
            _context = context;
            // สร้าง HttpClient พร้อม timeout และ configuration
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true, // อนุญาตให้ follow redirects
                MaxAutomaticRedirections = 5, // จำกัด redirects
                UseCookies = false // ไม่ใช้ cookies
            };
            
            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(15) // เพิ่ม timeout เป็น 15 วินาที
            };
            
            // ตั้งค่า default headers
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        [HttpPost("run")]
        public async Task<IActionResult> RunManualTest([FromBody] ManualTestRequest request)
        {
            if (string.IsNullOrEmpty(request.Target)) return BadRequest("Target is required");

            // Log the target being tested (no restrictions - accepts any target)
            Console.WriteLine($"[AgentController] Testing target: {request.Target}, Type: {request.MetricType ?? "PING"}");
            
            var sw = Stopwatch.StartNew();
            
            // ตัวแปรสำหรับเก็บลง Database
            string status = "Success";
            float value = 0; // ใช้เก็บ Latency หรือ Response Time
            float packetLoss = 0;
            int? statusCode = null;
            string metricType = request.MetricType ?? "PING"; // ใช้ MetricType จาก request หรือ default เป็น PING
            string message = "Manual test executed";

            try
            {
                // 🌐 กรณีเป็น HTTP (เว็บ) หรือ MetricType เป็น HTTP
                if (request.Target.StartsWith("http") || metricType == "HTTP")
                {
                    metricType = "HTTP";
                    // ถ้า target ไม่มี http:// หรือ https:// ให้เพิ่ม https:// อัตโนมัติ
                    string httpTarget = request.Target;
                    if (!httpTarget.StartsWith("http://") && !httpTarget.StartsWith("https://"))
                    {
                        httpTarget = "https://" + httpTarget;
                    }
                    
                    try
                    {
                        Console.WriteLine($"[HTTP] Testing: {httpTarget}");
                        
                        // ใช้ GetAsync ธรรมดา (headers ตั้งไว้แล้วใน constructor)
                        var response = await _httpClient.GetAsync(httpTarget);
                        sw.Stop();
                        
                        Console.WriteLine($"[HTTP] Response: Status={response.StatusCode}, Time={sw.ElapsedMilliseconds}ms");

                        status = response.IsSuccessStatusCode ? "Success" : "Investigate";
                        value = sw.ElapsedMilliseconds; // เก็บ Response Time
                        statusCode = (int)response.StatusCode;
                        message = response.IsSuccessStatusCode 
                            ? $"HTTP {statusCode} - {response.ReasonPhrase}" 
                            : $"HTTP {statusCode} - {response.ReasonPhrase}";
                        packetLoss = response.IsSuccessStatusCode ? 0 : 1;
                    }
                    catch (TaskCanceledException)
                    {
                        sw.Stop();
                        status = "Investigate";
                        value = sw.ElapsedMilliseconds > 0 ? sw.ElapsedMilliseconds : 0;
                        packetLoss = 1;
                        message = $"HTTP Timeout: Request took longer than 15 seconds";
                    }
                    catch (HttpRequestException httpEx)
                    {
                        sw.Stop();
                        status = "Investigate";
                        value = sw.ElapsedMilliseconds > 0 ? sw.ElapsedMilliseconds : 0;
                        packetLoss = 1;
                        message = $"HTTP Error: {httpEx.Message}";
                    }
                    catch (Exception httpErr)
                    {
                        sw.Stop();
                        status = "Investigate";
                        value = sw.ElapsedMilliseconds > 0 ? sw.ElapsedMilliseconds : 0;
                        packetLoss = 1;
                        message = $"HTTP Request Failed: {httpErr.Message}";
                    }
                }
                // 🔵 กรณีเป็น TRACEROUTE
                else if (metricType == "TRACEROUTE")
                {
                    metricType = "TRACEROUTE";
                    using var ping = new Ping();
                    var hops = new List<object>();
                    int maxHops = 30;
                    
                    for (int ttl = 1; ttl <= maxHops; ttl++)
                    {
                        var options = new PingOptions(ttl, true);
                        var buffer = new byte[32];
                        var timeout = 1000;

                        try
                        {
                            var reply = await ping.SendPingAsync(request.Target, timeout, buffer, options);
                            hops.Add(new { 
                                hop = ttl, 
                                ip = reply.Address?.ToString() ?? "*", 
                                status = reply.Status.ToString(), 
                                time = reply.RoundtripTime 
                            });

                            if (reply.Status == IPStatus.Success)
                            {
                                status = "Success";
                                break;
                            }
                        }
                        catch
                        {
                            hops.Add(new { hop = ttl, ip = "*", status = "TimedOut", time = 0 });
                        }
                    }
                    
                    sw.Stop();
                    value = sw.ElapsedMilliseconds;
                    message = $"Traceroute completed with {hops.Count} hops";
                    packetLoss = status == "Success" ? 0 : 1;
                    
                    // เก็บข้อมูล hops ลง ExtraData
                    var extraData = JsonSerializer.Serialize(new 
                    { 
                        Source = "Manual Run (Web Console)",
                        SelectedAgent = request.AgentId,
                        Message = message,
                        TotalHops = hops.Count,
                        Hops = hops
                    });
                    
                    // บันทึกลง Database
                    var tracerouteMetric = new NetworkMetric
                    {
                        Target = request.Target.Trim(),
                        MetricType = metricType,
                        Value = value,
                        PacketLoss = packetLoss,
                        StatusCode = null,
                        Status = status,
                        ExtraData = extraData,
                        Timestamp = DateTime.UtcNow
                    };

                    _context.NetworkMetrics.Add(tracerouteMetric);
                    await _context.SaveChangesAsync();

                    return Ok(new 
                    {
                        target = request.Target,
                        message = message,
                        status = status,
                        timestamp = tracerouteMetric.Timestamp,
                        latency = value + " ms",
                        hops = hops.Count
                    });
                }
                // 📡 กรณีเป็น Ping (IP หรือ Domain)
                else
                {
                    metricType = "PING";
                    using var ping = new Ping();
                    try
                    {
                        // ตั้ง timeout 5 วินาที
                        int timeout = 5000;
                        var reply = await ping.SendPingAsync(request.Target, timeout);
                        sw.Stop();

                        if (reply.Status == IPStatus.Success)
                        {
                            status = "Success";
                            value = reply.RoundtripTime; // เก็บ Latency
                            packetLoss = 0;
                            message = $"Ping Reply Received from {reply.Address}";
                        }
                        else
                        {
                            status = "Investigate";
                            value = 0;
                            packetLoss = 1; // Loss 100%
                            message = $"Ping Failed: {reply.Status}";
                        }
                    }
                    catch (PingException pingEx)
                    {
                        sw.Stop();
                        status = "Investigate";
                        value = 0;
                        packetLoss = 1;
                        message = $"Ping Exception: {pingEx.Message}";
                    }
                    catch (Exception pingErr)
                    {
                        sw.Stop();
                        status = "Investigate";
                        value = 0;
                        packetLoss = 1;
                        message = $"Ping Error: {pingErr.Message}";
                    }
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                status = "Investigate"; 
                message = $"General Error: {ex.Message}";
                packetLoss = 1;
                value = 0;
                // Log the full exception for debugging
                Console.WriteLine($"Error in RunManualTest: {ex}");
            }

            // --- 💾 บันทึกลงตาราง NetworkMetrics ---
            // ✅ บันทึก target ตามที่ user ใส่มา (ไม่มีการจำกัด target)
            var metric = new NetworkMetric
            {
                Target = request.Target.Trim(), // Trim whitespace but keep original target
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
                    Message = message,
                    OriginalTarget = request.Target // Keep original for reference
                }),
                Timestamp = DateTime.UtcNow
            };

            _context.NetworkMetrics.Add(metric);
            await _context.SaveChangesAsync();
            
            Console.WriteLine($"[AgentController] Saved metric: Target={metric.Target}, Status={status}, Value={value}ms");
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
        public string? MetricType { get; set; } // เพิ่ม MetricType เพื่อให้สามารถระบุประเภท test ได้
    }
}