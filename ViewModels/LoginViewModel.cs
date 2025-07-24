﻿using System.ComponentModel.DataAnnotations;

namespace MyWebApp.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Tên đăng nhập hoặc email là bắt buộc")]
    public string UserName { get; set; }

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [DataType(DataType.Password)]
    public string Password { get; set; }

    public bool RememberMe { get; set; }
}