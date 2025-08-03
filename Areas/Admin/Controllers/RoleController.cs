using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;
using MyWebApp.Models;
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

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRoleViewModel model)
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

        var model = new EditRoleViewModel
        {
            Id = role.Id,
            RoleName = role.Name
        };

        return View(model);
    }

    // POST: Admin/Role/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditRoleViewModel model)
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAjax([FromBody] DeleteRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.Id))
        {
            return Json(new { success = false, message = "ID không hợp lệ." });
        }

        var role = await _roleManager.FindByIdAsync(request.Id);
        if (role == null)
        {
            return Json(new { success = false, message = "Không tìm thấy quyền." });
        }

        var result = await _roleManager.DeleteAsync(role);
        if (result.Succeeded)
        {
            return Json(new { success = true, message = "Xóa quyền thành công!" });
        }

        var errorMessage = string.Join("; ", result.Errors.Select(e => e.Description));
        return Json(new { success = false, message = errorMessage });
    }

    [HttpGet]
    public async Task<IActionResult> Detail(string id)
    {
        var role = await _roleManager.FindByIdAsync(id);
        if (role == null) return NotFound();

        var allPermissions = await _context.Permissions.ToListAsync();
        var grantedPermissions = await _context.RolePermissions
            .Where(x => x.RoleId == id)
            .Select(x => x.PermissionId)
            .ToListAsync();

        var viewModel = new RolePermissionViewModel
        {
            RoleId = role.Id,
            RoleName = role.Name!,
            Permissions = allPermissions.Select(p => new PermissionItem
            {
                PermissionId = p.Id,
                PermissionName = p.Name,
                PermissionCode = p.Code,
                IsGranted = grantedPermissions.Contains(p.Id)
            }).ToList()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Detail(RolePermissionViewModel model)
    {
        var role = await _roleManager.FindByIdAsync(model.RoleId);
        if (role == null) return NotFound();

        // Xóa quyền cũ
        var oldPermissions = _context.RolePermissions.Where(x => x.RoleId == model.RoleId);
        _context.RolePermissions.RemoveRange(oldPermissions);

        // Thêm quyền mới
        var granted = model.Permissions.Where(p => p.IsGranted).ToList();
        foreach (var p in granted)
        {
            _context.RolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = model.RoleId,
                PermissionId = p.PermissionId
            });
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Cập nhật quyền hạn thành công!";
        return RedirectToAction(nameof(Index));
    }
}