using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;
using MyWebApp.Extensions;
using MyWebApp.Interface.Hubs;
using MyWebApp.Interface.Service;
using MyWebApp.Models;
using MyWebApp.Repository.Service;

var builder = WebApplication.CreateBuilder(args);

// ==========================
// 1. Add services
// ==========================

// HttpClient cho API ngoài (Viettel Post,...)
builder.Services.AddHttpClient();

// Controllers + Views
builder.Services.AddControllersWithViews();

// Database SQL Server
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Email service
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddTransient<IEmailSender, EmailSender>();

// Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.IsEssential = true;
});

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultProvider;
    })
    .AddEntityFrameworkStores<DataContext>()
    .AddDefaultTokenProviders()
    .AddTokenProvider<DataProtectorTokenProvider<ApplicationUser>>(TokenOptions.DefaultProvider);

// Identity Cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/User/Account/Login";
    options.AccessDeniedPath = "/User/Account/AccessDenied";

    options.ExpireTimeSpan = TimeSpan.FromDays(14); // nhớ login 1 lần giữ 14 ngày
    options.SlidingExpiration = true; // mỗi lần dùng sẽ gia hạn thêm
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Identity Options (Password, User...)
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
});

// SignalR
builder.Services.AddSignalR();

// Razor Pages
builder.Services.AddRazorPages();
builder.Services.Configure<GhnConfig>(builder.Configuration.GetSection("Ghn"));
builder.Services.AddHttpClient<IGhnService, GhnService>();
var app = builder.Build();

// ==========================
// 2. Middleware pipeline
// ==========================
app.UseStatusCodePagesWithReExecute("/Home/Error", "?statuscode={0}");

// Error handler
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseRouting();

// Session trước Authentication
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Map Hubs
app.MapHub<PresenceHub>("/presenceHub").RequireAuthorization();
app.MapHub<ChatHub>("/chatHub");

// Static files
app.MapStaticAssets();

// Routes
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}",
    defaults: new { area = "User" }
).WithStaticAssets();

app.MapRazorPages();

// ==========================
// 3. Seed Database
// ==========================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await SeedData.SeedingAsync(services);
}

app.Run();