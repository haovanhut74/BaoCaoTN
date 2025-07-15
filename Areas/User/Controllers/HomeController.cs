using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;
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

    public IActionResult Index()
    {
        var products =
            _context.Products.Include("Category").Include("Brand").ToList(); // Lấy danh sách sản phẩm từ cơ sở dữ liệu
        return View(products); // Trả về view với danh sách sản phẩm
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