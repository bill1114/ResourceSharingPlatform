using System.ComponentModel.DataAnnotations;

namespace ResourceSharingPlatform.Models.ViewModels
{
    public class DisposalViewModel
    {
        [Required(ErrorMessage = "請選擇據點")]
        public int LocationId { get; set; }

        [Required(ErrorMessage = "請選擇物資")]
        public int SupplyItemId { get; set; }

        [Required(ErrorMessage = "請輸入報廢數量")]
        [Range(1, int.MaxValue, ErrorMessage = "報廢數量必須大於 0")]
        public int DisposalQuantity { get; set; }

        [Required(ErrorMessage = "請選擇報廢原因")]
        public string Reason { get; set; } = DisposalReasons.Expired;

        public string? Remark { get; set; }
    }
}
