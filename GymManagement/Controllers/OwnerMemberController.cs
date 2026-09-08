using GymManagement.Helpers;
using GymManagement.Models;
using GymManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Controllers
{
    /// <summary>
    /// Quản lý danh sách hội viên, lịch sử tập luyện và đình chỉ hội viên tại phòng gym (OWN-15, OWN-16, OWN-23, OWN-24, OWN-25).
    /// </summary>
    [Authorize(Roles = "Owner")]
    public class OwnerMemberController : Controller
    {
        private readonly GymDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmailHelper _emailHelper;

        public OwnerMemberController(
            GymDbContext context,
            UserManager<ApplicationUser> userManager,
            EmailHelper emailHelper)
        {
            _context = context;
            _userManager = userManager;
            _emailHelper = emailHelper;
        }

        private async Task<string> GetCurrentUserIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.Id ?? string.Empty;
        }

        // ==================== OWN-15: DANH SÁCH HỘI VIÊN ====================
        // GET /OwnerMember/Index?gymId=...
        public async Task<IActionResult> Index(int? gymId)
        {
            var userId = await GetCurrentUserIdAsync();

            // Danh sách phòng Gym của Owner (đã duyệt)
            var myGyms = await _context.Gyms
                .Where(g => g.OwnerId == userId && g.Status == "Approved")
                .OrderBy(g => g.Name)
                .ToListAsync();

            var query = _context.MemberMemberships
                .Include(m => m.Member)
                .Include(m => m.Gym)
                .Include(m => m.Package)
                .Where(m => m.Gym.OwnerId == userId);

            if (gymId.HasValue && gymId.Value > 0)
            {
                query = query.Where(m => m.GymId == gymId.Value);
            }

            var memberships = await query
                .OrderByDescending(m => m.EndDate)
                .ToListAsync();

            // Lọc Distinct theo từng cặp (Hội viên + Cơ sở Gym):
            // - Trong cùng 1 phòng Gym: Hội viên chỉ xuất hiện 1 dòng duy nhất (lấy gói có thời hạn mới nhất).
            // - Khác phòng Gym: Hội viên vẫn xuất hiện đầy đủ ở từng cơ sở phòng Gym tương ứng.
            var distinctMemberships = memberships
                .GroupBy(m => new { m.MemberId, m.GymId })
                .Select(g => g.OrderByDescending(m => m.EndDate).ThenByDescending(m => m.PurchaseDate).First())
                .ToList();

            // Lấy danh sách các lệnh đình chỉ đang hiệu lực tại các cơ sở của Owner
            var now = VnTime.Now;
            var activeSuspensions = await _context.MemberSuspensions
                .Where(s => s.Gym.OwnerId == userId
                    && s.Status == "Active"
                    && (s.SuspensionType == "Permanent" || (s.EndDate != null && s.EndDate > now)))
                .ToListAsync();

            var vm = new OwnerMemberListViewModel
            {
                SelectedGymId = gymId,
                MyGyms = myGyms,
                Members = distinctMemberships.Select(m =>
                {
                    var susp = activeSuspensions.FirstOrDefault(s => s.GymId == m.GymId && s.MemberId == m.MemberId);
                    return new OwnerMemberItemViewModel
                    {
                        MembershipId       = m.Id,
                        MemberId           = m.MemberId,
                        FullName           = m.Member?.FullName ?? "—",
                        Email              = m.Member?.Email ?? "—",
                        PhoneNumber        = m.Member?.PhoneNumber ?? "—",
                        GymId              = m.GymId,
                        GymName            = m.Gym?.Name ?? "—",
                        PackageName        = m.Package?.Name ?? "—",
                        PackageTypeLabel   = m.Package?.PackageType == "Daily" ? "Vé ngày" : $"Gói {m.Package?.DurationInMonths} tháng",
                        StartDate          = m.StartDate,
                        EndDate            = m.EndDate,
                        PriceAtPurchase    = m.PriceAtPurchase,
                        PurchaseDate       = m.PurchaseDate,
                        IsSuspended        = susp != null,
                        SuspensionId       = susp?.Id,
                        SuspensionType     = susp?.SuspensionType,
                        SuspensionEndDate  = susp?.EndDate,
                        SuspensionReason   = susp?.Reason
                    };
                }).ToList()
            };

            return View(vm);
        }

        // ==================== OWN-16: CHI TIẾT 1 HỘI VIÊN ====================
        // GET /OwnerMember/Details?memberId=...&gymId=...
        public async Task<IActionResult> Details(string memberId, int? gymId)
        {
            var userId = await GetCurrentUserIdAsync();

            var member = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == memberId);

            if (member == null) return NotFound();

            var query = _context.MemberMemberships
                .Include(m => m.Gym)
                .Include(m => m.Package)
                .Include(m => m.Transactions)
                    .ThenInclude(t => t.Invoice)
                .Where(m => m.MemberId == memberId && m.Gym.OwnerId == userId);

            if (gymId.HasValue && gymId.Value > 0)
            {
                query = query.Where(m => m.GymId == gymId.Value);
            }

            var history = await query
                .OrderByDescending(m => m.PurchaseDate)
                .ToListAsync();

            if (!history.Any()) return NotFound();

            var firstGym = history.First().Gym;
            var targetGymId = gymId ?? firstGym.Id;

            // Module 3: VIP Loyalty Status tại Gym này
            var vipStatus = await _context.MemberVipStatuses
                .Include(v => v.CurrentTier)
                .FirstOrDefaultAsync(v => v.GymId == targetGymId && v.MemberId == memberId);

            var vm = new OwnerMemberDetailsViewModel
            {
                MemberId       = member.Id,
                FullName       = member.FullName ?? member.UserName ?? "—",
                Email          = member.Email ?? "—",
                PhoneNumber    = member.PhoneNumber ?? "—",
                GymId          = targetGymId,
                GymName        = firstGym?.Name ?? "—",
                GymAddress     = firstGym?.Address ?? "—",
                TotalSpent     = history.Sum(h => h.PriceAtPurchase),
                TotalPurchases = history.Count,
                CurrentTierId         = vipStatus?.CurrentTierId,
                VipTierName           = vipStatus?.CurrentTier?.TierName,
                VipBadgeColor         = vipStatus?.CurrentTier?.BadgeColor,
                VipDiscountPercent    = vipStatus?.CurrentTier?.DiscountPercent,
                VipBenefitDescription = vipStatus?.CurrentTier?.BenefitDescription,
                VipTotalPurchaseCount = vipStatus?.TotalPurchaseCount ?? 0,
                VipAchievedAt         = vipStatus?.AchievedAt,
                PurchaseHistory = history.Select(h => new OwnerMemberPurchaseHistoryItem
                {
                    MembershipId     = h.Id,
                    PackageName      = h.Package?.Name ?? "—",
                    PackageTypeLabel = h.Package?.PackageType == "Daily" ? "Vé ngày" : $"Gói {h.Package?.DurationInMonths} tháng",
                    StartDate        = h.StartDate,
                    EndDate          = h.EndDate,
                    PurchaseDate     = h.PurchaseDate,
                    PriceAtPurchase  = h.PriceAtPurchase,
                    InvoiceCode      = h.Transaction?.Invoice?.InvoiceCode
                }).ToList()
            };

            return View(vm);
        }

        // ==================== OWN-23: ĐÌNH CHỈ HỘI VIÊN ====================
        // GET /OwnerMember/Suspend?membershipId=... HOẶC ?gymId=...&memberId=...
        [HttpGet]
        public async Task<IActionResult> Suspend(int? membershipId, int? gymId, string? memberId)
        {
            var userId = await GetCurrentUserIdAsync();

            int targetGymId = 0;
            string targetMemberId = string.Empty;

            if (membershipId.HasValue && membershipId.Value > 0)
            {
                var membership = await _context.MemberMemberships
                    .Include(m => m.Gym)
                    .FirstOrDefaultAsync(m => m.Id == membershipId.Value && m.Gym.OwnerId == userId);

                if (membership == null) return NotFound("Không tìm thấy thông tin gói tập của hội viên.");

                targetGymId = membership.GymId;
                targetMemberId = membership.MemberId;
            }
            else if (gymId.HasValue && !string.IsNullOrEmpty(memberId))
            {
                targetGymId = gymId.Value;
                targetMemberId = memberId;
            }
            else
            {
                TempData["Error"] = "Thông tin yêu cầu đình chỉ không đầy đủ.";
                return RedirectToAction("Index");
            }

            // Kiểm tra Gym thuộc sở hữu của Owner
            var gym = await _context.Gyms.FirstOrDefaultAsync(g => g.Id == targetGymId && g.OwnerId == userId);
            if (gym == null) return Forbid();

            // Kiểm tra tài khoản hội viên
            var member = await _context.Users.FirstOrDefaultAsync(u => u.Id == targetMemberId);
            if (member == null) return NotFound("Không tìm thấy thông tin hội viên.");

            // Không thể đình chỉ chính mình hoặc Admin
            if (member.Id == userId)
            {
                TempData["Error"] = "Bạn không thể tự đình chỉ chính mình.";
                return RedirectToAction("Index", new { gymId = targetGymId });
            }

            if (await _userManager.IsInRoleAsync(member, "Admin"))
            {
                TempData["Error"] = "Không thể áp dụng hình thức đình chỉ đối với Quản trị viên hệ thống (Admin).";
                return RedirectToAction("Index", new { gymId = targetGymId });
            }

            var vm = new CreateSuspensionViewModel
            {
                GymId          = targetGymId,
                GymName        = gym.Name,
                MemberId       = targetMemberId,
                MemberName     = member.FullName ?? member.UserName ?? "—",
                MemberEmail    = member.Email ?? "—",
                SuspensionType = "Temporary",
                DurationDays   = 7,
                EndDate        = VnTime.Now.AddDays(7)
            };

            return View(vm);
        }

        // POST /OwnerMember/Suspend
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Suspend(CreateSuspensionViewModel model)
        {
            var userId = await GetCurrentUserIdAsync();

            var gym = await _context.Gyms.FirstOrDefaultAsync(g => g.Id == model.GymId && g.OwnerId == userId);
            if (gym == null) return Forbid();

            var member = await _context.Users.FirstOrDefaultAsync(u => u.Id == model.MemberId);
            if (member == null) return NotFound();

            if (member.Id == userId || await _userManager.IsInRoleAsync(member, "Admin"))
            {
                TempData["Error"] = "Thao tác không hợp lệ đối với tài khoản này.";
                return RedirectToAction("Index", new { gymId = model.GymId });
            }

            if (string.IsNullOrWhiteSpace(model.Reason))
            {
                ModelState.AddModelError("Reason", "Vui lòng nhập lý do đình chỉ cụ thể.");
            }

            DateTime? calculatedEndDate = null;
            if (model.SuspensionType == "Temporary")
            {
                if (model.DurationDays.HasValue && model.DurationDays.Value > 0)
                {
                    calculatedEndDate = VnTime.Now.AddDays(model.DurationDays.Value);
                }
                else if (model.EndDate.HasValue && model.EndDate.Value > VnTime.Now)
                {
                    calculatedEndDate = model.EndDate.Value;
                }
                else
                {
                    ModelState.AddModelError("DurationDays", "Vui lòng chọn số ngày đình chỉ hợp lệ hoặc đặt ngày kết thúc trong tương lai.");
                }
            }
            else
            {
                calculatedEndDate = null; // Vĩnh viễn
            }

            if (!ModelState.IsValid)
            {
                model.GymName = gym.Name;
                model.MemberName = member.FullName ?? member.UserName ?? "—";
                model.MemberEmail = member.Email ?? "—";
                return View(model);
            }

            // Gỡ bỏ hoặc vô hiệu hoá các lệnh đình chỉ active cũ của Member tại Gym này (nếu có)
            var oldSuspensions = await _context.MemberSuspensions
                .Where(s => s.GymId == model.GymId && s.MemberId == model.MemberId && s.Status == "Active")
                .ToListAsync();

            foreach (var old in oldSuspensions)
            {
                old.Status = "Lifted";
                old.LiftedAt = VnTime.Now;
                old.LiftedReason = "Được thay thế bằng quyết định đình chỉ mới.";
            }

            // Tạo quyết định đình chỉ mới
            var suspension = new MemberSuspension
            {
                GymId             = model.GymId,
                MemberId          = model.MemberId,
                SuspendedByUserId = userId,
                Reason            = model.Reason.Trim(),
                SuspensionType    = model.SuspensionType,
                StartDate         = VnTime.Now,
                EndDate           = calculatedEndDate,
                Status            = "Active",
                CreatedAt         = VnTime.Now
            };

            _context.MemberSuspensions.Add(suspension);
            await _context.SaveChangesAsync();

            // Ghi nhận nhật ký hệ thống (SystemLog)
            var currentUser = await _userManager.GetUserAsync(User);
            var ownerName = currentUser?.FullName ?? currentUser?.UserName ?? "Chủ phòng";
            var mName = member.FullName ?? member.UserName ?? "Hội viên";
            var typeLabel = suspension.SuspensionType == "Permanent" ? "Vĩnh viễn" : $"Tạm thời đến {calculatedEndDate:dd/MM/yyyy}";

            _context.SystemLogs.Add(new SystemLog
            {
                UserId      = userId,
                Action      = "MemberSuspended",
                Entity      = "MemberSuspension",
                EntityId    = suspension.Id.ToString(),
                Level       = "Warning",
                Description = $"Chủ phòng {ownerName} đã đình chỉ hội viên {mName} ({member.Email}) tại cơ sở \"{gym.Name}\". Hình thức: {typeLabel}. Lý do: \"{suspension.Reason}\".",
                CreatedAt   = VnTime.Now
            });
            await _context.SaveChangesAsync();

            // Gửi email thông báo cho hội viên
            if (!string.IsNullOrEmpty(member.Email))
            {
                await _emailHelper.SendSuspensionEmailAsync(
                    member.Email,
                    mName,
                    gym.Name,
                    suspension.SuspensionType,
                    suspension.EndDate,
                    suspension.Reason);
            }

            TempData["Success"] = $"Đã áp dụng lệnh đình chỉ đối với hội viên {mName} ({typeLabel}) thành công.";
            return RedirectToAction("Index", new { gymId = model.GymId });
        }

        // ==================== OWN-24: GỠ ĐÌNH CHỈ HỘI VIÊN ====================
        // POST /OwnerMember/LiftSuspension
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LiftSuspension(int suspensionId, string? liftedReason)
        {
            var userId = await GetCurrentUserIdAsync();

            var suspension = await _context.MemberSuspensions
                .Include(s => s.Gym)
                .Include(s => s.Member)
                .FirstOrDefaultAsync(s => s.Id == suspensionId && s.Gym.OwnerId == userId);

            if (suspension == null) return NotFound("Không tìm thấy quyết định đình chỉ này.");

            if (suspension.Status != "Active")
            {
                TempData["Error"] = "Lệnh đình chỉ này đã được gỡ hoặc không còn hiệu lực.";
                return RedirectToAction("Suspensions", new { gymId = suspension.GymId });
            }

            suspension.Status       = "Lifted";
            suspension.LiftedAt     = VnTime.Now;
            suspension.LiftedReason = string.IsNullOrWhiteSpace(liftedReason) ? "Chủ phòng quyết định khôi phục quyền lợi sớm." : liftedReason.Trim();

            var currentUser = await _userManager.GetUserAsync(User);
            var ownerName = currentUser?.FullName ?? currentUser?.UserName ?? "Chủ phòng";
            var mName = suspension.Member?.FullName ?? suspension.Member?.UserName ?? "Hội viên";

            // Ghi SystemLog
            _context.SystemLogs.Add(new SystemLog
            {
                UserId      = userId,
                Action      = "MemberSuspensionLifted",
                Entity      = "MemberSuspension",
                EntityId    = suspension.Id.ToString(),
                Level       = "Info",
                Description = $"Chủ phòng {ownerName} đã gỡ bỏ đình chỉ cho hội viên {mName} tại cơ sở \"{suspension.Gym?.Name}\". Lý do gỡ: \"{suspension.LiftedReason}\".",
                CreatedAt   = VnTime.Now
            });

            await _context.SaveChangesAsync();

            // Gửi email thông báo khôi phục
            if (suspension.Member != null && !string.IsNullOrEmpty(suspension.Member.Email))
            {
                await _emailHelper.SendSuspensionLiftedEmailAsync(
                    suspension.Member.Email,
                    mName,
                    suspension.Gym?.Name ?? "Phòng Gym",
                    suspension.LiftedReason);
            }

            TempData["Success"] = $"Đã gỡ bỏ đình chỉ thành công cho hội viên {mName}.";

            // Chuyển hướng quay lại trang thích hợp
            var referer = Request.Headers["Referer"].ToString();
            if (!string.IsNullOrEmpty(referer) && referer.Contains("Suspensions", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Suspensions", new { gymId = suspension.GymId });
            }

            return RedirectToAction("Index", new { gymId = suspension.GymId });
        }

        // ==================== OWN-25: DANH SÁCH ĐÌNH CHỈ ====================
        // GET /OwnerMember/Suspensions?gymId=...&status=...
        [HttpGet]
        public async Task<IActionResult> Suspensions(int? gymId, string? status)
        {
            var userId = await GetCurrentUserIdAsync();

            var myGyms = await _context.Gyms
                .Where(g => g.OwnerId == userId && g.Status == "Approved")
                .OrderBy(g => g.Name)
                .ToListAsync();

            var query = _context.MemberSuspensions
                .Include(s => s.Gym)
                .Include(s => s.Member)
                .Include(s => s.SuspendedByUser)
                .Where(s => s.Gym.OwnerId == userId);

            if (gymId.HasValue && gymId.Value > 0)
            {
                query = query.Where(s => s.GymId == gymId.Value);
            }

            var now = VnTime.Now;

            if (status == "Active")
            {
                query = query.Where(s => s.Status == "Active" && (s.SuspensionType == "Permanent" || (s.EndDate != null && s.EndDate > now)));
            }
            else if (status == "Lifted")
            {
                query = query.Where(s => s.Status == "Lifted");
            }
            else if (status == "Expired")
            {
                query = query.Where(s => s.Status == "Active" && s.SuspensionType == "Temporary" && s.EndDate != null && s.EndDate <= now);
            }

            var list = await query
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var vmList = list.Select(s => new SuspensionItemViewModel
            {
                Id                  = s.Id,
                GymId               = s.GymId,
                GymName             = s.Gym?.Name ?? "—",
                MemberId            = s.MemberId,
                MemberName          = s.Member?.FullName ?? s.Member?.UserName ?? "—",
                MemberEmail         = s.Member?.Email ?? "—",
                SuspendedByUserId   = s.SuspendedByUserId,
                SuspendedByUserName = s.SuspendedByUser?.FullName ?? s.SuspendedByUser?.UserName ?? "Chủ phòng",
                Reason              = s.Reason,
                SuspensionType      = s.SuspensionType,
                StartDate           = s.StartDate,
                EndDate             = s.EndDate,
                Status              = s.Status,
                LiftedAt            = s.LiftedAt,
                LiftedReason        = s.LiftedReason,
                CreatedAt           = s.CreatedAt
            }).ToList();

            ViewBag.MyGyms = myGyms;
            ViewBag.SelectedGymId = gymId;
            ViewBag.SelectedStatus = status ?? "All";

            return View(vmList);
        }
    }
}
