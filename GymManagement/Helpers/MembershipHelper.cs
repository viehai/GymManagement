namespace GymManagement.Helpers
{
    /// <summary>
    /// Helper dùng chung cho logic thành viên hội viên.
    /// Không phải Service layer — chỉ là static class tránh trùng code.
    /// </summary>
    public static class MembershipHelper
    {
        /// <summary>
        /// Tính ngày hết hạn vé kể từ hôm nay.
        /// - Daily  → hết hạn cuối ngày hôm nay (23:59:59)
        /// - Monthly → hôm nay + N tháng
        /// </summary>
        public static DateTime CalculateEndDate(string packageType, int? durationInMonths)
        {
            var today = DateTime.Today;

            if (packageType == "Daily")
                return today.AddDays(1).AddSeconds(-1); // 23:59:59 hôm nay

            if (packageType == "Monthly" && durationInMonths.HasValue)
                return today.AddMonths(durationInMonths.Value);

            return today.AddDays(1); // fallback
        }

        /// <summary>
        /// Tính ngày hết hạn khi gia hạn vé (Renew):
        /// - Nếu vé hiện tại còn hạn (currentEndDate > Hôm nay) -> cộng dồn tiếp từ currentEndDate.
        /// - Nếu vé hiện tại đã hết hạn (currentEndDate <= Hôm nay) -> tính bắt đầu từ hôm nay.
        /// </summary>
        public static DateTime CalculateRenewEndDate(DateTime currentEndDate, string packageType, int? durationInMonths)
        {
            var baseDate = currentEndDate > DateTime.Today ? currentEndDate : DateTime.Today;

            if (packageType == "Daily")
                return baseDate.AddDays(1);

            if (packageType == "Monthly" && durationInMonths.HasValue)
                return baseDate.AddMonths(durationInMonths.Value);

            return baseDate.AddDays(1);
        }

        /// <summary>
        /// Sinh mã hóa đơn duy nhất dạng INV-YYYYMMDD-XXXXXX.
        /// Ví dụ: INV-20260819-A3F9K2
        /// </summary>
        public static string GenerateInvoiceCode()
        {
            var datePart = DateTime.Now.ToString("yyyyMMdd");
            var randomPart = Guid.NewGuid().ToString("N")[..6].ToUpper();
            return $"INV-{datePart}-{randomPart}";
        }
    }
}
