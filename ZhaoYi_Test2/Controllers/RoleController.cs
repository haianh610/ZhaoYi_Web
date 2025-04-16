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
            // Ki?m tra xem ng??i dùng ?ã có vai trò ch?a
            if (User.Identity.IsAuthenticated)
            {
                //var user = _userManager.GetUserAsync(User).Result;
                //if (user.Role != 0) // ?ã có vai trò
                //{
                //    return RedirectToAction("Index", "Home");
                //}
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

            // Ki?m tra giá tr? selectedRole h?p l? (1: Phiên d?ch viên, 2: Nhà tuy?n d?ng)
            if (selectedRole != 1 && selectedRole != 2)
            {
                ModelState.AddModelError("", "Vui lòng ch?n vai trò h?p l?.");
                return View();
            }

            // C?p nh?t vai trò ng??i dùng
            user.Role = selectedRole;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);

                if (selectedRole == 1) // Interpreter
                {
                    TempData["StatusMessage"] = "Bạn đã chọn vai trò Phiên dịch viên. Hãy cập nhật hồ sơ của bạn.";
                    // Nếu Interpreter chưa có profile, cũng nên redirect đến trang tạo profile Interpreter
                    // (Logic này cần kiểm tra và thêm vào InterpreterController)
                    return RedirectToAction("Profile", "Interpreters");
                }
                else // Recruiter (selectedRole == 2)
                {
                    TempData["StatusMessage"] = "Bạn đã chọn vai trò Nhà tuyển dụng. Vui lòng hoàn thiện hồ sơ.";
                    // *** THAY ĐỔI Ở ĐÂY: Chuyển đến trang tạo profile Recruiter ***
                    return RedirectToAction("CreateProfile", "Recruiters");
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
