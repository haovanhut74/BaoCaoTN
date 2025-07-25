using Microsoft.AspNetCore.Mvc;

namespace MyWebApp.Models;

public class CartItem
{
    public Guid Id { get; set; } // product ID
    public string ProductName { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice => Price * Quantity;
    public string ImageUrl { get; set; }
    public CartItem() { }

    public CartItem(Product product)
    {
        ProductName = product.Name;
        Id = product.Id;
        Price = product.Price;
        Quantity = 1; // Default quantity for a new cart item
        ImageUrl = product.Image;
    }
}