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
                var user = _userManager.GetUserAsync(User).Result;
                if (user.Role != 0) // ?ã có vai trò
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            return View();
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
                // Refresh identity ?? c?p nh?t vai trò
                await _signInManager.RefreshSignInAsync(user);

                // Chuy?n h??ng d?a trên vai trò ???c ch?n
                if (selectedRole == 1) // Phiên d?ch viên
                {
                    TempData["StatusMessage"] = "B?n ?ã ch?n vai trò Phiên d?ch viên. Hãy c?p nh?t h? s? c?a b?n.";
                    return RedirectToAction("Profile", "Interpreters");
                }
                else // Nhà tuy?n d?ng
                {
                    TempData["StatusMessage"] = "B?n ?ã ch?n vai trò Nhà tuy?n d?ng.";
                    return RedirectToAction("Index", "Home"); // Ho?c trang t??ng ?ng cho nhà tuy?n d?ng
                }
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View();
        }
    }
}
