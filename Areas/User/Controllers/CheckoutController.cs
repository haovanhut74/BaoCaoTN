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

        var orderCode = Guid.NewGuid();
        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            OrderCode = orderCode.ToString(),
            UserName = userName,
            OrderDate = DateTime.Now,
            Status = 1,
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        List<CartItem> cartItems = HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? new List<CartItem>();

        foreach (var item in cartItems)
        {
            var orderDetail = new OrderDetail
            {
                OrderDetailId = Guid.NewGuid(),
                OrderCode = order.OrderCode,
                UserName = userName,
                ProductId = item.Id,
                Price = item.Price,
                Quantity = item.Quantity
            };

            _context.OrderDetails.Add(orderDetail);
        }

        await _context.SaveChangesAsync(); // Save all OrderDetails at once

        TempData["Success"] = "Đặt hàng thành công! Mã đơn hàng của bạn là: " + order.OrderCode;

        HttpContext.Session.Remove("Cart"); // Xoá giỏ hàng sau khi đặt hàng thành công

        return RedirectToAction("Index", "Home");
    }
}