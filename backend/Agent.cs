using System;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models
{
    public class Agent
    {
        [Key]
        public int AgentId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty; // เช่น "Core-Agent-BKK"

        public string Location { get; set; } = string.Empty; // เช่น "Bangkok"

        public string Status { get; set; } = "Offline"; // "Online", "Offline"

        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    }
}