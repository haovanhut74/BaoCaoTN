using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;

namespace MyWebApp.Areas.Admin.Controllers;

public class OrderController : BaseController
{
    public OrderController(DataContext context) : base(context) { }

    // Hiển thị danh sách đơn hàng
    public async Task<IActionResult> Index()
    {
        var orders = await _context.Orders
            .Include(o => o.OrderDetails) // Include để đếm số sản phẩm
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return View(orders);
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
}