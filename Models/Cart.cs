namespace MyWebApp.Models;

public class Cart
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    public List<CartItem> CartItems { get; set; } = new();
}