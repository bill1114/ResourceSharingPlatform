using System.ComponentModel.DataAnnotations;

namespace ResourceSharingPlatform.Models
{
    public class InventoryTypeSetting
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "請輸入物資種類")]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "請輸入物資名稱")]
        [StringLength(100)]
        public string ItemName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Specification { get; set; }

        [StringLength(20)]
        public string? Unit { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "安全庫存不可小於 0")]
        public int SafetyStock { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        public string DisplayName
        {
            get
            {
                var name = string.IsNullOrWhiteSpace(Specification)
                    ? $"{Category}｜{ItemName}"
                    : $"{Category}｜{ItemName}｜{Specification}";
                return string.IsNullOrWhiteSpace(Unit) ? name : $"{name}（{Unit}）";
            }
        }
    }
}
