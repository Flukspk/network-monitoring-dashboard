using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models
{
    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        // เชื่อมกับ User (1 ต่อ 1)
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User? User { get; set; }

        public string Channel { get; set; } = "Line"; // "Line", "Telegram"

        // เก็บ Token หรือ ChatID ในรูปแบบ JSON
        [Column(TypeName = "jsonb")]
        public string ConfigData { get; set; } = "{}";

        public bool IsEnabled { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}