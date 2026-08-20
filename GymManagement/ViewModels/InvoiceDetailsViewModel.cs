namespace GymManagement.ViewModels
{
    /// <summary>
    /// ViewModel cho trang chi tiết Hóa đơn (Member/InvoiceDetails/{id}).
    /// Hiển thị đầy đủ thông tin hóa đơn — dùng để in qua Ctrl+P của browser.
    /// </summary>
    public class InvoiceDetailsViewModel
    {
        // ── Hóa đơn ──
        public int InvoiceId { get; set; }
        public string InvoiceCode { get; set; } = string.Empty;
        public DateTime IssuedDate { get; set; }

        // ── Thành viên ──
        public string MemberName { get; set; } = string.Empty;
        public string MemberEmail { get; set; } = string.Empty;

        // ── Gym & Gói ──
        public string GymName { get; set; } = string.Empty;
        public string GymAddress { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;

        /// <summary>"Daily" hoặc "Monthly"</summary>
        public string PackageType { get; set; } = string.Empty;
        public int? DurationInMonths { get; set; }

        // ── Thanh toán ──
        public decimal Amount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // ── Computed ──
        public string TypeLabel =>
            PackageType == "Daily" ? "Vé ngày" : $"Gói {DurationInMonths} tháng";
    }
}
