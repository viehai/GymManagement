namespace GymManagement.ViewModels
{
    /// <summary>
    /// Item đại diện cho một người dùng trong danh sách quản lý Admin (ADM-10).
    /// </summary>
    public class AdminUserItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = "Member";
        public bool IsLocked { get; set; }
        public bool HasPendingGym { get; set; }
        public int GymCount { get; set; }
        public string PendingGymName { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel cho màn hình quản lý danh sách người dùng Admin (ADM-10, ADM-01).
    /// </summary>
    public class AdminUserListViewModel
    {
        public List<AdminUserItemViewModel> Users { get; set; } = new();
        public string CurrentFilter { get; set; } = "all";
        public string? SearchQuery { get; set; }

        public int TotalCount { get; set; }
        public int MemberCount { get; set; }
        public int OwnerCount { get; set; }
        public int PendingCount { get; set; }
        public int AdminCount { get; set; }
        public int LockedCount { get; set; }
    }
}
