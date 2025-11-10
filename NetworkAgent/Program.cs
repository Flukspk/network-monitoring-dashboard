using System;
using System.Net.NetworkInformation;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        string target = "8.8.8.8"; // เป้าหมายที่อยาก ping (Google DNS)
        string backendUrl = "http://localhost:5001/api/ping"; // API backend ของคุณ

        using var ping = new Ping();
        using var httpClient = new HttpClient();

        Console.WriteLine($"🔄 Starting ping to {target}...");

        while (true)
        {
            try
            {
                var reply = await ping.SendPingAsync(target, 1000);
                if (reply.Status == IPStatus.Success)
                {
                    Console.WriteLine($"✅ Reply from {target}: {reply.RoundtripTime} ms");

                    var metric = new
                    {
                        Target = target,
                        Latency = reply.RoundtripTime,
                        Timestamp = DateTime.UtcNow
                    };

                    string json = JsonSerializer.Serialize(metric);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    await httpClient.PostAsync(backendUrl, content);
                }
                else
                {
                    Console.WriteLine($"❌ Ping failed: {reply.Status}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error: {ex.Message}");
            }

            await Task.Delay(5000); // ping ทุก 5 วินาที
        }
    }
}
