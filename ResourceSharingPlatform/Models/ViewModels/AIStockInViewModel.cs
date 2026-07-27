using System.ComponentModel.DataAnnotations;

namespace ResourceSharingPlatform.Models.ViewModels
{
    // Confirmation-form fields for AI 智慧入庫, kept field-for-field identical to
    // SupplyItemController's Create binding list so the confirmed result maps
    // straight into a SupplyItem without any translation step.
    public class AIStockInViewModel
    {
        [Required(ErrorMessage = "請選擇據點")]
        public int LocationId { get; set; }

        [Required(ErrorMessage = "請輸入種類")]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "請輸入物資名稱")]
        [StringLength(100)]
        public string ItemName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Specification { get; set; }

        [Required(ErrorMessage = "請輸入數量")]
        [Range(0, int.MaxValue, ErrorMessage = "數量不可為負數")]
        public int Quantity { get; set; }

        [StringLength(20)]
        public string? Unit { get; set; }

        public string StockType { get; set; } = ResourceSharingPlatform.Models.StockTypes.HasExpiry;

        public DateTime? ExpirationDate { get; set; }

        public int SafetyStock { get; set; }

        public string? Remark { get; set; }

        // AI recognition metadata carried alongside the editable fields above
        public string SourceInputType { get; set; } = AIStockInInputTypes.Image;
        public string? SourceInputText { get; set; }
        public decimal? Confidence { get; set; }
    }
}
