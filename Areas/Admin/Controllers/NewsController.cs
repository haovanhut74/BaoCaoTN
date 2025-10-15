using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Areas.Permission;
using MyWebApp.Data;
using MyWebApp.Models;

namespace MyWebApp.Areas.Admin.Controllers;

[HasPermission("ManageNews")]
public class NewsController : BaseController
{
    public NewsController(DataContext context) : base(context) { }

    public async Task<IActionResult> Index()
    {
        var list = await _context.News.OrderByDescending(n => n.CreatedAt).ToListAsync();
        return View(list);
    }

    // Thêm mới
    [HttpGet]
    public IActionResult Create() => View();

    [HttpPost]
    [HasPermission("CreateNews")]
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
    [HasPermission("EditNews")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var news = await _context.News.FindAsync(id);
        return news == null ? NotFound() : View(news);
    }

    [HttpPost]
    [HasPermission("EditNews")]
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
    [HasPermission("DeleteNews")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var news = await _context.News.FindAsync(id);
        if (news == null) return NotFound();

        _context.News.Remove(news);
        await _context.SaveChangesAsync();
        return Ok(); // trả về 200
    }
}