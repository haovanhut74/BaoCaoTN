using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;
using MyWebApp.ViewModels;

namespace MyWebApp.Areas.User.Controllers;

public class OrderController : BaseController
{
    public OrderController(DataContext context) : base(context) { }

    // Mặc định show tất cả đơn hàng của user hiện tại
    public async Task<IActionResult> Index(int page = 1)
    {
        int pageSize = 10; // Số sản phẩm hiển thị trên mỗi trang
        var totalItems = await _context.Orders.CountAsync();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        string currentUser = User.Identity.Name;

        var orders = await _context.Orders
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
            .Where(o => o.UserName == currentUser)
            .OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        var viewModel = new ListViewModel
        {
            Orders = orders,
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling((double)totalItems / pageSize)
        };
        return View(viewModel);
    }

    // Kết quả tra cứu
    [HttpPost]
    public async Task<IActionResult> Search(string orderCode, string? status, string? dateFrom, string? dateTo, int page = 1)
    {
        int pageSize = 10;
        string currentUser = User.Identity.Name;

        var query = _context.Orders
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
            .Where(o => o.UserName == currentUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(orderCode))
            query = query.Where(o => o.GHNOrderCode.Contains(orderCode));

        if (!string.IsNullOrWhiteSpace(status) && int.TryParse(status, out int s))
            query = query.Where(o => o.Status == s);

        if (!string.IsNullOrWhiteSpace(dateFrom) && DateTime.TryParse(dateFrom, out var fromDate))
            query = query.Where(o => o.OrderDate.Date >= fromDate.Date);

        if (!string.IsNullOrWhiteSpace(dateTo) && DateTime.TryParse(dateTo, out var toDate))
        {
            toDate = toDate.Date.AddDays(1).AddTicks(-1);
            query = query.Where(o => o.OrderDate <= toDate);
        }

        var totalItems = await query.CountAsync();
        var orders = await query
            .OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var viewModel = new ListViewModel
        {
            Orders = orders,
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling((double)totalItems / pageSize),
            OrderCode = orderCode,
            Status = status,
            DateFrom = dateFrom,
            DateTo = dateTo,
            UserName = currentUser
        };

        return View("Index", viewModel);
    }


    // Chi tiết đơn hàng
    public async Task<IActionResult> Detail(Guid id)
    {
        string currentUser = User.Identity.Name;
        var order = await _context.Orders
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.OrderId == id && o.UserName == currentUser);

        if (order == null) return NotFound();
        return View(order);
    }
}