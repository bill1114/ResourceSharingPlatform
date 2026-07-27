namespace ResourceSharingPlatform.Models
{
    // Audit trail for each AI-assisted stock-in attempt: what was fed to the model
    // (photo or text), what it suggested, and whether the suggestion was confirmed
    // into an actual SupplyItem. Field names mirror SupplyItem's editable fields
    // (Category/ItemName/Specification/Quantity/Unit/StockType/ExpirationDate/
    // SafetyStock/Remark) so mapping into the SupplyItem Create flow stays 1:1.
    public class AIStockInLog
    {
        public int Id { get; set; }
        public int LocationId { get; set; }

        public string InputType { get; set; } = AIStockInInputTypes.Image;
        public string? InputText { get; set; }
        public string? InputImagePath { get; set; }

        public string? SuggestedCategory { get; set; }
        public string? SuggestedItemName { get; set; }
        public string? SuggestedSpecification { get; set; }
        public int? SuggestedQuantity { get; set; }
        public string? SuggestedUnit { get; set; }
        public string? SuggestedStockType { get; set; }
        public DateTime? SuggestedExpirationDate { get; set; }
        public int? SuggestedSafetyStock { get; set; }
        public string? SuggestedRemark { get; set; }

        public decimal? Confidence { get; set; }
        public string? RawResponse { get; set; }

        public bool IsConfirmed { get; set; }
        public int? ConfirmedSupplyItemId { get; set; }
        public string? Operator { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? ConfirmedAt { get; set; }

        // Navigation properties
        public SupplyLocation? Location { get; set; }
        public SupplyItem? ConfirmedSupplyItem { get; set; }
    }
}
