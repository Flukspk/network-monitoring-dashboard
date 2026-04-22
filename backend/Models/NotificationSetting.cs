using System;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    public class NotificationSetting
    {
        [Key]
        public int Id { get; set; } // แทน NotificationId ในรูป
        public int? UserId { get; set; } // เผื่ออนาคตทำระบบ Login
        
        public string? TelegramToken { get; set; }
        public string? TelegramChatId { get; set; }
        public string LineToken { get; set; }
        
        public bool IsEnable { get; set; } = true; // แทน IsRead (ใช้เปิด/ปิดแจ้งเตือนดีกว่า)
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}