using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;

namespace MyWebApp.Interface.Service;

public class DashboardService
{
    private readonly DataContext _context;

    public DashboardService(DataContext context)
    {
        _context = context;
    }

    public int GetTotalUsers() => _context.Users.Count();
    public int GetTotalOrders() => _context.Orders.Count();
    public decimal GetTotalRevenue() => _context.Orders.Where(o => o.Status == 5).Sum(o => o.TotalAmount);
    public int GetTotalProducts() => _context.Products.Count();

    public List<object> GetBestSellingProducts(int top = 5)
    {
        return _context.OrderDetails
            .Include(d => d.Product)
            .Where(d => d.Order.Status == 5)
            .GroupBy(d => d.Product)
            .Select(g => new
            {
                ProductName = g.Key.Name,
                Image = g.Key.MainImage,
                Quantity = g.Sum(x => x.Quantity)
            })
            .OrderByDescending(x => x.Quantity)
            .Take(top)
            .Cast<object>()
            .ToList();
    }

    public List<object> GetBestSellingByCategory(int top = 5)
    {
        return _context.OrderDetails
            .Include(d => d.Product)
            .ThenInclude(p => p.Category)
            .Where(d => d.Order.Status == 5)
            .GroupBy(d => new { d.Product.CategoryId, d.Product.Category.Name })
            .Select(g => new
            {
                CategoryName = g.Key.Name,
                Products = g.GroupBy(p => p.Product)
                    .Select(p => new
                    {
                        ProductName = p.Key.Name,
                        Image = p.Key.MainImage,
                        Quantity = p.Sum(x => x.Quantity)
                    })
                    .OrderByDescending(p => p.Quantity)
                    .Take(top)
                    .ToList()
            })
            .Cast<object>()
            .ToList();
    }

    public List<object> GetRevenueByDay(DateTime today)
    {
        return _context.Orders
            .Where(o => o.Status == 5 && o.OrderDate.Date == today.Date)
            .GroupBy(o => o.OrderDate.Hour)
            .Select(g => new { Hour = g.Key, Revenue = g.Sum(x => x.TotalAmount) })
            .Cast<object>()
            .ToList();
    }

    public List<object> GetRevenueByMonth(int year)
    {
        var revenueByMonth = _context.Orders
            .Where(o => o.Status == 5 && o.OrderDate.Year == year)
            .GroupBy(o => o.OrderDate.Month)
            .Select(g => new { Month = g.Key, Revenue = g.Sum(x => x.TotalAmount) })
            .ToList();

        return Enumerable.Range(1, 12)
            .Select(m => new { Month = m, Revenue = revenueByMonth.FirstOrDefault(x => x.Month == m)?.Revenue ?? 0 })
            .Cast<object>()
            .ToList();
    }

    public List<object> GetRevenueByYear()
    {
        return _context.Orders
            .Where(o => o.Status == 5)
            .GroupBy(o => o.OrderDate.Year)
            .Select(g => new { Year = g.Key, Revenue = g.Sum(x => x.TotalAmount) })
            .OrderBy(x => x.Year)
            .Cast<object>()
            .ToList();
    }
}