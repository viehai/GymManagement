using System;

namespace GymManagement.ViewModels
{
    public class QrPaymentViewModel
    {
        public int TransactionId { get; set; }
        public string OrderRef { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string BankId { get; set; } = "MB";
        public string BankName { get; set; } = "MBBank";
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string TransferContent { get; set; } = string.Empty;
        public string QrImageUrl { get; set; } = string.Empty;

        public string GymName { get; set; } = string.Empty;
        public string GymAddress { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;
        public string PackageType { get; set; } = string.Empty;
        public int? DurationInMonths { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class SepayWebhookDto
    {
        public long Id { get; set; }
        public string? Gateway { get; set; }
        public string? TransactionDate { get; set; }
        public string? AccountNumber { get; set; }
        public string? Code { get; set; }
        public string? Content { get; set; }
        public string? TransferType { get; set; }
        public decimal TransferAmount { get; set; }
        public decimal Accumulated { get; set; }
        public string? ReferenceCode { get; set; }
        public string? Description { get; set; }
    }
}
