namespace GymManagement.ViewModels
{
    /// <summary>
    /// ViewModel cho màn hình Chi tiết 1 hội viên tại phòng gym của Owner (OwnerMember/Details).
    /// </summary>
    public class OwnerMemberDetailsViewModel
    {
        // ── Thông tin hội viên ──
        public string MemberId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;

        // ── Phòng Gym ──
        public int GymId { get; set; }
        public string GymName { get; set; } = string.Empty;
        public string GymAddress { get; set; } = string.Empty;

        // ── Tổng kết của hội viên tại Gym này ──
        public decimal TotalSpent { get; set; }
        public int TotalPurchases { get; set; }

        // ── Lịch sử mua gói / vé tại Gym này ──
        public List<OwnerMemberPurchaseHistoryItem> PurchaseHistory { get; set; } = new();
    }

    public class OwnerMemberPurchaseHistoryItem
    {
        public int MembershipId { get; set; }
        public string PackageName { get; set; } = string.Empty;
        public string PackageTypeLabel { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime PurchaseDate { get; set; }
        public decimal PriceAtPurchase { get; set; }
        public string? InvoiceCode { get; set; }

        public bool IsCurrentlyActive => EndDate >= DateTime.Today;
    }
}
