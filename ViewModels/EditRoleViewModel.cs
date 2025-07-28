using System.ComponentModel.DataAnnotations;

namespace MyWebApp.ViewModels;

public class EditRoleViewModel
{
    [Required]
    public string Id { get; set; }

    [Required(ErrorMessage = "Tên quyền không được để trống.")]
    [Display(Name = "Tên quyền")]
    public string RoleName { get; set; }
}