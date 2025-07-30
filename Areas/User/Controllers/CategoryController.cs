using Microsoft.AspNetCore.Mvc;
using MyWebApp.Data;

namespace MyWebApp.Areas.User.Controllers;

[Area("User")]
public class CategoryController : BaseController
{
    public CategoryController(DataContext context) : base(context) { }
}