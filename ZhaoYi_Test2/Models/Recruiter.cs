using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZhaoYi_Test2.Models
{
    public class Recruiter
    {
        [Key]
        public int RecruiterId { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Tên nhà tuyển dụng")]
        public string RecruiterName { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Khu vực làm việc")]
        public string WorkLocation { get; set; }

        [StringLength(255)]
        [Display(Name = "Địa chỉ chi tiết")]
        public string DetailedAddress { get; set; }

        [StringLength(255)]
        [Display(Name = "Ảnh đại diện")]
        public string? AvatarPath { get; set; }
    }
}
