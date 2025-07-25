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
}