using PhoneStore.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace PhoneStore.Infrastructure.Services;

/// <summary>Gửi email "giả lập" — ghi ra log. Dùng cho demo khi chưa cấu hình SMTP thật.</summary>
public class LogEmailSender : IEmailSender
{
    private readonly ILogger<LogEmailSender> _logger;
    public LogEmailSender(ILogger<LogEmailSender> logger) => _logger = logger;

    public Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        _logger.LogInformation("📧 EMAIL (demo) → {To}\n   Tiêu đề: {Subject}\n   Nội dung:\n{Body}",
            toEmail, subject, htmlBody);
        return Task.CompletedTask;
    }
}
