using MyWebApp.Models;

namespace MyWebApp.ViewModels;

public class CartItemViewModel
{
    public List<CartItem> CartItems { get; set; }
    public decimal TotalPrice { get; set; }
}