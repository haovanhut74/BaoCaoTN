namespace MyWebApp.ViewModels;

public class CartItemDisplayViewModel
{
    public Guid CartItemId { get; set; } // <-- Đây là Id thực sự của CartItem
    public Guid ProductId { get; set; } // <-- Đây là Id của Product
    public string ProductName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice => Price * Quantity;
}