using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;
using MyWebApp.Models;

namespace MyWebApp.Areas.Admin.Controllers;

[Area("Admin")]
public class CategoryController : BaseController
{
    public CategoryController(DataContext context) : base(context) { }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
    {
        var categories = await _context.Categories.OrderByDescending(c => c.Id).ToListAsync();
        return View(categories);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category category)
    {
        // Kiểm tra Name có null hoặc rỗng không
        if (string.IsNullOrWhiteSpace(category.Name))
        {
            ModelState.AddModelError(nameof(category.Name), "Tên danh mục không được để trống");
            return View(category);
        }

        category.Slug = category.Name.Replace(" ", "-").ToLower();

        var slug = await _context.Categories.Where(p => p.Slug == category.Slug).FirstOrDefaultAsync();
        if (slug != null)
        {
            ModelState.AddModelError("", "Danh mục đã có trong Data");
            return View(category);
        }

        if (ModelState.IsValid)
        {
            _context.Categories.Add(category);
            TempData["message"] = "Thêm mới thành công";
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        TempData["error"] = "Thêm mới thất bại";
        List<string> errors = [];
        foreach (var item in ModelState.Values)
        {
            foreach (var error in item.Errors)
            {
                errors.Add(error.ErrorMessage);
            }
        }

        string errorMessage = string.Join("\n", errors);
        return BadRequest(errorMessage);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category != null)
        {
            return View(category);
        }

        return NotFound();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Category category)
    {
        // Kiểm tra Name có null hoặc rỗng không
        if (string.IsNullOrWhiteSpace(category.Name))
        {
            ModelState.AddModelError(nameof(category.Name), "Tên danh mục không được để trống");
            return View(category);
        }

        var oldCategory = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (oldCategory == null)
        {
            return NotFound();
        }

        category.Slug = category.Name.Replace(" ", "-").ToLower();

        var slug = await _context.Categories.Where(p => p.Slug == category.Slug && p.Id != id).FirstOrDefaultAsync();
        if (slug != null)
        {
            ModelState.AddModelError("", "Danh mục đã có trong Data");
            return View(category);
        }

        if (ModelState.IsValid)
        {
            _context.Categories.Update(category);
            TempData["message"] = "Cập nhật thành công";
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        TempData["error"] = "Cập nhật thất bại";
        List<string> errors = [];
        foreach (var item in ModelState.Values)
        {
            foreach (var error in item.Errors)
            {
                errors.Add(error.ErrorMessage);
            }
        }

        string errorMessage = string.Join("\n", errors);
        return BadRequest(errorMessage);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category != null)
        {
            _context.Categories.Remove(category);
            TempData["message"] = "Xóa thành công";
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        TempData["error"] = "Danh mục không tồn tại";
        return NotFound();
    }
}