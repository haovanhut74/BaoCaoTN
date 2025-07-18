using Microsoft.AspNetCore.Mvc;
using MyWebApp.Data;

namespace MyWebApp.Areas.Admin.Controllers;

[Area("Admin")]
public abstract class BaseController : Controller
{
    protected readonly DataContext _context;

    protected BaseController(DataContext context)
    {
        _context = context;
    }
}