using System.ComponentModel.DataAnnotations;

namespace GymManagement.ViewModels
{
    /// <summary>
    /// ViewModel cho màn hình phân quyền người dùng thủ công (ADM-12).
    /// </summary>
    public class AdminEditRoleViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string CurrentRole { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
        [Display(Name = "Vai trò (Role)")]
        public string SelectedRole { get; set; } = string.Empty;

        public List<string> AvailableRoles { get; set; } = new() { "Admin", "Owner", "Member" };
    }
}
