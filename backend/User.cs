using System;

namespace Backend.Models
{
    public class User
    {
        public int UserId { get; set; }       // primary key
        public string Username { get; set; }  // unique
        public string Password { get; set; }
        public string RoleId { get; set; }    // role_id
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
