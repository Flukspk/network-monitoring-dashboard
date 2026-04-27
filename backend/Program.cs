using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting; // จำเป็นสำหรับ UseUrls

var builder = WebApplication.CreateBuilder(args);

// 🔥 FIX 1: บังคับให้ Kestrel ฟังที่ IP 0.0.0.0 พอร์ต 5000 (สำคัญมากสำหรับ Docker)
builder.WebHost.UseUrls("http://0.0.0.0:5000");

// 1. Add Controllers
builder.Services.AddControllers();

// 2. Database Connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<BackendDbContext>(options =>
    options.UseNpgsql(connectionString));

// 3. CORS (อนุญาตให้ Frontend เข้าถึงได้)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// 4. Auto Migration & Seeding (ระบบสร้างตารางอัตโนมัติพร้อม Retry)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var db = services.GetRequiredService<BackendDbContext>();

    int retries = 10;
    while (retries > 0)
    {
        try
        {
            logger.LogInformation("Attempting database migration...");
            db.Database.Migrate();

            // สร้าง User เริ่มต้นถ้ายังไม่มี
            var defaultUser = db.Users.FirstOrDefault(u => u.Username == "intouch@gmail.com");
            if (defaultUser == null)
            {
                db.Users.Add(new User
                {
                    Username = "intouch@gmail.com",
                    Password = BCrypt.Net.BCrypt.HashPassword("123456"),
                    RoleId = 1,
                    CreatedAt = DateTime.UtcNow
                });
                db.SaveChanges();
                logger.LogInformation("Default user created.");
            }
            else if (!defaultUser.Password.StartsWith("$2"))
            {
                defaultUser.Password = BCrypt.Net.BCrypt.HashPassword(defaultUser.Password);
                db.SaveChanges();
                logger.LogInformation("Default user password migrated to BCrypt.");
            }
            
            logger.LogInformation("Database migration completed successfully.");
            break; 
        }
        catch (Exception ex)
        {
            retries--;
            logger.LogWarning($"Migration failed: {ex.Message}. Waiting 3 seconds... ({retries} retries left)");
            System.Threading.Thread.Sleep(3000); 
        }
    }
}

// 🔥 FIX 2: ปิด HTTPS Redirection (เพราะใน Docker เราใช้ HTTP ธรรมดา)
// app.UseHttpsRedirection(); 

app.UseCors("AllowAll");
app.MapControllers();

// Route สำหรับทดสอบว่า Backend ทำงานอยู่ไหม
app.MapGet("/", () => "Backend is running!");

// Login endpoint
app.MapPost("/api/auth/login", async (BackendDbContext db, UserLoginDto login) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Username == login.Username && u.Password == login.Password);
    if (user == null)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(new
    {
        user.UserId,
        user.Username,
        user.RoleId
    });
});

app.Run();

// DTO สำหรับ Login
public record UserLoginDto(string Username, string Password);