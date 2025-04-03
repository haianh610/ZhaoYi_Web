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
        public string InterpreterName { get; set; }

        [DataType(DataType.Date)]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public string Gender { get; set; }

        [Required]
        [StringLength(100)]
        public string WorkLocation { get; set; }

        [Required]
        [StringLength(255)]
        public string DetailedAddress { get; set; }

        [Required]
        public ExperienceLevel YearsOfExperience { get; set; }
    }
}
