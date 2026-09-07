namespace GymManagement.ViewModels
{
    /// <summary>ViewModel cho 1 ảnh trong gallery của Gym.</summary>
    public class GymImageViewModel
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public bool IsCover { get; set; }
    }
}
