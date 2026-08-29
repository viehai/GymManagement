using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GymManagement.Helpers;

namespace GymManagement.Models
{
    public class Invoice
    {
        public int Id { get; set; }

        [Required]
        public int TransactionId { get; set; }

        [Required]
        [StringLength(50)]
        public string InvoiceCode { get; set; }

        public DateTime IssuedDate { get; set; } = VnTime.Now;

        [StringLength(500)]
        public string PdfUrl { get; set; } = string.Empty;

        // Navigation property
        [ForeignKey("TransactionId")]
        public Transaction Transaction { get; set; }
    }
}