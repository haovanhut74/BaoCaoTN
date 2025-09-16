using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;

namespace MyWebApp.Areas.User.Controllers;

public class NewsController : BaseController
{
    public NewsController(DataContext context) : base(context) { }

    // Danh sách bài viết
    public async Task<IActionResult> Index()
    {
        var articles = await _context.News
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
        return View(articles);
    }

    // Chi tiết bài viết
    public async Task<IActionResult> Details(Guid id)
    {
        var article = await _context.News.FindAsync(id);
        if (article == null) return NotFound();
        return View(article);
    }
}