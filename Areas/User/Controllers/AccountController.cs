using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyWebApp.Models;
using MyWebApp.ViewModels;

namespace MyWebApp.Areas.User.Controllers;

[Area("User")]
[Route("User/[controller]/[action]")]
public class AccountController : Controller
{
    private UserManager<ApplicationUser> _userManager;
    private SignInManager<ApplicationUser> _signInManager;

    public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel userModel)
    {
        if (ModelState.IsValid)
        {
            var user = new ApplicationUser
            {
                UserName = userModel.UserName,
                Email = userModel.Email
            };
            var result = await _userManager.CreateAsync(user, userModel.Password);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }

        return View(userModel);
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel userModel)
    {
        if (ModelState.IsValid)
        {
            // Tìm user theo UserName hoặc Email
            var user = await _userManager.FindByNameAsync(userModel.UserName)
                       ?? await _userManager.FindByEmailAsync(userModel.UserName);

            if (user is { UserName: not null })
            {
                var result = await _signInManager.PasswordSignInAsync(
                    user.UserName, // phải dùng UserName thật
                    userModel.Password,
                    userModel.RememberMe,
                    lockoutOnFailure: false
                );

                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }
            }

            ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng");
        }

        return View(userModel);
    }
}