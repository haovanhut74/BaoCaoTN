using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;

namespace MyWebApp.Component.Views;

public class BrandViewComponent : BaseViewComponent
{
    public BrandViewComponent(DataContext context) : base(context) { }
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var brands = await _context.Brands.ToListAsync(); // truy vấn danh sách các thương hiệu từ cơ sở dữ liệu
        return View(brands); // trả về view với danh sách các thương hiệu
    }
}