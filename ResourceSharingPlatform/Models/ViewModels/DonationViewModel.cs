using System.ComponentModel.DataAnnotations;

namespace ResourceSharingPlatform.Models.ViewModels
{
    public class DonationViewModel
    {
        [Required(ErrorMessage = "請選擇據點")]
        public int LocationId { get; set; }

        [Required(ErrorMessage = "請選擇物資")]
        public int SupplyItemId { get; set; }

        [Required(ErrorMessage = "請輸入捐贈數量")]
        [Range(1, int.MaxValue, ErrorMessage = "捐贈數量必須大於 0")]
        public int DonationQuantity { get; set; }

        [Required(ErrorMessage = "請輸入捐贈者姓名")]
        [StringLength(50)]
        public string DonorName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? DonorContact { get; set; }

        public string? Remark { get; set; }
    }
}
