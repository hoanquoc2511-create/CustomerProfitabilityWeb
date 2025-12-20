using Microsoft.EntityFrameworkCore;
using CustomerProfitabilityWeb.Data;

var builder = WebApplication.CreateBuilder(args);

// ===== ĐĂNG KÝ SERVICES =====

// Thêm DbContext với SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Thêm Session (lưu trạng thái đăng nhập)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(480); // 8 giờ
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Thêm MVC Controllers + Views
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ===== CẤU HÌNH HTTP PIPELINE =====

// Xử lý lỗi (chỉ khi không phải môi trường Development)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Chuyển hướng HTTP sang HTTPS
app.UseHttpsRedirection();

// Cho phép truy cập file tĩnh (CSS, JS, images)
app.UseStaticFiles();

// Routing
app.UseRouting();

// Session phải nằm TRƯỚC Authorization
app.UseSession();

// Authorization (kiểm tra quyền)
app.UseAuthorization();

// ===== THÊM ĐOẠN NÀY ĐỂ FIX LỖI 405 =====
// Redirect trang chủ "/" về Login
app.MapGet("/", context =>
{
    context.Response.Redirect("/Account/Login");
    return Task.CompletedTask;
});
// ===== HẾT ĐOẠN THÊM =====

// Định tuyến mặc định
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"); // Mặc định vào trang Login

app.Run();