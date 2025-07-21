using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;

namespace MyWebApp.Component.Views;

public class BrandViewComponent : BaseViewComponent
{
    public BrandViewComponent(DataContext context) : base(context) { }

    public async Task<IViewComponentResult> InvokeAsync(List<string> selectedSlugBrands)
    {
        var brands = await _context.Brands.ToListAsync();
        ViewBag.SelectedSlugBrands = selectedSlugBrands;
        return View(brands);
    }

}