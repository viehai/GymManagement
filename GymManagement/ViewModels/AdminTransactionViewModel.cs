using System;
using System.Collections.Generic;

namespace GymManagement.ViewModels
{
    public class AdminTransactionItemViewModel
    {
        public int Id { get; set; }
        public string MemberId { get; set; } = string.Empty;
        public string MemberFullName { get; set; } = string.Empty;
        public string MemberEmail { get; set; } = string.Empty;
        public int GymId { get; set; }
        public string GymName { get; set; } = string.Empty;
        public string GymAddress { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;
        public string PackageType { get; set; } = string.Empty;
        public int? DurationInMonths { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = "Pending";
        public string PaymentMethod { get; set; } = "VNPay";
        public string? VnpTxnRef { get; set; }
        public string? InvoiceCode { get; set; }
        public int? InvoiceId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AdminTransactionListViewModel
    {
        public List<AdminTransactionItemViewModel> Transactions { get; set; } = new();

        // KPI Summary
        public decimal TotalRevenue { get; set; }
        public int TotalTransactions { get; set; }
        public int SuccessCount { get; set; }
        public int PendingCount { get; set; }
        public int FailedCount { get; set; }

        // Filter state
        public int? SelectedGymId { get; set; }
        public string SelectedStatus { get; set; } = "all";
        public string? SearchKeyword { get; set; }

        // Gyms for Dropdown
        public List<GymDropdownItem> AvailableGyms { get; set; } = new();
    }

    public class GymDropdownItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
