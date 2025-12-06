using System;
using System.Text.Json.Serialization;

namespace Backend.Models
{
    public class PingMetric
    {
        public int Id { get; set; }
        public string Target { get; set; } = null!;
        public string TargetType { get; set; } = null!;
        public float LatencyMs { get; set; }
        public float PacketLoss { get; set; }
        public float ResponseTimeMs { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // optional: allow storing extra JSON
        public string Extra { get; set; } = null!;

        // เพิ่มสองตัวนี้เพื่อให้ Controller ใช้งานได้
        public string Status { get; set; } = null!;
        public string Message { get; set; } = null!;
    }
}
