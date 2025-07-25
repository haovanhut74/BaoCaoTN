using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using MyWebApp.Data;
using MyWebApp.Extensions;
using MyWebApp.Models;

namespace MyWebApp.Areas.User.Controllers;

public class CheckoutController : BaseController
{
    public CheckoutController(DataContext context) : base(context) { }

    public async Task<IActionResult> Checkout()
    {
        var userName = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrEmpty(userName))
        {
            TempData["Error"] = "Bạn cần đăng nhập để thực hiện thanh toán.";
            return RedirectToAction("Login", "Account");
        }

        var cartItems = HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? new();
        if (!cartItems.Any())
        {
            TempData["Error"] = "Giỏ hàng trống.";
            return RedirectToAction("Index", "Home");
        }

        var orderCode = Guid.NewGuid().ToString();

        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            OrderCode = orderCode,
            UserName = userName,
            OrderDate = DateTime.Now,
            Status = 1,
        };

        _context.Orders.Add(order);

        foreach (var item in cartItems)
        {
            var orderDetail = new OrderDetail
            {
                OrderDetailId = Guid.NewGuid(), // Tạo mới ID riêng biệt
                OrderId = order.OrderId, // Liên kết đúng với OrderId
                OrderCode = orderCode,
                UserName = userName,
                ProductId = item.Id,
                Price = item.Price,
                Quantity = item.Quantity
            };

            _context.OrderDetails.Add(orderDetail);
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đặt hàng thành công! Mã đơn hàng của bạn là: {orderCode}";
        HttpContext.Session.Remove("Cart");

        return RedirectToAction("Index", "Home");
    }
}