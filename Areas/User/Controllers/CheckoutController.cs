using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using MyWebApp.Data;
using MyWebApp.Extensions;
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
        var receiver = "haovanhut74@gmail.com"; // Thay thế bằng email người nhận
        var subject = "Xác nhận đơn hàng";
        var message = $"Chào {userName},<br/>" +
                      $"Cảm ơn bạn đã đặt hàng tại cửa hàng của chúng tôi.<br/>" +
                      $"Mã đơn hàng của bạn là: <strong>{orderCode}</strong>.<br/>" +
                      $"Chúng tôi sẽ xử lý đơn hàng của bạn trong thời gian sớm nhất.<br/>" +
                      $"Nếu bạn có bất kỳ câu hỏi nào, vui lòng liên hệ với chúng tôi qua email này hoặc số điện thoại hỗ trợ.<br/>";
        await _emailSender.SendEmailAsync(receiver, subject, message);
        TempData["Success"] = $"Đặt hàng thành công! Mã đơn hàng của bạn là: {orderCode}";
        HttpContext.Session.Remove("Cart");

        return RedirectToAction("Index", "Home");
    }
}