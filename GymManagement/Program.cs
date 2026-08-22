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
    
    // Cấu hình chống Brute-force: Tự động khóa 5 phút sau 5 lần nhập sai mật khẩu liên tiếp
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
})
.AddEntityFrameworkStores<GymDbContext>()
.AddDefaultTokenProviders();

// B3: Thêm Service kết nối CSDL
builder.Services.AddDbContext<GymDbContext>(
    opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn")));

// B4: Cấu hình Cookie Authentication & Kiểm tra bảo mật tức thì (Instant Lockout / Force Logout)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

// Bắt buộc Identity kiểm tra lại SecurityStamp & Trạng thái Khóa (Lockout) ngay lập tức trên mỗi Request
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero;
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

    // Seed dữ liệu mẫu cho SystemLogs nếu đang trống
    var dbContext = scope.ServiceProvider.GetRequiredService<GymDbContext>();
    if (!await dbContext.SystemLogs.AnyAsync())
    {
        dbContext.SystemLogs.AddRange(
            new SystemLog
            {
                Action = "SystemStartup",
                Entity = "ApplicationCore",
                EntityId = "NET8",
                Level = "Info",
                Description = "Khởi động máy chủ GymPro Management Core và đồng bộ cơ sở dữ liệu thành công.",
                CreatedAt = DateTime.Now.AddHours(-12)
            },
            new SystemLog
            {
                Action = "OwnerApproved",
                Entity = "Gym",
                EntityId = "1",
                Level = "Info",
                Description = "Quản trị viên đã phê duyệt cơ sở phòng Gym và nâng cấp tài khoản thành Owner.",
                CreatedAt = DateTime.Now.AddHours(-8)
            },
            new SystemLog
            {
                Action = "SecurityAlert",
                Entity = "Account",
                EntityId = "Auth",
                Level = "Warning",
                Description = "Phát hiện nhiều lần đăng nhập không thành công từ địa chỉ IP không xác định.",
                CreatedAt = DateTime.Now.AddHours(-3)
            },
            new SystemLog
            {
                Action = "PaymentSuccess",
                Entity = "Transaction",
                EntityId = "101",
                Level = "Info",
                Description = "Hội viên thanh toán thành công gói tập qua phương thức chuyển khoản VietQR.",
                CreatedAt = DateTime.Now.AddMinutes(-45)
            }
        );
        await dbContext.SaveChangesAsync();
    }
}

// ===================== MIDDLEWARE PIPELINE =====================
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Cấu hình Culture: dùng en-US để định dạng số kiểu 200,000 (dấu phẩy ngăn cách hàng nghìn)
var cultureInfo = new System.Globalization.CultureInfo("en-US");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture(cultureInfo),
    SupportedCultures = new[] { cultureInfo },
    SupportedUICultures = new[] { cultureInfo }
});

// Bắt buộc UseAuthentication TRƯỚC UseAuthorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();