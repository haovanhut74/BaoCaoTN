using MyWebApp.Models;

namespace MyWebApp.ViewModels;

public class RolePermissionViewModel
{
    public string RoleId { get; set; } = null!;
    public string RoleName { get; set; } = null!;
    public List<PermissionItem> Permissions { get; set; } = new();
}