using Microsoft.AspNetCore.Mvc;
using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly BackendDbContext _context;

        public AuthController(BackendDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // ตรวจสอบ username + password
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.Password == request.Password);

            if (user == null)
                return Unauthorized(new { message = "Invalid username or password" });

            // ส่งข้อมูลกลับ (ต่อไปอาจเพิ่ม JWT)
            return Ok(new 
            {
                userId = user.UserId,
                username = user.Username,
                roleId = user.RoleId
            });
        }
    }
}
