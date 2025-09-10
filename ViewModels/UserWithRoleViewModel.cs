namespace MyWebApp.ViewModels;

public class UserWithRoleViewModel
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
    public string MainAddress { get; set; } = string.Empty;
    public string SubAddress { get; set; } = string.Empty;
    public DateTime? LastActivity { get; set; }

    public bool IsOnline => LastActivity.HasValue &&
                            (DateTime.UtcNow - LastActivity.Value).TotalMinutes < 1;
}