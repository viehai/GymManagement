using System;
using System.Collections.Generic;

namespace GymManagement.ViewModels
{
    public class AdminLogItemViewModel
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Entity { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Level { get; set; } = "Info";
        public DateTime CreatedAt { get; set; }
    }

    public class AdminLogListViewModel
    {
        public List<AdminLogItemViewModel> Items { get; set; } = new();

        // Filters
        public string CurrentLevel { get; set; } = "all";
        public string? CurrentAction { get; set; }
        public string? SearchKeyword { get; set; }

        // Counts
        public int TotalCount { get; set; }
        public int InfoCount { get; set; }
        public int WarningCount { get; set; }
        public int ErrorCount { get; set; }

        // Metadata
        public List<string> AvailableActions { get; set; } = new();
    }
}
