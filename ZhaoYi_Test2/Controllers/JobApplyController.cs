using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;
using ZhaoYi_Test2.Data;
using ZhaoYi_Test2.Models;

namespace ZhaoYi_Test2.Controllers
{
    // Removed [Authorize] from controller level to allow anonymous access to Details
    public class JobApplyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public JobApplyController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: JobApply/Details/5
        // Allow anonymous access to job details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var jobPosting = await _context.JobPostings
                .Include(j => j.Recruiter)
                .FirstOrDefaultAsync(m => m.JobPostingId == id);

            if (jobPosting == null)
            {
                return NotFound();
            }

            // Tăng số lượt xem
            jobPosting.ViewCount++;
            await _context.SaveChangesAsync();

            // Đặt các giá trị mặc định cho người dùng chưa đăng nhập
            bool hasApplied = false;
            int userRole = 0;
            ViewBag.Interpreter = null;

            // Chỉ kiểm tra đăng nhập và ứng tuyển nếu người dùng đã đăng nhập
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    userRole = user.Role;

                    if (user.Role == 1) // Phiên dịch viên
                    {
                        // Lấy thông tin phiên dịch viên
                        var interpreter = await _context.Interpreters
                            .FirstOrDefaultAsync(i => i.UserId == user.Id);

                        if (interpreter != null)
                        {
                            // Kiểm tra xem đã ứng tuyển chưa
                            hasApplied = await _context.JobApplications
                                .AnyAsync(a => a.JobPostingId == id && a.InterpreterId == interpreter.InterpreterId);

                            ViewBag.Interpreter = interpreter;
                        }
                    }
                }
            }

            ViewBag.HasApplied = hasApplied;
            ViewBag.UserRole = userRole;

            return View("DetailsMobile", jobPosting);
        }

        // GET: JobApply/Apply/5
        [Authorize] // Add Authorize attribute to individual actions that require authentication
        public async Task<IActionResult> Apply(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != 1) // Ch? phiên d?ch viên m?i ???c ?ng tuy?n
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Ki?m tra xem bài ??ng có t?n t?i không
            var jobPosting = await _context.JobPostings
                .FirstOrDefaultAsync(m => m.JobPostingId == id && m.Status == JobStatus.Active);

            if (jobPosting == null || jobPosting.ExpiryDate < DateTime.Now)
            {
                TempData["ErrorMessage"] = "Bài ??ng không t?n t?i ho?c ?ã h?t h?n.";
                return RedirectToAction("Index", "Home");
            }

            // L?y thông tin phiên d?ch viên
            var interpreter = await _context.Interpreters
                .FirstOrDefaultAsync(i => i.UserId == user.Id);

            if (interpreter == null)
            {
                TempData["ErrorMessage"] = "B?n c?n t?o h? s? phiên d?ch viên tr??c khi ?ng tuy?n.";
                return RedirectToAction("Profile", "Interpreters");
            }

            // Ki?m tra xem ?ã ?ng tuy?n ch?a
            var existingApplication = await _context.JobApplications
                .FirstOrDefaultAsync(a => a.JobPostingId == id && a.InterpreterId == interpreter.InterpreterId);

            if (existingApplication != null)
            {
                TempData["ErrorMessage"] = "B?n ?ã ?ng tuy?n vào v? trí này r?i.";
                return RedirectToAction("Details", new { id });
            }

            // T?o form ?ng tuy?n m?i
            var application = new JobApplication
            {
                JobPostingId = jobPosting.JobPostingId,
                InterpreterId = interpreter.InterpreterId,
                UserId = user.Id,
                InterpreterName = interpreter.InterpreterName,
                PhoneNumber = user.PhoneNumber ?? "",
                Email = user.Email,
                Status = ApplicationStatus.Pending,
                ApplicationDate = DateTime.Now
            };

            return View(application);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Apply(JobApplication application, IFormFile cvFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != 1)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Thêm log
            System.Diagnostics.Debug.WriteLine($"Received application: JobID={application.JobPostingId}, InterpreterID={application.InterpreterId}");

            if (cvFile != null)
            {
                System.Diagnostics.Debug.WriteLine($"File received: {cvFile.FileName}, Size: {cvFile.Length} bytes");
            }

            // Kiểm tra bài đăng
            var jobPosting = await _context.JobPostings
                .FirstOrDefaultAsync(jp => jp.JobPostingId == application.JobPostingId && jp.Status == JobStatus.Active);

            if (jobPosting == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Bài đăng không tồn tại hoặc đã hết hạn." });
                }
                TempData["ErrorMessage"] = "Bài đăng không tồn tại hoặc đã hết hạn.";
                return RedirectToAction("Dashboard", "Interpreters");
            }

            // Kiểm tra phiên dịch viên
            var interpreter = await _context.Interpreters
                .FirstOrDefaultAsync(i => i.InterpreterId == application.InterpreterId && i.UserId == user.Id);

            if (interpreter == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Thông tin phiên dịch viên không hợp lệ." });
                }
                TempData["ErrorMessage"] = "Thông tin phiên dịch viên không hợp lệ.";
                return RedirectToAction("Profile", "Interpreters");
            }

            // Xử lý upload file CV
            string uniqueFileName = null;

            if (cvFile != null && cvFile.Length > 0)
            {
                try
                {
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "cv");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Tạo tên file an toàn
                    uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(cvFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    System.Diagnostics.Debug.WriteLine($"Saving file to: {filePath}");

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await cvFile.CopyToAsync(fileStream);
                    }

                    System.Diagnostics.Debug.WriteLine("File copied successfully");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error uploading file: {ex.Message}");
                    ModelState.AddModelError("", $"Lỗi khi tải file: {ex.Message}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No file uploaded or file is empty");
            }

            // Xóa validation errors cho navigation properties 
            ModelState.Remove("JobPosting");
            ModelState.Remove("Interpreter");
            ModelState.Remove("User");
            
            // Log ModelState errors
            if (!ModelState.IsValid)
            {
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"ModelState Error: {error.ErrorMessage}");
                    }
                }

                // Khi sử dụng Ajax, không nhất thiết phải check ModelState.IsValid, 
                // vì đây là quá trình áp dụng và có thể chấp nhận dữ liệu không hoàn chỉnh
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    // Bỏ qua validation errors cho Ajax request
                    // return Json(new { success = false, message = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại thông tin." });
                }
            }

            // Tạo một JobApplication mới để đảm bảo tất cả trường đều có giá trị
            var newApplication = new JobApplication
            {
                JobPostingId = application.JobPostingId,
                InterpreterId = application.InterpreterId,
                UserId = user.Id,
                InterpreterName = interpreter.InterpreterName,
                PhoneNumber = !string.IsNullOrEmpty(application.PhoneNumber) ? application.PhoneNumber : user.PhoneNumber ?? "",
                Email = !string.IsNullOrEmpty(application.Email) ? application.Email : user.Email,
                Status = ApplicationStatus.Pending,
                ApplicationDate = DateTime.Now,
                CVFilePath = uniqueFileName  // Gán tên file vào đây
            };

            try
            {
                System.Diagnostics.Debug.WriteLine("Adding application to database");
                _context.JobApplications.Add(newApplication);
                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine("Application saved successfully");

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = "Đã gửi đơn ứng tuyển thành công." });
                }

                TempData["SuccessMessage"] = "Đã gửi đơn ứng tuyển thành công.";
                return RedirectToAction("MyApplications");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database Error: {ex.Message}");
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = $"Lỗi khi gửi đơn ứng tuyển: {ex.Message}" });
                }
                ModelState.AddModelError("", $"Lỗi khi gửi đơn ứng tuyển: {ex.Message}");
                return View(application);
            }
        }

        // GET: JobApply/MyApplications
        [Authorize]
        public async Task<IActionResult> MyApplications()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != 1)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Lấy thông tin phiên dịch viên
            var interpreter = await _context.Interpreters
                .FirstOrDefaultAsync(i => i.UserId == user.Id);

            if (interpreter == null)
            {
                TempData["ErrorMessage"] = "Bạn cần tạo hồ sơ phiên dịch viên.";
                return RedirectToAction("Profile", "Interpreters");
            }

            // Lấy danh sách đơn ứng tuyển VÀ THÊM INCLUDE CHO RECRUITER
            var applications = await _context.JobApplications
                .Where(a => a.InterpreterId == interpreter.InterpreterId)
                .Include(a => a.JobPosting)
                    .ThenInclude(jp => jp.Recruiter) // Thêm dòng này để load Recruiter
                .OrderByDescending(a => a.ApplicationDate)
                .ToListAsync();

            return View(applications);
        }


        // GET: JobApply/ApplicationDetails/5
        [Authorize]
        public async Task<IActionResult> ApplicationDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != 1)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Lấy interpreter của người dùng hiện tại
            var interpreter = await _context.Interpreters
                .FirstOrDefaultAsync(i => i.UserId == user.Id);

            if (interpreter == null)
            {
                TempData["ErrorMessage"] = "Bạn cần tạo hồ sơ phiên dịch viên.";
                return RedirectToAction("Profile", "Interpreters");
            }

            // Lấy đơn ứng tuyển cùng với thông tin bài đăng và nhà tuyển dụng
            var application = await _context.JobApplications
                .Include(a => a.JobPosting)
                    .ThenInclude(j => j.Recruiter)
                .FirstOrDefaultAsync(a => a.JobApplicationId == id && a.InterpreterId == interpreter.InterpreterId);

            if (application == null)
            {
                return NotFound();
            }

            return View(application);
        }

        // GET: JobApply/DeleteApplication/5
        [Authorize]
        public async Task<IActionResult> DeleteApplication(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != 1)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var interpreter = await _context.Interpreters
                .FirstOrDefaultAsync(i => i.UserId == user.Id);

            if (interpreter == null)
            {
                TempData["ErrorMessage"] = "Bạn cần tạo hồ sơ phiên dịch viên.";
                return RedirectToAction("Profile", "Interpreters");
            }

            // Lấy đơn ứng tuyển
            var application = await _context.JobApplications
                .FirstOrDefaultAsync(a => a.JobApplicationId == id && a.InterpreterId == interpreter.InterpreterId);

            if (application == null)
            {
                return NotFound();
            }

            // Chỉ cho phép xóa đơn ở trạng thái chờ phản hồi
            if (application.Status != ApplicationStatus.Pending)
            {
                TempData["ErrorMessage"] = "Chỉ có thể hủy đơn ứng tuyển ở trạng thái chờ phản hồi.";
                return RedirectToAction(nameof(MyApplications));
            }

            try
            {
                _context.JobApplications.Remove(application);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã hủy đơn ứng tuyển thành công.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi hủy đơn ứng tuyển: {ex.Message}";
            }

            return RedirectToAction(nameof(MyApplications));
        }

    }
}

