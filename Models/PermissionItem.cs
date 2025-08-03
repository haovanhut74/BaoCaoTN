namespace MyWebApp.Models;

public class PermissionItem
{
    public Guid PermissionId { get; set; }
    public string PermissionName { get; set; } = null!;
    public string PermissionCode { get; set; } = null!;
    public bool IsGranted { get; set; }
}