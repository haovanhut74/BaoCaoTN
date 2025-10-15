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

        // Luôn cập nhật quà trước khi hiển thị
        await UpdateGiftItemsAsync(cart);

        // Tải tất cả CartItems trước, sau đó filter ở client-side
        var allCartItems = await _context.CartItems
            .Include(ci => ci.Product)
            .Where(ci => ci.CartId == cart.Id)
            .ToListAsync();

        var cartViewModel = new CartItemViewModel
        {
            CartItems = allCartItems.Select(ci => new CartItemDisplayViewModel
            {
                CartItemId = ci.Id,
                ProductId = ci.ProductId,
                ProductName = ci.Product.Name,
                ImageUrl = ci.Product.MainImage,
                Quantity = ci.Quantity,
                Price = ci.Price, // giá gốc
                DiscountPrice = ci.Product.DiscountPrice,
                IsGift = ci.IsGift,
                GiftPromotionId = ci.GiftPromotionId
            }).ToList(),

            TotalPrice = allCartItems
                .Where(ci => !ci.IsGift)
                .Sum(ci =>
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
        // === Tự động thêm quà tặng ===
        var promotions = await _context.GiftPromotions
            .Include(g => g.GiftProduct)
            .Where(g => g.RequiredProductId == id &&
                        g.IsActive &&
                        g.StartDate <= DateTime.Now &&
                        g.EndDate.AddDays(1) >= DateTime.Now)
            .ToListAsync();

        foreach (var promo in promotions)
        {
            var giftProductId = promo.GiftProductId;
            var existingGift = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == cart.Id &&
                                           ci.ProductId == giftProductId &&
                                           ci.GiftPromotionId == promo.Id);

            if (existingItem != null && existingItem.Quantity >= promo.QuantityRequired && existingGift == null)
            {
                _context.CartItems.Add(new CartItem
                {
                    Id = Guid.NewGuid(),
                    CartId = cart.Id,
                    ProductId = giftProductId,
                    Quantity = promo.QuantityGift,
                    Price = 0,
                    IsGift = true,
                    GiftPromotionId = promo.Id,
                });
                await _context.SaveChangesAsync();
            }
        }

        // Reload cart để cập nhật số lượng
        cart = await GetOrCreateCartAsync();
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
        var item = cart.CartItems.FirstOrDefault(ci => ci.Id == id && !ci.IsGift);
        if (item == null)
            return Json(new
            {
                success = false, message = "Không tìm thấy sản phẩm!"
            });

        var product = await _context.Products.FindAsync(item.ProductId);
        if (product != null && item.Quantity >= product.Quantity)
            return Json(new
            {
                success = false,
                message = $"Không thể thêm nữa. Sản phẩm {product.Name} chỉ còn {product.Quantity} cái!"
            });

        item.Quantity++;

        await _context.SaveChangesAsync();

        // --- Cập nhật quà tặng ---
        await UpdateGiftItemsAsync(cart);
        var subtotal = (item.Product.DiscountPrice ?? item.Price) * item.Quantity;
        var totalPrice = cart.CartItems.Where(ci => !ci.IsGift)
            .Sum(ci => (ci.Product.DiscountPrice ?? ci.Price) * ci.Quantity);

        return Json(new
        {
            success = true,
            newQuantity = item.Quantity,
            newSubtotal = subtotal,
            newTotalPrice = totalPrice
        });
    }

    private async Task UpdateGiftItemsAsync(Cart cart)
    {
        var promotions = await _context.GiftPromotions
            .Include(p => p.RequiredProduct)
            .Include(p => p.GiftProduct)
            .Where(p => p.IsActive && p.StartDate <= DateTime.Now && p.EndDate.AddDays(1) >= DateTime.Now)
            .ToListAsync();

        var cartItems = await _context.CartItems
            .Where(ci => ci.CartId == cart.Id)
            .ToListAsync();

        var added = new List<CartItem>();
        var removed = new List<Guid>();
        var updated = new List<CartItem>();

        foreach (var promo in promotions)
        {
            var requiredItem = cartItems.FirstOrDefault(ci => ci.ProductId == promo.RequiredProductId && !ci.IsGift);
            int requiredQty = requiredItem?.Quantity ?? 0;
            int expectedGiftQty = requiredQty / promo.QuantityRequired;

            foreach (var gift in promo.GiftProduct != null ? new[] { promo.GiftProduct } : Array.Empty<Product>())
            {
                var giftItem = cartItems.FirstOrDefault(ci =>
                    ci.ProductId == gift.Id && ci.IsGift && ci.GiftPromotionId == promo.Id);

                if (expectedGiftQty > 0)
                {
                    if (giftItem == null)
                    {
                        var newGift = new CartItem
                        {
                            Id = Guid.NewGuid(),
                            CartId = cart.Id,
                            ProductId = gift.Id,
                            Quantity = expectedGiftQty,
                            Price = 0,
                            IsGift = true,
                            GiftPromotionId = promo.Id
                        };
                        _context.CartItems.Add(newGift);
                        added.Add(newGift);
                    }
                    else if (giftItem.Quantity != expectedGiftQty)
                    {
                        giftItem.Quantity = expectedGiftQty;
                        updated.Add(giftItem);
                    }
                }
                else if (giftItem != null)
                {
                    _context.CartItems.Remove(giftItem);
                    removed.Add(giftItem.Id);
                }
            }
        }

        await _context.SaveChangesAsync();
    }


    [HttpPost]
    public async Task<IActionResult> Decrease(Guid id)
    {
        var cart = await GetOrCreateCartAsync();
        var item = cart.CartItems.FirstOrDefault(ci => ci.Id == id && !ci.IsGift);
        if (item == null) return Json(new { success = false, message = "Không tìm thấy sản phẩm!" });

        if (item.Quantity > 1)
        {
            item.Quantity--;
        }
        else
        {
            // Xóa sản phẩm nếu quantity = 1
            cart.CartItems.Remove(item);
        }

        await _context.SaveChangesAsync();

        // --- Cập nhật quà tặng ---
        await UpdateGiftItemsAsync(cart);

        var subtotal = (item.Product.DiscountPrice ?? item.Price) * item.Quantity;
        var totalPrice = cart.CartItems.Where(ci => !ci.IsGift)
            .Sum(ci => (ci.Product.DiscountPrice ?? ci.Price) * ci.Quantity);

        bool isEmpty = !cart.CartItems.Any();

        return Json(new
        {
            success = true,
            newQuantity = item.Quantity,
            newSubtotal = subtotal,
            newTotalPrice = totalPrice,
            removed = item.Quantity == 0,
            cartEmpty = isEmpty
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
            // === Xóa quà tặng nếu không còn sản phẩm chính ===
            var promotions = await _context.GiftPromotions
                .Where(g => g.RequiredProductId == item.ProductId)
                .ToListAsync();

            foreach (var promo in promotions)
            {
                var giftsToRemove = cart.CartItems
                    .Where(ci => ci.GiftPromotionId == promo.Id && ci.IsGift)
                    .ToList();

                foreach (var gift in giftsToRemove)
                {
                    cart.CartItems.Remove(gift);
                }
            }

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
            .FirstOrDefaultAsync(d => d.Code == code && d.IsActive && d.StartDate <= now && d.EndDate.AddDays(1) >= now);

        if (discount == null)
            return Json(new { success = false, message = "Mã giảm giá không hợp lệ hoặc hết hạn" });

        // Kiểm tra số lượt sử dụng
        if (discount.UsageLimit > 0 && discount.UsedCount >= discount.UsageLimit)
            return Json(new { success = false, message = "Mã giảm giá đã hết lượt sử dụng" });

        return Json(new
        {
            success = true, discountAmount = discount.DiscountAmount,
            discountPercent = discount.DiscountPercent ?? 0,
            message = "Áp dụng mã thành công"
        });
    }

    [HttpPost]
    public async Task<IActionResult> GetGiftItems(Guid removedId)
    {
        var cart = await GetOrCreateCartAsync();
        var item = cart.CartItems.FirstOrDefault(ci => ci.Id == removedId);
        if (item == null) return Json(new { giftItemIds = new List<Guid>() });

        var promotions = await _context.GiftPromotions
            .Where(g => g.RequiredProductId == item.ProductId && g.QuantityRequired == item.Quantity)
            .ToListAsync();

        var giftItemIds = cart.CartItems
            .Where(ci => ci.IsGift && promotions.Any(p => p.Id == ci.GiftPromotionId))
            .Select(ci => ci.Id)
            .ToList();

        return Json(new { giftItemIds });
    }

    [HttpPost]
    public async Task<IActionResult> CheckGiftItems()
    {
        var cart = await GetOrCreateCartAsync();
        bool hasGifts = cart.CartItems.Any(ci => ci.IsGift);
        return Json(new { hasGifts });
    }

    [HttpPost]
    public async Task<IActionResult> RefreshGiftItems()
    {
        var cart = await GetOrCreateCartAsync();
        await UpdateGiftItemsAsync(cart); // Cập nhật quà tặng

        var giftItems = cart.CartItems
            .Where(ci => ci.IsGift)
            .Select(ci => new
            {
                CartItemId = ci.Id,
                ProductId = ci.ProductId,
                ProductName = ci.Product.Name,
                ImageUrl = ci.Product.MainImage,
                Quantity = ci.Quantity,
                Price = 0, // Quà tặng giá 0
                IsGift = ci.IsGift,
                GiftPromotionId = ci.GiftPromotionId
            }).ToList();

        var hasGifts = giftItems.Any();
        var totalPrice = cart.CartItems
            .Where(ci => !ci.IsGift)
            .Sum(ci => (ci.Product.DiscountPrice ?? ci.Price) * ci.Quantity);

        return Json(new
        {
            success = true,
            giftItems,
            hasGifts,
            newTotalPrice = totalPrice
        });
    }
}