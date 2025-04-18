using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using ZhaoYi_Test2.Data;
using ZhaoYi_Test2.Models;

namespace ZhaoYi_Test2.Controllers
{
    [Authorize] // Yêu cầu đăng nhập để truy cập
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<SettingsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        // GET: Settings
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Xác định vai trò người dùng
            string userRole = user.Role switch
            {
                1 => "Interpreter",
                2 => "Recruiter",
                _ => "User"
            };

            // Đặt ViewData cho layout
            ViewData["UserRole"] = userRole;
            ViewData["ShowBottomNav"] = true;

            // Lấy thông tin người dùng dựa vào vai trò
            if (userRole == "Interpreter")
            {
                var interpreter = await _context.Interpreters
                    .FirstOrDefaultAsync(i => i.UserId == user.Id);

                if (interpreter != null)
                {
                    ViewData["UserName"] = interpreter.InterpreterName;
                    ViewData["UserAvatar"] = !string.IsNullOrEmpty(interpreter.AvatarPath)
                        ? $"/uploads/avatars/{interpreter.AvatarPath}"
                        : "/images/default-avatar.png";
                }
            }
            else if (userRole == "Recruiter")
            {
                var recruiter = await _context.Recruiters
                    .FirstOrDefaultAsync(r => r.UserId == user.Id);

                if (recruiter != null)
                {
                    ViewData["UserName"] = recruiter.RecruiterName;
                    ViewData["UserAvatar"] = !string.IsNullOrEmpty(recruiter.AvatarPath)
                        ? $"/uploads/avatars/{recruiter.AvatarPath}"
                        : "/images/default-avatar.png";
                }
            }

            // Truyền các thông số giả định cho trang
            ViewData["FollowingCount"] = 0;
            ViewData["FollowersCount"] = 0;

            return View();
        }

        // Hành động đăng xuất
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            _logger.LogInformation("User logged out.");
            return RedirectToAction("Index", "Home");
        }

        // Các phương thức xử lý các tính năng cài đặt khác

        // // GET: Settings/ChangePassword
        // public IActionResult ChangePassword()
        // {
        //     return View();
        // }

        // // GET: Settings/SecuritySettings
        // public IActionResult SecuritySettings()
        // {
        //     return View();
        // }

        // // GET: Settings/MessageSettings
        // public IActionResult MessageSettings()
        // {
        //     return View();
        // }

        // // GET: Settings/About
        // public IActionResult About()
        // {
        //     return View();
        // }

        // // GET: Settings/Terms
        // public IActionResult Terms()
        // {
        //     return View();
        // }

        // // GET: Settings/Privacy
        // public IActionResult Privacy()
        // {
        //     return View();
        // }

        // // GET: Settings/Help
        // public IActionResult Help()
        // {
        //     return View();
        // }
    }
}
