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


    // GET: Edit
    public IActionResult Edit(Guid id)
    {
        var shiping = _context.Shipings.Find(id);
        if (shiping == null) return NotFound();
        return View(shiping);
    }

    // POST: Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
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
    public IActionResult Delete(Guid id)
    {
        var shiping = _context.Shipings.Find(id);
        if (shiping == null) return NotFound();
        return View(shiping);
    }

    [HttpPost]
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


    // // POST: Delete
    // [HttpPost, ActionName("Delete")]
    // [ValidateAntiForgeryToken]
    // public IActionResult DeleteConfirmed(Guid id)
    // {
    //     var shiping = _context.Shipings.Find(id);
    //     if (shiping == null) return NotFound();
    //     _context.Shipings.Remove(shiping);
    //     _context.SaveChanges();
    //     return RedirectToAction(nameof(Index));
    // }
}