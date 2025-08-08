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
            .ThenInclude(ci => ci.Product)
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
                Product = item.Product, // Gán luôn để email có tên
                Price = item.Price,
                Quantity = item.Quantity
            };
            _context.OrderDetails.Add(orderDetail);
        }

        // Xóa giỏ hàng sau khi đặt thành công
        _context.CartItems.RemoveRange(cart.CartItems);
        _context.Carts.Remove(cart);
        await _context.SaveChangesAsync();
        var user = await _context.Users.FindAsync(userId);
        var receiver = user?.Email;
        var subject = "Xác nhận đơn hàng";
        var message = $"""
                       <!DOCTYPE html>
                       <html lang="vi">
                       <head>
                           <meta charset="UTF-8" />
                           <title>Xác nhận đơn hàng</title>
                       </head>
                       <body style="font-family: Arial, sans-serif; background-color: #f9f9f9; margin: 0; padding: 0;">
                           <table align="center" width="600" cellpadding="0" cellspacing="0" 
                                  style="background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.1); padding: 20px;">
                               <tr>
                                   <td style="text-align: center; padding-bottom: 20px;">
                                       <h2 style="color: #007bff; margin: 0;">Cảm ơn bạn đã đặt hàng!</h2>
                                   </td>
                               </tr>
                               <tr>
                                   <td style="padding: 10px 0; font-size: 16px; color: #333333;">
                                       <p>Chào <strong>{order.UserName}</strong>,</p>
                                       <p>Cảm ơn bạn đã đặt hàng tại cửa hàng của chúng tôi.</p>
                                       <p>Mã đơn hàng của bạn là: <strong style="color: #28a745; font-size: 18px;">{orderCode}</strong></p>
                                       <p>Chúng tôi sẽ xử lý đơn hàng của bạn trong thời gian sớm nhất.</p>
                                       <p>Nếu bạn có bất kỳ câu hỏi nào, vui lòng liên hệ với chúng tôi qua email này hoặc số điện thoại hỗ trợ.</p>
                                   </td>
                               </tr>

                               <!-- Bảng chi tiết sản phẩm -->
                               <tr>
                                   <td style="padding: 10px 0;">
                                       <table width="100%" cellpadding="8" cellspacing="0" 
                                              style="border-collapse: collapse; font-size: 14px; border: 1px solid #ddd;">
                                           <thead style="background-color: #f2f2f2;">
                                               <tr>
                                                   <th align="left" style="border: 1px solid #ddd;">Tên sản phẩm</th>
                                                   <th align="center" style="border: 1px solid #ddd;">Số lượng</th>
                                                   <th align="right" style="border: 1px solid #ddd;">Giá</th>
                                                   <th align="right" style="border: 1px solid #ddd;">Thành tiền</th>
                                               </tr>
                                           </thead>
                                           <tbody>
                                               {string.Join("", order.OrderDetails.Select(d => $"""
                                                    "
                                                                            <tr>
                                                                                <td style='border: 1px solid #ddd;'>{d.Product.Name}</td>
                                                                                <td align='center' style='border: 1px solid #ddd;'>{d.Quantity}</td>
                                                                                <td align='right' style='border: 1px solid #ddd;'>{d.Price:C0}</td>
                                                                                <td align='right' style='border: 1px solid #ddd;'>{(d.Quantity * d.Price):C0}</td>
                                                                            </tr>
                                                                            
                                                    """))}
                                               <tr>
                                                   <td colspan="3" align="right" style="border: 1px solid #ddd; font-weight: bold;">Tổng cộng</td>
                                                   <td align="right" style="border: 1px solid #ddd; font-weight: bold; color: #d9534f;">
                                                       {order.OrderDetails.Sum(d => d.Quantity * d.Price):C0}
                                                   </td>
                                               </tr>
                                           </tbody>
                                       </table>
                                   </td>
                               </tr>

                               <tr>
                                   <td style="text-align: center; padding-top: 20px;">
                                       <a asp-action="Index" asp-controller="Home"
                                          style="background-color: #007bff; color: #ffffff; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold;">
                                          Xem chi tiết đơn hàng tại webstie
                                       </a>
                                   </td>
                               </tr>
                               <tr>
                                   <td style="font-size: 12px; color: #999999; text-align: center; padding-top: 30px;">
                                       © 2025 Your Shop. Bản quyền thuộc về Your Shop.
                                   </td>
                               </tr>
                           </table>
                       </body>
                       </html>
                       """;


        await _emailSender.SendEmailAsync(receiver, subject, message);

        TempData["Success"] = $"Đặt hàng thành công! Mã đơn hàng của bạn là: {orderCode}";

        return RedirectToAction("Index", "Home");
    }
}