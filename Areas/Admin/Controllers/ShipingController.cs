using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;
using MyWebApp.Models;

namespace MyWebApp.Areas.Admin.Controllers;

[Area("Admin")]
public class ShipingController : BaseController
{
    public ShipingController(DataContext context) : base(context) { }

    // Danh sách phí giao hàng
    public async Task<IActionResult> Index()
    {
        var list = await _context.Shipings.ToListAsync();
        return View(list);
    }

    // Hiển thị form tạo mới
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    // Lưu phí ship mới
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Shiping shiping)
    {
        if (!ModelState.IsValid)
            return View(shiping);

        var exist = await _context.Shipings.AnyAsync(x => x.City == shiping.City && x.District == shiping.District);
        if (exist)
        {
            ModelState.AddModelError("", "Phí vận chuyển cho khu vực này đã tồn tại.");
            return View(shiping);
        }

        _context.Shipings.Add(shiping);
        await _context.SaveChangesAsync();
        return RedirectToAction("Index");
    }
}