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

    public async Task<IActionResult> Index(List<Guid> selectedCategoryIds, List<Guid> selectedBrandIds)
    {
        var categories = await _context.Categories.ToListAsync();
        var brands = await _context.Brands.ToListAsync();

        IQueryable<Product> productsQuery = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand);

        if (selectedCategoryIds != null && selectedCategoryIds.Count > 0)
            productsQuery = productsQuery.Where(p => selectedCategoryIds.Contains(p.CategoryId));

        if (selectedBrandIds != null && selectedBrandIds.Count > 0)
            productsQuery = productsQuery.Where(p => selectedBrandIds.Contains(p.BrandId));

        var products = await productsQuery.OrderByDescending(p => p.Id).ToListAsync();

        var vm = new ProductFilterViewModel
        {
            Products = products,
            Categories = categories,
            Brands = brands,
            SelectedCategoryIds = selectedCategoryIds ?? new List<Guid>(),
            SelectedBrandIds = selectedBrandIds ?? new List<Guid>()
        };

        return View(vm);
    }

    public async Task<IActionResult> Detail(Guid id)
    {
        if (id == Guid.Empty)
        {
            return RedirectToAction("Index");
        }

        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            // Có thể trả về view "NotFound" hoặc redirect
            return NotFound(); // hoặc RedirectToAction("Index")
        }

        return View(product);
    }
}