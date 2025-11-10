// Các using directives cần thiết ở ĐẦU file Program.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DoAnWebBanDoHo.Data;
using DoAnWebBanDoHo.Areas.Identity.Data;

using DoAnWebBanDoHo.Services; // Cần thiết cho CartService

// Các using khác nếu cần
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.Google;


var builder = WebApplication.CreateBuilder(args);

// Cấu hình các dịch vụ cho ứng dụng (Add services to the container.)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                     throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Đăng ký ApplicationDbContext với Entity Framework Core
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Cấu hình Identity để sử dụng ApplicationUser và IdentityRole
// ĐÂY LÀ PHẦN SỬA LỖI QUAN TRỌNG:
builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>() // Thêm hỗ trợ Roles
    .AddEntityFrameworkStores<ApplicationDbContext>(); // Liên kết Identity với DbContext của bạn

builder.Services.AddAuthentication().AddGoogle(
    googleOptions =>
    {
        googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    });

// Thêm hỗ trợ cho các Controller và Views
builder.Services.AddControllersWithViews();

// Thêm hỗ trợ Razor Pages (cần thiết cho các trang UI của Identity)
builder.Services.AddRazorPages();

// Cấu hình Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Thời gian session không hoạt động
    options.Cookie.HttpOnly = true; // Cookie chỉ truy cập qua HTTP
    options.Cookie.IsEssential = true; // Cookie thiết yếu để session hoạt động
});

// Đăng ký CartService vào Dependency Injection
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddScoped<CartService>();


var app = builder.Build();

// Cấu hình HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Sử dụng trang lỗi dành cho phát triển (developer exception page) và endpoint migrations
    app.UseMigrationsEndPoint();
}
else
{
    // Cấu hình xử lý lỗi cho môi trường sản phẩm
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection(); // Chuyển hướng HTTP sang HTTPS
app.UseStaticFiles();      // Cho phép phục vụ các file tĩnh (CSS, JS, images)

app.UseRouting();          // Định tuyến các yêu cầu đến đúng endpoint

// <<<<<< THỨ TỰ QUAN TRỌNG CỦA AUTHENTICATION, AUTHORIZATION VÀ SESSION >>>>>>
app.UseAuthentication();   // PHẢI ĐẶT TRƯỚC UseAuthorization() để xác định người dùng
app.UseAuthorization();    // PHẢI ĐẶT SAU UseAuthentication() để kiểm tra quyền truy cập
app.UseSession();          // PHẢI ĐẶT SAU UseRouting và TRƯỚC MapControllerRoute/MapRazorPages

// Thêm phần seed data ở đây để tạo Roles và Admin User mặc định
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(); // Sử dụng ApplicationUser

    // Tạo Roles "Admin" và "User" nếu chúng chưa tồn tại
    string[] roleNames = { "Admin", "User" };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    // Tạo Admin User mặc định nếu chưa tồn tại
    string adminEmail = "admin@example.com";
    string adminPassword = "Admin@123"; // Đặt mật khẩu mạnh và an toàn cho admin
    if (userManager.FindByEmailAsync(adminEmail).Result == null)
    {
        // Tạo một đối tượng ApplicationUser mới
        ApplicationUser adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true, // Đặt là true để không cần xác nhận email qua email thật
            FullName = "Admin Cua Hang Dong Ho", // GÁN GIÁ TRỊ CHO THUỘC TÍNH FULLNAME
            Address = "123 Đường Lập Trình, Quận 1, TP.HCM", // GÁN GIÁ TRỊ CHO THUỘC TÍNH ADDRESS
            DateOfBirth = new DateTime(1990, 1, 1) // GÁN GIÁ TRỊ CHO THUỘC TÍNH DATEOFBIRTH
        };

        // Cố gắng tạo người dùng
        IdentityResult result = userManager.CreateAsync(adminUser, adminPassword).Result;
        if (result.Succeeded)
        {
            // Nếu tạo thành công, gán Role "Admin" cho người dùng này
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
        else
        {
            // In ra lỗi nếu quá trình tạo người dùng không thành công (để debug)
            Console.WriteLine("Error creating admin user:");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($" - {error.Description}");
            }
        }
    }
}

// Định tuyến mặc định cho các Controller
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Định tuyến cho các trang Razor Pages (bao gồm các trang Identity UI)
app.MapRazorPages();

// Khởi chạy ứng dụng
app.Run();