using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;

namespace MyWebApp.Areas.Admin.Controllers;

public class BrandController : BaseController
{
    public BrandController(DataContext context) : base(context) { }

    public async Task<IActionResult> Index()
    {
        var brands = await _context.Brands.OrderByDescending(b => b.Id).ToListAsync();
        return View(brands);
    }
}