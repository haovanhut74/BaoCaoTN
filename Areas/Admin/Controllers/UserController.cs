using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;

namespace MyWebApp.Areas.Admin.Controllers;

public class UserController : BaseController
{
    public UserController(DataContext context) : base(context) { }

    public async Task<IActionResult> Index()
    {
        var users = await _context.Users
            .OrderByDescending(u => u.Id)
            .ToListAsync();

        return View(users);
    }
}