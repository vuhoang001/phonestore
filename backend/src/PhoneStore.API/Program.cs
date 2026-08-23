using System.Text;
using System.Threading.RateLimiting;
using PhoneStore.API.Extensions;
using PhoneStore.API.Hubs;
using PhoneStore.API.Middleware;
using PhoneStore.API.Services;
using PhoneStore.Application;
using PhoneStore.Application.Common;
using PhoneStore.Application.Interfaces;
using PhoneStore.Infrastructure;
using PhoneStore.Infrastructure.Data;
using PhoneStore.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---------- Serilog ----------
builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console());

// ---------- Config ----------
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;
builder.Services.Configure<VnPaySettings>(builder.Configuration.GetSection("VnPay"));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<AuthSettings>(builder.Configuration.GetSection("Auth"));
builder.Services.Configure<MinioSettings>(builder.Configuration.GetSection("Minio"));
var seedSettings = builder.Configuration.GetSection("Seed").Get<SeedSettings>() ?? new SeedSettings();

// ---------- Layers ----------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient(); // dùng cho di trú ảnh (tải ảnh nguồn ngoài về MinIO)
builder.Services.AddScoped<ICurrentUser, CurrentUser>();

// ---------- Realtime (SignalR) ----------
builder.Services.AddSignalR();
// Hiện thực đẩy thông báo realtime (tầng Application chỉ biết interface IRealtimeNotifier).
builder.Services.AddScoped<IRealtimeNotifier, SignalRNotifier>();

// ---------- Auth ----------
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret))
        };
        // WebSocket không gửi header Authorization được → cho phép lấy token từ query "access_token"
        // khi gọi tới hub (/hubs/**). SignalR JS client tự thêm access_token vào query.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    ctx.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

// ---------- Rate limiting (chống brute-force / spam theo IP) ----------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // Đăng nhập/đăng ký: 10 request/phút mỗi IP.
    options.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
    // Thanh toán: 20 request/phút mỗi IP.
    options.AddPolicy("payment", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1) }));
});

// ---------- Background jobs ----------
builder.Services.AddHostedService<OrderExpiryService>();

// ---------- CORS ----------
const string CorsPolicy = "AllowFrontend";
builder.Services.AddCors(options => options.AddPolicy(CorsPolicy, policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? new[] { "http://localhost:5173" })
          .AllowAnyHeader().AllowAnyMethod()
          .AllowCredentials())); // SignalR (WebSocket có credential) cần AllowCredentials + origin cụ thể

// ---------- MVC + Swagger ----------
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "PhoneStore API", Version = "v1" });
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập token JWT (không cần tiền tố 'Bearer')."
    };
    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ---------- Migrate + Seed ----------
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var db = sp.GetRequiredService<AppDbContext>();
    // Tạo schema từ model. Khi đã cài dotnet-ef, có thể chuyển sang db.Database.MigrateAsync()
    // sau khi chạy: dotnet ef migrations add InitialCreate
    await db.Database.EnsureCreatedAsync();
    // Schema tạo trọn từ model qua EnsureCreated → không cần vá cột thủ công cho DB mới.

    await DbSeeder.SeedAsync(db, sp.GetRequiredService<IPasswordHasher>(), seedSettings);
}

// ---------- Uploads: phục vụ ảnh tĩnh từ wwwroot/uploads (ảnh lưu local) ----------
var uploadsRoot = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "uploads", "user");
Directory.CreateDirectory(uploadsRoot); // đảm bảo thư mục tồn tại

// ---------- Pipeline ----------
app.UseMiddleware<ExceptionMiddleware>();
app.UseStaticFiles(); // phục vụ /uploads/** từ wwwroot

// Swagger chỉ mở ở môi trường phát triển, tránh lộ API map ra production.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PhoneStore API v1"));
}
else
{
    app.UseHsts();
}

app.UseCors(CorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications"); // kênh thông báo realtime
app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
