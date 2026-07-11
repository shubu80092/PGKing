using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PGKing.Application.Interfaces.Services;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace PGKing.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var server = _configuration["SmtpSettings:Server"];
            var portStr = _configuration["SmtpSettings:Port"];
            var senderEmail = _configuration["SmtpSettings:SenderEmail"] ?? "info@pgking.in";
            var senderName = _configuration["SmtpSettings:SenderName"] ?? "PGKing Admin";
            var username = _configuration["SmtpSettings:Username"];
            var password = _configuration["SmtpSettings:Password"];
            var enableSslStr = _configuration["SmtpSettings:EnableSsl"] ?? "true";

            if (string.IsNullOrEmpty(server))
            {
                _logger.LogWarning("SMTP server is not configured. Email to {ToEmail} skipped.", toEmail);
                return;
            }

            int.TryParse(portStr, out int port);
            if (port == 0) port = 587;
            bool.TryParse(enableSslStr, out bool enableSsl);

            using (var mail = new MailMessage())
            {
                mail.From = new MailAddress(senderEmail, senderName);
                mail.To.Add(new MailAddress(toEmail));
                mail.Subject = subject;
                mail.Body = htmlMessage;
                mail.IsBodyHtml = true;

                using (var smtp = new SmtpClient(server, port))
                {
                    smtp.EnableSsl = enableSsl;
                    
                    if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                    {
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = new NetworkCredential(username, password);
                    }
                    else
                    {
                        smtp.UseDefaultCredentials = true;
                    }

                    _logger.LogInformation("Sending email notification to {ToEmail} via {Server}:{Port}...", toEmail, server, port);
                    await smtp.SendMailAsync(mail);
                    _logger.LogInformation("Email notification sent successfully.");
                }
            }
        }
    }
}
