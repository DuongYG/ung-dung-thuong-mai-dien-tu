using Microsoft.AspNetCore.Authentication.Cookies;
using SV22T1020583.BusinessLayers;

var builder = WebApplication.CreateBuilder(args);

// 1. KẾT NỐI DATABASE
string connectionString = builder.Configuration.GetConnectionString("LiteCommerceDB") ?? "";
Configuration.Initialize(connectionString);

// 2. ĐĂNG KÝ SERVICES
var mvcBuilder = builder.Services.AddControllersWithViews();

#if DEBUG
mvcBuilder.AddRazorRuntimeCompilation();
#endif

// Cấu hình Authentication (Xác thực Cookie)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => {
        options.Cookie.Name = "SV22T1020583.Shop.Auth";
        options.LoginPath = "/Account/Login";       // ĐÂY LÀ NƠI CHUYỂN HƯỚNG KHI CHƯA ĐĂNG NHẬP
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });

// Cấu hình Session cho Giỏ hàng
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(60); // Giỏ hàng tồn tại 60 phút
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// 3. CẤU HÌNH MIDDLEWARE (THỨ TỰ RẤT QUAN TRỌNG)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();
app.UseRouting();

// Thứ tự bắt buộc: Authentication -> Authorization -> Session
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();