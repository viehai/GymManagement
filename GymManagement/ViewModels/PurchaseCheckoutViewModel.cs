namespace GymManagement.ViewModels
{
    /// <summary>
    /// ViewModel cho trang Checkout — tóm tắt đơn hàng trước khi thanh toán.
    /// Truyền qua TempData (serialize JSON) giữa các bước của luồng Purchase.
    /// </summary>
    public class PurchaseCheckoutViewModel
    {
        // ── Gym ──
        public int GymId { get; set; }
        public string GymName { get; set; } = string.Empty;
        public string GymAddress { get; set; } = string.Empty;
        public string GymImage { get; set; } = string.Empty;

        // ── Package ──
        public int PackageId { get; set; }
        public string PackageName { get; set; } = string.Empty;

        /// <summary>"Daily" hoặc "Monthly"</summary>
        public string PackageType { get; set; } = string.Empty;
        public int? DurationInMonths { get; set; }
        public decimal Price { get; set; }

        // ── Computed ──
        /// <summary>Ngày bắt đầu hiệu lực (hôm nay).</summary>
        public DateTime StartDate => DateTime.Today;

        /// <summary>Nhãn loại: "Vé ngày" / "Gói X tháng"</summary>
        public string TypeLabel =>
            PackageType == "Daily" ? "Vé ngày" : $"Gói {DurationInMonths} tháng";
    }
}
