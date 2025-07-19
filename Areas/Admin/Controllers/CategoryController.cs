using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;

namespace MyWebApp.Areas.Admin.Controllers;

[Area("Admin")]
public class CategoryController : BaseController
{
    public CategoryController(DataContext context) : base(context) { }
    public async Task<IActionResult> Index()
    {
        var categories = await _context.Categories.OrderByDescending(c => c.Id).ToListAsync();
        return View(categories);
    }
}