using Microsoft.EntityFrameworkCore;

namespace MyWebApp.Data;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options) { }
}