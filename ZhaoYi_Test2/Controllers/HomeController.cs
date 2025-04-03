using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using ZhaoYi_Test2.Data;
using ZhaoYi_Test2.Models;

namespace ZhaoYi_Test2.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(
            ILogger<HomeController> logger,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Kiểm tra nếu người dùng đã đăng nhập và là phiên dịch viên (Role = 1)
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null && user.Role == 1)
                {
                    // Chuyển hướng đến trang chủ riêng cho phiên dịch viên
                    return RedirectToAction("Dashboard", "Interpreters");
                }
                else if (user != null && user.Role == 2)
                {
                    // Chuyển hướng đến trang chủ riêng cho nhà tuyển dụng
                    return RedirectToAction("Index", "JobPostings");
                }
            }

            // Trang chủ chung cho người chưa đăng nhập
            var featuredJobs = await _context.JobPostings
                .Where(jp => jp.Status == JobStatus.Active && jp.ExpiryDate >= DateTime.Now)
                .OrderByDescending(jp => jp.PostedDate)
                .Take(6)
                .ToListAsync();

            return View(featuredJobs);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
