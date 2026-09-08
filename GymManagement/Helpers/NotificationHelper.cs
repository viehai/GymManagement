using GymManagement.Hubs;
using GymManagement.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Helpers
{
    public static class NotificationHelper
    {
        /// <summary>
        /// Tạo mới một thông báo, lưu vào Database, và đẩy real-time qua SignalR.
        /// hubContext là tùy chọn – nếu null thì chỉ lưu DB (backward-compatible).
        /// </summary>
        public static async Task<Notification> CreateAsync(
            GymDbContext context,
            string userId,
            string title,
            string message,
            string type,
            string category,
            string? linkUrl = null,
            IHubContext<NotificationHub>? hubContext = null)
        {
            var notification = new Notification
            {
                UserId   = userId,
                Title    = title,
                Message  = message,
                Type     = type,
                Category = category,
                LinkUrl  = linkUrl,
                IsRead   = false,
                CreatedAt = VnTime.Now
            };

            context.Notifications.Add(notification);
            await context.SaveChangesAsync();

            // Đẩy real-time nếu có HubContext
            if (hubContext != null)
            {
                try
                {
                    var payload = new
                    {
                        id           = notification.Id,
                        title        = notification.Title,
                        message      = notification.Message,
                        type         = notification.Type,
                        category     = notification.Category,
                        categoryIcon = GetCategoryIcon(category, type),
                        linkUrl      = notification.LinkUrl,
                        isRead       = false,
                        timeAgo      = "Vừa xong"
                    };

                    await hubContext.Clients.Group(userId).SendAsync("ReceiveNotification", payload);
                    await hubContext.Clients.User(userId).SendAsync("ReceiveNotification", payload);
                }
                catch { /* Không ngắt luồng chính nếu SignalR lỗi */ }
            }

            return notification;
        }

        /// <summary>
        /// Tự động quét và sinh thông báo vé sắp hết hạn (<= 3 ngày) hoặc vừa hết hạn (<= 7 ngày).
        /// Có cơ chế chống trùng lặp trong vòng 48h, chỉ cảnh báo vé mới nhất theo từng phòng Gym.
        /// </summary>
        public static async Task CheckAndGenerateMembershipExpiryNotificationsAsync(
            GymDbContext context,
            string userId)
        {
            if (string.IsNullOrEmpty(userId)) return;

            var today = VnTime.Today;
            var warningThreshold = today.AddDays(3);
            var expiredThreshold = today.AddDays(-7);

            var candidateMemberships = await context.MemberMemberships
                .Include(m => m.Gym)
                .Include(m => m.Package)
                .Where(m => m.MemberId == userId && m.EndDate >= expiredThreshold && m.EndDate <= warningThreshold)
                .ToListAsync();

            if (!candidateMemberships.Any()) return;

            // Lấy vé mới nhất của mỗi phòng Gym để tránh báo trùng lặp với các vé cũ lịch sử
            var latestPerGym = candidateMemberships
                .GroupBy(m => m.GymId)
                .Select(g => g.OrderByDescending(m => m.EndDate).ThenByDescending(m => m.Id).First())
                .ToList();

            var recentCutoff = VnTime.Now.AddHours(-48);
            var recentNotifications = await context.Notifications
                .Where(n => n.UserId == userId && n.Category == "Membership" && n.CreatedAt >= recentCutoff)
                .ToListAsync();

            bool hasNew = false;

            foreach (var mem in latestPerGym)
            {
                // Vé ngày mới mua hôm nay thì không báo sắp hết hạn
                if (mem.Package?.PackageType == "Daily" && mem.StartDate.Date == today && mem.EndDate >= today)
                {
                    continue;
                }

                string gymName  = mem.Gym?.Name ?? "phòng Gym";
                string pkgName  = mem.Package?.Name ?? "Gói tập";
                string renewUrl = $"/Purchase/Renew?membershipId={mem.Id}";

                if (mem.EndDate >= today)
                {
                    int daysLeft = (mem.EndDate.Date - today).Days;
                    string daysText = daysLeft == 0 ? "hôm nay" : $"còn {daysLeft} ngày";

                    bool alreadyNotified = recentNotifications.Any(n =>
                        n.Type == "Warning" && (n.LinkUrl == renewUrl || n.Message.Contains(pkgName)));

                    if (!alreadyNotified)
                    {
                        var newNotif = new Notification
                        {
                            UserId    = userId,
                            Title     = "Vé tập sắp hết hạn",
                            Message   = $"Gói \"{pkgName}\" tại {gymName} sẽ hết hạn vào ngày {mem.EndDate:dd/MM/yyyy} ({daysText}). Hãy gia hạn sớm để không bị gián đoạn tập luyện!",
                            Type      = "Warning",
                            Category  = "Membership",
                            LinkUrl   = renewUrl,
                            IsRead    = false,
                            CreatedAt = VnTime.Now
                        };
                        context.Notifications.Add(newNotif);
                        recentNotifications.Add(newNotif);
                        hasNew = true;
                    }
                }
                else
                {
                    // Nếu hội viên đã có vé khác còn hạn tại cơ sở này thì không cần cảnh báo vé cũ hết hạn
                    bool hasActiveTicket = await context.MemberMemberships
                        .AnyAsync(m => m.MemberId == userId && m.GymId == mem.GymId && m.EndDate >= today);

                    if (hasActiveTicket) continue;

                    bool alreadyNotified = recentNotifications.Any(n =>
                        n.Type == "Danger" && (n.LinkUrl == renewUrl || n.Message.Contains(pkgName)));

                    if (!alreadyNotified)
                    {
                        var newNotif = new Notification
                        {
                            UserId    = userId,
                            Title     = "Vé tập đã hết hạn",
                            Message   = $"Gói \"{pkgName}\" tại {gymName} đã hết hạn vào ngày {mem.EndDate:dd/MM/yyyy}. Vui lòng gia hạn để tiếp tục vào phòng tập.",
                            Type      = "Danger",
                            Category  = "Membership",
                            LinkUrl   = renewUrl,
                            IsRead    = false,
                            CreatedAt = VnTime.Now
                        };
                        context.Notifications.Add(newNotif);
                        recentNotifications.Add(newNotif);
                        hasNew = true;
                    }
                }
            }

            if (hasNew)
            {
                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Định dạng thời gian tương đối thân thiện tiếng Việt.
        /// </summary>
        public static string GetTimeAgo(DateTime dt)
        {
            var span = VnTime.Now - dt;

            if (span.TotalSeconds < 60)
                return "Vừa xong";

            if (span.TotalMinutes < 60)
                return $"{(int)span.TotalMinutes} phút trước";

            if (span.TotalHours < 24)
                return $"{(int)span.TotalHours} giờ trước";

            if (span.TotalDays < 2)
                return $"Hôm qua lúc {dt:HH:mm}";

            if (span.TotalDays < 7)
                return $"{(int)span.TotalDays} ngày trước";

            return dt.ToString("dd/MM/yyyy HH:mm");
        }

        /// <summary>
        /// Lấy tên hiển thị tiếng Việt của Category.
        /// </summary>
        public static string GetCategoryDisplayName(string category)
        {
            return category switch
            {
                "Membership"  => "Gói tập & Vé",
                "Payment"     => "Thanh toán",
                "VipUpgrade"  => "Hạng VIP",
                "Suspension"  => "Kỷ luật & Đình chỉ",
                "GymApproval" => "Phê duyệt Gym",
                "Review"      => "Đánh giá",
                _             => "Hệ thống"
            };
        }

        /// <summary>
        /// Lấy Icon Bootstrap tương ứng với Category và Type.
        /// </summary>
        public static string GetCategoryIcon(string category, string type)
        {
            return category switch
            {
                "Membership"  => type == "Danger" ? "bi-exclamation-octagon" : "bi-card-checklist",
                "Payment"     => "bi-credit-card-2-front",
                "VipUpgrade"  => "bi-crown",
                "Suspension"  => "bi-slash-circle",
                "GymApproval" => "bi-building-check",
                "Review"      => "bi-star",
                _             => "bi-bell"
            };
        }
    }
}
