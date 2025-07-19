using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;
using MyWebApp.Models;

namespace MyWebApp.Areas.Admin.Controllers;

public class BrandController : BaseController
{
    public BrandController(DataContext context) : base(context) { }

    public async Task<IActionResult> Index()
    {
        var brands = await _context.Brands.OrderByDescending(b => b.Id).ToListAsync();
        return View(brands);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Brand brand)
    {
        // Kiểm tra Name có null hoặc rỗng không
        if (string.IsNullOrWhiteSpace(brand.Name))
        {
            ModelState.AddModelError(nameof(brand.Name), "Tên danh mục không được để trống");
            return View(brand);
        }

        brand.Slug = brand.Name.Replace(" ", "-").ToLower();

        var slug = await _context.Brands.Where(p => p.Slug == brand.Slug).FirstOrDefaultAsync();
        if (slug != null)
        {
            ModelState.AddModelError("", "Danh mục đã có trong Data");
            return View(brand);
        }

        if (ModelState.IsValid)
        {
            _context.Brands.Add(brand);
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
        var brand = await _context.Brands.FindAsync(id);
        if (brand != null)
        {
            return View(brand);
        }

        return NotFound();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Brand brand)
    {
        // Kiểm tra Name có null hoặc rỗng không
        if (string.IsNullOrWhiteSpace(brand.Name))
        {
            ModelState.AddModelError(nameof(brand.Name), "Tên danh mục không được để trống");
            return View(brand);
        }

        var oldbrand = await _context.Brands.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (oldbrand == null)
        {
            return NotFound();
        }

        brand.Slug = brand.Name.Replace(" ", "-").ToLower();

        // Sửa lại kiểm tra trùng slug đúng với bảng Brands
        var slug = await _context.Brands.Where(p => p.Slug == brand.Slug && p.Id != id).FirstOrDefaultAsync();
        if (slug != null)
        {
            ModelState.AddModelError("", "Danh mục đã có trong Data");
            return View(brand);
        }

        if (ModelState.IsValid)
        {
            _context.Brands.Update(brand);
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
}