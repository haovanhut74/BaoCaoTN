using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Areas.Permission;
using MyWebApp.Data;
using MyWebApp.ViewModels;

namespace MyWebApp.Areas.Admin.Controllers;

[HasPermission("ManageOrders")]
public class OrderController : BaseController
{
    public OrderController(DataContext context) : base(context) { }

    // Hiển thị danh sách đơn hàng
    public async Task<IActionResult> Index(string? orderCode, string? userName, string? status, string? dateFrom,
        string? dateTo, int page = 1)
    {
        var query = _context.Orders.Include(o => o.OrderDetails).AsQueryable();

        if (!string.IsNullOrWhiteSpace(orderCode))
            query = query.Where(o => o.OrderCode.Contains(orderCode));

        if (!string.IsNullOrWhiteSpace(userName))
            query = query.Where(o => o.UserName.Contains(userName));

        if (!string.IsNullOrWhiteSpace(status) && int.TryParse(status, out int s))
            query = query.Where(o => o.Status == s);

        if (!string.IsNullOrWhiteSpace(dateFrom) && DateTime.TryParse(dateFrom, out var fromDate))
            query = query.Where(o => o.OrderDate.Date >= fromDate.Date);

        if (!string.IsNullOrWhiteSpace(dateTo) && DateTime.TryParse(dateTo, out var toDate))
        {
            toDate = toDate.Date.AddDays(1).AddTicks(-1);
            query = query.Where(o => o.OrderDate <= toDate);
        }

        int pageSize = 10;
        int totalItems = await query.CountAsync();
        var orders = await query.OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var viewModel = new ListViewModel
        {
            Orders = orders,
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling((double)totalItems / pageSize),
            OrderCode = orderCode,
            UserName = userName,
            Status = status,
            DateFrom = dateFrom,
            DateTo = dateTo
        };

        return View(viewModel);
    }


    // Chi tiết đơn hàng
    [HasPermission("ViewOrder")]
    public async Task<IActionResult> Detail(Guid id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product) //❗ cần include Product
            .FirstOrDefaultAsync(o => o.OrderId == id);

        if (order == null) return NotFound();

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(Guid id, int newStatus)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound();

        order.Status = newStatus;
        await _context.SaveChangesAsync();

        return RedirectToAction("Detail", new { id });
    }
}