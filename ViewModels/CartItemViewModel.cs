using MyWebApp.Models;

namespace MyWebApp.ViewModels;

public class CartItemViewModel
{
    public List<CartItemDisplayViewModel> CartItems { get; set; }
    public decimal TotalPrice { get; set; }
}