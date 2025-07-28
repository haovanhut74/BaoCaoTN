using System.Net;
using System.Net.Mail;
using MyWebApp.Interface.Service;

namespace MyWebApp.Repository.Service;

public class EmailSeneder : IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string message)
    {
        var smtpClient = new SmtpClient("smtp.gmail.com")
        {
            Port = 587,
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential("haovanhut74@gmail.com", "rieo feyg hctk txce"), // app password
        };

        return smtpClient.SendMailAsync(
            new MailMessage()
            {
                From = new MailAddress("haovanhut74@gmail.com"), // Thay thế bằng địa chỉ email của bạn
                To = { email }, // Địa chỉ email người nhận
                Subject = subject, // Tiêu đề email
                Body = message, // Nội dung email
                IsBodyHtml = true, // Cho phép nội dung HTML trong email
            });
    }
}