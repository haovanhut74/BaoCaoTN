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

        // Seed permissions từng cái theo Code
        var predefinedPermissions = new List<Permission>
        {
            new() { Code = "ViewDashboard", Name = "Xem bảng điều khiển" },
            
            new() { Code = "ManageUsers", Name = "Quản lý User" },
            new() { Code = "EditUser", Name = "Sửa người dùng" },
            new() { Code = "DeleteUser", Name = "Xóa người dùng" },
            new() { Code = "CreateUser", Name = "Tạo người dùng mới" },
            
            new() { Code = "ManageRoles", Name = "Quản lý quyền" },
            new() { Code = "CreateRole", Name = "Tạo nquyền mới" },
            new() { Code = "EditRole", Name = "Sửa quyền" },
            new() { Code = "DeleteRole", Name = "Xóa quyền" },
            new() { Code = "AssignRoles", Name = "Phân quyền cho người dùng" },
            
            new() { Code = "ManageOrders", Name = "Quản lý đơn hàng" },
            new() { Code = "ViewOrder", Name = "Xem đơn hàng" },
            new() { Code = "DeleteOrder", Name = "Xóa đơn hàng" },
            
            new() { Code = "ViewReports", Name = "Xem báo cáo" },
            
            new() { Code = "ManageProducts", Name = "Quản lý sản phẩm" },
            new() { Code = "CreateProduct", Name = "Tạo sản phẩm mới" },
            new() { Code = "DeleteProduct", Name = "Xóa sản phẩm" },
            new() { Code = "EditProduct", Name = "Chỉnh sửa sản phẩm" },
            
            new() { Code = "ManageCategories", Name = "Quản lý danh mục" },
            new() { Code = "CreateCategory", Name = "Tạo danh mục mới" },
            new() { Code = "DeleteCategory", Name = "Xóa danh mục" },
            new() { Code = "EditCategory", Name = "Chỉnh sửa danh mục" },
            
            new() { Code = "ManageBrands", Name = "Quản lý thương hiệu" },
            new() { Code = "CreateBrand", Name = "Tạo thương hiệu mới" },
            new() { Code = "DeleteBrand", Name = "Xóa thương hiệu" },
            new() { Code = "EditBrand", Name = "Chỉnh sửa thương hiệu" },
            
            new() { Code = "ManageSettings", Name = "Quản lý cài đặt hệ thống" },
            new() { Code = "AccessAPI", Name = "Truy cập API" },
        };

        foreach (var permission in predefinedPermissions)
        {
            var exists = await context.Permissions.AnyAsync(p => p.Code == permission.Code);
            if (!exists)
            {
                permission.Id = Guid.NewGuid(); // Đảm bảo mỗi lần unique
                context.Permissions.Add(permission);
            }
        }

        await context.SaveChangesAsync();

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