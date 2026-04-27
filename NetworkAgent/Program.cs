using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Http.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json; 

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddFilter("System.Net.Http", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.Extensions.Http", LogLevel.Warning);
builder.Services.AddHostedService<Worker>();
builder.Services.AddHttpClient();
var host = builder.Build();
host.Run();

// --- Models ---
public class NetworkMetric
{
    public string Target { get; set; } = string.Empty;
    public string MetricType { get; set; } = "PING"; 
    public float Value { get; set; } 
    public float PacketLoss { get; set; }
    public int? StatusCode { get; set; }
    public string Status { get; set; } = "Success";
    public string ExtraData { get; set; } = "{}";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow; 
}

// ✅ Class ใหม่: เอาไว้รับรายชื่อ Target ที่ Backend ส่งมา
public class TargetConfig 
{
    public string Target { get; set; }
    public string MetricType { get; set; }
}

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly HttpClient _httpClient;
    
    private static readonly string BackendUrl =
        $"{Environment.GetEnvironmentVariable("BACKEND_URL") ?? "http://backend:5000"}/api/metrics";
    private static readonly string TargetApiUrl =
        $"{Environment.GetEnvironmentVariable("BACKEND_URL") ?? "http://backend:5000"}/api/metrics/targets";

    private readonly string AgentName = Environment.GetEnvironmentVariable("AGENT_NAME") ?? "Unknown-Agent";
    private readonly string AgentMode = Environment.GetEnvironmentVariable("AGENT_MODE") ?? "ALL"; 

    public Worker(ILogger<Worker> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"{AgentName} started. Waiting for targets from Web History...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try 
            {
                // 1. 🔄 วิ่งไปถาม Backend ว่า "มีอะไรให้ทำบ้าง?"
                List<TargetConfig> targets = new();
                try 
                {
                    targets = await _httpClient.GetFromJsonAsync<List<TargetConfig>>(TargetApiUrl) ?? new();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Could not fetch targets: {ex.Message}. Using existing DB data next time.");
                }

                if (targets.Count > 0)
                {
                    _logger.LogInformation($"Found {targets.Count} targets in history. Processing...");

                    foreach (var item in targets)
                    {
                        // 2. 🧹 กรองงาน: ถ้าฉันเป็น Agent PING ฉันจะไม่ทำ HTTP (ยกเว้นโหมด ALL)
                        if (AgentMode != "ALL" && item.MetricType != AgentMode) continue;

                        var metric = CreateMetric(item.Target, item.MetricType);

                        // 3. 🚀 เริ่มทำงานตามประเภท
                        if (item.MetricType == "PING") await RunPing(metric);
                        else if (item.MetricType == "HTTP") await RunHttp(metric);
                        else if (item.MetricType == "TRACEROUTE") await RunTraceroute(metric);

                        // 4. 📨 ส่งผลลัพธ์กลับ
                        await SendData(metric);
                    }
                }
                else
                {
                    _logger.LogInformation("History is empty. Please run a manual test on the website first.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Critical Loop Error: {ex.Message}");
            }

            // รอ 10 วินาที ก่อนวนรอบใหม่
            await Task.Delay(10000, stoppingToken);
        }
    }

    // --- (ส่วนที่เหลือ: CreateMetric, RunPing, RunHttp, RunTraceroute, SendData) ---
    // --- ก๊อปปี้ฟังก์ชันเดิมข้างล่างมาใส่ตรงนี้ได้เลยครับ ไม่มีการเปลี่ยนแปลง ---
    
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

    private async Task RunPing(NetworkMetric metric)
    {
        using var ping = new Ping();
        try
        {
            var reply = await ping.SendPingAsync(metric.Target);
            metric.Value = reply.Status == IPStatus.Success ? reply.RoundtripTime : 0;
            metric.Status = reply.Status == IPStatus.Success ? "Success" : "Failed";
            metric.PacketLoss = reply.Status == IPStatus.Success ? 0 : 1;
            _logger.LogInformation($"[PING] {metric.Target}: {metric.Status} ({metric.Value}ms)");
        }
        catch (Exception) { metric.Status = "Failed"; metric.PacketLoss = 1; }
    }

    private async Task RunHttp(NetworkMetric metric)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await _httpClient.GetAsync(metric.Target);
            sw.Stop();
            metric.Value = sw.ElapsedMilliseconds;
            metric.StatusCode = (int)response.StatusCode;
            metric.Status = response.IsSuccessStatusCode ? "Success" : "Failed";
            _logger.LogInformation($"[HTTP] {metric.Target}: {metric.Status} ({metric.Value}ms)");
        }
        catch (Exception) { metric.Value = 0; metric.Status = "Failed"; }
    }

    private static readonly string TraceServiceUrl =
        Environment.GetEnvironmentVariable("TRACE_SERVICE_URL") ?? "http://host.docker.internal:5051/trace";

    private async Task RunTraceroute(NetworkMetric metric)
    {
        _logger.LogInformation($"[TRACE] Starting trace to {metric.Target}...");
        var sw = Stopwatch.StartNew();
        var hops = new List<object>();
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(95) };
            var payload = new StringContent(
                JsonSerializer.Serialize(new { target = metric.Target }),
                System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync(TraceServiceUrl, payload);
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            foreach (var h in doc.RootElement.GetProperty("hops").EnumerateArray())
                hops.Add(new {
                    hop    = h.GetProperty("hop").GetInt32(),
                    ip     = h.GetProperty("ip").GetString(),
                    status = h.GetProperty("status").GetString(),
                    time   = h.GetProperty("time").GetInt64()
                });
            metric.Status = "Success";
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"[TRACE] Error: {ex.Message}");
            metric.Status = "Failed";
        }
        sw.Stop();
        metric.Value = sw.ElapsedMilliseconds;
        metric.ExtraData = JsonSerializer.Serialize(new { AgentName = AgentName, TotalHops = hops.Count, Hops = hops });
        _logger.LogInformation($"[TRACE] Finished {metric.Target} ({hops.Count} hops)");
    }

    private async Task SendData(NetworkMetric metric)
    {
        try { await _httpClient.PostAsJsonAsync(BackendUrl, metric); }
        catch (Exception ex) { _logger.LogError($"Send Error: {ex.Message}"); }
    }
}