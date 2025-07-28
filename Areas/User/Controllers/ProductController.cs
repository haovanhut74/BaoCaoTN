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
}