using System.ComponentModel.DataAnnotations;

namespace ZhaoYi_Test2.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email không ???c ?? tr?ng")]
        [EmailAddress(ErrorMessage = "Email không ?úng ??nh d?ng")]
        public string Email { get; set; }

        [Required(ErrorMessage = "M?t kh?u không ???c ?? tr?ng")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Ghi nh? ??ng nh?p")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Email không ???c ?? tr?ng")]
        [EmailAddress(ErrorMessage = "Email không ?úng ??nh d?ng")]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "M?t kh?u không ???c ?? tr?ng")]
        [StringLength(100, ErrorMessage = "M?t kh?u ph?i có ít nh?t {2} ký t?.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "M?t kh?u")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nh?n m?t kh?u")]
        [Compare("Password", ErrorMessage = "M?t kh?u và m?t kh?u xác nh?n không kh?p.")]
        public string ConfirmPassword { get; set; }
    }
}
