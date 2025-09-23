using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;

namespace MyWebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class DashboardController : BaseController
    {
        public DashboardController(DataContext context) : base(context) { }

        public IActionResult Index()
        {
            // Tổng quan
            ViewBag.TotalUsers = _context.Users.Count();
            ViewBag.TotalOrders = _context.Orders.Count();
            ViewBag.TotalRevenue = _context.Orders.Where(o => o.Status == 5).Sum(o => o.TotalAmount);
            ViewBag.TotalProducts = _context.Products.Count();

            // Top 5 sản phẩm bán chạy
            var bestSellingProducts = _context.OrderDetails
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
                .Take(5)
                .ToList();
            ViewBag.BestSellingProducts = bestSellingProducts;

            // Top sản phẩm theo loại
            var bestSellingByCategory = _context.OrderDetails
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
                        .Take(5)
                        .ToList()
                })
                .ToList();
            ViewBag.BestSellingByCategory = bestSellingByCategory;

            return View();
        }

        [HttpGet]
        public IActionResult GetRevenueByDay()
        {
            var today = DateTime.Now.Date;
            var data = _context.Orders
                .Where(o => o.Status == 5 && o.OrderDate.Date == today)
                .GroupBy(o => o.OrderDate.Hour)
                .Select(g => new { Hour = g.Key, Revenue = g.Sum(x => x.TotalAmount) })
                .ToList();
            return Json(data);
        }

        [HttpGet]
        public IActionResult GetRevenueByMonth()
        {
            var currentYear = DateTime.Now.Year;
            var revenueByMonth = _context.Orders
                .Where(o => o.Status == 5 && o.OrderDate.Year == currentYear)
                .GroupBy(o => o.OrderDate.Month)
                .Select(g => new { Month = g.Key, Revenue = g.Sum(x => x.TotalAmount) })
                .ToList();

            var result = Enumerable.Range(1, 12)
                .Select(m => new
                    { Month = m, Revenue = revenueByMonth.FirstOrDefault(x => x.Month == m)?.Revenue ?? 0 });

            return Json(result);
        }

        [HttpGet]
        public IActionResult GetRevenueByYear()
        {
            var revenueByYear = _context.Orders
                .Where(o => o.Status == 5)
                .GroupBy(o => o.OrderDate.Year)
                .Select(g => new { Year = g.Key, Revenue = g.Sum(x => x.TotalAmount) })
                .OrderBy(x => x.Year)
                .ToList();
            return Json(revenueByYear);
        }
    }
}