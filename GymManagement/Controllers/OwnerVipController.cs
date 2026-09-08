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
    /// Quản lý cấu hình các hạng VIP và danh sách hội viên VIP của Chủ phòng Gym (OWN-26, OWN-27).
    /// </summary>
    [Authorize(Roles = "Owner")]
    public class OwnerVipController : Controller
    {
        private readonly GymDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OwnerVipController(GymDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<string> GetCurrentUserIdAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user?.Id ?? string.Empty;
        }

        // ==================== OWN-26: CẤU HÌNH HẠNG VIP ====================
        // GET /OwnerVip/Settings?gymId=...
        [HttpGet]
        public async Task<IActionResult> Settings(int? gymId)
        {
            var userId = await GetCurrentUserIdAsync();

            var myGyms = await _context.Gyms
                .Where(g => g.OwnerId == userId && g.Status == "Approved")
                .OrderBy(g => g.Name)
                .ToListAsync();

            if (!myGyms.Any())
            {
                TempData["Error"] = "Bạn chưa có cơ sở phòng Gym nào được duyệt để cấu hình VIP.";
                return RedirectToAction("Index", "OwnerGym");
            }

            var targetGym = gymId.HasValue
                ? myGyms.FirstOrDefault(g => g.Id == gymId.Value) ?? myGyms.First()
                : myGyms.First();

            var tiers = await _context.VipTierSettings
                .Where(t => t.GymId == targetGym.Id)
                .OrderBy(t => t.DisplayOrder)
                .ThenBy(t => t.MinPurchaseCount)
                .ToListAsync();

            var vm = new OwnerVipTierListViewModel
            {
                SelectedGymId = targetGym.Id,
                SelectedGym   = targetGym,
                MyGyms        = myGyms,
                Tiers         = tiers
            };

            return View(vm);
        }

        // POST /OwnerVip/InitDefaultTiers?gymId=...
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InitDefaultTiers(int gymId)
        {
            var userId = await GetCurrentUserIdAsync();
            var gym = await _context.Gyms.FirstOrDefaultAsync(g => g.Id == gymId && g.OwnerId == userId);
            if (gym == null) return Forbid();

            bool hasTiers = await _context.VipTierSettings.AnyAsync(t => t.GymId == gymId);
            if (hasTiers)
            {
                TempData["Info"] = "Cơ sở này đã có cấu hình hạng VIP.";
                return RedirectToAction("Settings", new { gymId });
            }

            var defaultTiers = new List<VipTierSetting>
            {
                new VipTierSetting
                {
                    GymId = gymId,
                    TierName = "Bạc (Silver)",
                    MinPurchaseCount = 3,
                    DiscountPercent = 5,
                    BenefitDescription = "Giảm 5% cho tất cả vé ngày & gói tháng. Ưu tiên giữ chỗ lớp tập nhóm.",
                    BadgeColor = "#9CA3AF",
                    DisplayOrder = 1,
                    IsActive = true,
                    CreatedAt = VnTime.Now
                },
                new VipTierSetting
                {
                    GymId = gymId,
                    TierName = "Vàng (Gold)",
                    MinPurchaseCount = 10,
                    DiscountPercent = 10,
                    BenefitDescription = "Giảm 10% cho mọi dịch vụ. Miễn phí tủ đồ cá nhân và nước uống thể thao.",
                    BadgeColor = "#F59E0B",
                    DisplayOrder = 2,
                    IsActive = true,
                    CreatedAt = VnTime.Now
                },
                new VipTierSetting
                {
                    GymId = gymId,
                    TierName = "Kim Cương (Platinum)",
                    MinPurchaseCount = 25,
                    DiscountPercent = 15,
                    BenefitDescription = "Giảm 15% trọn đời cơ sở. Tặng 2 buổi kèm HLV cá nhân (PT) và khăn tập VIP.",
                    BadgeColor = "#6366F1",
                    DisplayOrder = 3,
                    IsActive = true,
                    CreatedAt = VnTime.Now
                }
            };

            _context.VipTierSettings.AddRange(defaultTiers);
            await _context.SaveChangesAsync();

            var user = await _userManager.GetUserAsync(User);
            _context.SystemLogs.Add(new SystemLog
            {
                UserId = userId,
                Action = "VipTiersInitialized",
                Entity = "VipTierSetting",
                EntityId = gymId.ToString(),
                Level = "Info",
                Description = $"Chủ phòng {user?.FullName} đã khởi tạo bộ hạng VIP tiêu chuẩn (Bạc, Vàng, Kim Cương) cho phòng Gym \"{gym.Name}\".",
                CreatedAt = VnTime.Now
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã khởi tạo thành công 3 hạng VIP tiêu chuẩn cho phòng Gym.";
            return RedirectToAction("Settings", new { gymId });
        }

        // POST /OwnerVip/SaveTier
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveTier(VipTierSettingFormViewModel model)
        {
            var userId = await GetCurrentUserIdAsync();
            var gym = await _context.Gyms.FirstOrDefaultAsync(g => g.Id == model.GymId && g.OwnerId == userId);
            if (gym == null) return Forbid();

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Dữ liệu nhập vào chưa hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction("Settings", new { gymId = model.GymId });
            }

            var user = await _userManager.GetUserAsync(User);

            if (model.Id == 0)
            {
                // Thêm mới
                var newTier = new VipTierSetting
                {
                    GymId              = model.GymId,
                    TierName           = model.TierName.Trim(),
                    MinPurchaseCount   = model.MinPurchaseCount,
                    DiscountPercent    = model.DiscountPercent,
                    BenefitDescription = model.BenefitDescription?.Trim(),
                    BadgeColor         = string.IsNullOrWhiteSpace(model.BadgeColor) ? "#C0C0C0" : model.BadgeColor.Trim(),
                    DisplayOrder       = model.DisplayOrder,
                    IsActive           = model.IsActive,
                    CreatedAt          = VnTime.Now
                };

                _context.VipTierSettings.Add(newTier);
                await _context.SaveChangesAsync();

                _context.SystemLogs.Add(new SystemLog
                {
                    UserId = userId,
                    Action = "VipTierCreated",
                    Entity = "VipTierSetting",
                    EntityId = newTier.Id.ToString(),
                    Level = "Info",
                    Description = $"Chủ phòng {user?.FullName} đã tạo hạng VIP mới: \"{newTier.TierName}\" (Mốc {newTier.MinPurchaseCount} lần mua, giảm {newTier.DiscountPercent:0.#}%) tại \"{gym.Name}\".",
                    CreatedAt = VnTime.Now
                });
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Đã thêm hạng VIP \"{newTier.TierName}\" thành công.";
            }
            else
            {
                // Cập nhật
                var tier = await _context.VipTierSettings.FirstOrDefaultAsync(t => t.Id == model.Id && t.GymId == model.GymId);
                if (tier == null) return NotFound();

                tier.TierName           = model.TierName.Trim();
                tier.MinPurchaseCount   = model.MinPurchaseCount;
                tier.DiscountPercent    = model.DiscountPercent;
                tier.BenefitDescription = model.BenefitDescription?.Trim();
                tier.BadgeColor         = string.IsNullOrWhiteSpace(model.BadgeColor) ? "#C0C0C0" : model.BadgeColor.Trim();
                tier.DisplayOrder       = model.DisplayOrder;
                tier.IsActive           = model.IsActive;

                _context.SystemLogs.Add(new SystemLog
                {
                    UserId = userId,
                    Action = "VipTierUpdated",
                    Entity = "VipTierSetting",
                    EntityId = tier.Id.ToString(),
                    Level = "Info",
                    Description = $"Chủ phòng {user?.FullName} đã cập nhật hạng VIP \"{tier.TierName}\" tại \"{gym.Name}\".",
                    CreatedAt = VnTime.Now
                });
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Đã cập nhật hạng VIP \"{tier.TierName}\" thành công.";
            }

            return RedirectToAction("Settings", new { gymId = model.GymId });
        }

        // POST /OwnerVip/ToggleTierStatus/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTierStatus(int id)
        {
            var userId = await GetCurrentUserIdAsync();
            var tier = await _context.VipTierSettings
                .Include(t => t.Gym)
                .FirstOrDefaultAsync(t => t.Id == id && t.Gym.OwnerId == userId);

            if (tier == null) return NotFound();

            tier.IsActive = !tier.IsActive;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã {(tier.IsActive ? "kích hoạt" : "tạm dừng")} hạng VIP \"{tier.TierName}\".";
            return RedirectToAction("Settings", new { gymId = tier.GymId });
        }

        // POST /OwnerVip/DeleteTier/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTier(int id)
        {
            var userId = await GetCurrentUserIdAsync();
            var tier = await _context.VipTierSettings
                .Include(t => t.Gym)
                .FirstOrDefaultAsync(t => t.Id == id && t.Gym.OwnerId == userId);

            if (tier == null) return NotFound();

            int gymId = tier.GymId;
            string tierName = tier.TierName;

            // Xóa liên kết của các Member đang ở tier này về null trước khi xóa
            var affectedMembers = await _context.MemberVipStatuses
                .Where(s => s.CurrentTierId == id)
                .ToListAsync();

            foreach (var m in affectedMembers)
            {
                m.CurrentTierId = null;
            }

            _context.VipTierSettings.Remove(tier);
            await _context.SaveChangesAsync();

            var user = await _userManager.GetUserAsync(User);
            _context.SystemLogs.Add(new SystemLog
            {
                UserId = userId,
                Action = "VipTierDeleted",
                Entity = "VipTierSetting",
                EntityId = id.ToString(),
                Level = "Warning",
                Description = $"Chủ phòng {user?.FullName} đã xóa hạng VIP \"{tierName}\" khỏi phòng Gym \"{tier.Gym?.Name}\".",
                CreatedAt = VnTime.Now
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã xóa hạng VIP \"{tierName}\" thành công.";
            return RedirectToAction("Settings", new { gymId });
        }

        // ==================== OWN-27: DANH SÁCH HỘI VIÊN VIP ====================
        // GET /OwnerVip/Members?gymId=...&tierId=...
        [HttpGet]
        public async Task<IActionResult> Members(int? gymId, int? tierId)
        {
            var userId = await GetCurrentUserIdAsync();

            var myGyms = await _context.Gyms
                .Where(g => g.OwnerId == userId && g.Status == "Approved")
                .OrderBy(g => g.Name)
                .ToListAsync();

            var myGymIds = myGyms.Select(g => g.Id).ToList();

            // Lọc theo gym cụ thể nếu có
            var targetGymIds = (gymId.HasValue && gymId.Value > 0)
                ? myGymIds.Where(id => id == gymId.Value).ToList()
                : myGymIds;

            // Lấy tất cả VipTierSetting của các gym này
            var allTiersQuery = _context.VipTierSettings
                .Where(t => myGymIds.Contains(t.GymId) && t.IsActive);
            if (gymId.HasValue && gymId.Value > 0)
                allTiersQuery = allTiersQuery.Where(t => t.GymId == gymId.Value);

            var allTiers = await allTiersQuery
                .OrderBy(t => t.GymId)
                .ThenByDescending(t => t.MinPurchaseCount)
                .ToListAsync();

            var availableTiers = allTiers.OrderBy(t => t.DisplayOrder).ToList();

            if (!allTiers.Any())
            {
                return View(new OwnerVipMemberListViewModel
                {
                    SelectedGymId  = gymId,
                    SelectedTierId = tierId,
                    MyGyms         = myGyms,
                    AvailableTiers = availableTiers,
                    VipMembers     = new()
                });
            }

            // Tính MinPurchaseCount tối thiểu để có VIP (hạng thấp nhất)
            int minRequired = allTiers.Min(t => t.MinPurchaseCount);

            // Lấy tất cả hội viên đã mua đủ số lần
            var memberPurchaseCounts = await _context.MemberMemberships
                .Where(m => targetGymIds.Contains(m.GymId))
                .GroupBy(m => new { m.GymId, m.MemberId })
                .Select(g => new { g.Key.GymId, g.Key.MemberId, Count = g.Count() })
                .Where(g => g.Count >= minRequired)
                .ToListAsync();

            if (!memberPurchaseCounts.Any())
            {
                return View(new OwnerVipMemberListViewModel
                {
                    SelectedGymId  = gymId,
                    SelectedTierId = tierId,
                    MyGyms         = myGyms,
                    AvailableTiers = availableTiers,
                    VipMembers     = new()
                });
            }

            // Lấy thông tin hội viên
            var memberIds = memberPurchaseCounts.Select(m => m.MemberId).Distinct().ToList();
            var members = await _context.Users
                .Where(u => memberIds.Contains(u.Id))
                .ToListAsync();

            // Lấy VipStatus để lấy AchievedAt / LastPurchaseAt
            var vipStatuses = await _context.MemberVipStatuses
                .Where(v => targetGymIds.Contains(v.GymId) && memberIds.Contains(v.MemberId))
                .ToListAsync();

            var gymMap = myGyms.ToDictionary(g => g.Id, g => g);
            var vmItems = new List<OwnerVipMemberItemViewModel>();

            foreach (var pc in memberPurchaseCounts)
            {
                // Tìm tier cao nhất phù hợp với số lần mua
                var gymTiers = allTiers
                    .Where(t => t.GymId == pc.GymId && t.MinPurchaseCount <= pc.Count)
                    .OrderByDescending(t => t.MinPurchaseCount)
                    .ToList();

                var currentTier = gymTiers.FirstOrDefault();
                if (currentTier == null) continue;

                // Lọc theo tierId nếu có
                if (tierId.HasValue && tierId.Value > 0 && currentTier.Id != tierId.Value) continue;

                var member = members.FirstOrDefault(m => m.Id == pc.MemberId);
                var vipStatus = vipStatuses.FirstOrDefault(v => v.GymId == pc.GymId && v.MemberId == pc.MemberId);
                var gym = gymMap.GetValueOrDefault(pc.GymId);

                vmItems.Add(new OwnerVipMemberItemViewModel
                {
                    MemberId           = pc.MemberId,
                    FullName           = member?.FullName ?? member?.UserName ?? "—",
                    Email              = member?.Email ?? "—",
                    PhoneNumber        = member?.PhoneNumber ?? "—",
                    GymId              = pc.GymId,
                    GymName            = gym?.Name ?? "—",
                    TierId             = currentTier.Id,
                    TierName           = currentTier.TierName,
                    BadgeColor         = currentTier.BadgeColor ?? "#C0C0C0",
                    DiscountPercent    = currentTier.DiscountPercent,
                    BenefitDescription = currentTier.BenefitDescription,
                    TotalPurchaseCount = pc.Count,
                    AchievedAt         = vipStatus?.AchievedAt,
                    LastPurchaseAt     = vipStatus?.LastPurchaseAt
                });
            }

            // Sắp xếp: tier cao nhất trước, nhiều lần mua trước
            vmItems = vmItems
                .OrderByDescending(v => allTiers.FirstOrDefault(t => t.Id == v.TierId)?.MinPurchaseCount ?? 0)
                .ThenByDescending(v => v.TotalPurchaseCount)
                .ToList();

            var vm = new OwnerVipMemberListViewModel
            {
                SelectedGymId  = gymId,
                SelectedTierId = tierId,
                MyGyms         = myGyms,
                AvailableTiers = availableTiers,
                VipMembers     = vmItems
            };

            return View(vm);
        }
    }
}
