using Microsoft.AspNetCore.Mvc;
using MyWebApp.Data;
using MyWebApp.Extensions;
using MyWebApp.Models;
using MyWebApp.ViewModels;

namespace MyWebApp.Areas.User.Controllers;

[Area("User")]
public class CartController : BaseController
{
    public CartController(DataContext context) : base(context) { }

    public IActionResult Index()
    {
        // Lấy giỏ hàng từ session, nếu không có thì khởi tạo giỏ hàng mới
        List<CartItem> cartItems = HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? new List<CartItem>();
        CartItemViewModel cartViewModel = new CartItemViewModel
        {
            CartItems = cartItems,
            TotalPrice = cartItems.Sum(item => item.Price * item.Quantity)
        };
        return View(cartViewModel);
    }
}