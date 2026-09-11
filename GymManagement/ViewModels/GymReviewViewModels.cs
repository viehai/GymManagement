using System.ComponentModel.DataAnnotations;
using GymManagement.Helpers;
using GymManagement.Models;

namespace GymManagement.ViewModels
{
    /// <summary>
    /// ViewModel hiển thị một đánh giá ra giao diện công khai hoặc trang quản lý.
    /// </summary>
    public class GymReviewDisplayViewModel
    {
        public int Id { get; set; }
        public int GymId { get; set; }
        public string GymName { get; set; } = string.Empty;
        public string GymAddress { get; set; } = string.Empty;
        public string GymImage { get; set; } = string.Empty;
        public string MemberId { get; set; } = string.Empty;
        public string MemberName { get; set; } = string.Empty;
        public string MemberAvatarLetter => !string.IsNullOrEmpty(MemberName) ? MemberName[..1].ToUpper() : "M";

        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsVisible { get; set; }

        public string? OwnerReply { get; set; }
        public DateTime? OwnerRepliedAt { get; set; }
        public bool HasOwnerReply => !string.IsNullOrWhiteSpace(OwnerReply);

        public string TimeAgo => NotificationHelper.GetTimeAgo(CreatedAt);
        public string? ReplyTimeAgo => OwnerRepliedAt.HasValue ? NotificationHelper.GetTimeAgo(OwnerRepliedAt.Value) : null;
    }

    /// <summary>
    /// Input model khi hội viên gửi hoặc sửa đánh giá.
    /// </summary>
    public class SubmitReviewInputModel
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "Không tìm thấy thông tin phòng gym.")]
        public int GymId { get; set; }

        [Range(1, 5, ErrorMessage = "Vui lòng chọn số sao từ 1 đến 5.")]
        public int Rating { get; set; } = 5;

        [StringLength(500, ErrorMessage = "Nhận xét không được vượt quá 500 ký tự.")]
        public string? Comment { get; set; }
    }

    /// <summary>
    /// ViewModel cho trang quản lý đánh giá của Owner (OwnerReview/Index).
    /// </summary>
    public class OwnerReviewIndexViewModel
    {
        public List<Gym> OwnerGyms { get; set; } = new();
        public int SelectedGymId { get; set; }
        public Gym? SelectedGym { get; set; }

        public int? FilterRating { get; set; }
        public bool? FilterVisibility { get; set; }

        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int HiddenReviewsCount { get; set; }
        public Dictionary<int, int> StarCounts { get; set; } = new();

        public List<GymReviewDisplayViewModel> Reviews { get; set; } = new();
    }

    /// <summary>
    /// ViewModel cho trang giám sát đánh giá của Admin (AdminReview/Index).
    /// </summary>
    public class AdminReviewIndexViewModel
    {
        public List<Gym> AllGyms { get; set; } = new();
        public int? FilterGymId { get; set; }
        public int? FilterRating { get; set; }
        public bool? FilterVisibility { get; set; }
        public string? SearchKeyword { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);

        public double SystemAverageRating { get; set; }
        public int TotalReviewsCount { get; set; }
        public int HiddenReviewsCount { get; set; }

        public List<GymReviewDisplayViewModel> Reviews { get; set; } = new();
    }

    /// <summary>
    /// ViewModel cho màn hình quản lý Từ ngữ cấm của Admin (AdminReview/BannedWords).
    /// </summary>
    public class BannedWordsManagementViewModel
    {
        public List<BannedWord> BannedWords { get; set; } = new();
        public string? SelectedCategory { get; set; }
        public string? SearchKeyword { get; set; }
        public List<string> Categories { get; set; } = new();

        // Form thêm mới
        [Required(ErrorMessage = "Vui lòng nhập từ ngữ cấm.")]
        [StringLength(100, ErrorMessage = "Tối đa 100 ký tự.")]
        public string NewWord { get; set; } = string.Empty;

        [StringLength(50)]
        public string NewCategory { get; set; } = "Thô tục";
    }
}
