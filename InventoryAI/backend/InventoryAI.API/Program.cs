using System.Text;
using InventoryAI.API.Data;
using InventoryAI.API.Repositories.Implementations;
using InventoryAI.API.Repositories.Interfaces;
using InventoryAI.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Force port 5000 (override Visual Studio's random port) ───
builder.WebHost.UseUrls("http://localhost:5000", "https://localhost:5001");

// ── Database ─────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Repositories ─────────────────────────────────────────────
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();

// ── Services ─────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAIService, AIService>();
builder.Services.AddHttpClient("ollama");

// ── JWT Auth ─────────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                                         Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

// ── CORS ─────────────────────────────────────────────────────
builder.Services.AddCors(opt => opt.AddPolicy("AllowAngular", p =>
    p.WithOrigins(
        "http://localhost:4200",
        "https://localhost:4200"
     )
     .AllowAnyHeader()
     .AllowAnyMethod()));

// ── Controllers + Swagger ────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        // Prevent circular reference serialization errors
        opt.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        opt.JsonSerializerOptions.WriteIndented = false;
        opt.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "InventoryAI API",
        Version = "v1",
        Description = "AI-Powered Inventory Management — RAG + Ollama"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Description = "Enter: Bearer {your_token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference
                { Type = ReferenceType.SecurityScheme, Id = "Bearer" }},
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ── Auto-fix admin password hash on startup ───────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.EnsureCreated();

        // Fix placeholder password hash → real BCrypt hash
        var admin = db.Users.FirstOrDefault(u => u.Username == "admin");
        if (admin != null && !admin.PasswordHash.StartsWith("$2"))
        {
            admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
            db.SaveChanges();
            Console.WriteLine("✅ Admin password set to: Admin@123");
        }
        Console.WriteLine("✅ Database connection OK");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️  DB: {ex.Message}");
        Console.WriteLine("   → Run sql/01_schema.sql in SSMS first, then restart");
    }
}

// ── Middleware ────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "InventoryAI v1");
    c.RoutePrefix = "swagger";
});

app.UseStaticFiles();
app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Redirect root → swagger so browser shows something useful
app.MapGet("/", () => Results.Redirect("/swagger"));

Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
Console.WriteLine("🚀  API        : http://localhost:5000");
Console.WriteLine("📖  Swagger    : http://localhost:5000/swagger");
Console.WriteLine("🤖  Ollama     : http://localhost:11434");
Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

app.Run();




//using System.Text;
//using InventoryAI.API.Data;
//using InventoryAI.API.Repositories.Implementations;
//using InventoryAI.API.Repositories.Interfaces;
//using InventoryAI.API.Services;
//using Microsoft.AspNetCore.Authentication.JwtBearer;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.IdentityModel.Tokens;
//using Microsoft.OpenApi.Models;

//var builder = WebApplication.CreateBuilder(args);

//// ── Force port 5000 (override Visual Studio's random port) ───
//builder.WebHost.UseUrls("http://localhost:5000", "https://localhost:5001");

//// ── Database ─────────────────────────────────────────────────
//builder.Services.AddDbContext<AppDbContext>(opt =>
//    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//// ── Repositories ─────────────────────────────────────────────
//builder.Services.AddScoped<IProductRepository, ProductRepository>();
//builder.Services.AddScoped<IOrderRepository, OrderRepository>();
//builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
//builder.Services.AddScoped<IUserRepository, UserRepository>();
//builder.Services.AddScoped<IReportRepository, ReportRepository>();
//builder.Services.AddScoped<IChatRepository, ChatRepository>();

//// ── Services ─────────────────────────────────────────────────
//builder.Services.AddScoped<IAuthService, AuthService>();
//builder.Services.AddScoped<IAIService, AIService>();
//builder.Services.AddHttpClient("ollama");

//// ── JWT Auth ─────────────────────────────────────────────────
//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddJwtBearer(opt =>
//    {
//        opt.TokenValidationParameters = new TokenValidationParameters
//        {
//            ValidateIssuer = true,
//            ValidateAudience = true,
//            ValidateLifetime = true,
//            ValidateIssuerSigningKey = true,
//            ValidIssuer = builder.Configuration["Jwt:Issuer"],
//            ValidAudience = builder.Configuration["Jwt:Audience"],
//            IssuerSigningKey = new SymmetricSecurityKey(
//                                         Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
//        };
//    });

//builder.Services.AddAuthorization();

//// ── CORS ─────────────────────────────────────────────────────
//builder.Services.AddCors(opt => opt.AddPolicy("AllowAngular", p =>
//    p.WithOrigins("http://localhost:4200")
//     .AllowAnyHeader()
//     .AllowAnyMethod()));

//// ── Controllers + Swagger ────────────────────────────────────
//builder.Services.AddControllers()
//    .AddJsonOptions(opt =>
//    {
//        // Prevent circular reference serialization errors
//        opt.JsonSerializerOptions.ReferenceHandler =
//            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
//        opt.JsonSerializerOptions.WriteIndented = false;
//        opt.JsonSerializerOptions.PropertyNamingPolicy =
//            System.Text.Json.JsonNamingPolicy.CamelCase;
//    });
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen(c =>
//{
//    c.SwaggerDoc("v1", new OpenApiInfo
//    {
//        Title = "InventoryAI API",
//        Version = "v1",
//        Description = "AI-Powered Inventory Management — RAG + Ollama"
//    });
//    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
//    {
//        In = ParameterLocation.Header,
//        Name = "Authorization",
//        Type = SecuritySchemeType.ApiKey,
//        Description = "Enter: Bearer {your_token}"
//    });
//    c.AddSecurityRequirement(new OpenApiSecurityRequirement
//    {
//        {
//            new OpenApiSecurityScheme { Reference = new OpenApiReference
//                { Type = ReferenceType.SecurityScheme, Id = "Bearer" }},
//            Array.Empty<string>()
//        }
//    });
//});

//var app = builder.Build();

//// ── Auto-fix admin password hash on startup ───────────────────
//using (var scope = app.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//    try
//    {
//        db.Database.EnsureCreated();

//        // Fix placeholder password hash → real BCrypt hash
//        var admin = db.Users.FirstOrDefault(u => u.Username == "admin");
//        if (admin != null && !admin.PasswordHash.StartsWith("$2"))
//        {
//            admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
//            db.SaveChanges();
//            Console.WriteLine("✅ Admin password set to: Admin@123");
//        }
//        Console.WriteLine("✅ Database connection OK");
//    }
//    catch (Exception ex)
//    {
//        Console.WriteLine($"⚠️  DB: {ex.Message}");
//        Console.WriteLine("   → Run sql/01_schema.sql in SSMS first, then restart");
//    }
//}

//// ── Middleware ────────────────────────────────────────────────
//app.UseSwagger();
//app.UseSwaggerUI(c =>
//{
//    c.SwaggerEndpoint("/swagger/v1/swagger.json", "InventoryAI v1");
//    c.RoutePrefix = "swagger";
//});

//app.UseStaticFiles();
//app.UseCors("AllowAngular");
//app.UseAuthentication();
//app.UseAuthorization();
//app.MapControllers();

//// Redirect root → swagger so browser shows something useful
//app.MapGet("/", () => Results.Redirect("/swagger"));

//Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
//Console.WriteLine("🚀  API        : http://localhost:5000");
//Console.WriteLine("📖  Swagger    : http://localhost:5000/swagger");
//Console.WriteLine("🤖  Ollama     : http://localhost:11434");
//Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

//app.Run();
