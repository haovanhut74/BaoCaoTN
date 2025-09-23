using System.ComponentModel.DataAnnotations;

namespace MyWebApp.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Tên người dùng là bắt buộc")]
    [MaxLength(30)]
    public string UserName { get; set; }

    [Required(ErrorMessage = "Email là bắt buộc")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; }

    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [DataType(DataType.Password)]
    public string Password { get; set; }
    
    [Required(ErrorMessage = "Quyền (Role) là bắt buộc")]
    public string Role { get; set; } = "Customer";
}
