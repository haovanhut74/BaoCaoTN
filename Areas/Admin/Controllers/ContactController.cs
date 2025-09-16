using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;
using MyWebApp.Interface.Service;

namespace MyWebApp.Areas.Admin.Controllers;

public class ContactController : BaseController
{
    private readonly IEmailSender _emailSender;

    public ContactController(DataContext context, IEmailSender emailSender) : base(context)
    {
        _emailSender = emailSender;
    }

    // Danh sách liên hệ
    public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10)
    {
        var query = _context.Contacts.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(c => c.Name.Contains(search) || c.Email.Contains(search) || c.Subject.Contains(search));
        }

        var total = await query.CountAsync();
        var contacts = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Search = search;
        ViewBag.Page = page;
        ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);

        return View(contacts);
    }

    // Xem chi tiết
    public async Task<IActionResult> Detail(int id)
    {
        var contact = await _context.Contacts.FindAsync(id);
        if (contact == null) return NotFound();
        return View(contact);
    }

    // Xóa liên hệ
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var contact = await _context.Contacts.FindAsync(id);
        if (contact != null)
        {
            _context.Contacts.Remove(contact);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: Hiển thị form trả lời
    public async Task<IActionResult> Reply(int id)
    {
        var contact = await _context.Contacts.FindAsync(id);
        if (contact == null) return NotFound();
        return View(contact);
    }

    // Gửi trả lời qua email
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int id, string replyMessage)
    {
        var contact = await _context.Contacts.FindAsync(id);
        if (contact == null) return NotFound();

        // Gửi mail
        await _emailSender.SendEmailAsync(
            contact.Email,
            $"Trả lời liên hệ: {contact.Subject}",
            replyMessage
        );

        TempData["Success"] = $"Đã gửi mail trả lời tới {contact.Email}";
        return RedirectToAction(nameof(Detail), new { id });
    }
}