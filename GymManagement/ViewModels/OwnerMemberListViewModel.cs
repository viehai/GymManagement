namespace GymManagement.ViewModels
{
    /// <summary>
    /// ViewModel cho màn hình Danh sách hội viên của Owner (OwnerMember/Index).
    /// </summary>
    public class OwnerMemberListViewModel
    {
        public int? SelectedGymId { get; set; }
        public List<GymManagement.Models.Gym> MyGyms { get; set; } = new();
        public List<OwnerMemberItemViewModel> Members { get; set; } = new();

        // Thống kê nhanh
        public int TotalMembers => Members.Select(m => m.MemberId).Distinct().Count();
        public int ActiveMembersCount => Members.Count(m => m.DaysRemaining >= 0);
        public int ExpiredMembersCount => Members.Count(m => m.DaysRemaining < 0);
    }

    public class OwnerMemberItemViewModel
    {
        public int MembershipId { get; set; }
        public string MemberId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;

        public int GymId { get; set; }
        public string GymName { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;
        public string PackageTypeLabel { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal PriceAtPurchase { get; set; }
        public DateTime PurchaseDate { get; set; }

        public int DaysRemaining => (EndDate.Date - DateTime.Today).Days;

        public string Status =>
            DaysRemaining < 0 ? "Expired" :
            DaysRemaining <= 3 ? "ExpiringSoon" : "Active";

        public string StatusLabel => Status switch
        {
            "Expired"      => "Đã hết hạn",
            "ExpiringSoon" => $"Sắp hết hạn ({DaysRemaining} ngày)",
            _              => $"Đang tập (còn {DaysRemaining} ngày)"
        };

        public string StatusBadgeClass => Status switch
        {
            "Expired"      => "badge-rejected",
            "ExpiringSoon" => "badge-pending",
            _              => "badge-approved"
        };
    }
}
