using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// kết nối database sql server
builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")
    ));

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Thời gian hết hạn phiên
    options.Cookie.IsEssential = true; // Cookie cần thiết cho phiên làm việc
});
var app = builder.Build();
app.UseStatusCodePagesWithReExecute("/Home/Error", "?statuscode={0}");
app.UseSession(); // Enable session support

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseRouting();

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

// Initialize the database with seed data
// var context = app.Services.CreateScope().ServiceProvider.GetRequiredService<DataContext>();
//
// SeedData.Seeding(context);

app.Run();