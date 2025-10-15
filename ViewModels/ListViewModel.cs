using MyWebApp.Models;

namespace MyWebApp.ViewModels;

public class ListViewModel
{
    public List<Product> Products { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<Brand> Brands { get; set; } = new();
    public List<Order> Orders { get; set; } = new();
    public List<UserWithRoleViewModel> Users { get; set; } = new();
    
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public string? ProductName { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
    public string? OrderCode { get; set; }
    public string? UserName { get; set; }
    public string? Status { get; set; }
    public string? DateFrom { get; set; }
    public string? DateTo { get; set; }
}