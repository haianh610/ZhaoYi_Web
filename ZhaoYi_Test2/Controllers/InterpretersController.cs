using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZhaoYi_Test2.Data;
using ZhaoYi_Test2.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;

namespace ZhaoYi_Test2.Controllers
{
    public class InterpretersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public InterpretersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        //// Action hiển thị trang chủ dành cho phiên dịch viên
        //[Authorize]
        //public async Task<IActionResult> Dashboard(string searchTerm, string location, decimal? minSalary, decimal? maxSalary, ExperienceLevel? experience)
        //{
        //    var user = await _userManager.GetUserAsync(User);
        //    if (user == null || user.Role != 1)
        //    {
        //        return RedirectToAction("Index", "Home");
        //    }

        //    // Lấy bài đăng có trạng thái Active (đang tuyển dụng)
        //    var jobsQuery = _context.JobPostings
        //        .Where(jp => jp.Status == JobStatus.Active && jp.ExpiryDate >= DateTime.Now)
        //        .Include(jp => jp.Recruiter)
        //        .OrderByDescending(jp => jp.PostedDate)
        //        .AsQueryable();

        //    // Lọc theo từ khóa tìm kiếm
        //    if (!string.IsNullOrEmpty(searchTerm))
        //    {
        //        searchTerm = searchTerm.ToLower();
        //        jobsQuery = jobsQuery.Where(jp =>
        //            jp.Title.ToLower().Contains(searchTerm) ||
        //            jp.JobDescription.ToLower().Contains(searchTerm) ||
        //            (jp.Field != null && jp.Field.ToLower().Contains(searchTerm)));
        //    }

        //    // Lọc theo địa điểm
        //    if (!string.IsNullOrEmpty(location))
        //    {
        //        location = location.ToLower();
        //        jobsQuery = jobsQuery.Where(jp => jp.WorkLocation.ToLower().Contains(location));
        //    }

        //    // Lọc theo mức lương
        //    if (minSalary.HasValue)
        //    {
        //        jobsQuery = jobsQuery.Where(jp => jp.Salary >= minSalary.Value);
        //    }

        //    if (maxSalary.HasValue)
        //    {
        //        jobsQuery = jobsQuery.Where(jp => jp.Salary <= maxSalary.Value);
        //    }

        //    // Lọc theo kinh nghiệm
        //    if (experience.HasValue)
        //    {
        //        jobsQuery = jobsQuery.Where(jp => jp.RequiredExperience.HasValue &&
        //                                   jp.RequiredExperience.Value == experience.Value);
        //    }

        //    var jobs = await jobsQuery.ToListAsync();

        //    // Lấy thông tin phiên dịch viên
        //    var interpreter = await _context.Interpreters
        //        .FirstOrDefaultAsync(i => i.UserId == user.Id);

        //    if (interpreter != null)
        //    {
        //        // Lấy danh sách các job mà phiên dịch viên đã ứng tuyển
        //        var appliedJobIds = await _context.JobApplications
        //            .Where(a => a.InterpreterId == interpreter.InterpreterId)
        //            .Select(a => a.JobPostingId)
        //            .ToListAsync();

        //        ViewBag.AppliedJobs = appliedJobIds;
        //    }
        //    else
        //    {
        //        ViewBag.AppliedJobs = new List<int>();
        //    }

        //    // Truyền dữ liệu lọc vào ViewBag để hiển thị lại trong form lọc
        //    ViewBag.SearchTerm = searchTerm;
        //    ViewBag.Location = location;
        //    ViewBag.MinSalary = minSalary;
        //    ViewBag.MaxSalary = maxSalary;
        //    ViewBag.Experience = experience;

        //    return View(jobs);
        //}

        // GET: Interpreters/Dashboard
        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != 1) // Chỉ cho phép phiên dịch viên
            {
                return RedirectToAction("Index", "Home");
            }

            // Lấy thông tin phiên dịch viên
            var interpreter = await _context.Interpreters
                .FirstOrDefaultAsync(i => i.UserId == user.Id);

            if (interpreter == null)
            {
                TempData["ErrorMessage"] = "Bạn cần tạo hồ sơ phiên dịch viên trước.";
                return RedirectToAction("Profile", "Interpreters");
            }

            // Lấy danh sách công việc phù hợp
            var jobs = await _context.JobPostings
                .Where(jp => jp.Status == JobStatus.Active && jp.ExpiryDate >= DateTime.Now)
                .Include(jp => jp.Recruiter)
                .OrderByDescending(jp => jp.PostedDate)
                .ToListAsync();

            // Truyền ViewBag cho thông tin thêm nếu cần
            ViewBag.Interpreter = interpreter;

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
        public async Task<IActionResult> Profile(Interpreter interpreter, IFormFile? avatarFile, bool removeAvatar = false)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // In ra thông tin để debug
            System.Diagnostics.Debug.WriteLine($"Received data: Name={interpreter.InterpreterName}, DOB={interpreter.DateOfBirth}");
            System.Diagnostics.Debug.WriteLine($"Remove Avatar: {removeAvatar}");

            if (avatarFile != null)
            {
                System.Diagnostics.Debug.WriteLine($"Avatar file received: {avatarFile.FileName}, Size: {avatarFile.Length} bytes");
            }

            // Xóa lỗi validation cho trường User nếu có
            if (ModelState.ContainsKey("User"))
            {
                ModelState.Remove("User");
            }

            // Xóa lỗi validation cho trường avatarFile nếu có
            if (ModelState.ContainsKey("avatarFile"))
            {
                ModelState.Remove("avatarFile");
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
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "avatars");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Xử lý trường hợp xóa ảnh đại diện
                if (removeAvatar && !string.IsNullOrEmpty(interpreter.AvatarPath))
                {
                    var oldFilePath = Path.Combine(uploadsFolder, interpreter.AvatarPath);
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        try
                        {
                            System.IO.File.Delete(oldFilePath);
                            System.Diagnostics.Debug.WriteLine("Deleted avatar file: " + oldFilePath);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error deleting avatar: {ex.Message}");
                        }
                    }
                    interpreter.AvatarPath = null; // Đặt lại đường dẫn ảnh
                }
                // Xử lý upload ảnh đại diện
                else if (avatarFile != null && avatarFile.Length > 0)
                {
                    // Kiểm tra kích thước file (tối đa 2MB)
                    if (avatarFile.Length > 2 * 1024 * 1024)
                    {
                        ModelState.AddModelError("", "Kích thước file quá lớn (tối đa 2MB).");
                        ViewBag.HasProfile = await _context.Interpreters.AnyAsync(i => i.UserId == user.Id);
                        return View(interpreter);
                    }

                    // Kiểm tra loại file (chỉ cho phép ảnh)
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var fileExtension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("", "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif).");
                        ViewBag.HasProfile = await _context.Interpreters.AnyAsync(i => i.UserId == user.Id);
                        return View(interpreter);
                    }

                    // Tạo tên file mới để tránh trùng lặp
                    var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Lưu file ảnh
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await avatarFile.CopyToAsync(fileStream);
                    }

                    // Xóa ảnh cũ nếu có
                    if (!string.IsNullOrEmpty(interpreter.AvatarPath) && interpreter.AvatarPath != uniqueFileName)
                    {
                        var oldFilePath = Path.Combine(uploadsFolder, interpreter.AvatarPath);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            try
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                            catch (Exception ex)
                            {
                                // Log lỗi nhưng không dừng quá trình
                                System.Diagnostics.Debug.WriteLine($"Error deleting old avatar: {ex.Message}");
                            }
                        }
                    }

                    // Cập nhật đường dẫn ảnh
                    interpreter.AvatarPath = uniqueFileName;
                }

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

                    // Cập nhật AvatarPath nếu có upload ảnh mới hoặc xóa ảnh
                    if (avatarFile != null && avatarFile.Length > 0 || removeAvatar)
                    {
                        existingInterpreter.AvatarPath = interpreter.AvatarPath;
                    }

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
