using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZhaoYi_Test2.Models
{
    public enum JobStatus
    {
        [Display(Name = "Đang tuyển dụng")]
        Active,
        
        [Display(Name = "Đã hoàn thành")]
        Completed,
        
        [Display(Name = "Đang thực hiện")]
        InProgress,
        
        [Display(Name = "Hết hạn")]
        Expired,
        
        [Display(Name = "Đã hủy")]
        Cancelled,
        
        [Display(Name = "Chờ duyệt")]
        Pending
    }

    public enum GenderRequirement
    {
        [Display(Name = "Không yêu cầu")]
        Any,
        
        [Display(Name = "Nam")]
        Male,
        
        [Display(Name = "Nữ")]
        Female
    }

    public class JobPosting
    {
        [Key]
        public int JobPostingId { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }
        [Required]
        public int RecruiterId { get; set; }

        [ForeignKey("RecruiterId")]
        public Recruiter Recruiter { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Tiêu đề")]
        public string Title { get; set; }

        [Required]
        [Display(Name = "Ngày đăng")]
        [DataType(DataType.DateTime)]
        public DateTime PostedDate { get; set; } = DateTime.Now;

        [Display(Name = "Ngày cập nhật")]
        [DataType(DataType.DateTime)]
        public DateTime? UpdatedDate { get; set; }

        [Required]
        [Display(Name = "Trạng thái")]
        public JobStatus Status { get; set; } = JobStatus.Pending;

        [StringLength(100)]
        [Display(Name = "Lĩnh vực")]
        public string Field { get; set; }

        [Required]
        [Display(Name = "Lương")]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Salary { get; set; }

        [Display(Name = "Yêu cầu kinh nghiệm")]
        public ExperienceLevel? RequiredExperience { get; set; }

        [Required]
        [Display(Name = "Mô tả công việc")]
        public string JobDescription { get; set; }

        [Display(Name = "Số lượt xem")]
        public int ViewCount { get; set; } = 0;

        [Display(Name = "Giới tính yêu cầu")]
        public GenderRequirement? GenderRequirement { get; set; }

        [Display(Name = "Số lượng tuyển")]
        public int? HiringCount { get; set; }

        [StringLength(100)]
        [Display(Name = "Trình độ")]
        public string EducationLevel { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Địa điểm làm việc")]
        public string WorkLocation { get; set; }

        [Required]
        [Display(Name = "Ngày hết hạn")]
        [DataType(DataType.Date)]
        public DateTime ExpiryDate { get; set; }
    }
}
