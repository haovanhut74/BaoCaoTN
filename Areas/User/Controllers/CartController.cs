using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;
using MyWebApp.Models;
using MyWebApp.ViewModels;
using System.Security.Claims;
using MyWebApp.Interface.Service;

namespace MyWebApp.Areas.User.Controllers;

[Area("User")]
[Authorize]
public class CartController : BaseController
{
    private readonly IGhnService _ghnService;

    public CartController(DataContext context, IGhnService ghnService) : base(context)
    {
        _ghnService = ghnService;
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private async Task<Cart> GetOrCreateCartAsync()
    {
        string userId = GetUserId();
        var cart = await _context.Carts
            .Include(c => c.CartItems)
            .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new Cart
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CartItems = new List<CartItem>()
            };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();
        }

        return cart;
    }

    public async Task<IActionResult> Index()
    {
        var cart = await GetOrCreateCartAsync();

        var cartViewModel = new CartItemViewModel
        {
            CartItems = cart.CartItems.Select(ci => new CartItemDisplayViewModel
            {
                CartItemId = ci.Id,
                ProductId = ci.ProductId,
                ProductName = ci.Product.Name,
                ImageUrl = ci.Product.MainImage,
                Quantity = ci.Quantity,
                Price = ci.Price, // giá gốc
                DiscountPrice = ci.Product.DiscountPrice // thêm giá giảm
            }).ToList(),

            TotalPrice = cart.CartItems.Sum(ci =>
                (ci.Product.DiscountPrice ?? ci.Price) * ci.Quantity)
        };

        return View(cartViewModel);
    }

    public async Task<IActionResult> Detail()
    {
        var cart = await GetOrCreateCartAsync();

        if (cart.CartItems == null || !cart.CartItems.Any())
        {
            TempData["Info"] = "Giỏ hàng trống!";
            return RedirectToAction("Index");
        }

        var cartViewModel = new CartItemViewModel
        {
            CartItems = cart.CartItems.Select(ci => new CartItemDisplayViewModel
            {
                CartItemId = ci.Id,
                ProductId = ci.ProductId,
                ProductName = ci.Product.Name,
                ImageUrl = ci.Product.MainImage,
                Quantity = ci.Quantity,
                Price = ci.Price, // giá gốc
                DiscountPrice = ci.Product.DiscountPrice,
            }).ToList(),

            TotalPrice = cart.CartItems.Sum(ci =>
                (ci.Product.DiscountPrice ?? ci.Price) * ci.Quantity)
        };

        return View(cartViewModel);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return Json(new { success = false, message = "❌ Sản phẩm không tồn tại!" });
        }

        var cart = await GetOrCreateCartAsync();
        var existingItem = await _context.CartItems
            .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.ProductId == id);

        if (existingItem != null)
        {
            if (existingItem.Quantity + 1 > product.Quantity)
            {
                return Json(new
                {
                    success = false,
                    message = $"⚠️ Sản phẩm \"{product.Name}\" chỉ còn {product.Quantity} cái trong kho!"
                });
            }

            existingItem.Quantity++;
        }
        else
        {
            if (product.Quantity <= 0)
            {
                return Json(new
                {
                    success = false,
                    message = $"⚠️ Sản phẩm \"{product.Name}\" đã hết hàng!"
                });
            }

            var newItem = new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                ProductId = id,
                Quantity = 1,
                Price = product.DiscountPrice ?? product.Price
            };
            _context.CartItems.Add(newItem);
        }

        await _context.SaveChangesAsync();

        return Json(new
        {
            success = true,
            message = $"🛒 Đã thêm \"{product.Name}\" vào giỏ hàng!",
            cartCount = cart.CartItems.Sum(ci => ci.Quantity)
        });
    }


    [HttpPost]
    public async Task<IActionResult> Increase(Guid id)
    {
        var cart = await GetOrCreateCartAsync();
        var item = cart.CartItems.FirstOrDefault(ci => ci.Id == id);

        if (item != null)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product != null)
            {
                if (item.Quantity < product.Quantity)
                {
                    item.Quantity++;
                    var subtotal = (item.Product.DiscountPrice ?? item.Price) * item.Quantity;
                    var totalPrice = cart.CartItems.Sum(ci => (ci.Product.DiscountPrice ?? ci.Price) * ci.Quantity);
                    await _context.SaveChangesAsync();
                    return Json(new
                    {
                        success = true, newQuantity = item.Quantity, newSubtotal = subtotal, newTotalPrice = totalPrice
                    });
                }

                return Json(new
                {
                    success = false,
                    message = $"Không thể thêm nữa. Sản phẩm {product.Name} chỉ còn {product.Quantity} cái!"
                });
            }
        }

        return Json(new { });
    }


    [HttpPost]
    public async Task<IActionResult> Decrease(Guid id)
    {
        var cart = await GetOrCreateCartAsync();
        var item = cart.CartItems.FirstOrDefault(ci => ci.Id == id);

        if (item != null)
        {
            if (item.Quantity > 1)
            {
                item.Quantity--;
                await _context.SaveChangesAsync();
            }
            else
            {
                cart.CartItems.Remove(item);
                await _context.SaveChangesAsync();
                bool isEmpty = !cart.CartItems.Any();
                return Json(new
                {
                    success = true,
                    removed = true,
                    cartEmpty = isEmpty,
                    newTotalPrice = cart.CartItems.Sum(ci => (ci.Product.DiscountPrice ?? ci.Price) * ci.Quantity)
                });
            }
        }

        var subtotal = (item.Product.DiscountPrice ?? item.Price) * item.Quantity;
        var totalPrice = cart.CartItems.Sum(ci => (ci.Product.DiscountPrice ?? ci.Price) * ci.Quantity);
        bool cartEmptyAfter = !cart.CartItems.Any();

        return Json(new
        {
            success = true,
            newQuantity = item.Quantity,
            newSubtotal = subtotal,
            newTotalPrice = totalPrice,
            cartEmpty = cartEmptyAfter
        });
    }

    [HttpPost]
    public async Task<IActionResult> Remove(Guid id)
    {
        var cart = await GetOrCreateCartAsync();
        var item = cart.CartItems.FirstOrDefault(ci => ci.Id == id);

        if (item != null)
        {
            cart.CartItems.Remove(item);
            await _context.SaveChangesAsync();
        }

        bool isEmpty = !cart.CartItems.Any();
        var totalPrice = cart.CartItems.Sum(ci => (ci.Product.DiscountPrice ?? ci.Price) * ci.Quantity);

        return Json(new
        {
            success = true,
            removedId = id,
            cartEmpty = isEmpty,
            newTotalPrice = totalPrice
        });
    }


    [HttpGet]
    public async Task<IActionResult> GetShippingFee(string city, string district)
    {
        if (string.IsNullOrEmpty(city) || string.IsNullOrEmpty(district))
            return Json(new { id = Guid.Empty, price = 0m });

        var ship = await _context.Shipings
            .FirstOrDefaultAsync(s => s.City == city && s.District == district);

        if (ship == null)
            return Json(new { id = Guid.Empty, price = 0m });

        return Json(new { id = ship.Id, price = ship.Price });
    }

    [HttpGet]
    public async Task<IActionResult> ApplyDiscount(string code)
    {
        if (string.IsNullOrEmpty(code))
            return Json(new { success = false, message = "Mã giảm giá trống" });

        var now = DateTime.Now;
        var discount = await _context.DiscountCodes
            .FirstOrDefaultAsync(d => d.Code == code && d.IsActive && d.StartDate <= now && d.EndDate >= now);

        if (discount == null)
            return Json(new { success = false, message = "Mã giảm giá không hợp lệ hoặc hết hạn" });

        // Kiểm tra số lượt sử dụng
        if (discount.UsageLimit > 0 && discount.UsedCount >= discount.UsageLimit)
            return Json(new { success = false, message = "Mã giảm giá đã hết lượt sử dụng" });

        return Json(new
        {
            success = true, discountAmount = discount.DiscountAmount, discountPercent = discount.DiscountPercent ?? 0,
            message = "Áp dụng mã thành công"
        });
    }
}