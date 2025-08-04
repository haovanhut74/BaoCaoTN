using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;
using MyWebApp.Models;
using System.Security.Claims;

namespace MyWebApp.Areas.Permission
{
    public class HasPermissionAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string _permissionCode;

        // Inject UserManager và DbContext qua constructor
        public HasPermissionAttribute(string permissionCode)
        {
            _permissionCode = permissionCode;
        }

        // Để inject UserManager và DbContext vào attribute, ta cần dùng IServiceProvider trong context
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var serviceProvider = context.HttpContext.RequestServices;
            var userManager = serviceProvider.GetService(typeof(UserManager<ApplicationUser>)) as UserManager<ApplicationUser>;
            var dbContext = serviceProvider.GetService(typeof(DataContext)) as DataContext;

            if (userManager == null || dbContext == null)
            {
                context.Result = new ForbidResult();
                return;
            }

            var userId = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                context.Result = new ChallengeResult();
                return;
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                context.Result = new ForbidResult();
                return;
            }

            var roleNames = await userManager.GetRolesAsync(user);
            if (!roleNames.Any())
            {
                context.Result = new ForbidResult();
                return;
            }

            var roleIds = await dbContext.Roles
                .Where(r => roleNames.Contains(r.Name))
                .Select(r => r.Id)
                .ToListAsync();

            var permissions = await dbContext.RolePermissions
                .Where(rp => roleIds.Contains(rp.RoleId))
                .Select(rp => rp.Permission.Code)
                .ToListAsync();

            if (!permissions.Contains(_permissionCode))
            {
                context.Result = new ForbidResult();
                return;
            }

            // Có quyền -> cho phép tiếp tục xử lý action
        }

    }
}
