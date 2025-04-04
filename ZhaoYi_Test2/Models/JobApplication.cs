using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ZhaoYi_Test2.Models
{
    public enum ApplicationStatus
    {
        [Display(Name = "Chờ phản hồi")]
        Pending,

        [Display(Name = "Chờ bắt đầu công việc")]
        WaitingToStart,

        [Display(Name = "Đang thực hiện")]
        InProgress,

        [Display(Name = "Hoàn thành")]
        Completed,

        [Display(Name = "Đã hủy")]
        Canceled,

        [Display(Name = "Bị từ chối")]
        Rejected
    }

    public class JobApplication
    {
        [Key]
        public int JobApplicationId { get; set; }

        [Required]
        [Display(Name = "Bài đăng việc làm")]
        public int JobPostingId { get; set; }

        [ForeignKey("JobPostingId")]
        public JobPosting JobPosting { get; set; }

        [Required]
        [Display(Name = "Phiên dịch viên")]
        public int InterpreterId { get; set; }

        [ForeignKey("InterpreterId")]
        public Interpreter Interpreter { get; set; }
        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Tên phiên dịch viên")]
        public string InterpreterName { get; set; }

        [Required]
        [Phone]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "File CV")]
        public string? CVFilePath { get; set; }


        [Required]
        [Display(Name = "Trạng thái ứng tuyển")]
        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;

        [Display(Name = "Ngày ứng tuyển")]
        [DataType(DataType.DateTime)]
        public DateTime ApplicationDate { get; set; } = DateTime.Now;
    }
}

