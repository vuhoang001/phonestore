using PhoneStore.Application.Common;
using PhoneStore.Application.Interfaces;
using PhoneStore.Infrastructure.Persistence;
using PhoneStore.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PhoneStore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ---- DbContext (PostgreSQL qua Npgsql) + expose IAppDbContext cho tầng Application ----
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // ---- Bind cấu hình dùng ở tầng Infrastructure (Jwt/Email/Minio) ----
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.Configure<EmailSettings>(configuration.GetSection("Email"));
        services.Configure<MinioSettings>(configuration.GetSection("Minio"));

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        // Chọn cách gửi email theo cấu hình: "Smtp" gửi thật, còn lại ghi log (demo).
        var emailProvider = configuration["Email:Provider"] ?? "Log";
        if (string.Equals(emailProvider, "Smtp", StringComparison.OrdinalIgnoreCase))
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        else
            services.AddSingleton<IEmailSender, LogEmailSender>();

        // Lưu ảnh lên MinIO (client thread-safe → singleton).
        services.AddSingleton<IFileStorage, MinioFileStorage>();

        return services;
    }
}
