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
        decimal? maxPrice,
        string sortOrder = "desc",
        int page = 1)
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

        // Áp dụng sắp xếp
        productsQuery = sortOrder switch
        {
            "asc" => productsQuery.OrderBy(p => p.Price),
            "newest" => productsQuery.OrderByDescending(p => p.CreatedAt),
            "oldest" => productsQuery.OrderBy(p => p.CreatedAt),
            _ => productsQuery.OrderByDescending(p => p.Price) // Mặc định giảm dần
        };

        int pageSize = 12;
        int totalItems = await productsQuery.CountAsync();
        int totalPages = (int)Math.Ceiling((decimal)totalItems / pageSize);
        var products = await productsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var vm = new ProductFilterViewModel
        {
            Products = products,
            Categories = categories,
            Brands = brands,
            SelectedSlugCategories = selectedSlugCategories ?? new List<string>(),
            SelectedSlugBrands = selectedSlugBrands ?? new List<string>(),
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            CurrentPage = page,
            TotalPages = totalPages,
            SortOrder = sortOrder
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> FilterPartial(
        List<string> selectedSlugBrands,
        List<string> selectedSlugCategories,
        decimal? minPrice,
        decimal? maxPrice,
        string sortOrder = "desc",
        int page = 1,
        int pageSize = 12)
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

        // Áp dụng sắp xếp
        productsQuery = sortOrder switch
        {
            "asc" => productsQuery.OrderBy(p => p.Price),
            "newest" => productsQuery.OrderByDescending(p => p.CreatedAt),
            "oldest" => productsQuery.OrderBy(p => p.CreatedAt),
            _ => productsQuery.OrderByDescending(p => p.Price) // Mặc định giảm dần
        };

        int totalItems = await productsQuery.CountAsync();
        int totalPages = (int)Math.Ceiling((decimal)totalItems / pageSize);

        var products = await productsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var vm = new ProductFilterViewModel
        {
            Products = products,
            SelectedSlugCategories = selectedSlugCategories ?? new List<string>(),
            SelectedSlugBrands = selectedSlugBrands ?? new List<string>(),
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            CurrentPage = page,
            TotalPages = totalPages,
            SortOrder = sortOrder
        };

        return PartialView("_ProductWithPaginationPartial", vm);
    }

    [HttpGet]
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

        // Lấy các khuyến mãi hiện hành liên quan đến sản phẩm
        var activePromotions = await _context.GiftPromotions
            .Include(p => p.RequiredProduct)
            .Include(p => p.GiftProduct)
            .Where(p => p.RequiredProductId == product.Id
                        && p.StartDate <= DateTime.Now
                        && p.EndDate.AddDays(1) >= DateTime.Now
                        && p.IsActive)
            .ToListAsync();

        ViewBag.ActivePromotions = activePromotions;
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


    // Dùng 1 action cho cả Search & Suggestion
    [HttpGet]
    public async Task<IActionResult> Search(string searchTerm, bool suggest = false)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            if (suggest)
                return Json(new { products = new List<object>(), keywords = new List<string>() });

            return View(new List<Product>());
        }

        // Chuẩn hóa và tách từ khóa
        searchTerm = searchTerm.Trim().ToLower();
        var keywords = searchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // Query chung
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .AsQueryable();

        // Áp dụng tìm kiếm cho từng từ khóa
        foreach (var keyword in keywords)
        {
            query = query.Where(p =>
                p.Name.ToLower().Contains(keyword) ||
                p.Category.Name.ToLower().Contains(keyword) ||
                p.Brand.Name.ToLower().Contains(keyword));
        }

        if (suggest)
        {
            // Trả JSON cho auto-suggest
            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Slug,
                    CategorySlug = p.Category.Slug,
                    BrandSlug = p.Brand.Slug,
                    p.MainImage,
                    p.Price
                })
                .ToListAsync();

            var keywordsList = await _context.Products
                .Where(p => keywords.Any(k =>
                    p.Name.ToLower().Contains(k) ||
                    p.Category.Name.ToLower().Contains(k) ||
                    p.Brand.Name.ToLower().Contains(k)))
                .Select(p => p.Name)
                .Distinct()
                .Take(5)
                .ToListAsync();

            return Json(new { products, keywords = keywordsList });
        }
        else
        {
            // Trả View kết quả tìm kiếm
            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            ViewBag.SearchTerm = searchTerm;
            return View(products);
        }
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