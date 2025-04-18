using System;
using System.Collections.Generic;
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

        [StringLength(100)]
        [Display(Name = "Lĩnh vực")]
        public string? Field { get; set; }

        [Display(Name = "Ẩn hồ sơ")]
        public bool isHidden { get; set; } = true;

        // Trường Học vấn - Có thể có nhiều trường học
        [Display(Name = "Học vấn")]
        public List<Education> Educations { get; set; } = new List<Education>();

        // Trường Kinh nghiệm làm việc - Có thể có nhiều kinh nghiệm
        [Display(Name = "Kinh nghiệm làm việc")]
        public List<WorkExperience> WorkExperiences { get; set; } = new List<WorkExperience>();

        // Trường Kỹ năng - Lưu dưới dạng chuỗi được phân tách bằng dấu phẩy
        [Display(Name = "Kỹ năng")]
        [StringLength(500)]
        public string? Skills { get; set; }

        // Trường Dự án/thành tựu - Có thể có nhiều dự án/thành tựu
        [Display(Name = "Dự án/thành tựu")]
        public List<Project> Projects { get; set; } = new List<Project>();
    }

    // Lớp mô tả Học vấn
    public class Education
    {
        [Key]
        public int EducationId { get; set; }

        [Required]
        public int InterpreterId { get; set; }

        [ForeignKey("InterpreterId")]
        public Interpreter Interpreter { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Tên trường")]
        public string SchoolName { get; set; }

        [StringLength(100)]
        [Display(Name = "Chuyên ngành đào tạo")]
        public string Major { get; set; }

        [StringLength(100)]
        [Display(Name = "Bằng cấp")]
        public string Degree { get; set; }

        [Required]
        [Display(Name = "Ngày bắt đầu")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Display(Name = "Ngày kết thúc")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [StringLength(500)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }
    }

    // Lớp mô tả Kinh nghiệm làm việc
    public class WorkExperience
    {
        [Key]
        public int WorkExperienceId { get; set; }

        [Required]
        public int InterpreterId { get; set; }

        [ForeignKey("InterpreterId")]
        public Interpreter Interpreter { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Chức vụ")]
        public string JobTitle { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Tên công ty")]
        public string CompanyName { get; set; }

        [Required]
        [Display(Name = "Ngày bắt đầu")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Display(Name = "Ngày kết thúc")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [StringLength(500)]
        [Display(Name = "Mô tả công việc")]
        public string? JobDescription { get; set; }
    }

    // Lớp mô tả Dự án/thành tựu
    public class Project
    {
        [Key]
        public int ProjectId { get; set; }

        [Required]
        public int InterpreterId { get; set; }

        [ForeignKey("InterpreterId")]
        public Interpreter Interpreter { get; set; }

        [Required]
        [StringLength(150)]
        [Display(Name = "Tên dự án/thành tựu")]
        public string ProjectName { get; set; }

        [Required]
        [Display(Name = "Thời gian bắt đầu")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Display(Name = "Thời gian kết thúc")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [StringLength(1000)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }
    }
}
