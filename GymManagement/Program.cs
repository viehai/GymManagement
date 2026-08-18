using GymManagement.Helpers;
using GymManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// B1: Thêm MVC Service
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<EmailHelper>();

// B2: Thêm Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedEmail = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false; // nới lỏng cho môi trường học tập, có thể bật lại nếu muốn chặt hơn
})
.AddEntityFrameworkStores<GymDbContext>()
.AddDefaultTokenProviders();

// B3: Thêm Service kết nối CSDL
builder.Services.AddDbContext<GymDbContext>(
    opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn")));

// B4: Cấu hình Cookie Authentication
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

var app = builder.Build();

// ===================== SEED DATA: 3 ROLE =====================
// Chạy 1 lần khi app khởi động, kiểm tra nếu Role đã tồn tại thì bỏ qua (an toàn khi restart nhiều lần)
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roleNames = { "Admin", "Owner", "Member" };

    foreach (var roleName in roleNames)
    {
        bool roleExists = await roleManager.RoleExistsAsync(roleName);
        if (!roleExists)
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    // (Tùy chọn) Seed sẵn 1 tài khoản Admin đầu tiên để có thể đăng nhập ngay,
    // vì màn hình Đăng ký công khai chỉ tạo được tài khoản Member.
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    string adminEmail = "admin@gym.com";

    var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
    if (existingAdmin == null)
    {
        var adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "System Admin",
            EmailConfirmed = true // bỏ qua bước xác nhận email cho tài khoản seed
        };

        var result = await userManager.CreateAsync(adminUser, "Admin@123");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}

// ===================== MIDDLEWARE PIPELINE =====================
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Bắt buộc UseAuthentication TRƯỚC UseAuthorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();