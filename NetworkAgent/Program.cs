using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Http.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// -----------------------------------------------------------
// ส่วนที่ 1: Main Entry Point (สั่งรันโปรแกรม)
// -----------------------------------------------------------
var builder = Host.CreateApplicationBuilder(args);

// ลงทะเบียน Worker ให้ระบบรู้จัก
builder.Services.AddHostedService<Worker>();
// ลงทะเบียน HttpClient
builder.Services.AddHttpClient();

var host = builder.Build();
host.Run();

// -----------------------------------------------------------
// ส่วนที่ 2: Class Definitions (Logic ของคุณ)
// -----------------------------------------------------------

public class PingMetric
{
    public string Target { get; set; }
    public string TargetType { get; set; } // "ICMP", "HTTP"
    public float LatencyMs { get; set; }
    public float PacketLoss { get; set; }
    public float ResponseTimeMs { get; set; }
    public string AgentName { get; set; } = "Agent-Docker-01";
    public bool IsSuccess { get; set; }
}

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly HttpClient _httpClient;
    
    // ** ตรวจสอบ URL Backend ให้ถูกต้อง (ใช้ชื่อ Service ใน Docker) **
private const string BackendUrl = "http://backend:5000/api/metrics";

    public Worker(ILogger<Worker> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 🟢 กำหนดเป้าหมายตรงนี้ (Hardcode ไปก่อนเพื่อความชัวร์)
        var targets = new[]
        {
            new { Url = "8.8.8.8", Type = "ICMP" },
            new { Url = "1.1.1.1", Type = "ICMP" },
            new { Url = "https://www.google.com", Type = "HTTP" },
            new { Url = "https://www.github.com", Type = "HTTP" }
        };

        _logger.LogInformation("Agent started with {count} targets.", targets.Length);

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var item in targets)
            {
                var metric = new PingMetric { Target = item.Url, TargetType = item.Type };

                if (item.Type == "ICMP") await RunPing(metric);
                else if (item.Type == "HTTP") await RunHttp(metric);

                await SendData(metric);
            }

            // รอ 5 วินาที แล้ววนรอบใหม่
            await Task.Delay(5000, stoppingToken);
        }
    }

    private async Task RunPing(PingMetric metric)
    {
        using var ping = new Ping();
        try
        {
            var reply = await ping.SendPingAsync(metric.Target);
            metric.LatencyMs = reply.Status == IPStatus.Success ? reply.RoundtripTime : 0;
            metric.PacketLoss = reply.Status == IPStatus.Success ? 0 : 1;
            metric.IsSuccess = reply.Status == IPStatus.Success;
        }
        catch
        {
            metric.PacketLoss = 1;
            metric.IsSuccess = false;
        }
    }

    private async Task RunHttp(PingMetric metric)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await _httpClient.GetAsync(metric.Target);
            sw.Stop();
            metric.ResponseTimeMs = sw.ElapsedMilliseconds;
            metric.PacketLoss = response.IsSuccessStatusCode ? 0 : 1;
            metric.IsSuccess = response.IsSuccessStatusCode;
        }
        catch
        {
            metric.PacketLoss = 1;
            metric.ResponseTimeMs = 0;
            metric.IsSuccess = false;
        }
    }

    private async Task SendData(PingMetric metric)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(BackendUrl, metric);
            if (response.IsSuccessStatusCode)
                _logger.LogInformation($"Sent {metric.TargetType} -> {metric.Target}: Success ({metric.LatencyMs + metric.ResponseTimeMs}ms)");
            else
                _logger.LogError($"Failed to send data for {metric.Target}. Status: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Backend connection error: {ex.Message}");
        }
    }
}