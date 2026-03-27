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

        // 🚨 Token บอทของคุณ
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

            string metricType = request.MetricType?.ToUpper() ?? "PING";
            string input = request.Target.Trim().ToLower();

            if (metricType == "HTTP" && !input.StartsWith("http://") && !input.StartsWith("https://"))
                return BadRequest("❌ Invalid Target: HTTP test requires 'http://' or 'https://' prefix.");
            
            if ((metricType == "PING" || metricType == "TRACEROUTE") && (input.StartsWith("http://") || input.StartsWith("https://")))
                return BadRequest($"❌ Invalid Target: {metricType} requires an IP or Domain WITHOUT 'http://' prefix (e.g. 8.8.8.8).");

            Console.WriteLine($"[ManualTest] Target: {request.Target}, Type: {metricType}");

            var setting = _context.NotificationSettings.FirstOrDefault();
            var sw = Stopwatch.StartNew();
            string status = "Success";
            float value = 0;
            float packetLoss = 0;
            int? statusCode = null;
            string message = "Manual test executed";
            object extraDetail = null;

            try
            {
                if (metricType == "HTTP")
                {
                    string httpTarget = request.Target;
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
                else
                {
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

            // 🚨 แจ้งเตือนเมื่อล่ม หรือ Latency เกิน Threshold ที่ตั้งไว้
            bool isAlertNeeded = false;
            string alertMsg = "";

            if (status == "Investigate")
            {
                isAlertNeeded = true;
                alertMsg = $"⚠️ Alert: {request.Target} is DOWN!\nType: {metricType}\nError: {message}";
            }
            else if (request.Threshold.HasValue && value > request.Threshold.Value)
            {
                isAlertNeeded = true;
                alertMsg = $"⚠️ Warning: High Latency detected on {request.Target}\nLatency: {value}ms (Threshold: {request.Threshold.Value}ms)\nType: {metricType}";
            }

            if (isAlertNeeded)
            {
                _ = SendLineNotify(alertMsg, setting);
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
                    Source = "Manual Run",
                    SelectedAgent = request.AgentId,
                    Message = message,
                    Threshold = request.Threshold, // บันทึก Threshold ลง DB เผื่อดูย้อนหลัง
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

            string metricType = request.MetricType?.ToUpper() ?? "PING";

            foreach (var t in request.Targets)
            {
                string input = t.Trim().ToLower();
                if (metricType == "HTTP" && !input.StartsWith("http://") && !input.StartsWith("https://"))
                    return BadRequest(new { error = $"❌ Invalid Target '{t}': HTTP test requires 'http://' or 'https://' prefix." });
                
                if ((metricType == "PING" || metricType == "TRACEROUTE") && (input.StartsWith("http://") || input.StartsWith("https://")))
                    return BadRequest(new { error = $"❌ Invalid Target '{t}': {metricType} requires an IP/Domain WITHOUT 'http://' prefix." });
            }

            Console.WriteLine($"[BatchTest] Processing {request.Targets.Count} targets...");
            
            var setting = _context.NotificationSettings.FirstOrDefault();
            var results = new List<object>();

            await Parallel.ForEachAsync(request.Targets, async (target, token) =>
            {
                var sw = Stopwatch.StartNew();
                string status = "Success";
                float value = 0;
                float packetLoss = 0;
                string message = "";
                int? statusCode = null;
                object extraDetail = null;

                try
                {
                    if (metricType == "HTTP")
                    {
                        string url = target.StartsWith("http") ? target : "https://" + target;
                        var response = await _httpClient.GetAsync(url);
                        sw.Stop();
                        statusCode = (int)response.StatusCode;
                        value = sw.ElapsedMilliseconds;
                        status = response.IsSuccessStatusCode ? "Success" : "Investigate";
                        packetLoss = response.IsSuccessStatusCode ? 0 : 1;
                        message = $"HTTP {statusCode} ({response.ReasonPhrase})";
                    }
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
                    else 
                    {
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

                // 🚨 แจ้งเตือนเมื่อล่ม หรือ Latency เกิน Threshold ที่ตั้งไว้ (ใน Batch)
                bool isAlertNeeded = false;
                string alertMsg = "";

                if (status == "Investigate")
                {
                    isAlertNeeded = true;
                    alertMsg = $"⚠️ Alert: {target} is DOWN!\nType: {metricType}\nError: {message}";
                }
                else if (request.Threshold.HasValue && value > request.Threshold.Value)
                {
                    isAlertNeeded = true;
                    alertMsg = $"⚠️ Warning: High Latency detected on {target}\nLatency: {value}ms (Threshold: {request.Threshold.Value}ms)\nType: {metricType}";
                }

                if (isAlertNeeded)
                {
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
                        Threshold = request.Threshold, // บันทึก Threshold ลง DB เผื่อดูย้อนหลัง
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
                if (setting == null || !setting.IsEnable || string.IsNullOrEmpty(setting.LineToken)) 
                {
                    return;
                }

                string botToken = BOT_ACCESS_TOKEN;
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

    // 📦 Request Models (🚨 เพิ่ม Threshold ตรงนี้)
    public class ManualTestRequest
    {
        public string AgentId { get; set; }
        public string Target { get; set; }
        public string MetricType { get; set; }
        public int? Threshold { get; set; } // ค่าที่จะรับมาจาก Vue.js
    }

    public class MultiTargetRequest
    {
        public string AgentId { get; set; }
        public List<string> Targets { get; set; }
        public string MetricType { get; set; }
        public int? Threshold { get; set; } // ค่าที่จะรับมาจาก Vue.js
    }
}