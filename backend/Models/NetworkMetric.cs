// backend/Models/NetworkMetric.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    public class NetworkMetric
    {
        [Key]
        public int Id { get; set; }
        public int? AgentId { get; set; }
        public string Target { get; set; } = string.Empty;
        public string MetricType { get; set; } = string.Empty;
        public float Value { get; set; } 
        public float PacketLoss { get; set; }
        public int? StatusCode { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ExtraData { get; set; } = "{}"; // ใช้ string ธรรมดาไปก่อน
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}