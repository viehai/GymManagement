using System.ComponentModel.DataAnnotations;
using GymManagement.Helpers;

namespace GymManagement.ViewModels
{
    public class CreateSuspensionViewModel
    {
        public int GymId { get; set; }
        public string GymName { get; set; } = string.Empty;

        public string MemberId { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public string MemberEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập lý do đình chỉ hội viên.")]
        [StringLength(500, ErrorMessage = "Lý do không được vượt quá 500 ký tự.")]
        [Display(Name = "Lý do đình chỉ")]
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// "Temporary" hoặc "Permanent"
        /// </summary>
        [Required]
        [Display(Name = "Hình thức xử phạt")]
        public string SuspensionType { get; set; } = "Temporary";

        /// <summary>
        /// Số ngày đình chỉ tạm thời (7, 14, 30, hoặc tùy chỉnh)
        /// </summary>
        public int? DurationDays { get; set; } = 7;

        [Display(Name = "Ngày kết thúc đình chỉ")]
        public DateTime? EndDate { get; set; }
    }

    public class LiftSuspensionViewModel
    {
        public int SuspensionId { get; set; }
        public int GymId { get; set; }
        public string MemberName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Lý do không được vượt quá 500 ký tự.")]
        [Display(Name = "Lý do gỡ đình chỉ")]
        public string? LiftedReason { get; set; }
    }

    public class SuspensionItemViewModel
    {
        public int Id { get; set; }
        public int GymId { get; set; }
        public string GymName { get; set; } = string.Empty;

        public string MemberId { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public string MemberEmail { get; set; } = string.Empty;

        public string SuspendedByUserId { get; set; } = string.Empty;
        public string SuspendedByUserName { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;
        public string SuspensionType { get; set; } = "Temporary";

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string Status { get; set; } = "Active";
        public DateTime? LiftedAt { get; set; }
        public string? LiftedReason { get; set; }
        public DateTime CreatedAt { get; set; }

        public bool IsCurrentlyActive =>
            Status == "Active" && (SuspensionType == "Permanent" || EndDate == null || EndDate > VnTime.Now);
    }
}
