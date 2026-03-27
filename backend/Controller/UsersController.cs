using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Net;
using System.Net.Mail;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly BackendDbContext _context;

        public UsersController(BackendDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1️⃣ GET: api/users (ดึงข้อมูลผู้ใช้)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.UserId,
                    u.Username,
                    u.RoleId,
                    u.CreatedAt,
                    // 💡 ทริค: ถ้ารหัสผ่านขึ้นต้นด้วย INVITE_ แปลว่ายังไม่ได้กดยืนยัน
                    Status = u.Password.StartsWith("INVITE_") ? "Pending" : "Active" 
                })
                .ToListAsync();

            return Ok(users);
        }

        // ==========================================
        // 2️⃣ POST: api/users/invite (ส่งลิงก์เชิญ)
        // ==========================================
        [HttpPost("invite")]
        public async Task<IActionResult> InviteUser([FromBody] InviteUserRequest request)
        {
            if (string.IsNullOrEmpty(request.Username))
                return BadRequest(new { message = "Email is required." });

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (existingUser != null)
                return BadRequest(new { message = "This email is already registered." });

            // 💡 สร้าง Token ลับ สำหรับส่งไปในลิงก์อีเมล
            string inviteToken = "INVITE_" + Guid.NewGuid().ToString("N");

            var newUser = new User
            {
                Username = request.Username.ToLower().Trim(),
                Password = inviteToken, // เซฟ Token ไว้ในช่องรหัสผ่านชั่วคราว
                RoleId = request.RoleId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            try
            {
                // 🚨 ใส่ Email และ App Password ของคุณเรียบร้อยแล้ว!
                string senderEmail = ""; 
                string appPassword = ""; // 👈 ใส่ให้แล้วครับ (ลบเว้นวรรคออกให้ด้วยเพื่อความชัวร์)

                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential(senderEmail, appPassword),
                    EnableSsl = true,
                };

                // 🚨 เปลี่ยน URL นี้ให้ตรงกับหน้าเว็บ Vue.js ของคุณ
                string inviteLink = $"http://localhost:8080/accept-invite?token={inviteToken}";

                var mailMessage = new MailMessage
                {
                    // 💡 ตั้งชื่อให้คนรับเห็นว่าส่งมาจากระบบ No-Reply
                    From = new MailAddress(senderEmail, "Network Monitor (No-Reply)"),
                    Subject = "Action Required: Accept your invitation",
                    Body = $@"
                        <div style='font-family: Arial, sans-serif; padding: 20px; color: #333; max-width: 600px; margin: auto; border: 1px solid #ddd; border-radius: 8px;'>
                            <h2 style='color: #238636;'>Welcome to Network Monitor</h2>
                            <p>Hello <strong>{request.Name}</strong>,</p>
                            <p>You've been invited to join the Network Monitoring Workspace.</p>
                            <br/>
                            <a href='{inviteLink}' style='background-color: #238636; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold; display: inline-block;'>
                                Accept Invitation & Set Password
                            </a>
                            <br/><br/>
                            <p style='font-size: 12px; color: #888;'>If the button doesn't work, copy and paste this link into your browser:<br/>{inviteLink}</p>
                        </div>
                    ",
                    IsBodyHtml = true,
                };
                
                mailMessage.To.Add(request.Username);
                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Email Error]: {ex.Message}");
                return Ok(new { message = "User created but email failed to send. Check SMTP." });
            }

            return Ok(new { message = "Invitation sent successfully." });
        }

        // ==========================================
        // 3️⃣ POST: api/users/accept (พนักงานกดยืนยันรหัสใหม่)
        // ==========================================
        [HttpPost("accept")]
        public async Task<IActionResult> AcceptInvite([FromBody] AcceptInviteRequest request)
        {
            // หา User ที่มี Token ตรงกัน
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Password == request.Token);
            
            if (user == null || !request.Token.StartsWith("INVITE_"))
                return BadRequest(new { message = "Invalid or expired invitation link." });

            if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 6)
                return BadRequest(new { message = "Password must be at least 6 characters long." });

            // อัปเดตรหัสผ่านใหม่ทับ Token เดิม (สถานะจะกลายเป็น Active อัตโนมัติ)
            user.Password = request.NewPassword; 
            await _context.SaveChangesAsync();

            return Ok(new { message = "Account activated! You can now log in." });
        }

        // ==========================================
        // 4️⃣ DELETE: api/users/{id} (ลบผู้ใช้งาน)
        // ==========================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User deleted successfully." });
        }

        // ==========================================
        // 5️⃣ POST: api/users/login (ตรวจสอบการล็อกอิน)
        // ==========================================
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = "Email and Password are required." });
            }

            // ค้นหา User จาก Email และ รหัสผ่านที่ตรงกัน
            var user = await _context.Users.FirstOrDefaultAsync(u => 
                u.Username == request.Username.ToLower().Trim() && 
                u.Password == request.Password);

            if (user == null)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            // ดักไว้เผื่อพนักงานใช้รหัส INVITE_ ล็อกอินโดยที่ยังไม่กดยืนยันในอีเมล
            if (user.Password.StartsWith("INVITE_"))
            {
                return Unauthorized(new { message = "Please check your email to activate your account first." });
            }

            // ล็อกอินสำเร็จ ส่งข้อมูลกลับไปให้ Vue.js ใช้งาน
            return Ok(new 
            { 
                message = "Login successful",
                userId = user.UserId,
                username = user.Username,
                roleId = user.RoleId // ส่ง Role กลับไปด้วย เพื่อเช็คสิทธิ์แสดงผลในหน้าเว็บ
            });
        }
    }

    // 📦 Models
    public class InviteUserRequest
    {
        public string Username { get; set; }
        public string Name { get; set; }
        public int RoleId { get; set; }
    }

    public class AcceptInviteRequest
    {
        public string Token { get; set; }
        public string NewPassword { get; set; }
    }

    // 🔴 โมเดลรับค่าสำหรับล็อกอิน
    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}