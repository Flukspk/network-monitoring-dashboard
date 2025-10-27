using Backend.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// อ่าน connection string จาก appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// เพิ่ม DbContext
builder.Services.AddDbContext<BackendDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

// ตัวอย่าง route สำหรับทดสอบ
app.MapGet("/", async (BackendDbContext db) =>
{
    return await db.Users.ToListAsync();
});

app.Run();
