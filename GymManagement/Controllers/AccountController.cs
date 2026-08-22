using GymManagement.Helpers;
using GymManagement.Models;
using GymManagement.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly GymDbContext _context;
        private readonly EmailHelper _emailHelper;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            GymDbContext context,
            EmailHelper emailHelper)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _emailHelper = emailHelper;
        }

        // ==================== ĐĂNG KÝ ====================

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Kiểm tra email đã tồn tại chưa (UserManager tự check nhưng check trước để báo lỗi rõ ràng hơn)
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email này đã được đăng ký.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Gán role Member mặc định cho mọi tài khoản đăng ký công khai
                await _userManager.AddToRoleAsync(user, "Member");

                // Sinh token xác nhận email của Identity + gửi link qua mail
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmLink = Url.Action("ConfirmEmail", "Account",
                    new { userId = user.Id, token = token }, protocol: Request.Scheme);

                string subject = "Xác nhận tài khoản - Gym Management";
                string body = $@"
                    <div style='font-family: Arial, sans-serif;'>
                        <h2>Chào {user.FullName},</h2>
                        <p>Vui lòng bấm vào link dưới đây để xác nhận tài khoản:</p>
                        <p><a href='{confirmLink}'>Xác nhận email</a></p>
                    </div>";

                bool emailSent = await _emailHelper.SendEmailAsync(user.Email, subject, body);

                if (emailSent)
                {
                    TempData["Message"] = "Đăng ký thành công! Vui lòng kiểm tra email để xác nhận tài khoản.";
                }
                else
                {
                    user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(user);
                    TempData["Message"] = "Đăng ký thành công! (Tự động xác nhận email do chưa cấu hình SMTP). Bạn có thể đăng nhập ngay.";
                }

                _context.SystemLogs.Add(new SystemLog
                {
                    UserId = user.Id,
                    Action = "UserRegistered",
                    Entity = "ApplicationUser",
                    EntityId = user.Id,
                    Level = "Info",
                    Description = $"Người dùng mới {user.FullName} ({user.Email}) đã đăng ký tài khoản thành công.",
                    CreatedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();

                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
                return RedirectToAction("Index", "Home");

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var result = await _userManager.ConfirmEmailAsync(user, token);
            TempData["Message"] = result.Succeeded
                ? "Xác nhận email thành công! Bạn có thể đăng nhập ngay bây giờ."
                : "Xác nhận email thất bại. Link có thể đã hết hạn.";

            return RedirectToAction("Login");
        }

        // ==================== ĐĂNG NHẬP / ĐĂNG XUẤT ====================

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
                return View(model);
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                ModelState.AddModelError("", "Tài khoản của bạn đã bị khóa bởi Quản trị viên. Vui lòng liên hệ hỗ trợ.");
                return View(model);
            }

            if (!user.EmailConfirmed)
            {
                ModelState.AddModelError("", "Tài khoản chưa xác nhận email. Vui lòng kiểm tra hộp thư.");
                return View(model);
            }

            // lockoutOnFailure: true -> tự khóa tạm thời sau nhiều lần đăng nhập sai (chống brute-force)
            var result = await _signInManager.PasswordSignInAsync(
                user.UserName, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    return Redirect(model.ReturnUrl);

                // Điều hướng theo Role
                var loggedInUser = await _userManager.FindByEmailAsync(model.Email);
                if (loggedInUser != null)
                {
                    if (await _userManager.IsInRoleAsync(loggedInUser, "Admin"))
                        return RedirectToAction("Dashboard", "Admin");

                    if (await _userManager.IsInRoleAsync(loggedInUser, "Owner"))
                        return RedirectToAction("Index", "OwnerGym");
                }

                return RedirectToAction("Profile", "Member");
            }

            if (result.IsLockedOut)
            {
                _context.SystemLogs.Add(new SystemLog
                {
                    UserId = user.Id,
                    Action = "SecurityAlert",
                    Entity = "Account",
                    EntityId = user.Id,
                    Level = "Warning",
                    Description = $"Phát hiện tài khoản {user.Email} bị tạm khóa 5 phút do nhập sai mật khẩu quá 5 lần liên tiếp (chống tấn công Brute-force).",
                    CreatedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();

                ModelState.AddModelError("", "Tài khoản tạm thời bị khóa 5 phút do đăng nhập sai quá nhiều lần. Vui lòng thử lại sau.");
                return View(model);
            }

            ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // ==================== QUÊN MẬT KHẨU / OTP ====================

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);

            // Bảo mật: không báo rõ email có tồn tại hay không, chỉ redirect sang VerifyOtp
            if (user == null)
            {
                return RedirectToAction("VerifyOtp", new { email = model.Email });
            }

            // Sinh mã OTP 6 chữ số ngẫu nhiên
            string otp = new Random().Next(100000, 999999).ToString();

            // Lưu vào bảng PasswordResetOtps (hết hạn sau 5 phút)
            var otpRecord = new PasswordResetOtp
            {
                UserId = user.Id,
                OtpCode = otp,
                ExpiredAt = DateTime.Now.AddMinutes(5),
                IsUsed = false,
                CreatedAt = DateTime.Now
            };
            _context.PasswordResetOtps.Add(otpRecord);
            await _context.SaveChangesAsync();

            // Gửi OTP qua email
            string subject = "Mã xác minh đặt lại mật khẩu - Gym Management";
            string body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 500px; margin: auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px;'>
                    <h2 style='color: #333;'>Yêu cầu đặt lại mật khẩu</h2>
                    <p>Chào <strong>{user.FullName}</strong>,</p>
                    <p>Mã OTP xác minh của bạn là:</p>
                    <div style='font-size: 28px; font-weight: bold; letter-spacing: 6px; color: #ff3e3e; padding: 12px 0; text-align: center;'>
                        {otp}
                    </div>
                    <p style='color: #888; font-size: 13px;'>Mã có hiệu lực trong <strong>5 phút</strong>. Không chia sẻ mã này cho bất kỳ ai.</p>
                </div>";

            await _emailHelper.SendEmailAsync(user.Email, subject, body);

            return RedirectToAction("VerifyOtp", new { email = model.Email });
        }

        [HttpGet]
        public IActionResult VerifyOtp(string email)
        {
            return View(new VerifyOtpViewModel { Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Mã OTP không đúng hoặc đã hết hạn.");
                return View(model);
            }

            // Tìm OTP hợp lệ chưa dùng và chưa hết hạn của user này
            var validOtp = await _context.PasswordResetOtps
                .Where(o => o.UserId == user.Id
                            && o.OtpCode == model.OtpCode
                            && !o.IsUsed
                            && o.ExpiredAt >= DateTime.Now)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (validOtp == null)
            {
                ModelState.AddModelError("", "Mã OTP không đúng hoặc đã hết hạn.");
                return View(model);
            }

            // Đánh dấu đã dùng ngay khi verify đúng, tránh dùng lại OTP này lần 2
            validOtp.IsUsed = true;
            await _context.SaveChangesAsync();

            return RedirectToAction("ResetPassword", new { email = model.Email, otpCode = model.OtpCode });
        }

        [HttpGet]
        public IActionResult ResetPassword(string email, string otpCode)
        {
            return View(new ResetPasswordViewModel { Email = email, OtpCode = otpCode });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Có lỗi xảy ra, vui lòng thử lại từ đầu.");
                return View(model);
            }

            // Double-check lại OTP đã từng được verify hợp lệ cho user + mã này (chống bypass thẳng vào URL ResetPassword)
            bool otpWasVerified = await _context.PasswordResetOtps
                .AnyAsync(o => o.UserId == user.Id && o.OtpCode == model.OtpCode && o.IsUsed);

            if (!otpWasVerified)
            {
                ModelState.AddModelError("", "Phiên đặt lại mật khẩu không hợp lệ. Vui lòng thực hiện lại từ đầu.");
                return View(model);
            }

            // Dùng token nội bộ của Identity để đổi mật khẩu (không thao tác trực tiếp PasswordHash)
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, model.NewPassword);

            if (result.Succeeded)
            {
                _context.SystemLogs.Add(new SystemLog
                {
                    UserId = user.Id,
                    Action = "PasswordResetSuccess",
                    Entity = "Account",
                    EntityId = user.Id,
                    Level = "Info",
                    Description = $"Tài khoản {user.Email} đã khôi phục mật khẩu thành công qua mã xác minh OTP.",
                    CreatedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();

                TempData["Message"] = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập lại.";
                return RedirectToAction("Login");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(model);
        }
    }
}