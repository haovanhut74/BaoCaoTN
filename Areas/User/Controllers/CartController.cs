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

    // ✅ AJAX-friendly thêm giỏ hàng
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(Guid Id)
    {
        var product = await _context.Products.FindAsync(Id);
        var cartItems = HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? new List<CartItem>();

        var existingItem = cartItems.FirstOrDefault(c => c.Id == Id);
        if (existingItem != null)
        {
            existingItem.Quantity++;
        }
        else if (product != null)
        {
            cartItems.Add(new CartItem(product));
        }

        HttpContext.Session.SetJson("Cart", cartItems);

        return Json(new
        {
            success = true,
            message = $"Đã thêm sản phẩm \"{product?.Name}\" vào giỏ hàng!",
            cartCount = cartItems.Count
        });
    }


    [HttpPost]
    public async Task<IActionResult> Increase(Guid id)
    {
        var cartItems = HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? new List<CartItem>();
        var item = cartItems.FirstOrDefault(c => c.Id == id);

        if (item != null)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                TempData["GlobalError"] = "Sản phẩm không tồn tại!";
            }
            else if (item.Quantity < product.Quantity)
            {
                item.Quantity++;
                HttpContext.Session.SetJson("Cart", cartItems);
            }
            else
            {
                TempData["Error"] = $"Sản phẩm {product.Name} chỉ còn {product.Quantity} cái!";
                TempData["ErrorProductId"] = item.Id;
            }
        }

        return RedirectToAction("Index", "Cart", new { area = "User" });
    }

    [HttpPost]
    public IActionResult Decrease(Guid id)
    {
        var cartItems = HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? new List<CartItem>();
        var item = cartItems.FirstOrDefault(c => c.Id == id);

        if (item != null)
        {
            if (item.Quantity > 1)
            {
                item.Quantity--;
            }
            else
            {
                TempData["Success"] = "Đã xóa sản phẩm khỏi giỏ hàng!";
                cartItems.Remove(item);
            }

            if (cartItems.Count == 0)

                HttpContext.Session.Remove("Cart");
            else
                HttpContext.Session.SetJson("Cart", cartItems);
        }

        return RedirectToAction("Index", "Cart", new { area = "User" });
    }

    [HttpPost]
    public IActionResult Remove(Guid id)
    {
        var cartItems = HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? new List<CartItem>();
        cartItems.RemoveAll(p => p.Id == id);

        if (cartItems.Count == 0)
            HttpContext.Session.Remove("Cart");
        else
            HttpContext.Session.SetJson("Cart", cartItems);

        TempData["Success"] = "Đã xóa sản phẩm khỏi giỏ hàng!";
        return RedirectToAction("Index", "Cart", new { area = "User" });
    }
}