using System.ComponentModel.DataAnnotations;

namespace MyWebApp.Models;

public class User
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Tên người dùng là bắt buộc")]
    public string UserName { get; set; }

    [Required(ErrorMessage = "Email là bắt buộc"),
     EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; }
    
    [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
    [DataType(DataType.Password)]
    public string Password { get; set; }
}