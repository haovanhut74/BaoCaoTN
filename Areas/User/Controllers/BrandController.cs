using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;
using MyWebApp.ViewModels;

namespace MyWebApp.Areas.User.Controllers;

[Area("User")]
public class BrandController : BaseController
{
    public BrandController(DataContext context) : base(context) { }

    public async Task<IActionResult> Index(List<Guid> selectedBrandIds)
    {
        var brands = await _context.Brands.ToListAsync();

        // Lấy tất cả sản phẩm, include Brand, Category
        var productsQuery = _context.Products
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .AsQueryable();

        if (selectedBrandIds != null && selectedBrandIds.Any())
        {
            productsQuery = productsQuery.Where(p => selectedBrandIds.Contains(p.BrandId));
        }

        var products = await productsQuery.OrderByDescending(p => p.Id).ToListAsync();

        var vm = new ProductFilterViewModel
        {
            Products = products,
            Brands = brands,
            SelectedBrandIds = selectedBrandIds ?? new List<Guid>()
        };

        return View(vm);
    }
}