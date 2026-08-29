namespace GymManagement.Helpers;

/// <summary>
/// Cung cấp thời gian theo múi giờ Việt Nam (UTC+7).
/// Dùng thay thế cho DateTime.Now hoặc DateTime.UtcNow trên môi trường Azure/UTC server.
/// </summary>
public static class VnTime
{
    private static readonly TimeZoneInfo _tz =
        TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time") // Windows
        ?? TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");  // Linux fallback

    /// <summary>Trả về DateTime hiện tại theo giờ Việt Nam (UTC+7).</summary>
    public static DateTime Now =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _tz);

    /// <summary>Trả về ngày hiện tại (không có phần giờ) theo giờ Việt Nam.</summary>
    public static DateTime Today => Now.Date;
}
