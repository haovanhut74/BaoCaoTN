using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;

namespace MyWebApp.Areas.Admin.Controllers;

[Area("Admin")]
public class ProductController : BaseController
{
    public ProductController(DataContext context) : base(context) { }

    public async Task<IActionResult> Index()
    {
        var products = await _context.Products.OrderByDescending(p => p.Id).Include(p => p.Category)
            .Include(p => p.Brand).ToListAsync();
        return View(products);
    }
}