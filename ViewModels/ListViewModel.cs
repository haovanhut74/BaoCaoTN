using MyWebApp.Models;

namespace MyWebApp.ViewModels;

public class ListViewModel
{
    public List<Product> Products { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<Brand> Brands { get; set; } = new();
    public List<Order> Orders { get; set; } = new();
    public List<User> Users { get; set; } = new();
    
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public string? OrderCode { get; set; }
    public string? UserName { get; set; }
    public string? Status { get; set; }
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
}