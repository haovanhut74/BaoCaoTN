using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;

namespace MyWebApp.Component.Views;

public class CategoryViewComponent : BaseViewComponent
{
    public CategoryViewComponent(DataContext context) : base(context) { }
    
    public async Task<IViewComponentResult> InvokeAsync(List<string> selectedSlugCategories)
    {
        var categories = await _context.Categories.ToListAsync();
        ViewBag.SelectedSlugCategories = selectedSlugCategories;
        return View(categories);
    }

}