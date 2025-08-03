using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyWebApp.Models;

namespace MyWebApp.Data;

public class SeedData
{
    public static async Task SeedingAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        // Seed roles
        string[] roles = { "Admin", "Customer", "Manager" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Seed permissions
        if (!await context.Permissions.AnyAsync())
        {
            var permissions = new List<Permission>
            {
                new() { Id = Guid.NewGuid(), Code = "ViewDashboard", Name = "Xem bảng điều khiển" },
                new() { Id = Guid.NewGuid(), Code = "EditUser", Name = "Sửa người dùng" },
                new() { Id = Guid.NewGuid(), Code = "DeleteUser", Name = "Xóa người dùng" },
                new() { Id = Guid.NewGuid(), Code = "CreateUser", Name = "Tạo người dùng mới" },
                new() { Id = Guid.NewGuid(), Code = "ManageRoles", Name = "Quản lý quyền" },
                new() { Id = Guid.NewGuid(), Code = "DeleteRole", Name = "Xóa quyền" },
                new() { Id = Guid.NewGuid(), Code = "AssignRoles", Name = "Phân quyền cho người dùng" },
                new() { Id = Guid.NewGuid(), Code = "ManageOrders", Name = "Quản lý đơn hàng" },
                new() { Id = Guid.NewGuid(), Code = "CreateOrder", Name = "Tạo đơn hàng" },
                new() { Id = Guid.NewGuid(), Code = "DeleteOrder", Name = "Xóa đơn hàng" },
                new() { Id = Guid.NewGuid(), Code = "ViewReports", Name = "Xem báo cáo" },
                new() { Id = Guid.NewGuid(), Code = "ManageProducts", Name = "Quản lý sản phẩm" },
                new() { Id = Guid.NewGuid(), Code = "CreateProduct", Name = "Tạo sản phẩm mới" },
                new() { Id = Guid.NewGuid(), Code = "DeleteProduct", Name = "Xóa sản phẩm" },
                new() { Id = Guid.NewGuid(), Code = "EditProduct", Name = "Chỉnh sửa sản phẩm" },
                new() { Id = Guid.NewGuid(), Code = "ManageCategories", Name = "Quản lý danh mục" },
                new() { Id = Guid.NewGuid(), Code = "ManageSettings", Name = "Quản lý cài đặt hệ thống" },
                new() { Id = Guid.NewGuid(), Code = "AccessAPI", Name = "Truy cập API" },
            };

            context.Permissions.AddRange(permissions);
            await context.SaveChangesAsync();
        }

        // Optional: Seed RolePermissions (gán quyền mặc định cho role)
        // Ví dụ: Gán tất cả quyền cho Admin
        var adminRole = await roleManager.FindByNameAsync("Admin");
        if (adminRole != null)
        {
            var adminRolePermissions = context.RolePermissions.Where(rp => rp.RoleId == adminRole.Id);
            if (!adminRolePermissions.Any())
            {
                var allPermissions = await context.Permissions.ToListAsync();
                foreach (var perm in allPermissions)
                {
                    context.RolePermissions.Add(new RolePermission
                    {
                        Id = Guid.NewGuid(),
                        RoleId = adminRole.Id,
                        PermissionId = perm.Id
                    });
                }

                await context.SaveChangesAsync();
            }
        }
    }
}