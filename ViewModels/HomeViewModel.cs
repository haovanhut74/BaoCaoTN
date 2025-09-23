using MyWebApp.Models;

namespace MyWebApp.ViewModels;

public class HomeViewModel
{
    public List<Sliders> Sliders { get; set; } = new();
    public List<Product> TopSale { get; set; }
    public List<Product> MostBought { get; set; }
    public List<Product> Newest { get; set; }
    public List<Product> MostReviewed { get; set; }
}