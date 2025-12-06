using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using System; // เพิ่มสำหรับ Console และ Thread
using System.Linq;

var builder = WebApplication.CreateBuilder(args);

// 1. [สำคัญมาก] ต้องเพิ่มบรรทัดนี้ เพื่อให้ระบบรู้จัก MetricsController ที่เราสร้างไว้
builder.Services.AddControllers();

// PostgreSQL connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<BackendDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add CORS policy
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

// 2. [แนะนำ] เพิ่มระบบ Retry Migration (กัน Database Error ตอนเริ่ม Docker)
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

            // Seeding User (โค้ดเดิมของคุณ)
            if (!db.Users.Any(u => u.Username == "Intouch@gmail.com"))
            {
                db.Users.Add(new User
                {
                    Username = "Intouch@gmail.com",
                    Password = "123456",
                    RoleId = 1,
                    CreatedAt = DateTime.UtcNow
                });
                db.SaveChanges();
                logger.LogInformation("Default user created.");
            }
            
            logger.LogInformation("Database migration completed successfully.");
            break; // ทำสำเร็จก็ออกจาก Loop
        }
        catch (Exception ex)
        {
            retries--;
            logger.LogWarning($"Migration failed: {ex.Message}. Waiting 3 seconds... ({retries} retries left)");
            System.Threading.Thread.Sleep(3000); // รอ 3 วินาทีแล้วลองใหม่
        }
    }
}

app.UseCors("AllowAll");

// 3. [สำคัญมาก] สั่งให้ระบบใช้งาน Controllers (MetricsController จะเริ่มทำงานตรงนี้)
app.MapControllers();

// Test route for browser
app.MapGet("/", () => "Backend is running!");

// Login endpoint (อันเดิมของคุณ - ใช้ร่วมกันได้ไม่มีปัญหา)
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

public record UserLoginDto(string Username, string Password);