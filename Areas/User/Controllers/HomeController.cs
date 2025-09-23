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

    public async Task<IActionResult> Index(List<string> selectedSlugBrands, List<string> selectedSlugCategories)
    {
        var sliders = await _context.Sliders.Where(s => s.Status == 1).ToListAsync();

        var topSale = await _context.Products
            .Where(p => p.DiscountPercent.HasValue && p.DiscountPercent > 0)
            .OrderByDescending(p => p.DiscountPercent)
            .Take(8)
            .Include(p => p.Category).Include(p => p.Brand)
            .ToListAsync();

        var mostBought = await _context.Products
            .OrderByDescending(p => p.Sold) // bạn cần có cột SoldQuantity
            .Take(8)
            .Include(p => p.Category).Include(p => p.Brand)
            .ToListAsync();

        var newest = await _context.Products
            .OrderByDescending(p => p.CreatedAt) // cần cột CreatedAt
            .Take(8)
            .Include(p => p.Category).Include(p => p.Brand)
            .ToListAsync();

        var mostReviewed = await _context.Products
            .OrderByDescending(p => p.Comments.Count) // cần cột ReviewCount
            .Take(8)
            .Include(p => p.Category).Include(p => p.Brand)
            .ToListAsync();

        var vm = new HomeViewModel
        {
            Sliders = sliders,
            TopSale = topSale,
            MostBought = mostBought,
            Newest = newest,
            MostReviewed = mostReviewed
        };
        return View(vm);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int statuscode)
    {
        if (statuscode == 404)
        {
            return View("NotFound");
        }
        else if (statuscode == 500)
        {
            return View("ServerError");
        }

        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}