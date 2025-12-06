using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Http;
using System.Threading.Tasks;
using System;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/agent")] 
    public class AgentController : ControllerBase
    {
        private readonly HttpClient _httpClient = new HttpClient();

        [HttpPost("run")]
        public async Task<IActionResult> RunManualTest([FromBody] ManualTestRequest request)
        {
            if (string.IsNullOrEmpty(request.Target)) return BadRequest("Target is required");

            var sw = Stopwatch.StartNew();
            string status = "Success";
            string latency = "0";

            try
            {
                if (request.Target.StartsWith("http"))
                {
                    var response = await _httpClient.GetAsync(request.Target);
                    status = response.IsSuccessStatusCode ? "Success" : "Investigate";
                }
                else
                {
                    using var ping = new Ping();
                    var reply = await ping.SendPingAsync(request.Target);
                    status = reply.Status == IPStatus.Success ? "Success" : "Investigate";
                    latency = reply.Status == IPStatus.Success ? reply.RoundtripTime.ToString() : "0";
                }
            }
            catch
            {
                status = "Investigate"; 
            }
            
            sw.Stop();
            if (latency == "0") latency = sw.ElapsedMilliseconds.ToString();

            return Ok(new 
            {
                target = request.Target,
                message = status == "Success" ? "Latency check passed" : "Connection timed out or failed",
                status = status,
                timestamp = DateTime.UtcNow,
                latency = latency + " ms"
            });
        }
    }

    public class ManualTestRequest
    {
        public string AgentId { get; set; }
        public string Target { get; set; }
    }
}