namespace GymManagement.ViewModels
{
    /// <summary>
    /// ViewModel chi tiết của 1 vé hội viên (Member/MembershipDetails/{id}).
    /// </summary>
    public class MembershipDetailsViewModel
    {
        public int MembershipId { get; set; }

        // ── Gym ──
        public int GymId { get; set; }
        public string GymName { get; set; } = string.Empty;
        public string GymAddress { get; set; } = string.Empty;
        public string GymDescription { get; set; } = string.Empty;
        public string GymImage { get; set; } = string.Empty;

        // ── Package ──
        public int PackageId { get; set; }
        public string PackageName { get; set; } = string.Empty;
        public string PackageType { get; set; } = string.Empty;
        public int? DurationInMonths { get; set; }

        // ── Thời hạn ──
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime PurchaseDate { get; set; }
        public decimal PriceAtPurchase { get; set; }

        // ── Giao dịch & Hóa đơn liên quan ──
        public int? TransactionId { get; set; }
        public string? VnpTxnRef { get; set; }
        public int? InvoiceId { get; set; }
        public string? InvoiceCode { get; set; }

        // ── Computed ──
        public int DaysRemaining => (EndDate.Date - DateTime.Today).Days;

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
