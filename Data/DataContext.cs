using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Models;

namespace MyWebApp.Data;

public class DataContext : IdentityDbContext<ApplicationUser>
{
    public DataContext(DbContextOptions<DataContext> options) : base(options) { }

    // dùng entity framework để quản lý các bảng trong cơ sở dữ liệu
    public DbSet<Product> Products { get; set; }
    public DbSet<Brand> Brands { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderDetail> OrderDetails { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<Comment> Comments { get; set; }
    public DbSet<Sliders> Sliders { get; set; }
    public DbSet<Contact> Contacts { get; set; }
    public DbSet<Shiping> Shipings { get; set; }
    public DbSet<ProductSpecification> ProductSpecifications { get; set; }
    public DbSet<SpecificationName> SpecificationNames { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<DiscountCode> DiscountCodes { get; set; }
    public DbSet<GiftPromotion> GiftPromotions { get; set; }
    public DbSet<News> News { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Comment>()
            .HasOne(c => c.ParentComment)
            .WithMany(c => c.Replies)
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<GiftPromotion>()
            .HasOne(g => g.RequiredProduct)
            .WithMany()
            .HasForeignKey(g => g.RequiredProductId)
            .OnDelete(DeleteBehavior.Restrict); // Không cascade

        modelBuilder.Entity<GiftPromotion>()
            .HasOne(g => g.GiftProduct)
            .WithMany()
            .HasForeignKey(g => g.GiftProductId)
            .OnDelete(DeleteBehavior.Restrict); // Không cascade
        modelBuilder.Entity<DiscountCode>()
            .Property(d => d.DiscountAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Order>()
            .Property(o => o.ShippingFee)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Order>()
            .Property(o => o.TotalAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Order>()
            .Property(o => o.TotalDiscount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<OrderDetail>()
            .Property(od => od.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Product>()
            .Property(p => p.DiscountPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Shiping>()
            .Property(s => s.Price)
            .HasPrecision(18, 2);
    }
}