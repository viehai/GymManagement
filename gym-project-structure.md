# Cấu trúc Code — Dự án Quản lý Phòng Gym (ASP.NET Core MVC)

## Nguyên tắc cấu trúc

- Mô hình: **MVC thuần** (Controller → DbContext trực tiếp qua EF Core), **KHÔNG dùng Service/Repository layer**
- Đúng theo mẫu code môn học: `Controllers/`, `Models/`, `Views/`, `Program.cs` cấu hình theo 3 bước (AddControllersWithViews → MapControllerRoute → AddDbContext)
- Logic dùng chung (tính EndDate, gửi email, sinh VietQR, sinh OTP, ghi log) được gom vào thư mục **`Helpers/`** — là các class C# thuần hoặc static class, KHÔNG phải Service theo pattern N-layer, chỉ để tránh code trùng giữa 2 người
- Cổng thanh toán: **VietQR (Napas 24/7)** kết hợp **SePay Webhook** tự động nhận biến động số dư chuyển tiền trực tiếp vào tài khoản ngân hàng
- DbContext đặt tên: `GymDbContext.cs`

---

## Cây thư mục tổng thể

```
GymManagement/
│
├── Controllers/
│   ├── AccountController.cs          → Auth (Đăng ký, Đăng nhập, OTP, Quên MK, Lockout)
│   ├── HomeController.cs             → Trang chủ, tìm kiếm gym
│   ├── GymController.cs              → Xem chi tiết gym public
│   ├── PurchaseController.cs         → Mua vé ngày, chọn gói tháng, Checkout, VietQR, SePay Webhook
│   ├── MemberController.cs           → Hồ sơ, lịch sử, vé đang active, chi tiết vé, đổi mật khẩu
│   │
│   ├── OwnerGymController.cs         → Đăng ký & Quản lý Gym
│   ├── OwnerEquipmentController.cs   → Quản lý thiết bị (Catalog gốc / Tự thêm)
│   ├── OwnerPackageController.cs     → Quản lý gói vé (Vé ngày / Gói tháng)
│   ├── OwnerMemberController.cs      → Danh sách hội viên phòng gym
│   ├── OwnerTransactionController.cs  → Lịch sử giao dịch & Duyệt tiền thủ công (Owner)
│   ├── OwnerDashboardController.cs   → Báo cáo doanh thu, KPI cơ sở
│   │
│   ├── AdminOwnerController.cs       → Duyệt hồ sơ Owner
│   ├── AdminGymController.cs         → Duyệt / Khóa / Mở lại Gym
│   ├── AdminEquipmentController.cs   → Quản lý Catalog thiết bị dùng chung
│   ├── AdminUserController.cs        → Quản lý toàn bộ tài khoản người dùng
│   ├── AdminTransactionController.cs → Quản lý giao dịch toàn sàn & Duyệt tiền (Admin)
│   ├── AdminLogController.cs         → Nhật ký hệ thống (SystemLogs)
│   └── AdminDashboardController.cs   → Thống kê tổng quan toàn sàn
│
├── Models/
│   ├── GymDbContext.cs               → DbContext kết nối SQL Server
│   ├── ApplicationUser.cs            → Kế thừa IdentityUser
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
│   ├── QrPaymentViewModel.cs         → ViewModel cho màn hình quét mã VietQR động & SePay DTO
│   ├── OwnerPackageFormViewModel.cs
│   ├── OwnerTransactionListViewModel.cs
│   ├── AdminTransactionViewModel.cs
│   └── ...
│
├── Helpers/                           → Class dùng chung, KHÔNG phải Service layer, chỉ để tránh trùng code
│   ├── MembershipHelper.cs           → CalculateEndDate(), CalculateRenewEndDate(), GenerateInvoiceCode()
│   ├── OtpHelper.cs                  → static method GenerateOtp()
│   ├── EmailHelper.cs                → SendEmailAsync() dùng Gmail SMTP
│   └── InvoicePdfHelper.cs           → GenerateInvoicePdf()
│
├── Views/
│   ├── Shared/
│   │   ├── _MemberLayout.cshtml       → Layout phong cách Nike Brutal Minimalism (Member)
│   │   ├── _OwnerAdminLayout.cshtml   → Layout Dashboard Sidebar (Owner & Admin)
│   │   ├── _ValidationScriptsPartial.cshtml
│   │   └── _Notification.cshtml       → PartialView thông báo Toast / Alert
│   │
│   ├── Account/
│   │   ├── Register.cshtml
│   │   ├── Login.cshtml
│   │   ├── ForgotPassword.cshtml
│   │   ├── VerifyOtp.cshtml
│   │   └── ResetPassword.cshtml
│   │
│   ├── Home/
│   │   └── Index.cshtml
│   │
│   ├── Gym/
│   │   ├── Search.cshtml
│   │   └── Details.cshtml
│   │
│   ├── Purchase/
│   │   ├── DailyPass.cshtml
│   │   ├── Package.cshtml             → Chọn gói tháng (MEM-07)
│   │   ├── Checkout.cshtml            → Tóm tắt & phương thức VietQR (MEM-08)
│   │   ├── QrPayment.cshtml           → Màn hình quét mã VietQR động 24/7 & Auto-Polling
│   │   ├── Result.cshtml              → Hóa đơn & kết quả thanh toán thành công
│   │   └── Renew.cshtml               → Gia hạn gói tập
│   │
│   ├── Member/
│   ├── OwnerGym/
│   ├── OwnerEquipment/
│   ├── OwnerPackage/
│   ├── OwnerMember/
│   ├── OwnerTransaction/
│   ├── OwnerDashboard/
│   ├── AdminOwner/
│   ├── AdminGym/
│   ├── AdminEquipment/
│   ├── AdminUser/
│   ├── AdminTransaction/
│   ├── AdminLog/
│   └── AdminDashboard/
│
├── wwwroot/
│   ├── css/site.css
│   ├── js/site.js
│   └── lib/                           → Bootstrap5, Bootstrap Icons, jQuery
│
├── appsettings.json                   → ConnectionString, SMTP config, VietQR & SePay config
└── Program.cs
```

---

## Hướng Dẫn Tích Hợp & Sử Dụng VietQR (Napas 24/7 + SePay Webhook)

### 1. Kiến trúc luồng thanh toán VietQR

```
[ Hội viên bấm Mua / Gia hạn ] 
              │
              ▼
[ Màn hình QrPayment.cshtml ] ──> Hiển thị Mã VietQR động (img.vietqr.io)
              │                   (STK, Tên chủ TK, Số tiền chính xác, Nội dung: GP{TransactionId})
              │
              ▼
[ Khách quét QR qua App Ngân hàng ] ──> [ Tiền chuyển thẳng vào TK Ngân hàng cá nhân ]
                                                           │
                                                           ▼ (Sau 1-2 giây)
                                                   [ SePay (sepay.vn) ]
                                                           │
                                                           ▼ (Gửi Webhook POST)
                                             [ POST /Purchase/SepayWebhook ]
                                                           │
              ┌────────────────────────────────────────────┴────────────────────────────────────────────┐
              ▼                                                                                         ▼
  [ Đổi Transaction.Status = Success ]                                                    [ Kích hoạt MemberMembership ]
              │                                                                                         │
              └────────────────────────────────────────────┬────────────────────────────────────────────┘
                                                           │
                                                           ▼ (JavaScript Polling mỗi 2s phát hiện)
                                         [ Màn hình tự động chuyển sang Result.cshtml 🎉 ]
```

---

### 2. Cấu hình trong `appsettings.json`

```json
"VietQrSettings": {
  "BankId": "MB",                       // Mã ngân hàng: MB, VCB, TCB, TPB, ACB, ICB, BIDV, VPB...
  "BankName": "MBBank (Ngân hàng Quân Đội)",
  "AccountNumber": "0965120204",        // Số tài khoản ngân hàng của bạn
  "AccountName": "CHU VIET HAI",        // Tên chủ tài khoản (Viết hoa không dấu)
  "Template": "compact2"                // Template mã QR: compact2, qr_only, compact
},

"SePaySettings": {
  "ApiKey": "API_KEY_CUA_BAN_TREN_SEPAY" // API Key cấu hình từ SePay (để bảo mật Webhook)
}
```

---

### 3. Cách sử dụng và vận hành VietQR

#### A. Môi trường Máy tính Cá nhân (Localhost):
- Khi chạy thử nghiệm tại `localhost:7272`, SePay trên Internet không thể gửi Webhook vào `localhost` nếu không có đường hầm mở cổng.
- **Cách 1 (Tự động 100% qua Ngrok)**:
  1. Chạy lệnh: `ngrok http https://localhost:7272`
  2. Lấy link Ngrok (ví dụ: `https://xxxx.ngrok-free.app`) và cấu hình Webhook trên **sepay.vn**:
     `https://xxxx.ngrok-free.app/Purchase/SepayWebhook`
- **Cách 2 (Duyệt chủ động cho Owner/Admin)**:
  - Khi hội viên quét QR chuyển tiền xong, Chủ phòng Gym (Owner) hoặc Admin vào menu **`Lịch sử giao dịch`** (`/OwnerTransaction` hoặc `/AdminTransaction`), bấm nút màu xanh lá **`Duyệt thành công`** để kích hoạt gói tập cho Hội viên ngay lập tức!

#### B. Môi trường Máy chủ Thật (Production / Deploy):
- Khi website đã được đưa lên Hosting / VPS có tên miền thật (ví dụ: `https://gympro.vn`):
- Chỉ cần cài đặt URL Webhook trên SePay:
  `https://gympro.vn/Purchase/SepayWebhook`
- Hệ thống sẽ hoạt động **hoàn toàn tự động 100%** mà không cần bất kỳ công cụ trung gian nào khác.

---

## `Program.cs` — cấu trúc MVC & Identity chuẩn

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GymManagement.Models;

var builder = WebApplication.CreateBuilder(args);

// B1: Thêm MVC Service
builder.Services.AddControllersWithViews();

// B2: Thêm Identity với cấu hình Lockout chống Brute-Force
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedEmail = true;
    options.Password.RequiredLength = 6;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
})
.AddEntityFrameworkStores<GymDbContext>()
.AddDefaultTokenProviders();

// B3: Thêm Service kết nối CSDL SQL Server
builder.Services.AddDbContext<GymDbContext>(
    opt => opt.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn")));

// B4: Cấu hình Cookie Authentication
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

## Đặt tên chuẩn để AI code theo đúng convention

Khi nhờ AI generate code cho từng Controller/View, luôn cung cấp kèm:
- Tên **Entity** đã định nghĩa sẵn trong `Models/` (không tự đổi tên field)
- Tên **ViewModel** tương ứng nếu form có nhiều field không map 1-1 với Entity
- Route pattern đã liệt kê ở file `gym-management-functions-screens.md` (VD: `OwnerPackage/Create`, `Purchase/Renew/{membershipId}`)
- Ghi rõ: **"Không tạo Service/Repository, Controller gọi thẳng `GymDbContext` qua constructor injection, dùng `Helpers/` cho logic dùng chung"**
