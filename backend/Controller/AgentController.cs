using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq; 
using Backend.Data;
using Backend.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/agent")]
    public class AgentController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly BackendDbContext _context;

        // 🚨 1. ใส่ Channel Access Token ของบอท NAPMA BOT ตรงนี้ (Token ยาวๆ)
        private const string BOT_ACCESS_TOKEN = "/5h2TXa+/hD36X32Vp8QeEhFMfyA+iVT7xjqZlzs0H0PE7np1Dpj46Eqq6q1Vc7me9GpvqHGhLHbAL+DI5wjfnXetcAUbcBiVyq9YSM1gqHN4JKP7Ja7enH3jH6bR93slgyRAnWCKmXmxZ2U0mDUvgdB04t89/1O/w1cDnyilFU="; 

        public AgentController(BackendDbContext context)
        {
            _context = context;
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 5,
                UseCookies = false,
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true 
            };

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(15)
            };

            _httpClient.DefaultRequestHeaders.Add("User-Agent", "NetworkMonitor/1.0");
        }

        // ==========================================
        // 1️⃣ Run Single Test (Manual Run)
        // ==========================================
        [HttpPost("run")]
        public async Task<IActionResult> RunManualTest([FromBody] ManualTestRequest request)
        {
            if (string.IsNullOrEmpty(request.Target)) return BadRequest("Target is required");

            Console.WriteLine($"[ManualTest] Target: {request.Target}, Type: {request.MetricType ?? "PING"}");

            // ✅ ดึง User ID จาก Database มารอไว้ก่อน
            var setting = _context.NotificationSettings.FirstOrDefault();

            var sw = Stopwatch.StartNew();
            string status = "Success";
            float value = 0;
            float packetLoss = 0;
            int? statusCode = null;
            string metricType = request.MetricType?.ToUpper() ?? "PING";
            string message = "Manual test executed";
            object extraDetail = null;

            try
            {
                // 🌐 HTTP Mode
                if (request.Target.StartsWith("http") || metricType == "HTTP")
                {
                    metricType = "HTTP";
                    string httpTarget = request.Target;
                    if (!httpTarget.StartsWith("http")) httpTarget = "https://" + httpTarget;

                    try
                    {
                        var response = await _httpClient.GetAsync(httpTarget);
                        sw.Stop();
                        status = response.IsSuccessStatusCode ? "Success" : "Investigate";
                        value = sw.ElapsedMilliseconds;
                        statusCode = (int)response.StatusCode;
                        packetLoss = response.IsSuccessStatusCode ? 0 : 1;
                        message = $"HTTP {statusCode} ({response.ReasonPhrase})";
                    }
                    catch (Exception ex)
                    {
                        sw.Stop();
                        status = "Investigate";
                        packetLoss = 1;
                        message = $"HTTP Error: {ex.Message}";
                    }
                }
                // 🛤️ Traceroute Mode
                else if (metricType == "TRACEROUTE")
                {
                    using var ping = new Ping();
                    var hops = new List<object>();
                    int maxHops = 30;
                    string destIp = request.Target;

                    try
                    {
                        var ips = await System.Net.Dns.GetHostAddressesAsync(request.Target);
                        destIp = ips.FirstOrDefault()?.ToString() ?? request.Target;
                    }
                    catch { }

                    for (int ttl = 1; ttl <= maxHops; ttl++)
                    {
                        var options = new PingOptions(ttl, true);
                        var buffer = new byte[32];
                        try
                        {
                            var reply = await ping.SendPingAsync(destIp, 1000, buffer, options);
                            hops.Add(new
                            {
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
                    message = $"Traceroute finished ({hops.Count} hops)";
                    extraDetail = new { Hops = hops };
                }
                // 📡 Ping Mode
                else
                {
                    metricType = "PING";
                    using var ping = new Ping();
                    try
                    {
                        var reply = await ping.SendPingAsync(request.Target, 5000);
                        sw.Stop();
                        if (reply.Status == IPStatus.Success)
                        {
                            status = "Success";
                            value = reply.RoundtripTime;
                            message = $"Reply from {reply.Address}";
                        }
                        else
                        {
                            status = "Investigate";
                            packetLoss = 1;
                            message = $"Ping Failed: {reply.Status}";
                        }
                    }
                    catch (Exception ex)
                    {
                        sw.Stop();
                        status = "Investigate";
                        packetLoss = 1;
                        message = $"Ping Error: {ex.Message}";
                    }
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                status = "Investigate";
                message = $"System Error: {ex.Message}";
            }

            // 🚨 2. ตรวจสอบและส่ง Alert ถ้าสถานะผิดปกติ
            if (status == "Investigate")
            {
                string alertMsg = $"⚠️ Alert: {request.Target} is down!\nType: {metricType}\nError: {message}";
                _ = SendLineNotify(alertMsg, setting); // ส่งแบบ Fire-and-forget ไม่ต้องรอ
            }

            var metric = new NetworkMetric
            {
                Target = request.Target.Trim(),
                MetricType = metricType,
                Value = value,
                PacketLoss = packetLoss,
                StatusCode = statusCode,
                Status = status,
                ExtraData = JsonSerializer.Serialize(new
                {
                    Source = "Manual Run (Web Console)",
                    SelectedAgent = request.AgentId,
                    Message = message,
                    Detail = extraDetail
                }),
                Timestamp = DateTime.UtcNow
            };

            _context.NetworkMetrics.Add(metric);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                target = request.Target,
                status = status,
                latency = $"{value} ms",
                message = message,
                timestamp = metric.Timestamp,
                hops = (extraDetail as dynamic)?.Hops?.Count 
            });
        }

        // ==========================================
        // 2️⃣ Run Batch Test (Multi-Target)
        // ==========================================
        [HttpPost("run-batch")]
        public async Task<IActionResult> RunBatchTest([FromBody] MultiTargetRequest request)
        {
            if (request.Targets == null || request.Targets.Count == 0)
                return BadRequest("Targets list is required");

            Console.WriteLine($"[BatchTest] Processing {request.Targets.Count} targets...");
            
            // ✅ 3. ดึง Setting ออกมา "ก่อน" เข้า Loop Parallel เพื่อป้องกัน DbContext Error
            var setting = _context.NotificationSettings.FirstOrDefault();
            
            var results = new List<object>();

            await Parallel.ForEachAsync(request.Targets, async (target, token) =>
            {
                var sw = Stopwatch.StartNew();
                string status = "Success";
                float value = 0;
                float packetLoss = 0;
                string message = "";
                string metricType = request.MetricType?.ToUpper() ?? "PING";
                int? statusCode = null;
                object extraDetail = null;

                try
                {
                    // 🌐 HTTP
                    if (metricType == "HTTP" || target.StartsWith("http"))
                    {
                        metricType = "HTTP";
                        string url = target.StartsWith("http") ? target : "https://" + target;
                        var response = await _httpClient.GetAsync(url);
                        sw.Stop();
                        statusCode = (int)response.StatusCode;
                        value = sw.ElapsedMilliseconds;
                        status = response.IsSuccessStatusCode ? "Success" : "Investigate";
                        packetLoss = response.IsSuccessStatusCode ? 0 : 1;
                        message = $"HTTP {statusCode} ({response.ReasonPhrase})";
                    }
                    // 🛤️ TRACEROUTE
                    else if (metricType == "TRACEROUTE")
                    {
                        using var ping = new Ping();
                        var hops = new List<object>();
                        int maxHops = 30;
                        string destIp = target;

                        try {
                            var ips = await System.Net.Dns.GetHostAddressesAsync(target);
                            destIp = ips.FirstOrDefault()?.ToString() ?? target;
                        } catch { }

                        for (int ttl = 1; ttl <= maxHops; ttl++)
                        {
                            var options = new PingOptions(ttl, true);
                            var buffer = new byte[32];
                            try
                            {
                                var reply = await ping.SendPingAsync(destIp, 1000, buffer, options);
                                hops.Add(new { hop = ttl, ip = reply.Address?.ToString() ?? "*", status = reply.Status.ToString(), time = reply.RoundtripTime });
                                if (reply.Status == IPStatus.Success) { status = "Success"; break; }
                            }
                            catch {
                                hops.Add(new { hop = ttl, ip = "*", status = "TimedOut", time = 0 });
                            }
                        }
                        sw.Stop();
                        value = sw.ElapsedMilliseconds;
                        message = $"Traceroute finished ({hops.Count} hops)";
                        extraDetail = new { Hops = hops };
                    }
                    // 📡 PING (Default)
                    else 
                    {
                        metricType = "PING";
                        using var ping = new Ping();
                        var reply = await ping.SendPingAsync(target, 5000);
                        sw.Stop();
                        if (reply.Status == IPStatus.Success)
                        {
                            status = "Success";
                            value = reply.RoundtripTime;
                            message = $"Reply from {reply.Address}";
                        }
                        else
                        {
                            status = "Investigate";
                            packetLoss = 1;
                            message = $"Ping Failed: {reply.Status}";
                        }
                    }
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    status = "Investigate";
                    packetLoss = 1;
                    message = ex.Message;
                }

                // 🚨 4. ส่ง Alert (ใช้ค่า setting ที่ดึงมาแล้ว)
                if (status == "Investigate")
                {
                    string alertMsg = $"⚠️ Batch Alert: {target} is down!\nType: {metricType}\nError: {message}";
                    _ = SendLineNotify(alertMsg, setting);
                }

                var metric = new NetworkMetric
                {
                    Target = target.Trim(),
                    MetricType = metricType,
                    Value = value,
                    PacketLoss = packetLoss,
                    StatusCode = statusCode,
                    Status = status,
                    ExtraData = JsonSerializer.Serialize(new { 
                        AgentId = request.AgentId, 
                        Message = message, 
                        Source = "Batch Run",
                        Hops = (extraDetail as dynamic)?.Hops 
                    }),
                    Timestamp = DateTime.UtcNow
                };

                lock (_context)
                {
                    _context.NetworkMetrics.Add(metric);
                }
                lock (results)
                {
                    results.Add(new { target, status, latency = value });
                }
            });

            await _context.SaveChangesAsync();

            return Ok(new { message = $"Completed {results.Count} targets", results = results });
        }

        // ==========================================
        // 🔔 5. ฟังก์ชันส่ง LINE Push Message (NAPMA BOT)
        // ==========================================
        private async Task SendLineNotify(string message, NotificationSetting setting)
        {
            try
            {
                // ตรวจสอบว่าเปิดใช้งานและมี User ID (ที่เก็บในช่อง LineToken) หรือไม่
                if (setting == null || !setting.IsEnable || string.IsNullOrEmpty(setting.LineToken)) 
                {
                    return;
                }

                // ใช้ Token ของบอทที่ Hardcode ไว้
                string botToken = BOT_ACCESS_TOKEN;
                
                // ใช้ User ID ที่ได้จาก DB (ช่อง LineToken)
                string targetUserId = setting.LineToken;

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", botToken);

                var payload = new
                {
                    to = targetUserId,
                    messages = new[]
                    {
                        new { type = "text", text = message }
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                await client.PostAsync("https://api.line.me/v2/bot/message/push", content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Bot Error] Could not send alert: {ex.Message}");
            }
        }
    }

    // 📦 Request Models
    public class ManualTestRequest
    {
        public string AgentId { get; set; }
        public string Target { get; set; }
        public string MetricType { get; set; }
    }

    public class MultiTargetRequest
    {
        public string AgentId { get; set; }
        public List<string> Targets { get; set; }
        public string MetricType { get; set; }
    }
}