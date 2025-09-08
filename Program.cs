using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;
using MyWebApp.Interface.Service;
using MyWebApp.Models;
using MyWebApp.Repository.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// kết nối database sql server
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")
    ));
builder.Services.AddTransient<IEmailSender, EmailSender>(); // Thêm dịch vụ gửi email
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Thời gian hết hạn phiên
    options.Cookie.IsEssential = true; // Cookie cần thiết cho phiên làm việc
});


builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultProvider;
    })
    .AddEntityFrameworkStores<DataContext>()
    .AddDefaultTokenProviders()
    .AddTokenProvider<DataProtectorTokenProvider<ApplicationUser>>(TokenOptions.DefaultProvider); // ✅ Đăng ký "Default"

builder.Services.ConfigureApplicationCookie(options =>
{
    // Nếu trang đăng nhập nằm trong Area User
    options.LoginPath = "/User/Account/Login";
    options.AccessDeniedPath = "/User/Account/AccessDenied"; // tuỳ chọn
});
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailSender, EmailSender>();

builder.Services.AddRazorPages();

builder.Services.Configure<IdentityOptions>(options =>
{
    // Password settings.
    options.Password.RequireDigit = true; // Bắt buộc có số
    options.Password.RequireLowercase = true; // Bắt buộc có chữ thường
    options.Password.RequireUppercase = true; // Bắt buộc có chữ hoa
    options.Password.RequireNonAlphanumeric = true; // Bắt buộc có ký tự đặc biệt
    options.Password.RequiredLength = 6; // Độ dài tối thiểu là 6
    options.Password.RequiredUniqueChars = 1; // Số lượng ký tự khác nhau tối thiểu

    // Lockout settings.
    //options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5); // Khóa tài khoản 5 phút
    //options.Lockout.MaxFailedAccessAttempts = 5; // Sau 5 lần đăng nhập sai sẽ bị khóa
    //options.Lockout.AllowedForNewUsers = true; // Áp dụng cho cả tài khoản mới

    // User settings.
    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true; // Không bắt buộc Email phải duy nhất
});


var app = builder.Build();
app.UseStatusCodePagesWithReExecute("/Home/Error", "?statuscode={0}");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseRouting();

// Đặt Session trước Authentication để session sẵn sàng cho auth
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}", // Mặc định vào area "user"
        defaults: new { area = "User" })
    .WithStaticAssets();
app.MapRazorPages(); // Cho Razor Pages Identity, nếu bạn dùng
// Initialize the database with seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await SeedData.SeedingAsync(services);
}

app.Run();