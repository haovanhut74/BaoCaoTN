using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Areas.Permission;
using MyWebApp.Data;
using MyWebApp.Models;
using MyWebApp.ViewModels;

namespace MyWebApp.Areas.Admin.Controllers;

[HasPermission("ManageBrands")]
public class BrandController : BaseController
{
    public BrandController(DataContext context) : base(context) { }

    public async Task<IActionResult> Index(int page = 1)
    {
        int pageSize = 10; // Số sản phẩm hiển thị trên mỗi trang

        var totalItems = await _context.Brands.CountAsync();
        var brands = await _context.Brands.OrderByDescending(b => b.Id).Skip((page - 1) * pageSize)
            .Take(pageSize).ToListAsync();
        var viewModel = new ListViewModel
        {
            Brands = brands,
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling((double)totalItems / pageSize)
        };
        return View(viewModel);
    }

    [HttpGet]
    [HasPermission("CreateBrand")]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("CreateBrand")]
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
    [HasPermission("EditBrand")]
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
    [HasPermission("EditBrand")]
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("DeleteBrand")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var brand = await _context.Brands.FindAsync(id);
        if (brand != null)
        {
            _context.Brands.Remove(brand);
            TempData["message"] = "Xóa thành công";
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        TempData["error"] = "Danh mục không tồn tại";
        return NotFound();
    }
}