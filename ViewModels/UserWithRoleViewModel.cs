namespace MyWebApp.ViewModels;

public class UserWithRoleViewModel
{
    public string Id { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public DateTime CreatedDate { get; set; }
    public string Role { get; set; }
    public bool IsLocked { get; set; }
}