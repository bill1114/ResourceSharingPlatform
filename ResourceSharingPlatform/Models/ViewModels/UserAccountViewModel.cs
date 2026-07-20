using System.ComponentModel.DataAnnotations;

namespace ResourceSharingPlatform.Models.ViewModels
{
    public class UserAccountViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "請輸入帳號")]
        [StringLength(50)]
        public string UserName { get; set; } = string.Empty;

        // Required on create; leave blank on edit to keep the existing password
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 4, ErrorMessage = "密碼長度至少 4 碼")]
        public string? Password { get; set; }

        [StringLength(50)]
        public string? DisplayName { get; set; }

        [Required(ErrorMessage = "請選擇角色")]
        public string RoleName { get; set; } = Models.Roles.SocialWorker;

        public int? LocationId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
