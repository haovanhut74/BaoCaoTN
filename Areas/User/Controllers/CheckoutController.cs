using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using MyWebApp.Data;
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

        var ordercode = Guid.NewGuid();
        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            OrderCode = ordercode.ToString(),
            UserName = userName,
            OrderDate = DateTime.Now,
            Status = 1,
        };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Đặt hàng thành công! Mã đơn hàng của bạn là: " + order.OrderCode;

        return View();
    }
}