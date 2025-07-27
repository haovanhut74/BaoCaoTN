using MyWebApp.Models;

namespace MyWebApp.ViewModels;

public class ListViewModel
{
    public List<Product> Products { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public List<Brand> Brands { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
}