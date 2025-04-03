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
        public string RecruiterName { get; set; }

        [Required]
        [StringLength(100)]
        public string WorkLocation { get; set; }

        [StringLength(255)]
        public string DetailedAddress { get; set; }
    }
}
