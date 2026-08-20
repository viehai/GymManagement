namespace GymManagement.ViewModels
{
    /// <summary>
    /// ViewModel cho màn hình Gia hạn vé (Purchase/Renew/{membershipId}).
    /// </summary>
    public class RenewMembershipViewModel
    {
        public int MembershipId { get; set; }

        // ── Gym ──
        public int GymId { get; set; }
        public string GymName { get; set; } = string.Empty;
        public string GymAddress { get; set; } = string.Empty;
        public string GymImage { get; set; } = string.Empty;

        // ── Thông tin vé hiện tại ──
        public string CurrentPackageName { get; set; } = string.Empty;
        public DateTime CurrentEndDate { get; set; }
        public bool IsCurrentActive => CurrentEndDate >= DateTime.Today;

        // ── Danh sách các gói tập có thể chọn để gia hạn ──
        public List<PackageOptionViewModel> AvailablePackages { get; set; } = new();

        // ── Gói được chọn ──
        public int SelectedPackageId { get; set; }
    }

    public class PackageOptionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PackageType { get; set; } = string.Empty;
        public int? DurationInMonths { get; set; }
        public decimal Price { get; set; }
        public DateTime CalculatedNewEndDate { get; set; }

        public string TypeLabel =>
            PackageType == "Daily" ? "Vé ngày (+1 ngày)" : $"Gói {DurationInMonths} tháng (+{DurationInMonths} tháng)";
    }
}
