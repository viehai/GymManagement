using GymManagement.Models;

namespace GymManagement.ViewModels
{
    /// <summary>
    /// ViewModel cho màn hình duyệt Catalog gốc để thêm thiết bị vào Gym (OWN-06).
    /// </summary>
    public class OwnerEquipmentCatalogViewModel
    {
        public Gym? CurrentGym { get; set; }
        public int GymId { get; set; }
        public List<Equipment> AvailableEquipments { get; set; } = new();
        public string? SelectedCategory { get; set; }
        public List<string> Categories { get; set; } = new();
    }
}
