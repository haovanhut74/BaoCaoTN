using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;
using MyWebApp.Models;

namespace MyWebApp.Areas.Admin.Controllers;

public class DiscountCodesController : BaseController
{
    public DiscountCodesController(DataContext context) : base(context) { }

    // GET: Admin/DiscountCodes
    public async Task<IActionResult> Index()
    {
        var codes = await _context.DiscountCodes.ToListAsync();
        return View(codes);
    }

    // GET: Admin/DiscountCodes/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Admin/DiscountCodes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DiscountCode discountCode)
    {
        if (ModelState.IsValid)
        {
            discountCode.Id = Guid.NewGuid(); 
            _context.DiscountCodes.Add(discountCode);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        return View(discountCode);
    }

    // GET: Admin/DiscountCodes/Edit/{id}
    public async Task<IActionResult> Edit(Guid id)
    {
        var discountCode = await _context.DiscountCodes.FindAsync(id);
        if (discountCode == null) return NotFound();
        return View(discountCode);
    }

    // POST: Admin/DiscountCodes/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, DiscountCode discountCode)
    {
        if (ModelState.IsValid)
        {
            var existing = await _context.DiscountCodes.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Code = discountCode.Code;
            existing.DiscountAmount = discountCode.DiscountAmount;
            existing.DiscountPercent = discountCode.DiscountPercent;
            existing.StartDate = discountCode.StartDate;
            existing.EndDate = discountCode.EndDate;
            existing.IsActive = discountCode.IsActive;
            existing.UsageLimit = discountCode.UsageLimit;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        return View(discountCode);
    }

    // GET: Admin/DiscountCodes/Delete/{id}
    public async Task<IActionResult> Delete(Guid id)
    {
        var discountCode = await _context.DiscountCodes.FindAsync(id);
        if (discountCode == null) return NotFound();
        return View(discountCode);
    }

    // POST: Admin/DiscountCodes/Delete/{id}
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var discountCode = await _context.DiscountCodes.FindAsync(id);
        _context.DiscountCodes.Remove(discountCode);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}