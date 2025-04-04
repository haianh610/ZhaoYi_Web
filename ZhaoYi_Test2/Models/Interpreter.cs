using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZhaoYi_Test2.Models
{
    public enum ExperienceLevel
    {
        [Display(Name = "Dưới 1 năm")]
        LessThanOneYear,

        [Display(Name = "1 năm")]
        OneYear,

        [Display(Name = "2 năm")]
        TwoYears,

        [Display(Name = "3 năm")]
        ThreeYears,

        [Display(Name = "4 năm")]
        FourYears,

        [Display(Name = "5 năm")]
        FiveYears,

        [Display(Name = "Hơn 5 năm")]
        MoreThanFiveYears
    }

    public class Interpreter
    {
        [Key]
        public int InterpreterId { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Họ và tên")]
        public string InterpreterName { get; set; }

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required]
        [Display(Name = "Giới tính")]
        public string Gender { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Khu vực làm việc")]
        public string WorkLocation { get; set; }

        [Required]
        [StringLength(255)]
        [Display(Name = "Địa chỉ chi tiết")]
        public string DetailedAddress { get; set; }

        [Required]
        [Display(Name = "Kinh nghiệm")]
        public ExperienceLevel YearsOfExperience { get; set; }

        [StringLength(255)]
        [Display(Name = "Ảnh đại diện")]
        public string? AvatarPath { get; set; }
    }
}
