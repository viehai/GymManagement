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
    }
}
