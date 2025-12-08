using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using System.Threading.Tasks;
using System.Linq;

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

        // GET: api/users
        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            // ดึงข้อมูล User ทั้งหมด (เลือกเฉพาะ field ที่ปลอดภัย ไม่เอา Password)
            var users = await _context.Users
                .Select(u => new
                {
                    u.UserId,
                    u.Username, // ในระบบเราใช้ Username เป็น Email
                    u.RoleId,
                    u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }
    }
}