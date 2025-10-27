using System;

namespace Backend.Models
{
    public class PingMetric
    {
        public int Id { get; set; }              // primary key
        public string Target { get; set; }       // target name or IP
        public string TargetType { get; set; }   // e.g., "ping", "http"
        public float LatencyMs { get; set; }
        public float PacketLoss { get; set; }
        public float ResponseTimeMs { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
