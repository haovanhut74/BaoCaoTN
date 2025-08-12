using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using MyWebApp.Interface.Service;
using MyWebApp.Models;

namespace MyWebApp.Repository.Service
{
    public class EmailSender : IEmailSender
    {
        private readonly EmailSettings _settings;

        public EmailSender(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            try
            {
                using var smtpClient = new SmtpClient(_settings.SmtpServer)
                {
                    Port = _settings.SmtpPort,
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_settings.SenderEmail, _settings.SenderPassword),
                    Timeout = 20000
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                    Subject = subject,
                    Body = message,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                throw new Exception($"Gửi email thất bại: {ex.Message}", ex);
            }
        }
    }
}