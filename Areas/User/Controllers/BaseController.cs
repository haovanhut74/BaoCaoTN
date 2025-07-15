using Microsoft.AspNetCore.Mvc;
using MyWebApp.Data;

namespace MyWebApp.Areas.User.Controllers;

public abstract class BaseController : Controller
{
    protected readonly DataContext _context;

    protected BaseController(DataContext context)
    {
        _context = context;
    }
}