namespace GymManagement.ViewModels
{
    /// <summary>
    /// ViewModel cho một dòng trong bảng Lịch sử giao dịch (Member/TransactionHistory).
    /// </summary>
    public class TransactionHistoryViewModel
    {
        public int TransactionId { get; set; }
        public string GymName { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;

        /// <summary>"Vé ngày" hoặc "Gói X tháng"</summary>
        public string PackageTypeLabel { get; set; } = string.Empty;
        public decimal Amount { get; set; }

        /// <summary>Pending / Success / Failed</summary>
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        /// <summary>Null nếu giao dịch chưa thành công (chưa tạo Invoice).</summary>
        public int? InvoiceId { get; set; }

        // ── Computed ──
        public string StatusBadgeClass => Status switch
        {
            "Success" => "badge-success",
            "Failed"  => "badge-failed",
            _         => "badge-pending"
        };

        public string StatusLabel => Status switch
        {
            "Success" => "Thành công",
            "Failed"  => "Thất bại",
            _         => "Đang xử lý"
        };
    }
}
