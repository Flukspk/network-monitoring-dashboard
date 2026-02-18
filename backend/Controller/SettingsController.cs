using Microsoft.AspNetCore.Mvc;
using Backend.Data;
using Backend.Models;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/settings")]
    public class SettingsController : ControllerBase
    {
        private readonly BackendDbContext _context;

        public SettingsController(BackendDbContext context)
        {
            _context = context;
        }

        // ดึงค่า Setting (เอาตัวแรกเสมอ)
        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotificationSettings()
        {
            var setting = await _context.NotificationSettings.FirstOrDefaultAsync();
            if (setting == null)
            {
                // ถ้ายังไม่มี ให้ส่งค่าว่างๆ กลับไป
                return Ok(new NotificationSetting { IsEnable = true });
            }
            return Ok(setting);
        }

        // บันทึก/แก้ไขค่า Setting
        [HttpPost("notifications")]
        public async Task<IActionResult> SaveNotificationSettings([FromBody] NotificationSetting request)
        {
            var setting = await _context.NotificationSettings.FirstOrDefaultAsync();
            
            if (setting == null)
            {
                // ถ้าไม่มีให้สร้างใหม่
                setting = new NotificationSetting
                {
                    TelegramToken = request.TelegramToken,
                    LineToken = request.LineToken,
                    IsEnable = request.IsEnable,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.NotificationSettings.Add(setting);
            }
            else
            {
                // ถ้ามีแล้วให้อัปเดต
                setting.TelegramToken = request.TelegramToken;
                setting.LineToken = request.LineToken;
                setting.IsEnable = request.IsEnable;
                setting.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(setting);
        }
    }
}