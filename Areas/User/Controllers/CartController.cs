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

    [HttpPost]
    public async Task<IActionResult> Add(Guid Id)
    {
        Product product = await _context.Products.FindAsync(Id);
        List<CartItem> cartItems = HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? new List<CartItem>();
        // Kiểm tra xem sản phẩm đã có trong giỏ hàng chưa
        CartItem existingItem = cartItems.Where(c => c.Id == product.Id).FirstOrDefault();
        if (existingItem != null)
        {
            // Nếu sản phẩm đã có, tăng số lượng
            existingItem.Quantity++;
        }
        else
        {
            // Nếu sản phẩm chưa có, thêm mới vào giỏ hàng
            if (product != null) cartItems.Add(new CartItem(product));
        }

        // Lưu giỏ hàng vào session
        HttpContext.Session.SetJson("Cart", cartItems);
        return RedirectToAction("Index", "Cart", new { area = "User" });
    }
}