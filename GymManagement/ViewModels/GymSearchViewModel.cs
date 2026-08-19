namespace GymManagement.ViewModels
{
    /// <summary>
    /// ViewModel dùng cho màn hình Tìm kiếm &amp; Danh sách phòng Gym (Views/Gym/Search.cshtml).
    /// </summary>
    public class GymSearchViewModel
    {
        public int Id { get; set; }

        /// <summary>Tên phòng Gym.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Địa chỉ phòng Gym.</summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>Mô tả ngắn, được cắt bớt nếu quá dài.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>URL ảnh bìa của phòng Gym (relative path hoặc absolute URL).</summary>
        public string ImageUrl { get; set; } = string.Empty;

        /// <summary>Trạng thái: Pending / Approved / Rejected / Suspended.</summary>
        public string Status { get; set; } = "Pending";

        /// <summary>Ngày tạo.</summary>
        public DateTime CreatedAt { get; set; }

        // ── Computed helpers ──

        /// <summary>Trả về ảnh mặc định khi ImageUrl rỗng.</summary>
        public string DisplayImage =>
            string.IsNullOrWhiteSpace(ImageUrl)
                ? "https://static.wixstatic.com/media/7e9c4c_5d4a9443f1fd4b7a8f8d0ca05ef2b8a8~mv2.jpg/v1/fill/w_1905,h_945,al_c,q_85,usm_0.66_1.00_0.01,enc_avif,quality_auto/7e9c4c_5d4a9443f1fd4b7a8f8d0ca05ef2b8a8~mv2.jpg"
                : ImageUrl;

        /// <summary>Mô tả rút gọn, tối đa 120 ký tự.</summary>
        public string ShortDescription =>
            string.IsNullOrWhiteSpace(Description)
                ? "Chưa có mô tả."
                : (Description.Length > 120 ? Description[..120] + "…" : Description);
    }
}
