using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GymManagement.Helpers;
using GymManagement.Models;
using GymManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Controllers
{
    [Authorize(Roles = "Owner")]
    public class OwnerCheckinController : Controller
    {
        private readonly GymDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OwnerCheckinController(GymDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<string> GetCurrentUserIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.Id ?? string.Empty;
        }

        // ==================== QUẦY CHECK-IN VÀO TẬP & CROWD METER ====================
        // GET: /OwnerCheckin/Scan?gymId=...
        public async Task<IActionResult> Scan(int? gymId)
        {
            var userId = await GetCurrentUserIdAsync();
            var myGyms = await _context.Gyms
                .Where(g => g.OwnerId == userId && g.Status == "Approved")
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();

            if (!myGyms.Any())
            {
                TempData["Warning"] = "Bạn chưa có phòng gym nào được duyệt để mở quầy check-in.";
                return RedirectToAction("Index", "OwnerGym");
            }

            var selectedGym = gymId.HasValue
                ? myGyms.FirstOrDefault(g => g.Id == gymId.Value) ?? myGyms.First()
                : myGyms.First();

            int targetGymId = selectedGym.Id;
            int maxCapacity = selectedGym.MaxCapacity > 0 ? selectedGym.MaxCapacity : 50;

            // Cơ chế Tự động bốc hơi 2 tiếng (Session Timeout 2h):
            var twoHoursAgo = VnTime.Now.AddHours(-2);
            var activeCheckins = await _context.CheckinLogs
                .Include(c => c.Member)
                .Include(c => c.Membership).ThenInclude(m => m!.Package)
                .Where(c => c.GymId == targetGymId 
                         && c.CheckinTime >= twoHoursAgo 
                         && c.CheckoutTime == null 
                         && c.Status == "Success")
                .OrderByDescending(c => c.CheckinTime)
                .ToListAsync();

            // Tính tỷ lệ % lấp đầy (Crowd Meter)
            int activeCount = activeCheckins.Count;
            int crowdPercentage = Math.Min(100, (int)Math.Round((double)activeCount / maxCapacity * 100));

            var (statusText, statusColor, statusIcon, recommendation) = GetCrowdStatus(crowdPercentage);

            // Lấy hạng VIP của các hội viên đang tập
            var memberIds = activeCheckins.Select(c => c.MemberId).Distinct().ToList();
            var vipStatuses = await _context.MemberVipStatuses
                .Include(v => v.CurrentTier)
                .Where(v => v.GymId == targetGymId && memberIds.Contains(v.MemberId))
                .ToDictionaryAsync(v => v.MemberId);

            var liveMembers = activeCheckins.Select(c =>
            {
                var vip = vipStatuses.GetValueOrDefault(c.MemberId);
                int minutesAgo = Math.Max(0, (int)(VnTime.Now - c.CheckinTime).TotalMinutes);
                return new LiveMemberInGymItem
                {
                    CheckinId     = c.Id,
                    MemberId      = c.MemberId,
                    MemberName    = c.Member.FullName ?? c.Member.UserName ?? "Hội viên",
                    MemberEmail   = c.Member.Email ?? "—",
                    MemberPhone   = c.Member.PhoneNumber ?? "—",
                    PackageName   = c.Membership?.Package?.Name ?? "Vé vào tập",
                    VipTierName   = vip?.CurrentTier?.TierName ?? "Standard",
                    VipBadgeColor = vip?.CurrentTier?.BadgeColor ?? "#64748b",
                    CheckinTime   = c.CheckinTime,
                    MinutesAgo    = minutesAgo,
                    CheckinMethod = c.CheckinMethod
                };
            }).ToList();

            var vm = new CheckinScanViewModel
            {
                SelectedGymId         = targetGymId,
                SelectedGymName       = selectedGym.Name,
                MyGyms                = myGyms,
                MaxCapacity           = maxCapacity,
                ActiveCheckinsCount   = activeCount,
                CrowdPercentage       = crowdPercentage,
                CrowdStatusText       = statusText,
                CrowdStatusColor      = statusColor,
                CrowdStatusIcon       = statusIcon,
                CrowdRecommendation   = recommendation,
                ActiveMembersInGym    = liveMembers
            };

            return View(vm);
        }

        // ==================== AJAX XÁC THỰC HỘI VIÊN (TỪ QR HOẶC SĐT/TÊN) ====================
        // POST: /OwnerCheckin/VerifyMember
        [HttpPost]
        public async Task<IActionResult> VerifyMember(int gymId, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new CheckinVerifyResultViewModel { Success = false, Message = "Vui lòng quét mã QR hoặc nhập Số điện thoại." });
            }

            var userId = await GetCurrentUserIdAsync();
            var gym = await _context.Gyms.FirstOrDefaultAsync(g => g.Id == gymId && g.OwnerId == userId);
            if (gym == null)
            {
                return Json(new CheckinVerifyResultViewModel { Success = false, Message = "Phòng gym không tồn tại hoặc bạn không có quyền quản lý." });
            }

            query = query.Trim();
            ApplicationUser? member = null;

            // 1. Kiểm tra nếu là QR Payload chuẩn: GYMPRO:MEMBER:{id}:{token}
            string? extractedId = QrCodeHelper.ExtractMemberIdFromPayload(query);
            if (!string.IsNullOrEmpty(extractedId))
            {
                member = await _userManager.FindByIdAsync(extractedId);
                if (member != null && !QrCodeHelper.ValidateMemberQrPayload(query, member, out _))
                {
                    return Json(new CheckinVerifyResultViewModel
                    {
                        Success = false,
                        Message = "Mã QR không hợp lệ hoặc đã bị chỉnh sửa bất hợp pháp!"
                    });
                }
            }

            // 2. Nếu không phải QR Payload thì tra cứu theo SĐT, Email, ID hoặc Tên
            if (member == null)
            {
                member = await _context.Users.FirstOrDefaultAsync(u =>
                    u.PhoneNumber == query ||
                    u.Email == query ||
                    u.Id == query ||
                    u.UserName == query);
            }

            if (member == null)
            {
                // Thử tìm theo họ tên gần đúng
                member = await _context.Users.FirstOrDefaultAsync(u => u.FullName.ToLower().Contains(query.ToLower()));
            }

            if (member == null)
            {
                return Json(new CheckinVerifyResultViewModel
                {
                    Success = false,
                    Message = $"Không tìm thấy hội viên nào khớp với từ khóa: \"{query}\"."
                });
            }

            // 3. Kiểm tra xem hội viên có đang bị KỶ LUẬT / ĐÌNH CHỈ tại gym này không
            var today = VnTime.Today;
            var activeSuspension = await _context.MemberSuspensions
                .FirstOrDefaultAsync(s => s.GymId == gymId 
                                       && s.MemberId == member.Id 
                                       && s.Status == "Active"
                                       && (s.SuspensionType == "Permanent" || (s.EndDate.HasValue && s.EndDate.Value >= today)));

            if (activeSuspension != null)
            {
                string suspensionInfo = activeSuspension.SuspensionType == "Permanent"
                    ? "Đình chỉ VĨNH VIỄN"
                    : $"Đình chỉ đến ngày {activeSuspension.EndDate:dd/MM/yyyy}";

                return Json(new CheckinVerifyResultViewModel
                {
                    Success          = false,
                    CanCheckin       = false,
                    IsSuspended      = true,
                    SuspensionReason = $"Lý do: {activeSuspension.Reason} ({suspensionInfo})",
                    MemberId         = member.Id,
                    FullName         = member.FullName ?? member.UserName ?? "Hội viên",
                    Email            = member.Email ?? "—",
                    PhoneNumber      = member.PhoneNumber ?? "—",
                    GymId            = gym.Id,
                    GymName          = gym.Name,
                    Message          = $"⚠️ CẢNH BÁO: Hội viên đang bị {suspensionInfo.ToUpper()} tại cơ sở này! Không được phép vào tập."
                });
            }

            // 4. Lấy thông tin Hạng VIP Loyalty
            var vipStatus = await _context.MemberVipStatuses
                .Include(v => v.CurrentTier)
                .FirstOrDefaultAsync(v => v.GymId == gymId && v.MemberId == member.Id);

            string vipName = vipStatus?.CurrentTier?.TierName ?? "Standard";
            string vipColor = vipStatus?.CurrentTier?.BadgeColor ?? "#64748b";

            // 5. Kiểm tra nếu mã QR được quét là mã đích danh của một gói tập cụ thể
            int? targetMembershipId = QrCodeHelper.ExtractMembershipIdFromPayload(query);
            MemberMembership? requestedMembership = null;

            if (targetMembershipId.HasValue)
            {
                requestedMembership = await _context.MemberMemberships
                    .Include(m => m.Gym)
                    .Include(m => m.Package)
                    .FirstOrDefaultAsync(m => m.Id == targetMembershipId.Value && m.MemberId == member.Id);

                if (requestedMembership == null)
                {
                    return Json(new CheckinVerifyResultViewModel
                    {
                        Success       = false,
                        CanCheckin    = false,
                        MemberId      = member.Id,
                        FullName      = member.FullName ?? member.UserName ?? "Hội viên",
                        Email         = member.Email ?? "—",
                        PhoneNumber   = member.PhoneNumber ?? "—",
                        GymId         = gym.Id,
                        GymName       = gym.Name,
                        VipTierName   = vipName,
                        VipBadgeColor = vipColor,
                        Message       = "❌ Mã QR gói tập không hợp lệ hoặc không thuộc về hội viên này."
                    });
                }

                // Nếu gói tập thuộc CƠ SỞ KHÁC -> TUYỆT ĐỐI TỪ CHỐI CHECK-IN TẠI ĐÂY
                if (requestedMembership.GymId != gymId)
                {
                    string otherGymName = requestedMembership.Gym?.Name ?? "cơ sở khác";
                    string pkgName = requestedMembership.Package?.Name ?? "Gói tập";

                    // Đếm xem hội viên có gói nào còn hạn tại cơ sở hiện tại không để hướng dẫn rõ ràng
                    var currentGymActiveCount = await _context.MemberMemberships
                        .CountAsync(m => m.GymId == gymId && m.MemberId == member.Id && m.EndDate >= today);

                    string advice = currentGymActiveCount > 0
                        ? $"\n\n💡 Hội viên có {currentGymActiveCount} gói tập còn hạn tại \"{gym.Name}\". Vui lòng yêu cầu hội viên chọn đúng mã QR của \"{gym.Name}\" hoặc xuất trình \"Mã QR Đa Năng\" để điểm danh."
                        : $"\n\n💡 Hội viên chưa đăng ký gói tập nào tại cơ sở \"{gym.Name}\".";

                    return Json(new CheckinVerifyResultViewModel
                    {
                        Success           = false,
                        CanCheckin        = false,
                        IsWrongGym        = true,
                        WrongGymName      = otherGymName,
                        TargetPackageName = pkgName,
                        MemberId          = member.Id,
                        FullName          = member.FullName ?? member.UserName ?? "Hội viên",
                        Email             = member.Email ?? "—",
                        PhoneNumber       = member.PhoneNumber ?? "—",
                        GymId             = gym.Id,
                        GymName           = gym.Name,
                        VipTierName       = vipName,
                        VipBadgeColor     = vipColor,
                        Message           = $"❌ SAI CƠ SỞ: Mã QR này là của gói \"{pkgName}\" tại {otherGymName}!\n\nKhông thể check-in tại {gym.Name}.{advice}"
                    });
                }

                // Nếu là gói của cơ sở này nhưng đã hết hạn
                if (requestedMembership.EndDate < today)
                {
                    return Json(new CheckinVerifyResultViewModel
                    {
                        Success           = false,
                        CanCheckin        = false,
                        IsExpired         = true,
                        TargetPackageName = requestedMembership.Package?.Name,
                        MemberId          = member.Id,
                        FullName          = member.FullName ?? member.UserName ?? "Hội viên",
                        Email             = member.Email ?? "—",
                        PhoneNumber       = member.PhoneNumber ?? "—",
                        GymId             = gym.Id,
                        GymName           = gym.Name,
                        VipTierName       = vipName,
                        VipBadgeColor     = vipColor,
                        Message           = $"❌ GÓI ĐÃ HẾT HẠN: Gói tập \"{requestedMembership.Package?.Name}\" trên mã QR đã hết hạn vào ngày {requestedMembership.EndDate:dd/MM/yyyy}. Vui lòng gia hạn hoặc chọn gói khác còn hạn."
                    });
                }
            }

            // 6. Lấy danh sách các gói tập còn hạn của hội viên tại cơ sở này
            var activeMemberships = await _context.MemberMemberships
                .Include(m => m.Package)
                .Where(m => m.GymId == gymId && m.MemberId == member.Id && m.EndDate >= today)
                .OrderBy(m => m.EndDate) // Ưu tiên gói sắp hết hạn trước (FEFO: First Expire, First Out)
                .ToListAsync();

            if (!activeMemberships.Any())
            {
                // Kiểm tra xem có gói ở các cơ sở khác không
                var otherGymMemberships = await _context.MemberMemberships
                    .Include(m => m.Gym)
                    .Include(m => m.Package)
                    .Where(m => m.MemberId == member.Id && m.GymId != gymId && m.EndDate >= today)
                    .ToListAsync();

                // Kiểm tra xem có gói nào vừa hết hạn gần đây tại gym này không
                var lastExpired = await _context.MemberMemberships
                    .Include(m => m.Package)
                    .Where(m => m.GymId == gymId && m.MemberId == member.Id)
                    .OrderByDescending(m => m.EndDate)
                    .FirstOrDefaultAsync();

                string reasonMsg;
                if (otherGymMemberships.Any())
                {
                    var gymNames = string.Join(", ", otherGymMemberships.Select(m => m.Gym?.Name).Distinct());
                    reasonMsg = $"Hội viên không có gói tập nào còn hạn tại cơ sở {gym.Name}. (Hội viên hiện đang có gói tại: {gymNames}).";
                }
                else if (lastExpired != null)
                {
                    reasonMsg = $"Gói tập gần nhất \"{lastExpired.Package?.Name}\" tại cơ sở này đã hết hạn vào ngày {lastExpired.EndDate:dd/MM/yyyy}.";
                }
                else
                {
                    reasonMsg = $"Hội viên chưa từng đăng ký gói tập nào tại phòng gym {gym.Name}.";
                }

                return Json(new CheckinVerifyResultViewModel
                {
                    Success             = true,
                    CanCheckin          = false,
                    HasActiveMembership = false,
                    IsExpired           = true,
                    MemberId            = member.Id,
                    FullName            = member.FullName ?? member.UserName ?? "Hội viên",
                    Email               = member.Email ?? "—",
                    PhoneNumber         = member.PhoneNumber ?? "—",
                    GymId               = gym.Id,
                    GymName             = gym.Name,
                    VipTierName         = vipName,
                    VipBadgeColor       = vipColor,
                    Message             = $"❌ {reasonMsg} Vui lòng mua hoặc gia hạn gói trước khi vào tập."
                });
            }

            // Chọn gói áp dụng:
            // - Nếu quét QR đích danh gói hợp lệ: chọn đúng gói đó
            // - Nếu quét QR Đa Năng hoặc tra cứu SĐT: ưu tiên gói sắp hết hạn trước (FEFO)
            var selectedMembership = (requestedMembership != null && activeMemberships.Any(m => m.Id == requestedMembership.Id))
                ? activeMemberships.First(m => m.Id == requestedMembership.Id)
                : activeMemberships.First();

            var availableOptions = activeMemberships.Select(m => new AvailableMembershipOptionItem
            {
                MembershipId  = m.Id,
                PackageName   = m.Package?.Name ?? "Vé vào tập",
                PackageType   = m.Package?.PackageType == "Daily" ? "Vé ngày" : "Gói định kỳ",
                StartDate     = m.StartDate,
                EndDate       = m.EndDate,
                DaysRemaining = Math.Max(0, (int)(m.EndDate.Date - today).TotalDays),
                IsSelected    = m.Id == selectedMembership.Id
            }).ToList();

            // 7. Kiểm tra xem hội viên có vừa mới check-in trong vòng 45 phút qua không (tránh check-in trùng lặp)
            var recentCheckin = await _context.CheckinLogs
                .Where(c => c.GymId == gymId && c.MemberId == member.Id && c.CheckinTime >= VnTime.Now.AddMinutes(-45) && c.Status == "Success" && c.CheckoutTime == null)
                .OrderByDescending(c => c.CheckinTime)
                .FirstOrDefaultAsync();

            bool alreadyIn = recentCheckin != null;
            int? minutesAgo = alreadyIn ? (int?)(VnTime.Now - recentCheckin!.CheckinTime).TotalMinutes : null;

            int daysRemaining = Math.Max(0, (int)(selectedMembership.EndDate.Date - today).TotalDays);

            string successMsg = alreadyIn
                ? $"Hội viên đã check-in cách đây {minutesAgo} phút và đang trong phòng tập."
                : (activeMemberships.Count > 1 
                    ? $"Hội viên có {activeMemberships.Count} gói tập hợp lệ tại cơ sở này. Đã chọn gói: \"{selectedMembership.Package?.Name}\"." 
                    : "Hội viên hợp lệ — Sẵn sàng vào tập!");

            return Json(new CheckinVerifyResultViewModel
            {
                Success                   = true,
                CanCheckin                = true,
                HasActiveMembership       = true,
                MemberId                  = member.Id,
                FullName                  = member.FullName ?? member.UserName ?? "Hội viên",
                Email                     = member.Email ?? "—",
                PhoneNumber               = member.PhoneNumber ?? "—",
                GymId                     = gym.Id,
                GymName                   = gym.Name,
                MembershipId              = selectedMembership.Id,
                PackageName               = selectedMembership.Package?.Name ?? "Vé vào tập",
                PackageType               = selectedMembership.Package?.PackageType == "Daily" ? "Vé ngày (Daily Pass)" : "Gói định kỳ",
                StartDate                 = selectedMembership.StartDate,
                EndDate                   = selectedMembership.EndDate,
                DaysRemaining             = daysRemaining,
                VipTierName               = vipName,
                VipBadgeColor             = vipColor,
                AlreadyCheckedInRecently  = alreadyIn,
                LastCheckinMinutesAgo     = minutesAgo,
                AvailableMemberships      = availableOptions,
                Message                   = successMsg
            });
        }

        // ==================== AJAX XÁC NHẬN CHO HỘI VIÊN VÀO TẬP ====================
        // POST: /OwnerCheckin/ConfirmCheckin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmCheckin(int gymId, string memberId, int? membershipId, string method = "QrScan")
        {
            var userId = await GetCurrentUserIdAsync();
            var gym = await _context.Gyms.FirstOrDefaultAsync(g => g.Id == gymId && g.OwnerId == userId);
            if (gym == null)
            {
                return Json(new { success = false, message = "Phòng gym không hợp lệ." });
            }

            var member = await _userManager.FindByIdAsync(memberId);
            if (member == null)
            {
                return Json(new { success = false, message = "Hội viên không tồn tại." });
            }

            // BẢO VỆ CHẶT CHẼ: Kiểm tra gói tập được chọn có hợp lệ tại gymId này hay không
            if (membershipId.HasValue)
            {
                var targetMs = await _context.MemberMemberships
                    .Include(m => m.Package)
                    .Include(m => m.Gym)
                    .FirstOrDefaultAsync(m => m.Id == membershipId.Value && m.MemberId == memberId);

                if (targetMs == null)
                {
                    return Json(new { success = false, message = "Gói tập không tồn tại hoặc không thuộc hội viên này." });
                }

                if (targetMs.GymId != gymId)
                {
                    return Json(new { 
                        success = false, 
                        message = $"Gói tập \"{targetMs.Package?.Name}\" thuộc cơ sở \"{targetMs.Gym?.Name}\", không thể dùng để check-in tại \"{gym.Name}\"!" 
                    });
                }

                if (targetMs.EndDate < VnTime.Today)
                {
                    return Json(new { 
                        success = false, 
                        message = $"Gói tập \"{targetMs.Package?.Name}\" đã hết hạn vào ngày {targetMs.EndDate:dd/MM/yyyy}, không thể check-in." 
                    });
                }
            }
            else
            {
                var hasActiveAtThisGym = await _context.MemberMemberships
                    .AnyAsync(m => m.GymId == gymId && m.MemberId == memberId && m.EndDate >= VnTime.Today);

                if (!hasActiveAtThisGym)
                {
                    return Json(new { success = false, message = $"Hội viên không có gói tập nào còn hạn tại cơ sở {gym.Name}." });
                }
            }

            // Ghi nhận CheckinLog
            var log = new CheckinLog
            {
                MemberId        = memberId,
                GymId           = gymId,
                MembershipId    = membershipId,
                CheckinTime     = VnTime.Now,
                CheckedByUserId = userId,
                Status          = "Success",
                CheckinMethod   = method switch
                {
                    "ManualPhone" => "ManualPhone",
                    "UsbScanner"  => "UsbScanner",
                    _             => "QrScan"
                }
            };

            _context.CheckinLogs.Add(log);
            await _context.SaveChangesAsync();

            // Gửi thông báo đến chuông của Member
            string timeStr = VnTime.Now.ToString("HH:mm");
            await NotificationHelper.CreateAsync(
                _context,
                userId: memberId,
                title: "Check-in vào tập thành công 🏋️",
                message: $"Bạn đã check-in thành công tại {gym.Name} lúc {timeStr}. Chúc bạn có một buổi tập hiệu quả và tràn đầy năng lượng!",
                type: "Success",
                category: "Membership",
                linkUrl: "/Member/CheckinHistory"
            );

            // Tính lại số người đang tập & % đám đông (Crowd Meter)
            int maxCapacity = gym.MaxCapacity > 0 ? gym.MaxCapacity : 50;
            var twoHoursAgo = VnTime.Now.AddHours(-2);
            int activeCount = await _context.CheckinLogs
                .CountAsync(c => c.GymId == gymId && c.CheckinTime >= twoHoursAgo && c.CheckoutTime == null && c.Status == "Success");

            int crowdPercentage = Math.Min(100, (int)Math.Round((double)activeCount / maxCapacity * 100));
            var (statusText, statusColor, statusIcon, recommendation) = GetCrowdStatus(crowdPercentage);

            // Lấy hạng VIP
            var vip = await _context.MemberVipStatuses
                .Include(v => v.CurrentTier)
                .FirstOrDefaultAsync(v => v.GymId == gymId && v.MemberId == memberId);

            var activeItem = new LiveMemberInGymItem
            {
                CheckinId     = log.Id,
                MemberId      = member.Id,
                MemberName    = member.FullName ?? member.UserName ?? "Hội viên",
                MemberEmail   = member.Email ?? "—",
                MemberPhone   = member.PhoneNumber ?? "—",
                PackageName   = (await _context.MemberMemberships.Include(m => m.Package).FirstOrDefaultAsync(m => m.Id == membershipId))?.Package?.Name ?? "Vé vào tập",
                VipTierName   = vip?.CurrentTier?.TierName ?? "Standard",
                VipBadgeColor = vip?.CurrentTier?.BadgeColor ?? "#64748b",
                CheckinTime   = log.CheckinTime,
                MinutesAgo    = 0,
                CheckinMethod = log.CheckinMethod
            };

            return Json(new
            {
                success = true,
                message = $"Đã xác nhận check-in thành công cho hội viên {member.FullName}!",
                activeCount = activeCount,
                maxCapacity = maxCapacity,
                crowdPercentage = crowdPercentage,
                statusText = statusText,
                statusColor = statusColor,
                statusIcon = statusIcon,
                recommendation = recommendation,
                newMember = activeItem
            });
        }

        // ==================== AJAX CHECK-OUT SỚM ====================
        // POST: /OwnerCheckin/CheckoutEarly
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckoutEarly(int checkinId)
        {
            var userId = await GetCurrentUserIdAsync();
            var checkin = await _context.CheckinLogs
                .Include(c => c.Gym)
                .Include(c => c.Member)
                .FirstOrDefaultAsync(c => c.Id == checkinId && c.Gym.OwnerId == userId);

            if (checkin == null)
            {
                return Json(new { success = false, message = "Lượt check-in không tồn tại hoặc bạn không có quyền xử lý." });
            }

            checkin.CheckoutTime = VnTime.Now;
            await _context.SaveChangesAsync();

            // Tính lại số người đang tập
            int maxCapacity = checkin.Gym.MaxCapacity > 0 ? checkin.Gym.MaxCapacity : 50;
            var twoHoursAgo = VnTime.Now.AddHours(-2);
            int activeCount = await _context.CheckinLogs
                .CountAsync(c => c.GymId == checkin.GymId && c.CheckinTime >= twoHoursAgo && c.CheckoutTime == null && c.Status == "Success");

            int crowdPercentage = Math.Min(100, (int)Math.Round((double)activeCount / maxCapacity * 100));
            var (statusText, statusColor, statusIcon, recommendation) = GetCrowdStatus(crowdPercentage);

            return Json(new
            {
                success = true,
                message = $"Hội viên {checkin.Member.FullName} đã check-out ra về.",
                checkinId = checkinId,
                activeCount = activeCount,
                maxCapacity = maxCapacity,
                crowdPercentage = crowdPercentage,
                statusText = statusText,
                statusColor = statusColor,
                statusIcon = statusIcon,
                recommendation = recommendation
            });
        }

        // ==================== LỊCH SỬ ĐIỂM DANH CHECK-IN CỦA CƠ SỞ ====================
        // GET: /OwnerCheckin/History?gymId=...&date=...&search=...
        public async Task<IActionResult> History(int? gymId, DateTime? date, string? search)
        {
            var userId = await GetCurrentUserIdAsync();
            var myGyms = await _context.Gyms
                .Where(g => g.OwnerId == userId && g.Status == "Approved")
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();

            if (!myGyms.Any())
            {
                return RedirectToAction("Index", "OwnerGym");
            }

            var selectedGym = gymId.HasValue
                ? myGyms.FirstOrDefault(g => g.Id == gymId.Value) ?? myGyms.First()
                : myGyms.First();

            var filterDate = date ?? VnTime.Today;
            var startOfDay = filterDate.Date;
            var endOfDay = startOfDay.AddDays(1).AddTicks(-1);

            var query = _context.CheckinLogs
                .Include(c => c.Member)
                .Include(c => c.Membership).ThenInclude(m => m!.Package)
                .Include(c => c.CheckedByUser)
                .Where(c => c.GymId == selectedGym.Id && c.CheckinTime >= startOfDay && c.CheckinTime <= endOfDay)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var kw = search.Trim().ToLower();
                query = query.Where(c =>
                    c.Member.FullName.ToLower().Contains(kw) ||
                    (c.Member.Email != null && c.Member.Email.ToLower().Contains(kw)) ||
                    (c.Member.PhoneNumber != null && c.Member.PhoneNumber.Contains(kw)));
            }

            var checkinsList = await query.OrderByDescending(c => c.CheckinTime).ToListAsync();

            // Tính giờ cao điểm trong ngày
            string peakHourText = "—";
            if (checkinsList.Any())
            {
                var peakGroup = checkinsList
                    .GroupBy(c => c.CheckinTime.Hour)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault();

                if (peakGroup != null)
                {
                    peakHourText = $"{peakGroup.Key}:00 - {peakGroup.Key + 1}:00 ({peakGroup.Count()} lượt)";
                }
            }

            var vm = new CheckinHistoryViewModel
            {
                SelectedGymId       = selectedGym.Id,
                SelectedGymName     = selectedGym.Name,
                MyGyms              = myGyms,
                FilterDate          = filterDate,
                SearchQuery         = search,
                TotalCheckinsCount  = checkinsList.Count,
                UniqueMembersCount  = checkinsList.Select(c => c.MemberId).Distinct().Count(),
                PeakHourText        = peakHourText,
                Checkins = checkinsList.Select(c => new CheckinHistoryRowItem
                {
                    Id            = c.Id,
                    MemberId      = c.MemberId,
                    MemberName    = c.Member?.FullName ?? c.Member?.UserName ?? "Hội viên",
                    MemberEmail   = c.Member?.Email ?? "—",
                    MemberPhone   = c.Member?.PhoneNumber ?? "—",
                    GymName       = selectedGym.Name,
                    PackageName   = c.Membership?.Package?.Name ?? "Vé vào tập",
                    CheckinTime   = c.CheckinTime,
                    CheckoutTime  = c.CheckoutTime,
                    Status        = c.Status,
                    CheckinMethod = c.CheckinMethod,
                    StaffName     = c.CheckedByUser?.FullName ?? c.CheckedByUser?.UserName ?? "Hệ thống"
                }).ToList()
            };

            return View(vm);
        }

        // Helper tính toán nhãn và màu sắc Crowd Meter
        private static (string text, string color, string icon, string recommendation) GetCrowdStatus(int percentage)
        {
            if (percentage < 35)
            {
                return ("Đang vắng", "#10b981", "bi-emoji-smile", "Phòng tập đang vắng, máy tập thoáng đãng — Thời điểm lý tưởng để bạn tập luyện!");
            }
            if (percentage <= 70)
            {
                return ("Khá đông", "#f59e0b", "bi-people", "Phòng tập có lượng khách vừa phải, có thể cần chia sẻ máy tạ hoặc đổi bài linh hoạt.");
            }
            return ("Rất đông / Giờ cao điểm", "#ef4444", "bi-exclamation-octagon", "Phòng tập đang trong giờ cao điểm, đông đúc — Nên cân nhắc sắp xếp giờ tập khác.");
        }
    }
}
