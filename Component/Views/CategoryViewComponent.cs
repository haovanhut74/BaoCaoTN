using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;

namespace MyWebApp.Component.Views;

public class CategoryViewComponent : BaseViewComponent
{
    public CategoryViewComponent(DataContext context) : base(context) { }
    
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var categories = await _context.Categories.ToListAsync(); // truy vấn danh sách các danh mục từ cơ sở dữ liệu
        return View(categories); // trả về view với danh sách các danh mục
    }
}