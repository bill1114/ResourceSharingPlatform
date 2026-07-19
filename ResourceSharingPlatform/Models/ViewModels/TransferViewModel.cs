using System.ComponentModel.DataAnnotations;

namespace ResourceSharingPlatform.Models.ViewModels
{
    public class TransferLineViewModel
    {
        [Required(ErrorMessage = "請選擇物資")]
        public int SupplyItemId { get; set; }

        [Required(ErrorMessage = "請輸入轉移數量")]
        [Range(1, int.MaxValue, ErrorMessage = "轉移數量必須大於 0")]
        public int TransferQuantity { get; set; }
    }

    public class TransferBatchViewModel
    {
        [Required(ErrorMessage = "請選擇來源據點")]
        public int FromLocationId { get; set; }

        [Required(ErrorMessage = "請選擇目標據點")]
        public int ToLocationId { get; set; }

        public string? Operator { get; set; }
        public string? Remark { get; set; }

        public List<TransferLineViewModel> Lines { get; set; } = new() { new TransferLineViewModel() };
    }
}
