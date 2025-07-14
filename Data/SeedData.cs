using Microsoft.EntityFrameworkCore;
using MyWebApp.Models;

namespace MyWebApp.Data;

public class SeedData
{
    public static void Seeding(DataContext context)
    {
        context.Database.Migrate(); // Đảm bảo cơ sở dữ liệu đã được tạo và cập nhật

        if (!context.Products.Any())
        {
            // Tạo brand và category
            var acer = new Models.Brand
            {
                Name = "ACER",
                Description = "Mô tả thương hiệu A",
                Slug = "thuong-hieu-ACER",
                Status = 1
            };
            var asus = new Models.Brand
            {
                Name = "ASUS",
                Description = "Mô tả thương hiệu B",
                Slug = "thuong-hieu-ASUS",
                Status = 1
            };
            var laptop = new Models.Category
            {
                Name = "Laptop",
                Description = "Mô tả danh mục A",
                Slug = "danh-muc-Laptop",
                status = 1
            };
            var pc = new Models.Category
            {
                Name = "PC",
                Description = "Mô tả danh mục B",
                Slug = "danh-muc-PC",
                status = 1
            };

            // Thêm vào database rồi lưu lại để sinh Id
            context.Brands.AddRange(acer, asus);
            context.Categories.AddRange(laptop, pc);
            context.SaveChanges();

            // Sau khi đã có Id, tạo Product
            context.Products.AddRange(
                new Models.Product
                {
                    Name = "Laptop Acer Aspire 5",
                    Description = "Mô tả sản phẩm A",
                    Slug = "laptop-acer-aspire-5",
                    Price = 15000000,
                    Quantity = 10,
                    Image = "laptop-acer-aspire-5.jpg",
                    CreatedAt = DateTime.Now,
                    CategoryId = laptop.Id, // Đã có Id sau khi SaveChanges
                    BrandId = acer.Id
                },
                new Models.Product
                {
                    Name = "PC Gaming ASUS ROG",
                    Description = "Mô tả sản phẩm B",
                    Slug = "pc-gaming-asus-rog",
                    Price = 20000000,
                    Quantity = 5,
                    Image = "pc-gaming-asus-rog.jpg",
                    CreatedAt = DateTime.Now,
                    CategoryId = pc.Id,
                    BrandId = asus.Id
                }
            );

            context.SaveChanges();
        }
    }
}