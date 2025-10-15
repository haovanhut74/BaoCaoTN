using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Areas.Permission;
using MyWebApp.Data;
using MyWebApp.Models;

namespace MyWebApp.Areas.Admin.Controllers;

[HasPermission("ManageShiping")]
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
    [HasPermission("CreateShiping")]
    public IActionResult Create()
    {
        return View();
    }

    // Lưu phí ship mới
    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("CreateShiping")]
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


    // GET: Edit
    [HasPermission("EditShiping")]
    public IActionResult Edit(Guid id)
    {
        var shiping = _context.Shipings.Find(id);
        if (shiping == null) return NotFound();
        return View(shiping);
    }

    // POST: Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("EditShiping")]
    public IActionResult Edit(Shiping shiping)
    {
        if (ModelState.IsValid)
        {
            _context.Shipings.Update(shiping);
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        return View(shiping);
    }

    // GET: Delete
    [HasPermission("DeleteShiping")]
    public IActionResult Delete(Guid id)
    {
        var shiping = _context.Shipings.Find(id);
        if (shiping == null) return NotFound();
        return View(shiping);
    }

    [HttpPost]
    [HasPermission("DeleteShiping")]
    public IActionResult DeleteAjax(Guid id)
    {
        var ship = _context.Shipings.Find(id);
        if (ship == null)
        {
            return Json(new { success = false, message = "Không tìm thấy dữ liệu" });
        }

        _context.Shipings.Remove(ship);
        _context.SaveChanges();

        return Json(new { success = true });
    }
}