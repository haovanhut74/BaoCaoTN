using Microsoft.AspNetCore.Mvc;
using MyWebApp.Data;
using MyWebApp.Interface.Service;
using MyWebApp.Models;

namespace MyWebApp.Areas.User.Controllers;

public class ContactController : BaseController
{
    private readonly IEmailSender _emailSender;

    public ContactController(DataContext context, IEmailSender emailSender) : base(context)
    {
        _emailSender = emailSender;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Index(Contact contact)
    {
        if (ModelState.IsValid)
        {
            // Lưu vào database
            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync();
            
            // Gửi email cho Admin
            await _emailSender.SendEmailAsync(
                "haovanhut74@gmail.com",
                $"📩 Liên hệ mới: {contact.Subject}",
                $"""

                     <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; padding: 20px; background-color: #f9f9f9;'>
                         <h2 style='color: #4CAF50; margin-bottom: 10px;'>📬 Thông tin liên hệ mới</h2>
                         <table style='width: 100%; border-collapse: collapse;'>
                             <tr>
                                 <td style='padding: 8px; font-weight: bold; width: 120px;'>👤 Họ và tên:</td>
                                 <td style='padding: 8px; background-color: #fff;'>{contact.Name}</td>
                             </tr>
                             <tr>
                                 <td style='padding: 8px; font-weight: bold;'>📧 Email:</td>
                                 <td style='padding: 8px; background-color: #fff;'>{contact.Email}</td>
                             </tr>
                             <tr>
                                 <td style='padding: 8px; font-weight: bold;'>📌 Chủ đề:</td>
                                 <td style='padding: 8px; background-color: #fff;'>{contact.Subject}</td>
                             </tr>
                         </table>
                         <div style='margin-top: 20px; padding: 15px; background-color: #fff; border-left: 4px solid #4CAF50;'>
                             <p style='margin: 0; font-weight: bold;'>💬 Nội dung tin nhắn:</p>
                             <p style='margin: 5px 0 0 0;'>{contact.Message}</p>
                         </div>
                         <p style='margin-top: 30px; font-size: 12px; color: #888;'>Email này được gửi tự động từ hệ thống liên hệ website.</p>
                     </div>
                     
                 """
            );


            ViewBag.Success = "Cảm ơn bạn! Tin nhắn đã được gửi.";
            ModelState.Clear();
        }

        return View();
    }
}