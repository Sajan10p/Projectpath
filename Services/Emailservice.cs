using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Projectpath.Models;

namespace Projectpath.Services
{
    public class EmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                return;

            try
            {
                using var message = new MailMessage
                {
                    From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                message.To.Add(toEmail);

                using var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
                {
                    Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                    EnableSsl = true
                };

                await client.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email sending failed to {Email}", toEmail);
            }
        }

        public async Task SendEmailToManyAsync(IEnumerable<string?> emails, string subject, string body)
        {
            foreach (var email in emails.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct())
            {
                await SendEmailAsync(email!, subject, body);
            }
        }
    }
}