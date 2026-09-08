using System.ComponentModel.DataAnnotations;
using GymManagement.Models;

namespace GymManagement.ViewModels
{
    // ═══════════════════════════════════════════════
    // VIEWMODELS CHO CHỦ PHÒNG (OWNER VIP)
    // ═══════════════════════════════════════════════

    /// <summary>
    /// ViewModel cấu hình hạng VIP (Tạo mới / Cập nhật).
    /// </summary>
    public class VipTierSettingFormViewModel
    {
        public int Id { get; set; }

        [Required]
        public int GymId { get; set; }
        public string GymName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên hạng VIP (VD: Bạc, Vàng, Kim Cương).")]
        [StringLength(50, ErrorMessage = "Tên hạng VIP không quá 50 ký tự.")]
        [Display(Name = "Tên hạng VIP")]
        public string TierName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số lần mua vé tối thiểu để đạt hạng.")]
        [Range(1, 10000, ErrorMessage = "Số lần mua tối thiểu phải từ 1 trở lên.")]
        [Display(Name = "Số lần mua tích lũy")]
        public int MinPurchaseCount { get; set; } = 5;

        [Range(0, 100, ErrorMessage = "% giảm giá từ 0% đến 100%.")]
        [Display(Name = "% Giảm giá khi mua vé")]
        public decimal? DiscountPercent { get; set; }

        [StringLength(500, ErrorMessage = "Mô tả quyền lợi không quá 500 ký tự.")]
        [Display(Name = "Quyền lợi & Đặc quyền")]
        public string? BenefitDescription { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn màu hiển thị cho huy hiệu VIP.")]
        [StringLength(20)]
        [Display(Name = "Màu sắc huy hiệu")]
        public string BadgeColor { get; set; } = "#C0C0C0";

        [Required]
        [Range(1, 100)]
        [Display(Name = "Thứ tự cấp bậc")]
        public int DisplayOrder { get; set; } = 1;

        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// ViewModel danh sách các hạng VIP của phòng Gym.
    /// </summary>
    public class OwnerVipTierListViewModel
    {
        public int SelectedGymId { get; set; }
        public Gym? SelectedGym { get; set; }
        public List<Gym> MyGyms { get; set; } = new();
        public List<VipTierSetting> Tiers { get; set; } = new();
    }

    /// <summary>
    /// ViewModel danh sách hội viên đạt VIP của phòng Gym (OWN-27).
    /// </summary>
    public class OwnerVipMemberListViewModel
    {
        public int? SelectedGymId { get; set; }
        public int? SelectedTierId { get; set; }
        public List<Gym> MyGyms { get; set; } = new();
        public List<VipTierSetting> AvailableTiers { get; set; } = new();
        public List<OwnerVipMemberItemViewModel> VipMembers { get; set; } = new();

        public int TotalVipMembers => VipMembers.Count;
    }

    public class OwnerVipMemberItemViewModel
    {
        public string MemberId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;

        public int GymId { get; set; }
        public string GymName { get; set; } = string.Empty;

        public int TierId { get; set; }
        public string TierName { get; set; } = string.Empty;
        public string BadgeColor { get; set; } = "#C0C0C0";
        public decimal? DiscountPercent { get; set; }
        public string? BenefitDescription { get; set; }

        public int TotalPurchaseCount { get; set; }
        public DateTime? AchievedAt { get; set; }
        public DateTime? LastPurchaseAt { get; set; }
    }

    // ═══════════════════════════════════════════════
    // VIEWMODELS CHO HỘI VIÊN (MEMBER VIP)
    // ═══════════════════════════════════════════════

    /// <summary>
    /// Màn hình theo dõi tiến trình VIP của Member tại tất cả Gym đã tham gia (MEM-18).
    /// </summary>
    public class MyVipOverviewViewModel
    {
        public List<MemberGymVipProgressViewModel> GymVipList { get; set; } = new();
    }

    public class MemberGymVipProgressViewModel
    {
        public int GymId { get; set; }
        public string GymName { get; set; } = string.Empty;
        public string GymAddress { get; set; } = string.Empty;
        public string GymImage { get; set; } = string.Empty;

        // Hạng hiện tại
        public bool HasVipTier => CurrentTierId.HasValue;
        public int? CurrentTierId { get; set; }
        public string CurrentTierName { get; set; } = "Chưa có hạng";
        public string CurrentTierColor { get; set; } = "#9CA3AF";
        public decimal? CurrentDiscountPercent { get; set; }
        public string? CurrentBenefitDescription { get; set; }

        public int TotalPurchases { get; set; }
        public DateTime? AchievedAt { get; set; }

        // Hạng kế tiếp
        public bool HasNextTier => !string.IsNullOrEmpty(NextTierName);
        public string? NextTierName { get; set; }
        public string? NextTierColor { get; set; }
        public int? NextTierMinPurchases { get; set; }
        public decimal? NextTierDiscountPercent { get; set; }
        public string? NextTierBenefit { get; set; }

        // Số lần mua cần thêm
        public int PurchasesNeededForNextTier =>
            HasNextTier && NextTierMinPurchases.HasValue
                ? Math.Max(0, NextTierMinPurchases.Value - TotalPurchases)
                : 0;

        // % Tiến trình đến hạng kế tiếp (0 - 100%)
        public int ProgressPercent
        {
            get
            {
                if (!HasNextTier || !NextTierMinPurchases.HasValue || NextTierMinPurchases.Value <= 0)
                    return 100;

                int basePurchases = 0; // mốc của tier hiện tại
                int targetPurchases = NextTierMinPurchases.Value;

                int diff = targetPurchases - basePurchases;
                if (diff <= 0) return 100;

                int currentProgress = Math.Max(0, TotalPurchases - basePurchases);
                int pct = (int)Math.Round((double)currentProgress / diff * 100.0);
                return Math.Clamp(pct, 0, 100);
            }
        }
    }

    /// <summary>
    /// Bảng đặc quyền VIP công khai tại một phòng Gym (MEM-19).
    /// </summary>
    public class GymVipBenefitsViewModel
    {
        public int GymId { get; set; }
        public string GymName { get; set; } = string.Empty;
        public string GymAddress { get; set; } = string.Empty;
        public string GymImage { get; set; } = string.Empty;

        public List<VipTierSetting> Tiers { get; set; } = new();
    }
}
