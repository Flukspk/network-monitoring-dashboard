using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    public class NetworkMetric
    {
        [Key]
        public int Id { get; set; }

        public int? AgentId { get; set; }

        [Required]
        public string Target { get; set; } = string.Empty;

        [Required]
        public string MetricType { get; set; } = string.Empty;

        // --- ใช้ Value เป็นค่าหลักสำหรับกราฟเหมือนเดิม ---
        public float Value { get; set; } 

        // --- เพิ่มเจาะจงกลับมาได้ครับ ถ้าต้องการ ---
        public float LatencyMs { get; set; } // เผื่ออยากเก็บเวลา Connect
        public float ResponseTimeMs { get; set; } // เก็บเวลา Load หน้าเว็บ

        public float PacketLoss { get; set; }
        public int? StatusCode { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty;

        [Column(TypeName = "jsonb")] 
        public string ExtraData { get; set; } = "{}";

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}