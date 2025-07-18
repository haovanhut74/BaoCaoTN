using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;
using MyWebApp.Models;
using MyWebApp.ViewModels;

namespace MyWebApp.Areas.User.Controllers;

[Area("User")]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly DataContext _context;

    public HomeController(ILogger<HomeController> logger, DataContext context)
    {
        _logger = logger;
        _context = context;
    }
    
    public async Task<IActionResult> Index(List<Guid> selectedCategoryIds, List<Guid> selectedBrandIds)
    {
        var categories = await _context.Categories.ToListAsync();
        var brands = await _context.Brands.ToListAsync();

        IQueryable<Product> productsQuery = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand);

        if (selectedCategoryIds?.Any() == true)
            productsQuery = productsQuery.Where(p => selectedCategoryIds.Contains(p.CategoryId));

        if (selectedBrandIds?.Any() == true)
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


    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}