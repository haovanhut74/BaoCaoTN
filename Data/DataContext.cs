using Microsoft.EntityFrameworkCore;

namespace MyWebApp.Data;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options) { }
    
    // dùng entity framework để quản lý các bảng trong cơ sở dữ liệu
    public DbSet<Models.Product> Products { get; set; }
    public DbSet<Models.Brand> Brands { get; set; }
    public DbSet<Models.Category> Categories { get; set; }
}