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

}