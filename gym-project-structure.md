# Cấu trúc Code — Dự án Quản lý Phòng Gym (ASP.NET Core MVC)

## Nguyên tắc cấu trúc

- Mô hình: **MVC thuần** (Controller → DbContext trực tiếp qua EF Core), **KHÔNG dùng Service/Repository layer**
- Đúng theo mẫu code môn học: `Controllers/`, `Models/`, `Views/`, `Program.cs` cấu hình theo 3 bước (AddControllersWithViews → MapControllerRoute → AddDbContext)
- Logic dùng chung (tính EndDate, gửi email, gọi VNPay, sinh OTP, ghi log) được gom vào thư mục **`Helpers/`** — là các class C# thuần hoặc static class, KHÔNG phải Service theo pattern N-layer, chỉ để tránh code trùng giữa 2 người
- DbContext đặt tên: `GymDbContext.cs`

---

## Cây thư mục tổng thể

```
GymManagement/
│
├── Controllers/
│   ├── AccountController.cs          → Auth (Người 1)
│   ├── HomeController.cs             → Trang chủ, tìm kiếm gym (Người 1)
│   ├── GymController.cs              → Xem chi tiết gym public (Người 1)
│   ├── PurchaseController.cs         → Mua vé, checkout, VNPay callback (Người 1)
│   ├── MemberController.cs           → Hồ sơ, lịch sử, vé đang active, gia hạn (Người 1)
│   │
│   ├── OwnerGymController.cs         → CRUD gym (Người 2)
│   ├── OwnerEquipmentController.cs   → Equipment catalog/custom (Người 2)
│   ├── OwnerPackageController.cs     → CRUD package (Người 2)
│   ├── OwnerMemberController.cs      → Danh sách hội viên, doanh thu (Người 2)
│   │
│   ├── AdminOwnerController.cs       → Duyệt Owner (Người 2)
│   ├── AdminGymController.cs         → Duyệt Gym (Người 2)
│   ├── AdminEquipmentController.cs   → Catalog gốc (Người 2)
│   ├── AdminUserController.cs        → Quản lý User chung (Người 2)
│   └── AdminLogController.cs         → Xem SystemLog (Người 2)
│
├── Models/
│   ├── GymDbContext.cs               → DbContext (2 người cùng thống nhất 1 lần, sau đó KHÔNG sửa riêng lẻ)
│   ├── ApplicationUser.cs            → Kế thừa IdentityUser (Người 1 sở hữu)
│   ├── Gym.cs
│   ├── Equipment.cs
│   ├── GymEquipment.cs
│   ├── MembershipPackage.cs
│   ├── MemberMembership.cs
│   ├── Transaction.cs
│   ├── Invoice.cs
│   ├── PasswordResetOtp.cs
│   └── SystemLog.cs
│
├── ViewModels/                        → Model riêng cho từng Form (KHÔNG dùng chung Entity cho View)
│   ├── RegisterViewModel.cs
│   ├── LoginViewModel.cs
│   ├── ForgotPasswordViewModel.cs
│   ├── ResetPasswordViewModel.cs
│   ├── PurchaseCheckoutViewModel.cs
│   ├── OwnerPackageFormViewModel.cs
│   └── ...
│
├── Helpers/                           → Class dùng chung, KHÔNG phải Service layer, chỉ để tránh trùng code
│   ├── MembershipHelper.cs           → static method CalculateNewEndDate()
│   ├── OtpHelper.cs                  → static method GenerateOtp()
│   ├── EmailHelper.cs                → SendEmailAsync() dùng SMTP
│   ├── VnPayHelper.cs                → BuildPaymentUrl(), ValidateSignature()
│   ├── InvoicePdfHelper.cs           → GenerateInvoicePdf() dùng QuestPDF
│   └── LogHelper.cs                  → static method WriteLog()
│
├── Views/
│   ├── Shared/
│   │   ├── _MemberLayout.cshtml       → Layout cho Guest/Member (Người 1)
│   │   ├── _OwnerAdminLayout.cshtml   → Layout cho Owner/Admin (Người 2)
│   │   ├── _ValidationScriptsPartial.cshtml
│   │   └── _Notification.cshtml       → PartialView thông báo dùng chung
│   │
│   ├── Account/                       (Người 1)
│   │   ├── Register.cshtml
│   │   ├── Login.cshtml
│   │   ├── ForgotPassword.cshtml
│   │   ├── VerifyOtp.cshtml
│   │   └── ResetPassword.cshtml
│   │
│   ├── Home/                          (Người 1)
│   │   └── Index.cshtml
│   │
│   ├── Gym/                           (Người 1 — public view)
│   │   ├── Search.cshtml
│   │   └── Details.cshtml
│   │
│   ├── Purchase/                      (Người 1)
│   │   ├── DailyPass.cshtml
│   │   ├── Package.cshtml
│   │   ├── Checkout.cshtml
│   │   ├── Result.cshtml
│   │   └── Renew.cshtml
│   │
│   ├── Member/                        (Người 1)
│   │   ├── Profile.cshtml
│   │   ├── ChangePassword.cshtml
│   │   ├── TransactionHistory.cshtml
│   │   ├── InvoiceDetails.cshtml
│   │   ├── MyMemberships.cshtml
│   │   └── MembershipDetails.cshtml
│   │
│   ├── OwnerGym/                      (Người 2)
│   ├── OwnerEquipment/                (Người 2)
│   ├── OwnerPackage/                  (Người 2)
│   ├── OwnerMember/                   (Người 2)
│   ├── OwnerDashboard/                (Người 2)
│   │
│   ├── AdminOwner/                    (Người 2)
│   ├── AdminGym/                      (Người 2)
│   ├── AdminEquipment/                (Người 2)
│   ├── AdminUser/                     (Người 2)
│   ├── AdminLog/                      (Người 2)
│   └── AdminDashboard/                (Người 2)
│
├── wwwroot/
│   ├── css/site.css
│   ├── js/site.js
│   └── lib/                           → Bootstrap5, jQuery (thư viện)
│
├── appsettings.json                   → ConnectionString, SMTP config, VNPay config
└── Program.cs
```

---

## `Program.cs` — cấu trúc mở rộng từ mẫu gốc

Giữ nguyên 3 bước gốc, chỉ bổ sung Identity + đăng ký Helper (nếu cần DI) + Session (nếu OTP lưu tạm bằng session thay vì DB):

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GymManagement.Models;

var builder = WebApplication.CreateBuilder(args);

// B1: Thêm MVC Service
builder.Services.AddControllersWithViews();

// B2: Thêm Identity (thay cho Role.cs tự viết tay)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedEmail = true;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<GymDbContext>()
.AddDefaultTokenProviders();

// B3: Thêm Service kết nối CSDL
builder.Services.AddDbContext<GymDbContext>(
    opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn")));

// B4: Cấu hình Cookie Authentication (Session-based, không dùng JWT)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// B5: Bắt buộc có Authentication TRƯỚC Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```

---

## Quy tắc phối hợp bắt buộc (tránh conflict code)

1. **`GymDbContext.cs` và toàn bộ file trong `Models/`**: 2 người thống nhất field/bảng theo ERD **trước khi code**, 1 người tạo migration đầu tiên (`Add-Migration InitialCreate`) chứa toàn bộ bảng, người còn lại `git pull` về rồi mới bắt đầu code Controller của mình. **Không ai tự thêm migration mới mà không báo trước.**

2. **`ApplicationUser.cs`**: Người 1 (làm Auth) sở hữu và chỉnh sửa file này. Người 2 chỉ được đọc, nếu cần thêm field phải nhắn trước.

3. **Thư mục `Helpers/`**: đây là điểm chạm chung nhiều nhất.
   - `MembershipHelper.CalculateNewEndDate()` → Người 1 viết (dùng cho Purchase/Renew), Người 2 **không tự viết lại** hàm này, nếu Owner Dashboard cần tính cũng gọi lại hàm này.
   - `EmailHelper`, `OtpHelper` → Người 1 viết (dùng cho Auth + gửi hóa đơn).
   - `VnPayHelper`, `InvoicePdfHelper` → Người 1 viết (thuộc luồng Payment).
   - `LogHelper.WriteLog()` → viết 1 lần, thống nhất chữ ký hàm trước (VD: `WriteLog(string userId, string action, string entity, string entityId, string description, string level)`), cả 2 người đều gọi hàm này ở Controller của mình, **không sửa logic bên trong** nếu không bàn trước.

4. **`Views/Shared/_Layout.cshtml`**: tách riêng `_MemberLayout.cshtml` (Người 1) và `_OwnerAdminLayout.cshtml` (Người 2) ngay từ đầu, mỗi Controller khai báo `Layout = "_MemberLayout"` hoặc tương ứng trong action hoặc `_ViewStart.cshtml` riêng theo từng thư mục Views con.

5. **`appsettings.json`**: chỉ thêm key mới, không xóa/sửa key người khác đã thêm (ConnectionString, Smtp, VnPay section).

6. **Migration & Database**: dùng chung 1 database (không tách 2 DB riêng), luôn `git pull` trước khi `Update-Database` để tránh xung đột schema.

---

## Đặt tên chuẩn để AI code theo đúng convention

Khi nhờ AI generate code cho từng Controller/View, luôn cung cấp kèm:
- Tên **Entity** đã định nghĩa sẵn trong `Models/` (không tự đổi tên field)
- Tên **ViewModel** tương ứng nếu form có nhiều field không map 1-1 với Entity
- Route pattern đã liệt kê ở file `gym-management-functions-screens.md` (VD: `OwnerPackage/Create`, `Purchase/Renew/{membershipId}`)
- Ghi rõ: **"Không tạo Service/Repository, Controller gọi thẳng `GymDbContext` qua constructor injection, dùng `Helpers/` cho logic dùng chung"**
