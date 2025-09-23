using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyWebApp.Interface.Service;
using MyWebApp.Models;
using MyWebApp.ViewModels;

namespace MyWebApp.Areas.User.Controllers;

[Area("User")]
[Route("User/[controller]/[action]")]
public class AccountController : Controller
{
    private UserManager<ApplicationUser> _userManager;
    private SignInManager<ApplicationUser> _signInManager;
    private readonly IEmailSender _emailSender;

    public AccountController(IEmailSender emailSender, UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
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
                // Đăng nhập thành công
                TempData["Success"] = "Đăng nhập thành công!";

                var receiver = user.Email; // Gửi đến email user vừa đăng nhập
                var subject = "Thông báo đăng nhập thành công";
                var message = $"Chào {user.UserName}, bạn đã đăng nhập thành công vào hệ thống vào lúc {DateTime.Now}.";

                await _emailSender.SendEmailAsync(receiver, subject, message);

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
                    // Đăng nhập thành công
                    TempData["Success"] = "Đăng nhập thành công!";
                    var receiver = "haovanhut74@gmail.com";
                    var subject = "Thông báo đăng nhập thành công";
                    var message =
                        $"Chào {user.UserName}, bạn đã đăng nhập thành công vào hệ thống vào lúc {DateTime.Now}.";
                    await _emailSender.SendEmailAsync(receiver, subject, message);
                    return RedirectToAction("Index", "Home");
                }
            }

            if (user != null && user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
            {
                TempData["Error"] = "Tài khoản của bạn đã bị khóa., vui lòng liên hệ với quản trị viên.";
                return RedirectToAction("Login");
            }


            ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng");
        }

        return View(userModel);
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        TempData["Success"] = "Đăng xuất thành công!";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Không tiết lộ user có tồn tại hay không
                TempData["Success"] = "Nếu email hợp lệ, bạn sẽ nhận được hướng dẫn đặt lại mật khẩu.";
                return RedirectToAction("Login");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = Url.Action(
                "ResetPassword", "Account",
                new { token, email = user.Email },
                Request.Scheme);

            // Gửi email
            var subject = "Đặt lại mật khẩu";
            var message =
                $"Chào {user.UserName}, nhấn vào link sau để đặt lại mật khẩu: <a href='{resetLink}'>Reset Password</a>";
            await _emailSender.SendEmailAsync(user.Email, subject, message);

            TempData["Success"] = "Email đặt lại mật khẩu đã được gửi!";
            return RedirectToAction("Login");
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(string token, string email)
    {
        if (token == null || email == null)
        {
            TempData["Error"] = "Liên kết đặt lại mật khẩu không hợp lệ.";
            return RedirectToAction("Login");
        }

        // ✅ Đăng xuất ngay khi người dùng click link
        if (User.Identity is { IsAuthenticated: true })
        {
            await _signInManager.SignOutAsync();
        }

        var model = new ResetPasswordViewModel { Token = token, Email = email };
        return View(model); // Hiển thị form nhập password mới
    }


    [HttpPost]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng.";
                return RedirectToAction("Login");
            }

            // Đăng xuất user đang đăng nhập (nếu có)
            await _signInManager.SignOutAsync();
            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                TempData["Success"] = "Đặt lại mật khẩu thành công!";
                // ✅ Ngay sau khi reset thành công, invalidate token cũ
                await _userManager.UpdateSecurityStampAsync(user);
                return RedirectToAction("Login");
            }

            // Nếu token hết hạn hoặc không hợp lệ
            if (result.Errors.Any(e => e.Code == "InvalidToken"))
            {
                TempData["Error"] = "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.";
                return RedirectToAction("ForgotPassword");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }

        return View(model);
    }
}