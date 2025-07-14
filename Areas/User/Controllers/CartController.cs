using Microsoft.AspNetCore.Mvc;

namespace MyWebApp.Areas.User.Controllers;

[Area("User")]
public class CartController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}