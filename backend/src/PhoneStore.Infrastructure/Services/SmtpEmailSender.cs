using System.Net;
using System.Net.Mail;
using PhoneStore.Application.Common;
using PhoneStore.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace PhoneStore.Infrastructure.Services;

/// <summary>Gửi email thật qua SMTP (Gmail, SendGrid SMTP, Mailtrap...).</summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _cfg;
    public SmtpEmailSender(IOptions<EmailSettings> cfg) => _cfg = cfg.Value;

    public async Task SendAsync(string toEmail, string subject, string htmlBody)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(_cfg.FromEmail, _cfg.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(_cfg.Host, _cfg.Port) { EnableSsl = _cfg.EnableSsl };
        if (!string.IsNullOrEmpty(_cfg.Username))
            client.Credentials = new NetworkCredential(_cfg.Username, _cfg.Password);

        await client.SendMailAsync(message);
    }
}
