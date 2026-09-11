namespace GymManagement.ViewModels
{
    /// <summary>ViewModel cho một thiết bị hiển thị trên trang chi tiết Gym.</summary>
    public class GymEquipmentDisplayViewModel
    {
        /// <summary>Tên hiển thị: CustomName (custom) hoặc Equipment.Name (catalog).</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Ảnh hiển thị: CustomImage (custom) hoặc Equipment.ImageUrl (catalog).</summary>
        public string DisplayImage { get; set; } = string.Empty;

        /// <summary>Phân loại nhóm cơ (Cardio, Strength - Ngực, v.v.).</summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>Có phải máy do Owner tự thêm không.</summary>
        public bool IsCustom { get; set; }
    }

    /// <summary>ViewModel cho một gói vé hiển thị trên trang chi tiết Gym.</summary>
    public class PackageDisplayViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        /// <summary>"Daily" hoặc "Monthly".</summary>
        public string PackageType { get; set; } = string.Empty;

        /// <summary>Số tháng (chỉ có khi PackageType = "Monthly").</summary>
        public int? DurationInMonths { get; set; }

        public decimal Price { get; set; }

        /// <summary>Nhãn loại gói: "Vé ngày" hoặc "Gói X tháng".</summary>
        public string TypeLabel =>
            PackageType == "Daily"
                ? "Vé ngày"
                : $"Gói {DurationInMonths} tháng";
    }

    /// <summary>
    /// ViewModel tổng hợp dùng cho trang chi tiết phòng Gym (Views/Gym/Details.cshtml).
    /// </summary>
    public class GymDetailsViewModel
    {
        // ── Thông tin cơ bản ──
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string OwnerId { get; set; } = string.Empty;
        public bool IsOwnerOfThisGym { get; set; }

        // ── Gallery ảnh (V2) ──
        public List<GymImageViewModel> GalleryImages { get; set; } = new();

        /// <summary>Ảnh bìa: ưu tiên GalleryImages có IsCover=true, fallback ImageUrl V1.</summary>
        public string CoverImage =>
            GalleryImages.FirstOrDefault(i => i.IsCover)?.ImageUrl
            ?? GalleryImages.FirstOrDefault()?.ImageUrl
            ?? ImageUrl;

        public bool HasGallery => GalleryImages.Count > 0;

        /// <summary>Ảnh đại diện có fallback.</summary>
        public string DisplayImage =>
            string.IsNullOrWhiteSpace(ImageUrl)
                ? "https://static.wixstatic.com/media/7e9c4c_5d4a9443f1fd4b7a8f8d0ca05ef2b8a8~mv2.jpg/v1/fill/w_1905,h_945,al_c,q_85,usm_0.66_1.00_0.01,enc_avif,quality_auto/7e9c4c_5d4a9443f1fd4b7a8f8d0ca05ef2b8a8~mv2.jpg"
                : ImageUrl;

        // ── Thiết bị (IsVisible = true) ──
        public List<GymEquipmentDisplayViewModel> Equipments { get; set; } = new();

        // ── Gói vé (IsActive = true, sắp xếp theo giá tăng dần) ──
        public List<PackageDisplayViewModel> Packages { get; set; } = new();

        // ── Computed helpers ──
        public bool HasEquipments => Equipments.Count > 0;
        public bool HasPackages => Packages.Count > 0;

        /// <summary>Có gói vé ngày không (để hiện nút "Mua vé ngày").</summary>
        public bool HasDailyPass => Packages.Any(p => p.PackageType == "Daily");

        /// <summary>Có gói tháng không (để hiện nút "Đăng ký gói tháng").</summary>
        public bool HasMonthlyPackage => Packages.Any(p => p.PackageType == "Monthly");

        // ── Thước đo độ đông đúc (Live Crowd Meter - V2) ──
        public int MaxCapacity { get; set; } = 50;
        public int CurrentActiveMembers { get; set; }
        public int CrowdPercentage { get; set; }
        public string CrowdStatusText { get; set; } = "Đang vắng";
        public string CrowdStatusColor { get; set; } = "#10b981";
        public string CrowdStatusIcon { get; set; } = "bi-emoji-smile";
        public string CrowdRecommendation { get; set; } = "Phòng tập đang vắng, máy tập thoáng đãng — Thời điểm lý tưởng!";

        // ── Rating & Review (V2) ──
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public bool HasReviews => TotalReviews > 0;
        public Dictionary<int, int> StarCounts { get; set; } = new() { { 5, 0 }, { 4, 0 }, { 3, 0 }, { 2, 0 }, { 1, 0 } };
        public List<GymReviewDisplayViewModel> Reviews { get; set; } = new();

        /// <summary>Hội viên hiện tại có quyền viết đánh giá cho gym này không.</summary>
        public bool CanReview { get; set; }
        public string? CannotReviewReason { get; set; }

        /// <summary>Đánh giá đã có của hội viên hiện tại (nếu có để cho phép sửa/xóa).</summary>
        public GymReviewDisplayViewModel? CurrentUserReview { get; set; }
        public bool HasUserReviewed => CurrentUserReview != null;
    }
}
