using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;
using MyWebApp.Models;

namespace MyWebApp.Areas.Admin.Controllers;

public class NewsController : BaseController
{
    public NewsController(DataContext context) : base(context) { }

    public async Task<IActionResult> Index()
    {
        var list = await _context.News.OrderByDescending(n => n.CreatedAt).ToListAsync();
        return View(list);
    }

    // Thêm mới
    public IActionResult Create() => View();

    [HttpPost]
    public async Task<IActionResult> Create(News model)
    {
        if (ModelState.IsValid)
        {
            model.Id = Guid.NewGuid();
            model.CreatedAt = DateTime.Now;
            _context.News.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    // Sửa
    public async Task<IActionResult> Edit(Guid id)
    {
        var news = await _context.News.FindAsync(id);
        return news == null ? NotFound() : View(news);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, News model)
    {
        if (id != model.Id) return NotFound();
        if (ModelState.IsValid)
        {
            model.UpdatedAt = DateTime.Now;
            _context.Update(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        return View(model);
    }

    // Xóa
    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var news = await _context.News.FindAsync(id);
        if (news == null) return NotFound();

        _context.News.Remove(news);
        await _context.SaveChangesAsync();
        return Ok(); // trả về 200
    }
}