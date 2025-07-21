using MyWebApp.Models;

namespace MyWebApp.ViewModels;

public class ProductFilterViewModel
{
    public List<Product> Products { get; set; }
    public List<Category> Categories { get; set; }
    public List<Brand> Brands { get; set; }
    public List<Guid> SelectedCategoryIds { get; set; } = new();
    public List<Guid> SelectedBrandIds { get; set; } = new();
    public List<string> SelectedSlugCategories { get; set; } = new();
    public List<string> SelectedSlugBrands { get; set; } = new();
}