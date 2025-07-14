using Microsoft.AspNetCore.Mvc;

namespace MyWebApp.Areas.User.Controllers;

[Area("User")]
public class ProductController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Detail()
    {
        return View();
    }
}