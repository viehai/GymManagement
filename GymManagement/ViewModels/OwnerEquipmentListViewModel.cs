using GymManagement.Models;

namespace GymManagement.ViewModels
{
    /// <summary>
    /// ViewModel cho màn hình quản lý danh sách thiết bị của phòng Gym (OWN-10).
    /// </summary>
    public class OwnerEquipmentListViewModel
    {
        public Gym? CurrentGym { get; set; }
        public int SelectedGymId { get; set; }
        public List<Gym> MyGyms { get; set; } = new();
        public List<GymEquipment> Equipments { get; set; } = new();
        public int TotalCount => Equipments.Count;
        public int VisibleCount => Equipments.Count(e => e.IsVisible);
        public int HiddenCount => Equipments.Count(e => !e.IsVisible);
        public int CatalogCount => Equipments.Count(e => !e.IsCustom);
        public int CustomCount => Equipments.Count(e => e.IsCustom);
    }
}
