using Microsoft.AspNetCore.Identity;

namespace MyWebApp.Models;

public class RolePermission
{
    public Guid Id { get; set; }
    public string RoleId { get; set; } = null!;
    public Guid PermissionId { get; set; }

    public IdentityRole Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}