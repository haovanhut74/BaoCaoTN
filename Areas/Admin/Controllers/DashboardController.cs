using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Areas.Permission;
using MyWebApp.Data;

namespace MyWebApp.Areas.Admin.Controllers
{

    [HasPermission("ViewDashboard")]
    public class DashboardController : BaseController
    {
        public DashboardController(DataContext context) : base(context) { }

        public IActionResult Index(DateTime? startDate, DateTime? endDate)
        {
            if (startDate.HasValue && endDate.HasValue && startDate > endDate)
            {
                ModelState.AddModelError("", "Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc.");
                return View();
            }

            var query = _context.Orders.AsQueryable();
            if (startDate.HasValue && endDate.HasValue)
            {
                query = query.Where(o =>
                    o.OrderDate.Date >= startDate.Value.Date && o.OrderDate.Date <= endDate.Value.Date);
                ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
                ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");
            }

            ViewBag.TotalUsers = _context.Users.Count();
            ViewBag.TotalOrders = query.Count(o => o.Status == 5);
            ViewBag.TotalRevenue = query.Where(o => o.Status == 5).Sum(o => o.TotalAmount);
            ViewBag.TotalProducts = _context.Products.Count();

            var bestSellingProducts = _context.OrderDetails
                .Include(d => d.Product)
                .Where(d => d.Order.Status == 5 && d.IsGift != true)
                .Where(d => !startDate.HasValue || !endDate.HasValue ||
                            (d.Order.OrderDate.Date >= startDate.Value.Date &&
                             d.Order.OrderDate.Date <= endDate.Value.Date))
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

            var bestSellingByCategory = _context.OrderDetails
                .Include(d => d.Product)
                .ThenInclude(p => p.Category)
                .Where(d => d.Order.Status == 5 && d.IsGift != true)
                .Where(d => !startDate.HasValue || !endDate.HasValue ||
                            (d.Order.OrderDate.Date >= startDate.Value.Date &&
                             d.Order.OrderDate.Date <= endDate.Value.Date))
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

            // Thêm thống kê chi tiêu của User
            var topSpendingUsers = _context.Orders
                .Where(o => o.Status == 5)
                .Where(o => !startDate.HasValue || !endDate.HasValue ||
                            (o.OrderDate.Date >= startDate.Value.Date &&
                             o.OrderDate.Date <= endDate.Value.Date))
                .GroupBy(o => o.UserName)
                .Select(g => new
                {
                    UserName = g.Key,
                    TotalSpent = g.Sum(o => o.TotalAmount)
                })
                .OrderByDescending(x => x.TotalSpent)
                .Take(5)
                .ToList();
            ViewBag.TopSpendingUsers = topSpendingUsers;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetRevenueByRange(DateTime startDate, DateTime endDate)
        {
            var data = await _context.Orders
                .Where(o => o.Status == 5 && o.OrderDate.Date >= startDate.Date && o.OrderDate.Date <= endDate.Date)
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(x => x.TotalAmount) })
                .OrderBy(g => g.Date)
                .ToListAsync();
            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> GetRevenueByMonth()
        {
            var currentYear = DateTime.Now.Year;
            var revenueByMonth = await _context.Orders
                .Where(o => o.Status == 5 && o.OrderDate.Year == currentYear)
                .GroupBy(o => o.OrderDate.Month)
                .Select(g => new { Month = g.Key, Revenue = g.Sum(x => x.TotalAmount) })
                .OrderBy(g => g.Month)
                .ToListAsync();
            var result = Enumerable.Range(1, 12)
                .Select(m => new
                    { Month = m, Revenue = revenueByMonth.FirstOrDefault(x => x.Month == m)?.Revenue ?? 0 })
                .ToList();
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetRevenueByYear()
        {
            var revenueByYear = await _context.Orders
                .Where(o => o.Status == 5)
                .GroupBy(o => o.OrderDate.Year)
                .Select(g => new { Year = g.Key, Revenue = g.Sum(x => x.TotalAmount) })
                .OrderBy(x => x.Year)
                .ToListAsync();
            return Json(revenueByYear);
        }
    }
}