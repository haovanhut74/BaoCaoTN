using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Areas.Permission;
using MyWebApp.Data;
using MyWebApp.Models;
using MyWebApp.ViewModels;

namespace MyWebApp.Areas.Admin.Controllers;

[HasPermission("ManageProducts")]
public class ProductSpecificationController : BaseController
{
    public ProductSpecificationController(DataContext context) : base(context) { }

    // Quản lý thông số của 1 sản phẩm
    public async Task<IActionResult> Index(Guid productId)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null) return NotFound();

        var specs = await _context.ProductSpecifications
            .Include(s => s.SpecName) // thêm dòng này
            .Where(s => s.ProductId == productId)
            .ToListAsync();

        ViewBag.Product = product;
        return View(specs);
    }

    [HttpGet]
    [HasPermission("CreateProduct")]
    public async Task<IActionResult> Create(Guid productId)
    {
        ViewBag.Product = await _context.Products.FindAsync(productId);

        var specNames = await _context.SpecificationNames.ToListAsync();
        ViewBag.SpecNames = specNames
            .Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.Name
            });

        return View(new ProductSpecification { ProductId = productId });
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("CreateProduct")]
    public async Task<IActionResult> Create(ProductSpecification model)
    {
        if (model.SpecNameId == null && string.IsNullOrWhiteSpace(model.Key))
        {
            ModelState.AddModelError("Key", "Bạn phải chọn hoặc nhập tên thông số.");
            // load lại dropdown
            ViewBag.SpecNames = (await _context.SpecificationNames.ToListAsync())
                .Select(s => new SelectListItem { Value = s.Id.ToString(), Text = s.Name });
            return View(model);
        }

        // Nếu người dùng nhập thông số mới
        if (!string.IsNullOrWhiteSpace(model.Key))
        {
            var existingSpec = await _context.SpecificationNames
                .FirstOrDefaultAsync(s => s.Name.ToLower() == model.Key.ToLower());

            if (existingSpec == null)
            {
                var newSpec = new SpecificationName { Name = model.Key };
                _context.SpecificationNames.Add(newSpec);
                await _context.SaveChangesAsync();
                model.SpecNameId = newSpec.Id; // liên kết với SpecName mới
            }
            else
            {
                model.SpecNameId = existingSpec.Id;
            }
        }

        model.Key = null; // để thống nhất dùng SpecNameId

        _context.ProductSpecifications.Add(model);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index", new { productId = model.ProductId });
    }


    [HttpGet]
    [HasPermission("CreateProduct")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var spec = await _context.ProductSpecifications.FindAsync(id);
        if (spec == null) return NotFound();

        var specNames = await _context.SpecificationNames.ToListAsync();

        var viewModel = new ProductSpecificationEditViewModel
        {
            Id = spec.Id,
            ProductId = spec.ProductId,
            Name = spec.SpecName,
            Value = spec.Value,
            WarrantyMonths = spec.WarrantyMonths,
            SpecNames = specNames
                .Select(s => new SelectListItem { Text = s.Name, Value = s.Id.ToString() })
                .ToList()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("CreateProduct")]
    public async Task<IActionResult> Edit(ProductSpecificationEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var specNames = await _context.SpecificationNames.ToListAsync();
            model.SpecNames = specNames
                .Select(s => new SelectListItem { Text = s.Name, Value = s.Id.ToString() })
                .ToList();

            return View(model);
        }

        var spec = await _context.ProductSpecifications.FindAsync(model.Id);
        if (spec == null) return NotFound();

        // Kiểm tra dropdown có chọn
        if (model.SpecNameId == null)
        {
            ModelState.AddModelError("", "Bạn phải chọn thông số.");
            model.SpecNames = await _context.SpecificationNames
                .Select(s => new SelectListItem { Text = s.Name, Value = s.Id.ToString() })
                .ToListAsync();
            return View(model);
        }

        // Gán giá trị hợp lệ
        spec.SpecNameId = model.SpecNameId.Value;
        spec.Value = model.Value;
        spec.WarrantyMonths = model.WarrantyMonths;

        _context.ProductSpecifications.Update(spec);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index", new { productId = spec.ProductId });
    }


    // Xóa thông số
    [HttpGet]
    [HasPermission("DeleteProduct")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var spec = await _context.ProductSpecifications.FindAsync(id);
        if (spec == null) return NotFound();

        _context.ProductSpecifications.Remove(spec);
        await _context.SaveChangesAsync();
        return RedirectToAction("Index", new { productId = spec.ProductId });
    }
}