using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // EF Core namespace
using MyWebApp.Data;
using MyWebApp.Interface.Service;
using MyWebApp.Models;

namespace MyWebApp.Areas.User.Controllers;

public class CheckoutController : BaseController
{
    private readonly IEmailSender _emailSender;

    public CheckoutController(DataContext context, IEmailSender emailSender) : base(context)
    {
        _emailSender = emailSender;
    }

    public async Task<IActionResult> Checkout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            TempData["Error"] = "Bạn cần đăng nhập để thực hiện thanh toán.";
            return RedirectToAction("Login", "Account");
        }

        var cart = await _context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null || !cart.CartItems.Any())
        {
            TempData["Error"] = "Giỏ hàng trống.";
            return RedirectToAction("Index", "Home");
        }

        var orderCode = Guid.NewGuid().ToString();

        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            OrderCode = orderCode,
            UserName = User.Identity?.Name ?? "Unknown",
            OrderDate = DateTime.Now,
            Status = 1,
        };

        _context.Orders.Add(order);

        foreach (var item in cart.CartItems)
        {
            var orderDetail = new OrderDetail
            {
                OrderDetailId = Guid.NewGuid(),
                OrderId = order.OrderId,
                OrderCode = orderCode,
                UserName = order.UserName,
                ProductId = item.ProductId,
                Price = item.Price,
                Quantity = item.Quantity
            };
            _context.OrderDetails.Add(orderDetail);
        }

        await _context.SaveChangesAsync();

        // Xóa giỏ hàng sau khi đặt thành công
        _context.CartItems.RemoveRange(cart.CartItems);
        _context.Carts.Remove(cart);
        await _context.SaveChangesAsync();

        var receiver = "haovanhut74@gmail.com";
        var subject = "Xác nhận đơn hàng";
        var message = $"Chào {order.UserName},<br/>" +
                      $"Cảm ơn bạn đã đặt hàng tại cửa hàng của chúng tôi.<br/>" +
                      $"Mã đơn hàng của bạn là: <strong>{orderCode}</strong>.<br/>" +
                      $"Chúng tôi sẽ xử lý đơn hàng của bạn trong thời gian sớm nhất.<br/>" +
                      $"Nếu bạn có bất kỳ câu hỏi nào, vui lòng liên hệ với chúng tôi qua email này hoặc số điện thoại hỗ trợ.<br/>";

        await _emailSender.SendEmailAsync(receiver, subject, message);

        TempData["Success"] = $"Đặt hàng thành công! Mã đơn hàng của bạn là: {orderCode}";

        return RedirectToAction("Index", "Home");
    }
}