using Microsoft.AspNetCore.Mvc;
using MyWebApp.Data;

namespace MyWebApp.Component.Views;

public abstract class BaseViewComponent : ViewComponent
{
    protected readonly DataContext _context;

    protected BaseViewComponent(DataContext context)
    {
        _context = context;
    }

}