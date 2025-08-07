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
            .FirstOrDefaultAsync(p =>
                p.Slug == productSlug &&
                p.Category != null &&
                p.Category.Slug == categorySlug &&
                p.Brand != null &&
                p.Brand.Slug == brandSlug);

        if (product == null)
            return NotFound();

        // Lấy riêng các comment đã được duyệt, kèm reply
        product.Comments = await _context.Comments
            .Where(c => c.ProductId == product.Id && c.Status == CommentStatus.Approved)
            .Include(c => c.User)
            .Include(c => c.Replies)
            .ToListAsync();

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

        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Comments) // cần để hiển thị bình luận luôn
            .ThenInclude(c => c.User)
            .FirstOrDefaultAsync(p => p.Id == ProductId);

        if (product == null)
            return NotFound();

        // Kiểm tra lỗi
        if (CommentFilter.ContainsBannedWord(Content))
        {
            ModelState.AddModelError("Content", "Nội dung bình luận chứa từ ngữ không phù hợp.");
            TempData["error"] = "Nội dung bình luận chứa từ ngữ không phù hợp.";
        }

        if (CommentFilter.ContainsLink(Content))
        {
            ModelState.AddModelError("Content", "Bình luận không được chứa link.");
            TempData["error"] = "Bình luận không được chứa link.";
        }


        // Nếu có lỗi thì trả lại view với lỗi hiển thị
        if (!ModelState.IsValid)
        {
            // Trả về view cùng model sản phẩm (đã include comment) để giữ lại các comment cũ
            return View("Detail", product);
        }
        // Nếu là phản hồi thì đặt Rating = 0 hoặc không lưu Rating
        if (ParentCommentId.HasValue)
        {
            Rating = 0; // Hoặc không truyền rating từ form cho reply, tùy cách thiết kế
        }
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

        // Sau khi thêm thành công, redirect để tránh gửi lại form khi refresh
        return RedirectToAction(nameof(Detail), new
        {
            categorySlug = product.Category?.Slug,
            brandSlug = product.Brand?.Slug,
            productSlug = product.Slug
        });
    }
}