using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZhaoYi_Test2.Data;
using ZhaoYi_Test2.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace ZhaoYi_Test2.Controllers
{
    public class InterpretersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public InterpretersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Action hiển thị trang chủ dành cho phiên dịch viên
        [Authorize]
        public async Task<IActionResult> Dashboard(string searchTerm, string location, decimal? minSalary, decimal? maxSalary, ExperienceLevel? experience)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            // Lấy bài đăng có trạng thái Active (đang tuyển dụng)
            var jobsQuery = _context.JobPostings
                .Where(jp => jp.Status == JobStatus.Active && jp.ExpiryDate >= DateTime.Now)
                .Include(jp => jp.Recruiter)
                .OrderByDescending(jp => jp.PostedDate)
                .AsQueryable();

            // Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                jobsQuery = jobsQuery.Where(jp =>
                    jp.Title.ToLower().Contains(searchTerm) ||
                    jp.JobDescription.ToLower().Contains(searchTerm) ||
                    (jp.Field != null && jp.Field.ToLower().Contains(searchTerm)));
            }

            // Lọc theo địa điểm
            if (!string.IsNullOrEmpty(location))
            {
                location = location.ToLower();
                jobsQuery = jobsQuery.Where(jp => jp.WorkLocation.ToLower().Contains(location));
            }

            // Lọc theo mức lương
            if (minSalary.HasValue)
            {
                jobsQuery = jobsQuery.Where(jp => jp.Salary >= minSalary.Value);
            }

            if (maxSalary.HasValue)
            {
                jobsQuery = jobsQuery.Where(jp => jp.Salary <= maxSalary.Value);
            }

            // Lọc theo kinh nghiệm
            if (experience.HasValue)
            {
                jobsQuery = jobsQuery.Where(jp => jp.RequiredExperience.HasValue &&
                                           jp.RequiredExperience.Value == experience.Value);
            }

            var jobs = await jobsQuery.ToListAsync();

            // Lấy thông tin phiên dịch viên
            var interpreter = await _context.Interpreters
                .FirstOrDefaultAsync(i => i.UserId == user.Id);

            if (interpreter != null)
            {
                // Lấy danh sách các job mà phiên dịch viên đã ứng tuyển
                var appliedJobIds = await _context.JobApplications
                    .Where(a => a.InterpreterId == interpreter.InterpreterId)
                    .Select(a => a.JobPostingId)
                    .ToListAsync();

                ViewBag.AppliedJobs = appliedJobIds;
            }
            else
            {
                ViewBag.AppliedJobs = new List<int>();
            }

            // Truyền dữ liệu lọc vào ViewBag để hiển thị lại trong form lọc
            ViewBag.SearchTerm = searchTerm;
            ViewBag.Location = location;
            ViewBag.MinSalary = minSalary;
            ViewBag.MaxSalary = maxSalary;
            ViewBag.Experience = experience;

            return View(jobs);
        }

        // Hiển thị trang hồ sơ phiên dịch viên
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Chỉ cho phép người dùng có vai trò phiên dịch viên (Role = 1) truy cập trang này
            if (user.Role != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            // Kiểm tra xem người dùng đã có hồ sơ phiên dịch viên chưa
            var interpreter = await _context.Interpreters
                .FirstOrDefaultAsync(m => m.UserId == user.Id);

            if (interpreter == null)
            {
                interpreter = new Interpreter { UserId = user.Id };
                ViewBag.HasProfile = false;
            }
            else
            {
                ViewBag.HasProfile = true;
            }

            return View(interpreter);
        }

        // Xử lý yêu cầu POST để lưu hoặc cập nhật hồ sơ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(Interpreter interpreter)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // In ra thông tin để debug
            System.Diagnostics.Debug.WriteLine($"Received data: Name={interpreter.InterpreterName}, DOB={interpreter.DateOfBirth}");

            // Xóa lỗi validation cho trường User nếu có
            if (ModelState.ContainsKey("User"))
            {
                ModelState.Remove("User");
            }

            if (!ModelState.IsValid)
            {
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"ModelState Error: {error.ErrorMessage}");
                    }
                }

                ViewBag.HasProfile = await _context.Interpreters.AnyAsync(i => i.UserId == user.Id);
                return View(interpreter);
            }

            try
            {
                // Kiểm tra xem đang cập nhật hay tạo mới
                var existingInterpreter = await _context.Interpreters
                    .FirstOrDefaultAsync(m => m.UserId == user.Id);

                if (existingInterpreter == null)
                {
                    // Tạo mới hồ sơ
                    interpreter.UserId = user.Id;

                    System.Diagnostics.Debug.WriteLine($"Creating new interpreter: {interpreter.InterpreterName} for user {user.Id}");

                    _context.Interpreters.Add(interpreter);
                    await _context.SaveChangesAsync();

                    TempData["StatusMessage"] = "Hồ sơ phiên dịch viên đã được tạo thành công.";
                    System.Diagnostics.Debug.WriteLine("Profile created successfully");
                }
                else
                {
                    // Cập nhật hồ sơ hiện có
                    existingInterpreter.InterpreterName = interpreter.InterpreterName;
                    existingInterpreter.DateOfBirth = interpreter.DateOfBirth;
                    existingInterpreter.Gender = interpreter.Gender;
                    existingInterpreter.WorkLocation = interpreter.WorkLocation;
                    existingInterpreter.DetailedAddress = interpreter.DetailedAddress;
                    existingInterpreter.YearsOfExperience = interpreter.YearsOfExperience;

                    System.Diagnostics.Debug.WriteLine($"Updating interpreter: {existingInterpreter.InterpreterName}");

                    _context.Update(existingInterpreter);
                    await _context.SaveChangesAsync();

                    TempData["StatusMessage"] = "Hồ sơ phiên dịch viên đã được cập nhật thành công.";
                    System.Diagnostics.Debug.WriteLine("Profile updated successfully");
                }

                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                // Bắt lỗi và hiển thị
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
                ModelState.AddModelError("", $"Không thể lưu dữ liệu: {ex.Message}");
                ViewBag.HasProfile = await _context.Interpreters.AnyAsync(i => i.UserId == user.Id);
                return View(interpreter);
            }
        }

    }
}
