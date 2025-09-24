using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // EF Core namespace
using MyWebApp.Data;
using MyWebApp.Interface.Service;
using MyWebApp.Models;
using Newtonsoft.Json.Linq;

namespace MyWebApp.Areas.User.Controllers;

public class CheckoutController : BaseController
{
    private readonly IEmailSender _emailSender;
    private readonly IGhnService _ghnService;

    public CheckoutController(DataContext context, IEmailSender emailSender, IGhnService ghnService) : base(context)
    {
        _emailSender = emailSender;
        _ghnService = ghnService;
    }

    [HttpPost]
    public async Task<IActionResult> Checkout(Guid shipingId, string discountCode, string fullAddress,
        string PaymentMethod, string PhoneNumber)
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

        // Lấy phí ship từ DB
        if (shipingId == Guid.Empty)
        {
            TempData["Error"] = "Chưa chọn phí vận chuyển!";
            return RedirectToAction("Index", "Cart");
        }

        var shipping = await _context.Shipings.FirstOrDefaultAsync(x => x.Id == shipingId);
        if (shipping == null)
        {
            TempData["Error"] = "Không tìm thấy phí vận chuyển.";
            return RedirectToAction("Index", "Cart");
        }

        decimal shippingFee = shipping.Price;

        var orderCode = Guid.NewGuid().ToString();
        var subtotal = cart.CartItems
            .Where(ci => !ci.IsGift)
            .Sum(i => (i.Product.DiscountPrice ?? i.Product.Price) * i.Quantity);
        // Kiểm tra mã giảm giá
        decimal discountAmount = 0;
        if (!string.IsNullOrEmpty(discountCode))
        {
            var now = DateTime.Now;
            var discount = await _context.DiscountCodes
                .FirstOrDefaultAsync(d =>
                    d.Code == discountCode && d.IsActive && d.StartDate <= now && d.EndDate >= now);

            if (discount != null)
            {
                if (discount.UsageLimit == 0 || discount.UsedCount < discount.UsageLimit)
                {
                    discountAmount = (decimal)(discount.DiscountAmount +
                                               subtotal * (decimal?)(discount.DiscountPercent ?? 0) / 100);
                    discount.UsedCount++;
                    _context.DiscountCodes.Update(discount);
                }
            }
        }

        string paymentMethodDisplay = PaymentMethod switch
        {
            "COD" => "Thanh toán khi nhận hàng",
            "MOMO" => "Thanh toán qua Momo",
            _ => "Khác"
        };
        if (PaymentMethod == "COD")
        {
            // Tổng cộng cả phí ship
            var order = new Order
            {
                OrderId = Guid.NewGuid(),
                OrderCode = orderCode,
                UserName = User.Identity?.Name ?? "Unknown",
                OrderDate = DateTime.Now,
                Status = 1,
                Address = shipping.City + ", " + shipping.District + ", " + fullAddress,
                ShippingFee = shippingFee,
                TotalDiscount = discountAmount,
                TotalAmount = subtotal + shippingFee - discountAmount,
                FullAddress = fullAddress,
                PaymentMethod = paymentMethodDisplay,
                PhoneNumber = PhoneNumber
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
                    Product = item.Product,
                    Price = item.IsGift ? 0 : (item.Product.DiscountPrice ?? item.Product.Price),
                    Quantity = item.Quantity,
                    IsGift = item.IsGift
                };
                _context.OrderDetails.Add(orderDetail);

                if (item.IsGift)
                {
                    item.Product.Quantity -= item.Quantity;
                    if (item.Product.Quantity < 0)
                        item.Product.Quantity = 0;
                }

                if (!item.IsGift)
                {
                    item.Product.Quantity -= item.Quantity;
                    item.Product.Sold += item.Quantity;
                    if (item.Product.Quantity < 0)
                        item.Product.Quantity = 0;
                }

                _context.Products.Update(item.Product);
            }

            // Sau khi lưu order + orderDetails vào DB
            await _context.SaveChangesAsync();

            // --- Gọi GHN API tối giản ---
            var ghnOrderCode = await _ghnService.CreateOrderAsync(order);
            if (!string.IsNullOrEmpty(ghnOrderCode))
            {
                order.GHNOrderCode = ghnOrderCode;
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();
            }

            // Xóa giỏ hàng sau khi đặt thành công
            _context.CartItems.RemoveRange(cart.CartItems);
            _context.Carts.Remove(cart);
            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(userId);
            var receiver = user?.Email;
            var subject = "Xác nhận đơn hàng";

            var message = $$$$"""
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
                                                  <p>Chào <strong>{{{{order.UserName}}}}</strong>,</p>
                                                  <p>Cảm ơn bạn đã đặt hàng tại cửa hàng của chúng tôi.</p>
                                                  <p>Mã đơn hàng của bạn là: <strong>{{{{order.OrderCode}}}}</strong></p>
                                                  <p>Phương thức thanh toán: <strong>{{{{paymentMethodDisplay}}}}</strong></p>
                                                  <p>Địa chỉ giao hàng: <strong>{{{{order.FullAddress}}}},{{{{shipping.District}}}},{{{{shipping.City}}}}</strong></p>

                                              </td>
                                          </tr>
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
                                                          {{{{string.Join("", order.OrderDetails.Select(d => $$"""
                                                                    <tr>
                                                                        <td style='border: 1px solid #ddd;'>{{d.Product.Name}}{{(d.IsGift ? " <span style='color: green;'>(Quà tặng)</span>" : "")}}</td>
                                                                        <td align='center' style='border: 1px solid #ddd;'>{{d.Quantity}}</td>
                                                                        <td align='right' style='border: 1px solid #ddd;'>{{d.Price:C0}}</td>
                                                                        <td align='right' style='border: 1px solid #ddd;'>{{(d.Quantity * d.Price):C0}}</td>
                                                                    </tr>
                                                                """))}}}}
                                                          <tr>
                                                              <td colspan="3" align="right" style="border: 1px solid #ddd;">Phí vận chuyển</td>
                                                              <td align="right" style="border: 1px solid #ddd;">{{{{order.ShippingFee:C0}}}}</td>
                                                          </tr>
                                                          <tr>
                                                             <td colspan="3" align="right" style="border: 1px solid #ddd;">Mã giảm giá</td>
                                                             <td align="right" style="border: 1px solid #ddd;">{{{{discountAmount:C0}}}}</td>
                                                         </tr>
                                                          <tr>
                                                              <td colspan="3" align="right" style="border: 1px solid #ddd; font-weight: bold;">Tổng cộng</td>
                                                              <td align="right" style="border: 1px solid #ddd; font-weight: bold; color: #d9534f;">
                                                                  {{{{order.TotalAmount:C0}}}}
                                                              </td>
                                                          </tr>
                                                      </tbody>
                                                  </table>
                                              </td>
                                          </tr>
                                          <tr>
                                              <td style="text-align: center; padding-top: 20px;">
                                                  <a href="https://yourdomain.com/User/Order/Index"
                                                     style="background-color: #007bff; color: #ffffff; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold;">
                                                     Xem chi tiết đơn hàng
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

        // Nếu Momo, redirect sang action CreateMomoPayment
        return RedirectToAction("CreateMomoPayment", new
        {
            FullAddress = fullAddress,
            shipingId,
            discountCode
        });
    }

    [HttpGet]
    public async Task<IActionResult> CreateMomoPayment(string FullAddress, Guid shipingId, string discountCode,
        string phone)
    {
        if (string.IsNullOrEmpty(FullAddress))
        {
            TempData["Error"] = "Địa chỉ giao hàng chưa được cung cấp!";
            return RedirectToAction("Index", "Cart");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return RedirectToAction("Login", "Account");

        var cart = await _context.Carts
            .Include(c => c.CartItems)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null || !cart.CartItems.Any())
        {
            TempData["Error"] = "Giỏ hàng trống.";
            return RedirectToAction("Index", "Cart");
        }

        var shipping = await _context.Shipings.FindAsync(shipingId);
        decimal shippingFee = shipping?.Price ?? 0;

        decimal subtotal = cart.CartItems.Sum(i => (i.Product.DiscountPrice ?? i.Product.Price) * i.Quantity);

        decimal discountAmount = 0;
        if (!string.IsNullOrEmpty(discountCode))
        {
            var now = DateTime.Now;
            var discount = await _context.DiscountCodes
                .FirstOrDefaultAsync(d =>
                    d.Code == discountCode && d.IsActive && d.StartDate <= now && d.EndDate >= now);

            if (discount != null)
                discountAmount = (decimal)(discount.DiscountAmount +
                                           subtotal * (decimal?)(discount.DiscountPercent ?? 0) / 100);
        }

        decimal totalAmount = subtotal + shippingFee - discountAmount;
        long totalAmountLong = (long)totalAmount;

        // Tạo order tạm thời
        var orderCode = Guid.NewGuid().ToString();
        var order = new Order
        {
            OrderId = Guid.NewGuid(),
            OrderCode = orderCode,
            UserName = User.Identity?.Name ?? "Unknown",
            OrderDate = DateTime.Now,
            Address = shipping?.City + ", " + shipping?.District + ", " + FullAddress,
            Status = 0, // Chưa thanh toán
            ShippingFee = shippingFee,
            TotalDiscount = discountAmount,
            TotalAmount = totalAmountLong, // max là 50 củ, kiểu long
            FullAddress = FullAddress,
            PaymentMethod = "MOMO",
            PhoneNumber = phone
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
                Product = item.Product,
                Price = item.Product.DiscountPrice ?? item.Product.Price,
                Quantity = item.Quantity
            };
            _context.OrderDetails.Add(orderDetail);
            _context.Products.Update(item.Product);
        }

        await _context.SaveChangesAsync();

        // Thông tin MoMo
        var momoEndpoint = "https://test-payment.momo.vn/gw_payment/transactionProcessor";
        var partnerCode = "MOMO";
        var accessKey = "F8BBA842ECF85";
        var secretKey = "K951B6PE1waDMi640xX08PD3vg6EkVlz";
        var returnUrl = "https://localhost:5182/Checkout/PaymentCallBack";
        var notifyUrl = "https://localhost:5182/Checkout/MomoNotify";

        string orderId = order.OrderId.ToString();
        string requestId = Guid.NewGuid().ToString();
        string amount = totalAmount.ToString("0");
        string orderInfo = $"Thanh toán đơn hàng {order.OrderCode}";
        string requestType = "captureMoMoWallet";

        string rawHash =
            $"partnerCode={partnerCode}" +
            $"&accessKey={accessKey}" +
            $"&requestId={requestId}" +
            $"&amount={amount}" +
            $"&orderId={orderId}" +
            $"&orderInfo={orderInfo}" +
            $"&returnUrl={returnUrl}" +
            $"&notifyUrl={notifyUrl}" +
            $"&extraData=";
        string signature;
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
        {
            signature = Convert.ToHexStringLower(hmac.ComputeHash(Encoding.UTF8.GetBytes(rawHash))
            );
        }

        var payload = new
        {
            partnerCode,
            accessKey,
            requestId,
            amount,
            orderId,
            orderInfo,
            returnUrl,
            notifyUrl,
            extraData = "",
            requestType,
            signature
        };

        using var client = new HttpClient();
        var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), Encoding.UTF8,
            "application/json");
        var response = await client.PostAsync(momoEndpoint, content);
        var responseString = await response.Content.ReadAsStringAsync();
        var json = JObject.Parse(responseString);
        var payUrl = json["payUrl"]?.ToString();

        if (!string.IsNullOrEmpty(payUrl))
            return Redirect(payUrl);

        TempData["Error"] = "Thanh toán MoMo không thành công.";
        return RedirectToAction("Index", "Cart");
    }

    [HttpGet]
    public async Task<IActionResult> PaymentCallBack(string orderId, string errorCode)
    {
        var order = await _context.Orders.FindAsync(Guid.Parse(orderId));
        if (order == null)
            return NotFound();

        if (errorCode == "0") // Thanh toán thành công
        {
            order.Status = 1;

            var orderDetails = _context.OrderDetails
                .Include(od => od.Product)
                .Where(od => od.OrderId == order.OrderId)
                .ToList();

            foreach (var detail in orderDetails)
            {
                detail.Product.Quantity -= detail.Quantity;
                detail.Product.Sold += detail.Quantity;
                if (detail.Product.Quantity < 0)
                    detail.Product.Quantity = 0;

                _context.Products.Update(detail.Product);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Xoá giỏ hàng
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart != null)
            {
                _context.CartItems.RemoveRange(cart.CartItems);
                _context.Carts.Remove(cart);
            }
        }
        else
        {
            order.Status = -1; // thất bại
        }

        _context.Orders.Update(order);
        await _context.SaveChangesAsync();

        TempData["Success"] = order.Status == 1
            ? $"Thanh toán thành công cho đơn hàng {order.OrderCode}"
            : $"Thanh toán thất bại cho đơn hàng {order.OrderCode}";
        return RedirectToAction(order.Status == 1 ? "Index" : "Cart", "Cart");
    }
}