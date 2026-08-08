using System.ComponentModel.DataAnnotations;

namespace ResourceSharingPlatform.Models.ViewModels
{
    public class InventoryDefinitionFormViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "請輸入物資種類")]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "請輸入物資名稱")]
        [StringLength(100)]
        public string ItemName { get; set; } = string.Empty;

        [Required(ErrorMessage = "請輸入最小計算單位")]
        [StringLength(20)]
        public string Unit { get; set; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "總量安全庫存不可小於 0")]
        public int GlobalSafetyStock { get; set; }

        [Required(ErrorMessage = "請選擇庫存分類")]
        public string StockType { get; set; } = ResourceSharingPlatform.Models.StockTypes.HasExpiry;

        [StringLength(200)]
        public string? InitialSpecification { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class LocationSafetyStockFormViewModel
    {
        public int DefinitionId { get; set; }
        public string DefinitionName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public List<LocationSafetyStockLineViewModel> Locations { get; set; } = new();
    }

    public class LocationSafetyStockLineViewModel
    {
        public int LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "據點安全庫存不可小於 0")]
        public int SafetyStock { get; set; }
    }
}
