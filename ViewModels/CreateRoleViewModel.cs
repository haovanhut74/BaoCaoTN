using System.ComponentModel.DataAnnotations;

namespace MyWebApp.ViewModels;

public class CreateRoleViewModel
{
    [Required(ErrorMessage = "Tên quyền không được để trống.")]
    public string RoleName { get; set; }
}