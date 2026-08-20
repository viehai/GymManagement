namespace GymManagement.ViewModels
{
    /// <summary>
    /// ViewModel cho màn hình Danh sách giao dịch của Owner (OwnerTransaction/Index).
    /// </summary>
    public class OwnerTransactionListViewModel
    {
        public int? SelectedGymId { get; set; }
        public string? SelectedStatus { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public List<GymManagement.Models.Gym> MyGyms { get; set; } = new();
        public List<OwnerTransactionItemViewModel> Transactions { get; set; } = new();

        // Thống kê nhanh
        public decimal TotalAmount => Transactions.Where(t => t.Status == "Success").Sum(t => t.Amount);
        public int SuccessCount => Transactions.Count(t => t.Status == "Success");
        public int PendingCount => Transactions.Count(t => t.Status == "Pending");
        public int FailedCount => Transactions.Count(t => t.Status == "Failed");
    }

    public class OwnerTransactionItemViewModel
    {
        public int TransactionId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string MemberEmail { get; set; } = string.Empty;
        public string GymName { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string? VnpTxnRef { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? InvoiceCode { get; set; }

        public string StatusBadgeClass => Status switch
        {
            "Success" => "badge-approved",
            "Failed"  => "badge-rejected",
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
