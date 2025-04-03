using System.ComponentModel.DataAnnotations;

namespace ZhaoYi_Test2.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email kh�ng ???c ?? tr?ng")]
        [EmailAddress(ErrorMessage = "Email kh�ng ?�ng ??nh d?ng")]
        public string Email { get; set; }

        [Required(ErrorMessage = "M?t kh?u kh�ng ???c ?? tr?ng")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Ghi nh? ??ng nh?p")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Email kh�ng ???c ?? tr?ng")]
        [EmailAddress(ErrorMessage = "Email kh�ng ?�ng ??nh d?ng")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "M?t kh?u kh�ng ???c ?? tr?ng")]
        [StringLength(100, ErrorMessage = "M?t kh?u ph?i c� �t nh?t {2} k� t?.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "M?t kh?u")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "X�c nh?n m?t kh?u")]
        [Compare("Password", ErrorMessage = "M?t kh?u v� m?t kh?u x�c nh?n kh�ng kh?p.")]
        public string ConfirmPassword { get; set; }
    }
}
