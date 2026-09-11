using System;
using System.Collections.Generic;
using GymManagement.Models;

namespace GymManagement.ViewModels
{
    public class CheckinHistoryViewModel
    {
        public int SelectedGymId { get; set; }
        public string SelectedGymName { get; set; } = string.Empty;
        public List<Gym> MyGyms { get; set; } = new();

        public DateTime FilterDate { get; set; }
        public string? SearchQuery { get; set; }

        public int TotalCheckinsCount { get; set; }
        public int UniqueMembersCount { get; set; }
        public string PeakHourText { get; set; } = "—";

        public List<CheckinHistoryRowItem> Checkins { get; set; } = new();
    }

    public class CheckinHistoryRowItem
    {
        public int Id { get; set; }
        public string MemberId { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public string MemberEmail { get; set; } = string.Empty;
        public string MemberPhone { get; set; } = string.Empty;

        public string GymName { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;

        public DateTime CheckinTime { get; set; }
        public DateTime? CheckoutTime { get; set; }
        public string Status { get; set; } = "Success";
        public string CheckinMethod { get; set; } = "QrScan";
        public string? StaffName { get; set; }
    }

    public class MemberPersonalCheckinHistoryViewModel
    {
        public string MemberName { get; set; } = string.Empty;
        public int TotalVisits { get; set; }
        public int ThisMonthVisits { get; set; }
        public List<CheckinHistoryRowItem> Visits { get; set; } = new();
    }
}
