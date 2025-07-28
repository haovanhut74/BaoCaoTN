namespace MyWebApp.Interface.Service;

public interface IEmailSender
{
    // Hàm gửi email(mail gửi, tiêu đề, nội dung)
    Task SendEmailAsync(string email, string subject, string message);
}