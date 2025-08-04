using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Areas.Permission;
using MyWebApp.Data;
using MyWebApp.Models;
using MyWebApp.ViewModels;

namespace MyWebApp.Areas.Admin.Controllers;

[Area("Admin")]
[HasPermission("ManageProducts")]
public class ProductController : BaseController
{
    private readonly IWebHostEnvironment _env;

    public ProductController(DataContext context, IWebHostEnvironment env) : base(context)
    {
        _env = env;
    }

    public async Task<IActionResult> Index(int page = 1)
    {
        int pageSize = 10; // Số sản phẩm hiển thị trên mỗi trang

        var totalItems = await _context.Products.CountAsync();

        var products = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .OrderByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var viewModel = new ListViewModel
        {
            Products = products,
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling((double)totalItems / pageSize)
        };

        return View(viewModel);
    }


    [HttpGet]
    [HasPermission("CreateProduct")]
    public IActionResult Create()
    {
        ViewBag.Categories = new SelectList(_context.Categories.ToList(), "Id", "Name");
        ViewBag.Brands = new SelectList(_context.Brands.ToList(), "Id", "Name");
        return View();
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("CreateProduct")]
    public async Task<IActionResult> Create(Product product)
    {
        ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
        ViewBag.Brands = new SelectList(_context.Brands, "Id", "Name", product.BrandId);

        if (product.ImageFile != null)
        {
            // Upload ảnh từ file như cũ
            string uploadDir = Path.Combine(_env.WebRootPath, "img/product");
            string imgName = Guid.NewGuid().ToString() + "_" + product.ImageFile.FileName;
            string filePath = Path.Combine(uploadDir, imgName);

            using FileStream fs = new(filePath, FileMode.Create);
            await product.ImageFile.CopyToAsync(fs);
            product.Image = imgName;
        }
        else if (!string.IsNullOrEmpty(product.ImageUrl))
        {
            // Tải ảnh từ URL
            try
            {
                using var httpClient = new HttpClient();
                var imageBytes = await httpClient.GetByteArrayAsync(product.ImageUrl);

                string ext = Path.GetExtension(product.ImageUrl).Split('?')[0]; // tránh query string
                string imgName = Guid.NewGuid().ToString() + ext;
                string filePath = Path.Combine(_env.WebRootPath, "img/product", imgName);

                await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);
                product.Image = imgName;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("ImageUrl", "Không thể tải ảnh từ liên kết.");
            }
        }
        else
        {
            ModelState.AddModelError("ImageFile", "Vui lòng tải ảnh hoặc nhập link ảnh.");
        }

        product.Slug = product.Name.Replace(" ", "-").ToLower();
        var slug = await _context.Products.Where(p => p.Slug == product.Slug).FirstOrDefaultAsync();
        if (slug != null)
        {
            ModelState.AddModelError("", "Sản phẩm đã có trong Data");
            return View(product);
        }

        if (ModelState.IsValid)
        {
            _context.Products.Add(product);
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
    [HasPermission("EditProduct")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product != null)
        {
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewBag.Brands = new SelectList(_context.Brands, "Id", "Name", product.BrandId);
            return View(product);
        }

        return NotFound();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("EditProduct")]
    public async Task<IActionResult> Edit(Guid id, Product product)
    {
        ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
        ViewBag.Brands = new SelectList(_context.Brands, "Id", "Name", product.BrandId);

        var oldProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (oldProduct == null)
        {
            return NotFound();
        }

        string uploadDir = Path.Combine(_env.WebRootPath, "img/product");

        // Trường hợp có file upload
        if (product.ImageFile != null && product.ImageFile.Length > 0)
        {
            // Xóa ảnh cũ nếu có
            if (!string.IsNullOrEmpty(oldProduct.Image))
            {
                string oldPath = Path.Combine(uploadDir, oldProduct.Image);
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            // Lưu ảnh mới
            string newImgName = Guid.NewGuid() + Path.GetExtension(product.ImageFile.FileName);
            string newFilePath = Path.Combine(uploadDir, newImgName);
            using var fs = new FileStream(newFilePath, FileMode.Create);
            await product.ImageFile.CopyToAsync(fs);
            product.Image = newImgName;
        }
        // Trường hợp có link ảnh và không upload file
        else if (!string.IsNullOrEmpty(product.ImageUrl))
        {
            try
            {
                using var httpClient = new HttpClient();
                var imgBytes = await httpClient.GetByteArrayAsync(product.ImageUrl);

                string ext = Path.GetExtension(product.ImageUrl).ToLowerInvariant();
                if (!new[] { ".jpg", ".jpeg", ".png", ".gif" }.Contains(ext))
                {
                    ModelState.AddModelError("ImageUrl", "Định dạng ảnh không hợp lệ.");
                    return View(product);
                }

                // Xóa ảnh cũ
                if (!string.IsNullOrEmpty(oldProduct.Image))
                {
                    string oldPath = Path.Combine(uploadDir, oldProduct.Image);
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                string newImgName = Guid.NewGuid() + ext;
                string savePath = Path.Combine(uploadDir, newImgName);
                await System.IO.File.WriteAllBytesAsync(savePath, imgBytes);

                product.Image = newImgName;
            }
            catch
            {
                ModelState.AddModelError("ImageUrl", "Không thể tải ảnh từ đường dẫn.");
                return View(product);
            }
        }
        else
        {
            // Không có file, không có link -> giữ nguyên ảnh cũ
            product.Image = oldProduct.Image;
        }

        // Slug kiểm tra trùng
        product.Slug = product.Name.Replace(" ", "-").ToLower();
        var slugExists = await _context.Products
            .AnyAsync(p => p.Slug == product.Slug && p.Id != id);
        if (slugExists)
        {
            ModelState.AddModelError("", "Sản phẩm đã tồn tại.");
            return View(product);
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(product);
                await _context.SaveChangesAsync();
                TempData["message"] = "Cập nhật sản phẩm thành công";
                return RedirectToAction("Index");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Products.Any(p => p.Id == id))
                    return NotFound();
                throw;
            }
        }

        TempData["error"] = "Cập nhật thất bại";
        return View(product);
    }

    [HttpGet]
    [HasPermission("DeleteProduct")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product != null)
        {
            // Xoá file ảnh (nếu có)
            if (!string.IsNullOrEmpty(product.Image))
            {
                string uploadDir = Path.Combine(_env.WebRootPath, "img/product");
                string imgName = product.Image.Replace("img/product/", "").Replace("img/product", "");
                string filePath = Path.Combine(uploadDir, imgName);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        return NotFound();
    }
}