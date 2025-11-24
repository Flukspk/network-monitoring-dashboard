using System;

namespace Backend.Models
{
    public class User
    {
        public int UserId { get; set; }   
        public string Username { get; set; }  
        public string Password { get; set; }
       public int RoleId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
