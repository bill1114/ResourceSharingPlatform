using System.ComponentModel.DataAnnotations;

namespace ResourceSharingPlatform.Models
{
    public class InventoryItemDefinition
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

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        public List<InventoryItemVariant> Variants { get; set; } = new();
        public List<LocationInventorySafetyStock> LocationSafetyStocks { get; set; } = new();

        public string DisplayName => $"{Category}｜{ItemName}";
    }
}
