using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;

namespace MyWebApp.Areas.Admin.Controllers;

public class OrderController : BaseController
{
    public OrderController(DataContext context) : base(context) { }

    // Hiển thị danh sách đơn hàng
    public async Task<IActionResult> Index(string? orderCode, string? userName, string? status, string? dateFrom,
        string? dateTo)
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
            toDate = toDate.Date.AddDays(1).AddTicks(-1); // đến hết ngày đó
            query = query.Where(o => o.OrderDate <= toDate);
        }

        var list = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
        return View(list);
    }


    // Chi tiết đơn hàng
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