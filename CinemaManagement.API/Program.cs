using System.Text;
using CinemaManagement.API.Data;
using CinemaManagement.API.Middleware;
using CinemaManagement.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<CinemaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<MembershipService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<JwtTokenService>();

// ----- JWT Authentication -----
var jwtKey = builder.Configuration["Jwt:Key"]
             ?? throw new InvalidOperationException("Thiếu cấu hình Jwt:Key trong appsettings.json");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            RoleClaimType = "role",
            NameClaimType = "username"
        };
    });

builder.Services.AddAuthorization();

// Thư mục chứa ảnh poster
var postersPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "posters");
Directory.CreateDirectory(postersPath);

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        // Tự gắn token từ localStorage (khóa "jwt") vào mọi request
        options.UseRequestInterceptor(
            "(req) => { const t = localStorage.getItem('jwt'); if (t) { req.headers['Authorization'] = 'Bearer ' + t; } return req; }");
    });
}

app.UseHttpsRedirection();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(postersPath),
    RequestPath = "/posters"
});

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();