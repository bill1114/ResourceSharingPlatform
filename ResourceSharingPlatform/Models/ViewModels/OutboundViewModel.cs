using System.ComponentModel.DataAnnotations;

namespace ResourceSharingPlatform.Models.ViewModels
{
    public class OutboundViewModel
    {
        [Required(ErrorMessage = "請選擇物資")]
        public int SupplyItemId { get; set; }

        [Required(ErrorMessage = "請輸入出庫數量")]
        [Range(1, int.MaxValue, ErrorMessage = "出庫數量必須大於 0")]
        public int OutboundQuantity { get; set; }

        [Required(ErrorMessage = "請輸入領用人姓名")]
        [StringLength(50)]
        public string RecipientName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? RecipientContact { get; set; }

        public string? Remark { get; set; }
    }
}
