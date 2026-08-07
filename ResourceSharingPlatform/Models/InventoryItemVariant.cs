using System.ComponentModel.DataAnnotations;

namespace ResourceSharingPlatform.Models
{
    public class InventoryItemVariant
    {
        public int Id { get; set; }
        public int InventoryItemDefinitionId { get; set; }

        [StringLength(200)]
        public string? Specification { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        public InventoryItemDefinition? InventoryItemDefinition { get; set; }
        public string DisplayName => string.IsNullOrWhiteSpace(Specification) ? "無規格" : Specification;
    }
}
