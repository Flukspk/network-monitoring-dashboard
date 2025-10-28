using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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


app.UseCors("AllowAll");

// Test route for browser
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

public record UserLoginDto(string Username, string Password);
