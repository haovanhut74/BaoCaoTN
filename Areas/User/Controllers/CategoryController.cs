using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;
using MyWebApp.Models;
using MyWebApp.ViewModels;

namespace MyWebApp.Areas.User.Controllers;

[Area("User")]
public class CategoryController : BaseController
{
    public CategoryController(DataContext context) : base(context) { }
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

}