using Microsoft.AspNetCore.Mvc;
using MyWebApp.Areas.Permission;
using MyWebApp.Data;
using MyWebApp.Models;

namespace MyWebApp.Areas.Admin.Controllers;

[HasPermission("ManageSlider")]
public class SlidersController : BaseController
{
    public SlidersController(DataContext context, IWebHostEnvironment env) : base(context)
    {
        _env = env;
    }

    private readonly IWebHostEnvironment _env;

    // GET: Danh sách
    public IActionResult Index()
    {
        var sliders = _context.Sliders.ToList();
        return View(sliders);
    }

    // GET: Tạo mới
    [HttpGet]
    [HasPermission("CreateSlider")]
    public IActionResult Create()
    {
        return View();
    }

    // POST: Tạo mới
    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("CreateSlider")]
    public IActionResult Create(Sliders slider)
    {
        if (ModelState.IsValid)
        {
            if (slider.ImageFile != null)
            {
                string fileName = Guid.NewGuid() + Path.GetExtension(slider.ImageFile.FileName);
                string uploadPath = Path.Combine(_env.WebRootPath, "img/Slider");
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                using (var stream = new FileStream(Path.Combine(uploadPath, fileName), FileMode.Create))
                {
                    slider.ImageFile.CopyTo(stream);
                }

                slider.Image = fileName;
            }

            slider.Id = Guid.NewGuid();
            _context.Sliders.Add(slider);
            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        return View(slider);
    }

    // GET: Sửa
    [HttpGet]
    [HasPermission("EditSlider")]
    public IActionResult Edit(Guid id)
    {
        var slider = _context.Sliders.Find(id);
        if (slider == null) return NotFound();
        return View(slider);
    }

    // POST: Sửa
    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("EditSlider")]
    public IActionResult Edit(Guid id, Sliders slider)
    {
        if (id != slider.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var existing = _context.Sliders.Find(id);
            if (existing == null) return NotFound();

            existing.Title = slider.Title;
            existing.Description = slider.Description;
            existing.Status = slider.Status;

            if (slider.ImageFile != null)
            {
                string fileName = Guid.NewGuid() + Path.GetExtension(slider.ImageFile.FileName);
                string uploadPath = Path.Combine(_env.WebRootPath, "uploads/sliders");

                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                using (var stream = new FileStream(Path.Combine(uploadPath, fileName), FileMode.Create))
                {
                    slider.ImageFile.CopyTo(stream);
                }

                existing.Image = fileName;
            }

            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        return View(slider);
    }

    // POST: Xóa
    [HttpPost]
    [HasPermission("DeleteSlider")]
    public IActionResult Delete(Guid id)
    {
        var slider = _context.Sliders.Find(id);
        if (slider != null)
        {
            _context.Sliders.Remove(slider);
            _context.SaveChanges();
        }

        return RedirectToAction(nameof(Index));
    }
}