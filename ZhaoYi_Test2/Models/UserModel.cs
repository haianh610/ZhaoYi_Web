using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ZhaoYi_Test2.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        public int Role { get; set; } = 0; // 0: Chưa chọn vai trò, 1: Interpreter, 2: Recruiter
    }
}
