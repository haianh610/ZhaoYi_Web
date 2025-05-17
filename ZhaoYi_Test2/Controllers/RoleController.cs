using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ZhaoYi_Test2.Models;
using System.Threading.Tasks;

namespace ZhaoYi_Test2.Controllers
{
    public class RoleController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public RoleController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: /Role/ChooseRole
        public IActionResult ChooseRole()
        {
            // Kiểm tra xem người dùng đã có vai trò chưa
            if (User.Identity.IsAuthenticated)
            {
                var user = _userManager.GetUserAsync(User).Result;
                if (user.Role != 0) // Đã có vai trò
                {
                   return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            return View("ChooseRoleMobile");
        }

        // POST: /Role/ChooseRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChooseRole(int selectedRole)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // Kiểm tra giá trị selectedRole hợp lệ (1: Phiên dịch viên, 2: Nhà tuyển dụng)
            if (selectedRole != 1 && selectedRole != 2)
            {
                ModelState.AddModelError("", "Vui lòng chọn vai trò hợp lệ.");
                return View();
            }

            // Cập nhật vai trò người dùng
            user.Role = selectedRole;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);

                if (selectedRole == 1) // Interpreter
                {
                    TempData["StatusMessage"] = "Bạn đã chọn vai trò Phiên dịch viên. Bạn có thể cập nhật hồ sơ của bạn từ trang cá nhân.";
                    // Chuyển đến trang Dashboard của Interpreter thay vì trang tạo profile
                    return RedirectToAction("Dashboard", "Interpreters");
                }
                else // Recruiter (selectedRole == 2)
                {
                    TempData["StatusMessage"] = "Bạn đã chọn vai trò Nhà tuyển dụng. Bạn có thể hoàn thiện hồ sơ từ trang cá nhân.";
                    // Chuyển đến trang Dashboard của Recruiter thay vì trang tạo profile
                    return RedirectToAction("Dashboard", "Recruiters");
                }
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View("ChooseRoleMobile");
        }
    }
}
