using Microsoft.AspNetCore.Mvc;
using MyWebApp.Data;

namespace MyWebApp.Areas.User.Controllers;

[Area("User")]
[Route("User/[controller]/[action]")]
public abstract class BaseController : Controller
{
    protected readonly DataContext _context;

    protected BaseController(DataContext context)
    {
        _context = context;
    }
}