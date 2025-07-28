using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;
using MyWebApp.ViewModels;

namespace MyWebApp.Areas.Admin.Controllers;

public class RoleController : BaseController
{
    private readonly RoleManager<IdentityRole> _roleManager;

    public RoleController(DataContext context, RoleManager<IdentityRole> RoleManager) : base(context)
    {
        _roleManager = RoleManager;
    }

    public IActionResult Index()
    {
        var roles = _roleManager.Roles.ToList();
        return View(roles);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoleViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var roleExists = await _roleManager.RoleExistsAsync(model.RoleName);
        if (roleExists)
        {
            ModelState.AddModelError(string.Empty, "Tên quyền đã tồn tại.");
            return View(model);
        }

        var result = await _roleManager.CreateAsync(new IdentityRole(model.RoleName));
        if (result.Succeeded)
        {
            TempData["Success"] = "Tạo quyền thành công!";
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }


    // GET: Admin/Role/Edit/{id}
    public async Task<IActionResult> Edit(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null)
        {
            return NotFound();
        }

        var model = new RoleViewModel
        {
            Id = role.Id,
            RoleName = role.Name
        };

        return View(model);
    }

// POST: Admin/Role/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(RoleViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var role = await _roleManager.FindByIdAsync(model.Id);
        if (role == null)
        {
            return NotFound();
        }

        // Kiểm tra trùng tên với Role khác
        var existingRole = await _roleManager.FindByNameAsync(model.RoleName);
        if (existingRole != null && existingRole.Id != model.Id)
        {
            ModelState.AddModelError(string.Empty, "Tên quyền này đã được sử dụng.");
            return View(model);
        }

        role.Name = model.RoleName;
        var result = await _roleManager.UpdateAsync(role);

        if (result.Succeeded)
        {
            TempData["Success"] = "Cập nhật quyền thành công!";
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }
}