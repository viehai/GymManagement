using GymManagement.Models;
using GymManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GymManagement.Helpers;
using System.Text.Json;

namespace GymManagement.Controllers
{
    [Authorize]
    public class MemberController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly GymDbContext _context;
        private readonly IWebHostEnvironment _env;

        public MemberController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            GymDbContext context,
            IWebHostEnvironment env)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _env = env;
        }

        // ==================== TRANG CÁ NHÂN ====================
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var roles = await _userManager.GetRolesAsync(user);
            var myGyms = await _context.Gyms
                .Where(g => g.OwnerId == user.Id)
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();

            var activeMembershipsCount = await _context.MemberMemberships
                .CountAsync(m => m.MemberId == user.Id && m.EndDate >= VnTime.Today);

            var transactionCount = await _context.Transactions
                .CountAsync(t => t.MemberId == user.Id);

            ViewBag.Roles = roles;
            ViewBag.Gyms = myGyms;
            ViewBag.ActiveMembershipsCount = activeMembershipsCount;
            ViewBag.TransactionCount = transactionCount;
            return View(user);
        }

        // ==================== MEM-02 / OWN-20: ĐỔI MẬT KHẨU ====================
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                _context.SystemLogs.Add(new SystemLog
                {
                    UserId = user.Id,
                    Action = "PasswordChanged",
                    Entity = "Account",
                    EntityId = user.Id,
                    Level = "Info",
                    Description = $"Người dùng {user.Email} đã đổi mật khẩu tài khoản thành công.",
                    CreatedAt = VnTime.Now
                });
                await _context.SaveChangesAsync();

                await _signInManager.RefreshSignInAsync(user);
                TempData["Success"] = "Đổi mật khẩu thành công!";
                return RedirectToAction("Profile");
            }

            foreach (var error in result.Errors)
            {
                // Dịch thông báo lỗi phổ biến của Identity
                string desc = error.Code switch
                {
                    "PasswordMismatch" => "Mật khẩu hiện tại không chính xác.",
                    "PasswordTooShort" => "Mật khẩu mới quá ngắn.",
                    _ => error.Description
                };
                ModelState.AddModelError(string.Empty, desc);
            }

            return View(model);
        }

        // ==================== ĐĂNG KÝ MỞ PHÒNG GYM ====================
        [HttpGet]
        public IActionResult RegisterGym()
        {
            return View(new RegisterGymViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterGym(RegisterGymViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Upload ảnh nếu có
            string imageUrl = "";
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = Path.GetExtension(model.ImageFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("ImageFile", "Chỉ chấp nhận file ảnh (.jpg, .jpeg, .png, .webp).");
                    return View(model);
                }

                if (model.ImageFile.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("ImageFile", "Ảnh không được lớn hơn 5MB.");
                    return View(model);
                }

                var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "gyms");
                Directory.CreateDirectory(uploadFolder);

                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadFolder, fileName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await model.ImageFile.CopyToAsync(stream);

                imageUrl = $"/uploads/gyms/{fileName}";
            }

            var gym = new Gym
            {
                OwnerId = user.Id,
                Name = model.Name,
                Address = model.Address,
                Description = model.Description,
                ImageUrl = imageUrl,
                Status = "Pending",
                CreatedAt = VnTime.Now
            };

            _context.Gyms.Add(gym);

            _context.SystemLogs.Add(new SystemLog
            {
                UserId = user.Id,
                Action = "GymRegistrationSubmitted",
                Entity = "Gym",
                EntityId = gym.Id.ToString(),
                Level = "Info",
                Description = $"Người dùng {user.FullName} ({user.Email}) đã gửi hồ sơ đăng ký mở phòng Gym mới: \"{gym.Name}\" ({gym.Address}).",
                CreatedAt = VnTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "Yêu cầu đăng ký phòng Gym đã được gửi! Chúng tôi sẽ xem xét và phản hồi trong thời gian sớm nhất.";
            return RedirectToAction("Profile");
        }

        // ==================== MEM-11: LỊCH SỬ GIAO DỊCH ====================
        public async Task<IActionResult> TransactionHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var transactions = await _context.Transactions
                .Where(t => t.MemberId == user.Id)
                .Include(t => t.Membership)
                    .ThenInclude(m => m.Gym)
                .Include(t => t.Membership)
                    .ThenInclude(m => m.Package)
                .Include(t => t.Invoice)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            var vm = transactions.Select(t => new TransactionHistoryViewModel
            {
                TransactionId    = t.Id,
                GymName          = t.Membership?.Gym?.Name ?? "—",
                PackageName      = t.Membership?.Package?.Name ?? "—",
                PackageTypeLabel = t.Membership?.Package?.PackageType == "Daily"
                                       ? "Vé ngày"
                                       : $"Gói {t.Membership?.Package?.DurationInMonths} tháng",
                Amount    = t.Amount,
                Status    = t.Status,
                CreatedAt = t.CreatedAt,
                InvoiceId = t.Invoice?.Id
            }).ToList();

            return View(vm);
        }

        // ==================== MEM-12: CHI TIẾT HÓA ĐƠN ====================
        public async Task<IActionResult> InvoiceDetails(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var invoice = await _context.Invoices
                .Include(i => i.Transaction)
                    .ThenInclude(t => t.Member)
                .Include(i => i.Transaction)
                    .ThenInclude(t => t.Membership)
                        .ThenInclude(m => m.Gym)
                .Include(i => i.Transaction)
                    .ThenInclude(t => t.Membership)
                        .ThenInclude(m => m.Package)
                .FirstOrDefaultAsync(i => i.Id == id && i.Transaction.MemberId == user.Id);

            if (invoice == null) return NotFound();

            var membership = invoice.Transaction.Membership;
            var member     = invoice.Transaction.Member;
            var pkg        = membership?.Package;
            var gym        = membership?.Gym;

            var vm = new InvoiceDetailsViewModel
            {
                InvoiceId        = invoice.Id,
                InvoiceCode      = invoice.InvoiceCode,
                IssuedDate       = invoice.IssuedDate,
                MemberName       = member?.FullName ?? string.Empty,
                MemberEmail      = member?.Email ?? string.Empty,
                GymName          = gym?.Name ?? string.Empty,
                GymAddress       = gym?.Address ?? string.Empty,
                PackageName      = pkg?.Name ?? string.Empty,
                PackageType      = pkg?.PackageType ?? string.Empty,
                DurationInMonths = pkg?.DurationInMonths,
                Amount           = invoice.Transaction.Amount,
                StartDate        = membership?.StartDate ?? VnTime.Today,
                EndDate          = membership?.EndDate ?? VnTime.Today
            };

            return View(vm);
        }

        // ==================== MEM-13: DANH SÁCH VÉ TẬP / HỘI VIÊN ====================
        public async Task<IActionResult> MyMemberships()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var memberships = await _context.MemberMemberships
                .Where(m => m.MemberId == user.Id)
                .Include(m => m.Gym)
                .Include(m => m.Package)
                .OrderByDescending(m => m.EndDate)
                .ToListAsync();

            var activeSuspensions = await _context.MemberSuspensions
                .Where(s => s.MemberId == user.Id && s.Status == "Active" &&
                            (s.SuspensionType == "Permanent" || s.EndDate == null || s.EndDate > VnTime.Now))
                .ToListAsync();

            var vmList = memberships.Select(m =>
            {
                var susp = activeSuspensions.FirstOrDefault(s => s.GymId == m.GymId);
                return new MyMembershipViewModel
                {
                    MembershipId       = m.Id,
                    GymId              = m.GymId,
                    GymName            = m.Gym?.Name ?? "—",
                    GymAddress         = m.Gym?.Address ?? "—",
                    GymImage           = m.Gym?.ImageUrl ?? string.Empty,
                    PackageId          = m.PackageId,
                    PackageName        = m.Package?.Name ?? "—",
                    PackageType        = m.Package?.PackageType ?? "Daily",
                    DurationInMonths   = m.Package?.DurationInMonths,
                    StartDate          = m.StartDate,
                    EndDate            = m.EndDate,
                    PurchaseDate       = m.PurchaseDate,
                    PriceAtPurchase    = m.PriceAtPurchase,
                    IsSuspended        = susp != null,
                    SuspensionType     = susp?.SuspensionType,
                    SuspensionEndDate  = susp?.EndDate,
                    SuspensionReason   = susp?.Reason
                };
            }).ToList();

            return View(vmList);
        }

        // ==================== MEM-14: CHI TIẾT 1 VÉ HỘI VIÊN ====================
        public async Task<IActionResult> MembershipDetails(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var membership = await _context.MemberMemberships
                .Include(m => m.Gym)
                .Include(m => m.Package)
                .Include(m => m.Transactions)
                    .ThenInclude(t => t.Invoice)
                .FirstOrDefaultAsync(m => m.Id == id && m.MemberId == user.Id);

            if (membership == null) return NotFound();

            var vm = new MembershipDetailsViewModel
            {
                MembershipId     = membership.Id,
                GymId            = membership.GymId,
                GymName          = membership.Gym?.Name ?? "—",
                GymAddress       = membership.Gym?.Address ?? "—",
                GymDescription   = membership.Gym?.Description ?? string.Empty,
                GymImage         = membership.Gym?.ImageUrl ?? string.Empty,
                PackageId        = membership.PackageId,
                PackageName      = membership.Package?.Name ?? "—",
                PackageType      = membership.Package?.PackageType ?? "Daily",
                DurationInMonths = membership.Package?.DurationInMonths,
                StartDate        = membership.StartDate,
                EndDate          = membership.EndDate,
                PurchaseDate     = membership.PurchaseDate,
                PriceAtPurchase  = membership.PriceAtPurchase,
                TransactionId    = membership.Transaction?.Id,
                VnpTxnRef        = membership.Transaction?.VnpTxnRef,
                InvoiceId        = membership.Transaction?.Invoice?.Id,
                InvoiceCode      = membership.Transaction?.Invoice?.InvoiceCode
            };

            return View(vm);
        }

        // ==================== MEM-18: TIẾ́N TRÌNH VIP CỦA HỘI VIÊN ====================
        [HttpGet]
        public async Task<IActionResult> MyVipStatus()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Tìm tất cả các phòng Gym mà hội viên đã từng mua vé (nguồn truth: MemberMemberships)
            var gymIdsFromPurchases = await _context.MemberMemberships
                .Where(m => m.MemberId == user.Id)
                .Select(m => m.GymId)
                .Distinct()
                .ToListAsync();

            if (!gymIdsFromPurchases.Any())
            {
                return View(new MyVipOverviewViewModel { GymVipList = new() });
            }

            var gyms = await _context.Gyms
                .Where(g => gymIdsFromPurchases.Contains(g.Id))
                .ToListAsync();

            // Lấy tất cả VipTierSetting đang hoạt động của các Gym này (sắp xếp tăng dần)
            var allTiers = await _context.VipTierSettings
                .Where(t => gymIdsFromPurchases.Contains(t.GymId) && t.IsActive)
                .OrderBy(t => t.GymId)
                .ThenBy(t => t.MinPurchaseCount)
                .ToListAsync();

            // Đếm số lần mua thực tế từng gym (nguồn truth thay vì MemberVipStatus)
            var purchaseCounts = await _context.MemberMemberships
                .Where(m => m.MemberId == user.Id && gymIdsFromPurchases.Contains(m.GymId))
                .GroupBy(m => m.GymId)
                .Select(g => new { GymId = g.Key, Count = g.Count() })
                .ToListAsync();

            // Lấy VipStatus để lấy AchievedAt (thời điểm thăng hạng)
            var vipStatuses = await _context.MemberVipStatuses
                .Where(v => v.MemberId == user.Id && gymIdsFromPurchases.Contains(v.GymId))
                .ToListAsync();

            var vmList = new List<MemberGymVipProgressViewModel>();

            foreach (var gym in gyms)
            {
                var gymTiers = allTiers.Where(t => t.GymId == gym.Id).OrderBy(t => t.MinPurchaseCount).ToList();
                if (!gymTiers.Any()) continue; // Gym chưa cấu hình VIP thì bỏ qua

                // Đếm số gói thực tế
                int currentPurchases = purchaseCounts.FirstOrDefault(p => p.GymId == gym.Id)?.Count ?? 0;

                // Tính tier hiện tại dựa trên số lần mua thực tế
                var currentTier = gymTiers
                    .Where(t => t.MinPurchaseCount <= currentPurchases)
                    .OrderByDescending(t => t.MinPurchaseCount)
                    .FirstOrDefault();

                // Tìm hạng kế tiếp
                var nextTier = gymTiers
                    .Where(t => t.MinPurchaseCount > currentPurchases)
                    .OrderBy(t => t.MinPurchaseCount)
                    .FirstOrDefault();

                var vipStatus = vipStatuses.FirstOrDefault(v => v.GymId == gym.Id);

                vmList.Add(new MemberGymVipProgressViewModel
                {
                    GymId = gym.Id,
                    GymName = gym.Name,
                    GymAddress = gym.Address,
                    GymImage = gym.ImageUrl ?? string.Empty,
                    CurrentTierId = currentTier?.Id,
                    CurrentTierName = currentTier?.TierName ?? "Chưa có hạng",
                    CurrentTierColor = currentTier?.BadgeColor ?? "#9CA3AF",
                    CurrentDiscountPercent = currentTier?.DiscountPercent,
                    CurrentBenefitDescription = currentTier?.BenefitDescription,
                    TotalPurchases = currentPurchases,
                    AchievedAt = vipStatus?.AchievedAt,
                    NextTierName = nextTier?.TierName,
                    NextTierColor = nextTier?.BadgeColor,
                    NextTierMinPurchases = nextTier?.MinPurchaseCount,
                    NextTierDiscountPercent = nextTier?.DiscountPercent,
                    NextTierBenefit = nextTier?.BenefitDescription
                });
            }

            var vm = new MyVipOverviewViewModel
            {
                GymVipList = vmList
            };

            return View(vm);
        }


        // ==================== MEM-19: BẢNG ĐẶC QUYỀN VIP CÔNG KHAI TẠI 1 PHÒNG GYM ====================
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> VipBenefits(int gymId)
        {
            var gym = await _context.Gyms.FirstOrDefaultAsync(g => g.Id == gymId);
            if (gym == null) return NotFound("Không tìm thấy thông tin cơ sở phòng gym.");

            var tiers = await _context.VipTierSettings
                .Where(t => t.GymId == gymId && t.IsActive)
                .OrderBy(t => t.MinPurchaseCount)
                .ThenBy(t => t.DisplayOrder)
                .ToListAsync();

            var vm = new GymVipBenefitsViewModel
            {
                GymId = gym.Id,
                GymName = gym.Name,
                GymAddress = gym.Address,
                GymImage = gym.ImageUrl ?? string.Empty,
                Tiers = tiers
            };

            return View(vm);
        }

        // ==================== MEM-20: MÃ QR CÁ NHÂN VÀO TẬP (DIGITAL MEMBER CARD) ====================
        [HttpGet]
        public async Task<IActionResult> MyQrCode()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            string qrPayload = QrCodeHelper.GenerateMemberQrPayload(user);
            var today = VnTime.Today;

            var activeMemberships = await _context.MemberMemberships
                .Include(m => m.Gym)
                .Include(m => m.Package)
                .Where(m => m.MemberId == user.Id && m.EndDate >= today)
                .OrderByDescending(m => m.EndDate)
                .Select(m => new ActiveMemberCardItem
                {
                    MembershipId  = m.Id,
                    GymId         = m.GymId,
                    GymName       = m.Gym.Name,
                    GymAddress    = m.Gym.Address,
                    PackageName   = m.Package.Name,
                    PackageType   = m.Package.PackageType == "Daily" ? "Vé ngày" : $"Gói {m.Package.DurationInMonths} tháng",
                    StartDate     = m.StartDate,
                    EndDate       = m.EndDate,
                    DaysRemaining = Math.Max(0, (int)(m.EndDate.Date - today).TotalDays)
                })
                .ToListAsync();

            foreach (var item in activeMemberships)
            {
                item.PackageQrPayload = QrCodeHelper.GenerateMemberQrPayload(user, item.MembershipId);
            }

            // Tìm hạng VIP cao nhất (nếu có)
            var highestVip = await _context.MemberVipStatuses
                .Include(v => v.CurrentTier)
                .Where(v => v.MemberId == user.Id && v.CurrentTier != null)
                .OrderByDescending(v => v.CurrentTier!.DisplayOrder)
                .FirstOrDefaultAsync();

            var vm = new MemberQrViewModel
            {
                MemberId          = user.Id,
                FullName          = user.FullName ?? user.UserName ?? "Hội viên",
                Email             = user.Email ?? "—",
                PhoneNumber       = user.PhoneNumber ?? "—",
                QrPayload         = qrPayload,
                VipTierName       = highestVip?.CurrentTier?.TierName ?? "Standard",
                VipBadgeColor     = highestVip?.CurrentTier?.BadgeColor ?? "#64748b",
                ActiveMemberships = activeMemberships
            };

            return View(vm);
        }

        // ==================== MEM-21: LỊCH SỬ ĐIỂM DANH CHECK-IN ====================
        [HttpGet]
        public async Task<IActionResult> CheckinHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var logs = await _context.CheckinLogs
                .Include(c => c.Gym)
                .Include(c => c.Membership).ThenInclude(m => m!.Package)
                .Include(c => c.CheckedByUser)
                .Where(c => c.MemberId == user.Id)
                .OrderByDescending(c => c.CheckinTime)
                .ToListAsync();

            var startOfMonth = new DateTime(VnTime.Now.Year, VnTime.Now.Month, 1);
            int thisMonthCount = logs.Count(l => l.CheckinTime >= startOfMonth);

            var vm = new MemberPersonalCheckinHistoryViewModel
            {
                MemberName       = user.FullName ?? user.UserName ?? "Hội viên",
                TotalVisits      = logs.Count,
                ThisMonthVisits  = thisMonthCount,
                Visits = logs.Select(l => new CheckinHistoryRowItem
                {
                    Id            = l.Id,
                    MemberId      = l.MemberId,
                    MemberName    = user.FullName ?? user.UserName ?? "Hội viên",
                    MemberEmail   = user.Email ?? "—",
                    MemberPhone   = user.PhoneNumber ?? "—",
                    GymName       = l.Gym?.Name ?? "—",
                    PackageName   = l.Membership?.Package?.Name ?? "Vé vào tập",
                    CheckinTime   = l.CheckinTime,
                    CheckoutTime  = l.CheckoutTime,
                    Status        = l.Status,
                    CheckinMethod = l.CheckinMethod,
                    StaffName     = l.CheckedByUser?.FullName ?? l.CheckedByUser?.UserName ?? "Hệ thống"
                }).ToList()
            };

            return View(vm);
        }

        // ==================== MEM-23: QUẢN LÝ ĐÁNH GIÁ CỦA TÔI ====================
        public async Task<IActionResult> MyReviews()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var reviews = await _context.GymReviews
                .Include(r => r.Gym)
                    .ThenInclude(g => g.GymImages)
                .Where(r => r.MemberId == user.Id)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new GymReviewDisplayViewModel
                {
                    Id = r.Id,
                    GymId = r.GymId,
                    GymName = r.Gym.Name,
                    GymAddress = r.Gym.Address,
                    GymImage = r.Gym.GymImages.Where(i => i.IsCover).Select(i => i.ImageUrl).FirstOrDefault()
                               ?? r.Gym.GymImages.OrderBy(i => i.DisplayOrder).Select(i => i.ImageUrl).FirstOrDefault()
                               ?? r.Gym.ImageUrl ?? string.Empty,
                    MemberId = r.MemberId,
                    MemberName = user.FullName ?? user.UserName ?? "Hội viên",
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt,
                    UpdatedAt = r.UpdatedAt,
                    IsVisible = r.IsVisible,
                    OwnerReply = r.OwnerReply,
                    OwnerRepliedAt = r.OwnerRepliedAt
                })
                .ToListAsync();

            return View(reviews);
        }

        // ==================== MEM-24 (V3): QUẢN LÝ FACE ID ĐIỂM DANH ====================
        [HttpGet]
        public async Task<IActionResult> FaceId()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var faceProfile = await _context.MemberFaceProfiles
                .FirstOrDefaultAsync(f => f.MemberId == user.Id);

            var vm = new MemberFaceIdViewModel
            {
                HasFaceRegistered = faceProfile != null,
                SampleImageUrl = faceProfile?.SampleImageUrl,
                RegisteredAt = faceProfile?.CreatedAt,
                UpdatedAt = faceProfile?.UpdatedAt,
                QualityScore = faceProfile?.QualityScore ?? 1.0,
                IsActive = faceProfile?.IsActive ?? true,
                FullName = user.FullName ?? user.UserName ?? "Hội viên",
                Email = user.Email ?? "—"
            };

            return View(vm);
        }

        // POST: /Member/RegisterFace
        [HttpPost]
        public async Task<IActionResult> RegisterFace([FromBody] RegisterFaceRequestDto model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized(new { success = false, message = "Vui lòng đăng nhập lại." });

            if (model == null || model.Descriptor == null || model.Descriptor.Length != 128)
            {
                return Json(new { success = false, message = "Vector đặc trưng khuôn mặt không hợp lệ (cần đủ 128 chiều FaceNet)." });
            }

            // Lưu ảnh thumbnail khuôn mặt nếu có gửi lên dạng Base64
            string? sampleImageUrl = null;
            if (!string.IsNullOrWhiteSpace(model.ImageBase64))
            {
                try
                {
                    string base64Data = model.ImageBase64;
                    if (base64Data.Contains(","))
                    {
                        base64Data = base64Data.Substring(base64Data.IndexOf(",") + 1);
                    }

                    byte[] imageBytes = Convert.FromBase64String(base64Data);
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "faces");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string fileName = $"{user.Id}_{DateTime.UtcNow.Ticks}.jpg";
                    string filePath = Path.Combine(uploadsFolder, fileName);
                    await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);
                    sampleImageUrl = $"/uploads/faces/{fileName}";
                }
                catch
                {
                    // Nếu lỗi lưu ảnh thì không chặn lưu vector
                }
            }

            string embeddingJson = JsonSerializer.Serialize(model.Descriptor);

            var existingProfile = await _context.MemberFaceProfiles
                .FirstOrDefaultAsync(f => f.MemberId == user.Id);

            if (existingProfile == null)
            {
                existingProfile = new MemberFaceProfile
                {
                    MemberId = user.Id,
                    FaceEmbeddingJson = embeddingJson,
                    SampleImageUrl = sampleImageUrl,
                    QualityScore = model.QualityScore > 0 ? model.QualityScore : 1.0,
                    IsActive = true,
                    CreatedAt = VnTime.Now
                };
                _context.MemberFaceProfiles.Add(existingProfile);
            }
            else
            {
                existingProfile.FaceEmbeddingJson = embeddingJson;
                if (!string.IsNullOrEmpty(sampleImageUrl))
                {
                    existingProfile.SampleImageUrl = sampleImageUrl;
                }
                existingProfile.QualityScore = model.QualityScore > 0 ? model.QualityScore : 1.0;
                existingProfile.IsActive = true;
                existingProfile.UpdatedAt = VnTime.Now;
            }

            _context.SystemLogs.Add(new SystemLog
            {
                UserId = user.Id,
                Action = "FaceIdEnrolled",
                Entity = "MemberFaceProfile",
                EntityId = user.Id,
                Level = "Info",
                Description = $"Hội viên {user.FullName} ({user.Email}) đã kích hoạt/cập nhật Face ID FaceNet thành công.",
                CreatedAt = VnTime.Now
            });

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Thiết lập Face ID thành công! Bạn có thể sử dụng khuôn mặt để Check-in/Check-out tại quầy lễ tân.",
                sampleImageUrl = existingProfile.SampleImageUrl,
                registeredAt = existingProfile.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            });
        }

        // POST: /Member/ToggleFaceId
        [HttpPost]
        public async Task<IActionResult> ToggleFaceId([FromBody] bool isActive)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized(new { success = false });

            var profile = await _context.MemberFaceProfiles.FirstOrDefaultAsync(f => f.MemberId == user.Id);
            if (profile == null)
            {
                return Json(new { success = false, message = "Bạn chưa đăng ký Face ID." });
            }

            profile.IsActive = isActive;
            profile.UpdatedAt = VnTime.Now;
            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                isActive = profile.IsActive, 
                message = profile.IsActive ? "Đã bật tính năng điểm danh bằng Face ID." : "Đã tạm dừng tính năng Face ID." 
            });
        }

        // POST: /Member/DeleteFaceId
        [HttpPost]
        public async Task<IActionResult> DeleteFaceId()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized(new { success = false });

            var profile = await _context.MemberFaceProfiles.FirstOrDefaultAsync(f => f.MemberId == user.Id);
            if (profile != null)
            {
                _context.MemberFaceProfiles.Remove(profile);
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, message = "Đã gỡ bỏ dữ liệu Face ID." });
        }
    }
}
