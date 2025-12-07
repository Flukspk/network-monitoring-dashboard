using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Http.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json; 

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddHttpClient();
var host = builder.Build();
host.Run();

// --- Models (แก้ให้ตรงกับ Backend เป๊ะๆ) ---
public class NetworkMetric
{
    public string Target { get; set; } = string.Empty;
    public string MetricType { get; set; } = "PING"; 
    
    // ✅ ใช้ Value ตัวเดียว (รวม Latency/ResponseTime)
    public float Value { get; set; } 
    
    public float PacketLoss { get; set; }
    public int? StatusCode { get; set; }
    public string Status { get; set; } = "Success";
    
    // ✅ ส่งเป็น JSON String
    public string ExtraData { get; set; } = "{}";
    
    // ✅ ต้องมี Timestamp
    public DateTime Timestamp { get; set; } = DateTime.UtcNow; 
}

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly HttpClient _httpClient;
    
    // URL Backend (พอร์ต 5000)
    private const string BackendUrl = "http://backend:5000/api/metrics"; 

    private readonly string AgentName = Environment.GetEnvironmentVariable("AGENT_NAME") ?? "Unknown-Agent";
    private readonly string AgentMode = Environment.GetEnvironmentVariable("AGENT_MODE") ?? "ALL"; 

    public Worker(ILogger<Worker> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // รายการเป้าหมาย
        var pingTargets = new[] { "8.8.8.8", "1.1.1.1" };
        var httpTargets = new[] { "https://www.google.com", "https://www.github.com" };
        var traceTargets = new[] { "8.8.8.8" };

        _logger.LogInformation($"{AgentName} started in [{AgentMode}] mode.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try 
            {
                // 🔴 Agent 1: PING ONLY
                if (AgentMode == "PING" || AgentMode == "ALL")
                {
                    foreach (var target in pingTargets)
                    {
                        var metric = CreateMetric(target, "PING");
                        await RunPing(metric);
                        await SendData(metric);
                    }
                }

                // 🟢 Agent 2: HTTP ONLY
                if (AgentMode == "HTTP" || AgentMode == "ALL")
                {
                    foreach (var target in httpTargets)
                    {
                        var metric = CreateMetric(target, "HTTP");
                        await RunHttp(metric);
                        await SendData(metric);
                    }
                }

                // 🔵 Agent 3: TRACEROUTE ONLY
                if (AgentMode == "TRACEROUTE" || AgentMode == "ALL")
                {
                    foreach (var target in traceTargets)
                    {
                        var metric = CreateMetric(target, "TRACEROUTE");
                        await RunTraceroute(metric);
                        await SendData(metric);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Critical Loop Error: {ex.Message}");
            }

            // รอ 10 วินาที
            await Task.Delay(10000, stoppingToken);
        }
    }

    private NetworkMetric CreateMetric(string target, string type)
    {
        var baseInfo = new { AgentName = AgentName };
        return new NetworkMetric 
        { 
            Target = target, 
            MetricType = type,
            ExtraData = JsonSerializer.Serialize(baseInfo),
            Timestamp = DateTime.UtcNow
        };
    }

    // --- Logic: PING ---
    private async Task RunPing(NetworkMetric metric)
    {
        using var ping = new Ping();
        try
        {
            var reply = await ping.SendPingAsync(metric.Target);
            // ✅ เก็บค่าลง Value
            metric.Value = reply.Status == IPStatus.Success ? reply.RoundtripTime : 0;
            metric.Status = reply.Status == IPStatus.Success ? "Success" : "Failed";
            metric.PacketLoss = reply.Status == IPStatus.Success ? 0 : 1;
            _logger.LogInformation($"[PING] {metric.Target}: {metric.Status} ({metric.Value}ms)");
        }
        catch (Exception) 
        { 
            metric.Status = "Failed"; 
            metric.PacketLoss = 1; 
        }
    }

    // --- Logic: HTTP ---
    private async Task RunHttp(NetworkMetric metric)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await _httpClient.GetAsync(metric.Target);
            sw.Stop();
            // ✅ เก็บค่าลง Value
            metric.Value = sw.ElapsedMilliseconds;
            metric.StatusCode = (int)response.StatusCode;
            metric.Status = response.IsSuccessStatusCode ? "Success" : "Failed";
            _logger.LogInformation($"[HTTP] {metric.Target}: {metric.Status} ({metric.Value}ms)");
        }
        catch (Exception) 
        { 
            metric.Value = 0; 
            metric.Status = "Failed"; 
        }
    }

    // --- Logic: TRACEROUTE ---
    private async Task RunTraceroute(NetworkMetric metric)
    {
        _logger.LogInformation($"[TRACE] Starting trace to {metric.Target}...");
        var sw = Stopwatch.StartNew();
        var hops = new List<object>();
        
        using var ping = new Ping();
        int maxHops = 30;
        
        try 
        {
            for (int ttl = 1; ttl <= maxHops; ttl++)
            {
                var options = new PingOptions(ttl, true);
                var buffer = new byte[32]; 
                var timeout = 1000;

                PingReply reply;
                try 
                {
                    reply = await ping.SendPingAsync(metric.Target, timeout, buffer, options);
                }
                catch 
                {
                    hops.Add(new { hop = ttl, ip = "*", status = "TimedOut", time = 0 });
                    continue;
                }
                
                hops.Add(new { 
                    hop = ttl, 
                    ip = reply.Address?.ToString() ?? "*", 
                    status = reply.Status.ToString(), 
                    time = reply.RoundtripTime 
                });

                if (reply.Status == IPStatus.Success) 
                {
                    metric.Status = "Success";
                    break; 
                }
            }
            if(metric.Status != "Success") metric.Status = "Success"; 
        }
        catch (Exception ex)
        { 
            metric.Status = "Failed";
            _logger.LogError($"Trace Error: {ex.Message}");
        }
        
        sw.Stop();
        // ✅ เก็บค่าลง Value
        metric.Value = sw.ElapsedMilliseconds; 
        
        metric.ExtraData = JsonSerializer.Serialize(new { 
            AgentName = AgentName, 
            TotalHops = hops.Count,
            Hops = hops 
        });
        
        _logger.LogInformation($"[TRACE] Finished {metric.Target}. Hops: {hops.Count}");
    }

    private async Task SendData(NetworkMetric metric)
    {
        try
        {
            var res = await _httpClient.PostAsJsonAsync(BackendUrl, metric);
            if(!res.IsSuccessStatusCode)
            {
                 // อ่านข้อความ Error จาก Backend มาโชว์เลย จะได้รู้ว่าผิดอะไร
                 var errorMsg = await res.Content.ReadAsStringAsync();
                 _logger.LogError($"Failed to send to Backend: {res.StatusCode}. Msg: {errorMsg}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Backend connection failed: {ex.Message}");
        }
    }
}