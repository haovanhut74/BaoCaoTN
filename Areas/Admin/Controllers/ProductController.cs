using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;
using MyWebApp.Models;

namespace MyWebApp.Areas.Admin.Controllers;

[Area("Admin")]
public class ProductController : BaseController
{
    private readonly IWebHostEnvironment _env;

    public ProductController(DataContext context, IWebHostEnvironment env) : base(context)
    {
        _env = env;
    }

    public async Task<IActionResult> Index()
    {
        var products = await _context.Products.OrderByDescending(p => p.Id).Include(p => p.Category)
            .Include(p => p.Brand).ToListAsync();
        return View(products);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.Categories = new SelectList(_context.Categories.ToList(), "Id", "Name");
        ViewBag.Brands = new SelectList(_context.Brands.ToList(), "Id", "Name");
        return View();
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product)
    {
        ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
        ViewBag.Brands = new SelectList(_context.Brands, "Id", "Name", product.BrandId);

        if (product.ImageFile != null)
        {
            string uploadDir = Path.Combine(_env.WebRootPath, "img/product");
            string imgName = Guid.NewGuid().ToString() + "_" + product.ImageFile.FileName;
            string filePath = Path.Combine(uploadDir, imgName);

            FileStream fs = new FileStream(filePath, FileMode.Create);
            await product.ImageFile.CopyToAsync(fs);
            fs.Close();
            product.Image = imgName;
        }
        else
        {
            ModelState.AddModelError("ImageFile", "Yêu cầu nhập hình ảnh");
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
    public async Task<IActionResult> Edit(Guid id, Product product)
    {
        ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
        ViewBag.Brands = new SelectList(_context.Brands, "Id", "Name", product.BrandId);

        var oldProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);

        if (product.ImageFile != null && oldProduct != null)
        {
            // Xoá ảnh cũ (nếu có)
            if (!string.IsNullOrEmpty(oldProduct.Image))
            {
                string uploadDir = Path.Combine(_env.WebRootPath, "img/product");
                string oldImgName = oldProduct.Image.Replace("img/product/", "").Replace("img/product", "");
                string oldFilePath = Path.Combine(uploadDir, oldImgName);
                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }
            }

            // Lưu ảnh mới
            string uploadDirNew = Path.Combine(_env.WebRootPath, "img/product");
            string newImgName = Guid.NewGuid().ToString() + "_" + product.ImageFile.FileName;
            string newFilePath = Path.Combine(uploadDirNew, newImgName);

            await using (FileStream fs = new FileStream(newFilePath, FileMode.Create))
            {
                await product.ImageFile.CopyToAsync(fs);
            }

            product.Image = newImgName;
        }
        else if (oldProduct != null)
        {
            // Nếu không up ảnh mới, giữ nguyên ảnh cũ
            product.Image = oldProduct.Image;
        }

        product.Slug = product.Name.Replace(" ", "-").ToLower();

        var slug = await _context.Products
            .Where(p => p.Slug == product.Slug && p.Id != id)
            .FirstOrDefaultAsync();
        if (slug != null)
        {
            ModelState.AddModelError("", "Sản phẩm đã có trong Data");
            return View(product);
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(product);
                TempData["message"] = "Cập nhật thành công";
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Products.Any(e => e.Id == id))
                {
                    return NotFound();
                }

                throw;
            }
        }

        TempData["error"] = "Cập nhật thất bại";
        return View(product);
    }
}