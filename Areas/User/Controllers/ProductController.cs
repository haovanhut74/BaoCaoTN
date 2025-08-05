using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;
using MyWebApp.Models;
using MyWebApp.ViewModels;

namespace MyWebApp.Areas.User.Controllers;

[Area("User")]
public class ProductController : BaseController
{
    public ProductController(DataContext context) : base(context) { }


    public async Task<IActionResult> Index(List<string> selectedSlugBrands, List<string> selectedSlugCategories)
    {
        var categories = await _context.Categories.ToListAsync();
        var brands = await _context.Brands.ToListAsync();

        IQueryable<Product> productsQuery = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand);

        if (selectedSlugCategories is { Count: > 0 })
            productsQuery = productsQuery.Where(p => selectedSlugCategories.Contains(p.Category.Slug));

        if (selectedSlugBrands is { Count: > 0 })
            productsQuery = productsQuery.Where(p => selectedSlugBrands.Contains(p.Brand.Slug));

        var products = await productsQuery.OrderByDescending(p => p.Id).ToListAsync();

        var vm = new ProductFilterViewModel
        {
            Products = products,
            Categories = categories,
            Brands = brands,
            SelectedSlugCategories = selectedSlugCategories.Count > 0 ? selectedSlugCategories : new List<string>(),
            SelectedSlugBrands = selectedSlugBrands.Count > 0 ? selectedSlugBrands : new List<string>()
        };

        return View(vm);
    }


    [Route("{categorySlug}/{brandSlug}/{productSlug}")]
    public async Task<IActionResult> Detail(string categorySlug, string brandSlug, string productSlug)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Comments) // thêm dòng này
            .ThenInclude(c => c.User) // nếu muốn hiển thị tên người dùng
            .FirstOrDefaultAsync(p =>
                p.Slug == productSlug &&
                p.Category != null &&
                p.Category.Slug == categorySlug &&
                p.Brand != null &&
                p.Brand.Slug == brandSlug);

        if (product == null)
            return NotFound();

        return View(product);
    }


    [HttpGet]
    public async Task<IActionResult> FilterPartial(List<string> selectedSlugBrands, List<string> selectedSlugCategories)
    {
        IQueryable<Product> productsQuery = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand);

        if (selectedSlugCategories?.Count > 0)
            productsQuery = productsQuery.Where(p => selectedSlugCategories.Contains(p.Category.Slug));

        if (selectedSlugBrands?.Count > 0)
            productsQuery = productsQuery.Where(p => selectedSlugBrands.Contains(p.Brand.Slug));

        var products = await productsQuery.OrderByDescending(p => p.Id).ToListAsync();

        return PartialView("_ProductListPartial", products);
    }

    [HttpPost]
    public async Task<IActionResult> Search(string searchTerm)
    {
        var products = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Where(p => p.Name.Contains(searchTerm) || p.Description.Contains(searchTerm))
            .ToListAsync();
        ViewBag.SearchTerm = searchTerm;
        return View(products);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(Guid ProductId, int Rating, string Content, Guid? ParentCommentId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var comment = new Comment
        {
            ProductId = ProductId,
            Rating = Rating,
            Content = Content,
            UserId = userId,
            CreatedAt = DateTime.Now,
            ParentCommentId = ParentCommentId // ✅ Gán phản hồi nếu có
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .FirstOrDefaultAsync(p => p.Id == ProductId);

        if (product == null)
            return NotFound();

        return RedirectToAction(nameof(Detail), new
        {
            categorySlug = product.Category.Slug,
            brandSlug = product.Brand.Slug,
            productSlug = product.Slug
        });
    }
}