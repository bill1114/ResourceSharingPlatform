using System.ComponentModel.DataAnnotations;

namespace ResourceSharingPlatform.Models
{
    public class LocationInventorySafetyStock
    {
        public int Id { get; set; }
        public int LocationId { get; set; }
        public int InventoryItemDefinitionId { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "據點安全庫存不可小於 0")]
        public int SafetyStock { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        public SupplyLocation? Location { get; set; }
        public InventoryItemDefinition? InventoryItemDefinition { get; set; }
    }
}
