using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MyWebApp.Data;
using MyWebApp.Models;
using MyWebApp.ViewModels;

namespace MyWebApp.Areas.Admin.Controllers;

public class UserController : BaseController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserController(DataContext context, UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> RoleManager) : base(context)
    {
        _userManager = userManager;
        _roleManager = RoleManager;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users
            .OrderByDescending(u => u.Id)
            .ToListAsync();

        var userWithRoles = new List<UserWithRoleViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userWithRoles.Add(new UserWithRoleViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                CreatedDate = user.CreatedDate,
                IsLocked = user.LockoutEnabled && user.LockoutEnd > DateTimeOffset.Now,
                Role = roles.FirstOrDefault() ?? "Chưa có"
            });
        }

        return View(userWithRoles);
    }

    [HttpGet]
    public async Task<IActionResult> AddUser()
    {
        var roles = await _roleManager.Roles.ToListAsync();
        ViewBag.Roles = new SelectList(roles, "Name", "Name");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> AddUser(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(roles, "Name", "Name");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.UserName,
            Email = model.Email,
            CreatedDate = DateTime.Now
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, model.Role);
            TempData["Success"] = "Tạo người dùng thành công.";
            return RedirectToAction("Index");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }

        var rolesList = await _roleManager.Roles.ToListAsync();
        ViewBag.Roles = new SelectList(rolesList, "Name", "Name");
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return NotFound();

        var userRoles = await _userManager.GetRolesAsync(user);
        var currentRole = userRoles.FirstOrDefault();

        var model = new UserWithRoleViewModel
        {
            UserName = user.UserName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = currentRole ?? "",
            Id = user.Id
        };

        var roles = await _roleManager.Roles.ToListAsync();
        ViewBag.Roles = new SelectList(roles, "Name", "Name", currentRole);

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(UserWithRoleViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(roles, "Name", "Name", model.Role);
            return View(model);
        }

        var user = await _userManager.FindByIdAsync(model.Id);
        if (user == null) return NotFound();

        user.Email = model.Email;
        user.PhoneNumber = model.PhoneNumber;

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, model.Role);

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            TempData["Success"] = "Cập nhật người dùng thành công.";
            return RedirectToAction("Index");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError("", error.Description);
        }

        var rolesList = await _roleManager.Roles.ToListAsync();
        ViewBag.Roles = new SelectList(rolesList, "Name", "Name", model.Role);
        return View(model);
    }


    [HttpGet]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (id == Guid.Empty) return NotFound();

        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();

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