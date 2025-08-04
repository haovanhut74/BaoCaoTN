using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MyWebApp.Areas.Permission;
using MyWebApp.Data;
using MyWebApp.Models;
using MyWebApp.ViewModels;

namespace MyWebApp.Areas.Admin.Controllers;

// Controller quản lý người dùng trong khu vực Admin
[HasPermission("ManageUsers")]
public class UserController : BaseController
{
    // Inject UserManager để thao tác với người dùng
    private readonly UserManager<ApplicationUser> _userManager;

    // Inject RoleManager để thao tác với vai trò
    private readonly RoleManager<IdentityRole> _roleManager;

    // Constructor nhận DataContext, UserManager, RoleManager
    public UserController(DataContext context, UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> RoleManager) : base(context)
    {
        _userManager = userManager;
        _roleManager = RoleManager;
    }

    // Trang danh sách người dùng
    [HasPermission("ManageUsers")]
    public async Task<IActionResult> Index()
    {
        // Lấy tất cả người dùng và sắp xếp theo Id giảm dần
        var users = await _userManager.Users.OrderByDescending(u => u.Id).ToListAsync();
        var userIds = users.Select(u => u.Id).ToList();

        var userRoles = await _context.UserRoles
            .Where(ur => userIds.Contains(ur.UserId))
            .ToListAsync();

        var roles = await _roleManager.Roles.ToListAsync();

        var userWithRoles = users.Select(user =>
        {
            var roleId = userRoles.FirstOrDefault(ur => ur.UserId == user.Id)?.RoleId;
            var roleName = roles.FirstOrDefault(r => r.Id == roleId)?.Name ?? "Chưa có";
            return new UserWithRoleViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                CreatedDate = user.CreatedDate,
                IsLocked = user.LockoutEnabled && user.LockoutEnd > DateTimeOffset.Now,
                Role = roleName
            };
        }).ToList();

        return View(userWithRoles); // Trả về view danh sách người dùng
    }

    // Hiển thị form thêm người dùng
    [HttpGet]
    [HasPermission("AddUser")]
    public async Task<IActionResult> AddUser()
    {
        // Gửi danh sách roles để chọn trong form
        var roles = await _roleManager.Roles.ToListAsync();
        ViewBag.Roles = new SelectList(roles, "Name", "Name");
        return View();
    }

    // Xử lý khi submit form thêm người dùng
    [HttpPost]
    [HasPermission("AddUser")]
    public async Task<IActionResult> AddUser(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            // Nếu dữ liệu không hợp lệ, load lại form với roles
            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(roles, "Name", "Name");
            return View(model);
        }

        // Tạo người dùng mới
        var user = new ApplicationUser
        {
            UserName = model.UserName,
            Email = model.Email,
            CreatedDate = DateTime.Now,
        };

        // Tạo tài khoản người dùng với mật khẩu
        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            // Thêm người dùng vào vai trò đã chọn
            await _userManager.AddToRoleAsync(user, model.Role);
            TempData["Success"] = "Tạo người dùng thành công.";
            return RedirectToAction("Index");
        }

        // Nếu có lỗi khi tạo, hiển thị lỗi
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }

        // Load lại danh sách vai trò khi có lỗi
        var rolesList = await _roleManager.Roles.ToListAsync();
        ViewBag.Roles = new SelectList(rolesList, "Name", "Name");
        return View(model);
    }

    // Hiển thị form chỉnh sửa người dùng
    [HttpGet]
    [HasPermission("EditUser")]
    public async Task<IActionResult> Edit(string id)
    {
        // Tìm người dùng theo id
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        // Lấy vai trò hiện tại của người dùng
        var userRoles = await _userManager.GetRolesAsync(user);
        var currentRole = userRoles.FirstOrDefault();

        // Đổ dữ liệu người dùng vào model
        var model = new UserWithRoleViewModel
        {
            UserName = user.UserName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = currentRole ?? "",
            Id = user.Id,
            MainAddress = user.MainAddress,
            SubAddress = user.SubAddress,
        };

        // Gửi danh sách roles vào view
        var roles = await _roleManager.Roles.ToListAsync();
        ViewBag.Roles = new SelectList(roles, "Name", "Name", currentRole);

        return View(model);
    }

    // Xử lý khi submit form chỉnh sửa người dùng
    [HttpPost]
    [HasPermission("EditUser")]
    public async Task<IActionResult> Edit(UserWithRoleViewModel model)
    {
        if (!ModelState.IsValid)
        {
            // Nếu dữ liệu không hợp lệ, load lại roles và hiển thị lại form
            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(roles, "Name", "Name", model.Role);
            return View(model);
        }

        // Tìm người dùng cần sửa
        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null) return NotFound();

        // Cập nhật thông tin cơ bản
        user.Email = model.Email;
        user.PhoneNumber = model.PhoneNumber;
        user.MainAddress = model.MainAddress;
        user.SubAddress = model.SubAddress;

        // Xóa tất cả vai trò hiện tại
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);

        // Thêm vai trò mới
        await _userManager.AddToRoleAsync(user, model.Role);

        // Cập nhật người dùng vào database
        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            TempData["Success"] = "Cập nhật người dùng thành công.";
            return RedirectToAction("Index");
        }

        // Hiển thị lỗi nếu update không thành công
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }

        // Load lại danh sách vai trò
        var rolesList = await _roleManager.Roles.ToListAsync();
        ViewBag.Roles = new SelectList(rolesList, "Name", "Name", model.Role);
        return View(model);
    }

    // Xóa người dùng theo id
    [HttpGet]
    [HasPermission("DeleteUser")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (id == Guid.Empty) return NotFound();

        // Tìm người dùng theo id
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

        // Thực hiện xóa
        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            TempData["Success"] = "Xóa người dùng thành công.";
        }
        else
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }

        return RedirectToAction("Index");
    }
}