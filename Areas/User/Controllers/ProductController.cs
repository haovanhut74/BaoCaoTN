using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;
using MyWebApp.Extensions.Validation;
using MyWebApp.Models;
using MyWebApp.ViewModels;

namespace MyWebApp.Areas.User.Controllers;

[Area("User")]
public class ProductController : BaseController
{
    public ProductController(DataContext context) : base(context) { }


    public async Task<IActionResult> Index(
        List<string> selectedSlugBrands,
        List<string> selectedSlugCategories,
        decimal? minPrice,
        decimal? maxPrice)
    {
        var categories = await _context.Categories.ToListAsync();
        var brands = await _context.Brands.ToListAsync();

        IQueryable<Product> productsQuery = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand);

        if (selectedSlugCategories?.Count > 0)
            productsQuery = productsQuery.Where(p => selectedSlugCategories.Contains(p.Category.Slug));

        if (selectedSlugBrands?.Count > 0)
            productsQuery = productsQuery.Where(p => selectedSlugBrands.Contains(p.Brand.Slug));

        if (minPrice.HasValue)
            productsQuery = productsQuery.Where(p => p.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            productsQuery = productsQuery.Where(p => p.Price <= maxPrice.Value);

        var products = await productsQuery.OrderByDescending(p => p.Id).ToListAsync();

        var vm = new ProductFilterViewModel
        {
            Products = products,
            Categories = categories,
            Brands = brands,
            SelectedSlugCategories = selectedSlugCategories ?? new List<string>(),
            SelectedSlugBrands = selectedSlugBrands ?? new List<string>(),
            MinPrice = minPrice,
            MaxPrice = maxPrice
        };

        return View(vm);
    }


    [Route("{categorySlug}/{brandSlug}/{productSlug}")]
    public async Task<IActionResult> Detail(string categorySlug, string brandSlug, string productSlug)
    {
        // Decode productSlug để xử lý %2F
        productSlug = Uri.UnescapeDataString(productSlug);

        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Images)
            .Include(p => p.Specifications)
            .ThenInclude(s => s.SpecName) // quan trọng: load SpecName
            .FirstOrDefaultAsync(p =>
                p.Slug == Uri.UnescapeDataString(productSlug) &&
                p.Category.Slug == categorySlug &&
                p.Brand.Slug == brandSlug);


        if (product == null)
            return NotFound();

        // Lấy bình luận đã duyệt
        product.Comments = await _context.Comments
            .Where(c => c.ProductId == product.Id && c.Status == CommentStatus.Approved)
            .Include(c => c.User)
            .Include(c => c.Replies)
            .ToListAsync();

        // Lấy danh sách sản phẩm liên quan (cùng danh mục)
        var relatedProducts = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id)
            .OrderByDescending(p => p.CreatedAt)
            .Take(4) // Giới hạn số lượng hiển thị
            .ToListAsync();

        ViewBag.RelatedProducts = relatedProducts;

        return View(product);
    }


    [HttpGet]
    public async Task<IActionResult> FilterPartial(
        List<string> selectedSlugBrands,
        List<string> selectedSlugCategories,
        decimal? minPrice,
        decimal? maxPrice)
    {
        IQueryable<Product> productsQuery = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand);

        if (selectedSlugCategories?.Count > 0)
            productsQuery = productsQuery.Where(p => selectedSlugCategories.Contains(p.Category.Slug));

        if (selectedSlugBrands?.Count > 0)
            productsQuery = productsQuery.Where(p => selectedSlugBrands.Contains(p.Brand.Slug));

        if (minPrice.HasValue)
            productsQuery = productsQuery.Where(p => p.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            productsQuery = productsQuery.Where(p => p.Price <= maxPrice.Value);

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

        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .FirstOrDefaultAsync(p => p.Id == ProductId);

        if (product == null)
            return NotFound();

        // Validate
        if (CommentFilter.ContainsBannedWord(Content))
        {
            TempData["error"] = "Nội dung bình luận chứa từ ngữ không phù hợp.";
            return RedirectToAction(nameof(Detail),
                new
                {
                    categorySlug = product.Category?.Slug, brandSlug = product.Brand?.Slug, productSlug = product.Slug
                });
        }

        if (CommentFilter.ContainsLink(Content))
        {
            TempData["error"] = "Bình luận không được chứa link.";
            return RedirectToAction(nameof(Detail),
                new
                {
                    categorySlug = product.Category?.Slug, brandSlug = product.Brand?.Slug, productSlug = product.Slug
                });
        }

        if (ParentCommentId.HasValue)
            Rating = 0;

        var comment = new Comment
        {
            ProductId = ProductId,
            Rating = Rating,
            Content = Content,
            UserId = userId,
            CreatedAt = DateTime.Now,
            ParentCommentId = ParentCommentId
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Detail), new
        {
            categorySlug = product.Category?.Slug,
            brandSlug = product.Brand?.Slug,
            productSlug = product.Slug
        });
    }

    [HttpGet]
    public async Task<IActionResult> ComparePartial(List<Guid> productIds)
    {
        var products = await _context.Products
            .Include(p => p.Brand)
            .Include(p => p.Specifications)
            .ThenInclude(s => s.SpecName)
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        return PartialView("_ComparePartial", products);
    }
}