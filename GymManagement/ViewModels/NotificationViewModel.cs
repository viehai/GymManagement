using GymManagement.Helpers;

namespace GymManagement.ViewModels
{
    public class NotificationItemViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "Info";
        public string Category { get; set; } = "System";
        public string? LinkUrl { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }

        public string TimeAgo => NotificationHelper.GetTimeAgo(CreatedAt);
        public string CategoryDisplayName => NotificationHelper.GetCategoryDisplayName(Category);
        public string CategoryIcon => NotificationHelper.GetCategoryIcon(Category, Type);

        public string TypeBadgeClass => Type switch
        {
            "Success" => "badge-success",
            "Warning" => "badge-warning",
            "Danger"  => "badge-danger",
            _         => "badge-info"
        };
    }

    public class NotificationIndexViewModel
    {
        public List<NotificationItemViewModel> Notifications { get; set; } = new();
        public string CurrentFilter { get; set; } = "all"; // all, unread, read
        public string? CurrentCategory { get; set; }
        public int TotalCount { get; set; }
        public int UnreadCount { get; set; }
        public int ReadCount { get; set; }

        // Phân trang
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}
