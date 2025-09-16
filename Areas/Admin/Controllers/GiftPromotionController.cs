using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;
using MyWebApp.Models;

namespace MyWebApp.Areas.Admin.Controllers
{
    public class GiftPromotionController : BaseController
    {
        // GET: Index
        public GiftPromotionController(DataContext context) : base(context) { }

        public IActionResult Index()
        {
            var promos = _context.GiftPromotions
                .Include(g => g.RequiredProduct)
                .Include(g => g.GiftProduct)
                .ToList();
            return View(promos);
        }

        // GET: Create
        public IActionResult Create()
        {
            ViewBag.Products = new SelectList(_context.Products.ToList(), "Id", "Name");
            return View();
        }

        // POST: Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(GiftPromotion promotion)
        {
            ViewBag.Products = new SelectList(_context.Products.ToList(), "Id", "Name");
            if (ModelState.IsValid)
            {
                _context.GiftPromotions.Add(promotion);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Products = new SelectList(_context.Products, "Id", "Name");
            return View(promotion);
        }


        // POST: Edit
        public IActionResult Edit(Guid id)
        {
            var promo = _context.GiftPromotions.Find(id);
            if (promo == null) return NotFound();

            // Load danh sách sản phẩm cho dropdown
            ViewBag.Products = new SelectList(_context.Products, "Id", "Name", promo.RequiredProductId);
            ViewBag.GiftProducts = new SelectList(_context.Products, "Id", "Name", promo.GiftProductId);

            return View(promo);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(GiftPromotion promotion)
        {
            if (ModelState.IsValid)
            {
                // Nếu muốn gán navigation property
                promotion.RequiredProduct = _context.Products.Find(promotion.RequiredProductId);
                promotion.GiftProduct = _context.Products.Find(promotion.GiftProductId);

                _context.GiftPromotions.Update(promotion);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }

            // Nếu ModelState lỗi, reload dropdown
            ViewBag.Products = new SelectList(_context.Products, "Id", "Name", promotion.RequiredProductId);
            ViewBag.GiftProducts = new SelectList(_context.Products, "Id", "Name", promotion.GiftProductId);
            return View(promotion);
        }


        // POST: Delete Confirm
        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(Guid id)
        {
            var promo = _context.GiftPromotions.Find(id);
            if (promo == null) return NotFound();

            _context.GiftPromotions.Remove(promo);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}