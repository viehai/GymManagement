namespace GymManagement.ViewModels
{
    /// <summary>
    /// ViewModel cho từng gói vé trong danh sách vé của hội viên (Member/MyMemberships).
    /// </summary>
    public class MyMembershipViewModel
    {
        public int MembershipId { get; set; }
        public int GymId { get; set; }
        public string GymName { get; set; } = string.Empty;
        public string GymAddress { get; set; } = string.Empty;
        public string GymImage { get; set; } = string.Empty;

        public int PackageId { get; set; }
        public string PackageName { get; set; } = string.Empty;
        public string PackageType { get; set; } = string.Empty; // "Daily" hoặc "Monthly"
        public int? DurationInMonths { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime PurchaseDate { get; set; }
        public decimal PriceAtPurchase { get; set; }

        // ── Computed Properties ──
        public int DaysRemaining => (EndDate.Date - DateTime.Today).Days;

        /// <summary>
        /// Trạng thái: "Active" (Còn hạn), "ExpiringSoon" (Còn <= 3 ngày), "Expired" (Hết hạn)
        /// </summary>
        public string Status =>
            DaysRemaining < 0 ? "Expired" :
            DaysRemaining <= 3 ? "ExpiringSoon" : "Active";

        public string StatusLabel => Status switch
        {
            "Expired"      => "Đã hết hạn",
            "ExpiringSoon" => $"Sắp hết hạn (còn {DaysRemaining} ngày)",
            _              => $"Đang hoạt động (còn {DaysRemaining} ngày)"
        };

        public string StatusBadgeClass => Status switch
        {
            "Expired"      => "status-expired",
            "ExpiringSoon" => "status-expiring-soon",
            _              => "status-active"
        };

        public string PackageTypeLabel =>
            PackageType == "Daily" ? "Vé ngày" : $"Gói {DurationInMonths} tháng";
    }
}
