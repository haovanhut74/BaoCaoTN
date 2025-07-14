using Microsoft.AspNetCore.Mvc;

namespace MyWebApp.Areas.User.Controllers;

[Area("User")]
public class BrandController : Controller
{
    public IActionResult Index()
    {
        // This action will return the view for the brand management page
        return View();
    }
}