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

        // GET: Interpreters/Dashboard
        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            // Get interpreter profile for this user
            var interpreter = await _context.Interpreters
                .FirstOrDefaultAsync(i => i.UserId == user.Id);

            // Get active jobs
            var activeJobs = await _context.JobPostings
                .Include(j => j.Recruiter)
                .Where(jp => jp.Status == JobStatus.Active && jp.ExpiryDate >= DateTime.Now)
                .OrderByDescending(jp => jp.PostedDate)
                .ToListAsync();

            // Identify urgent jobs (expiring in 24 hours)
            var urgentJobs = activeJobs
                .Where(j => (j.ExpiryDate - DateTime.Now).TotalHours < 24)
                .ToList();

            // Set up recommended jobs based on interpreter profile
            var recommendedJobs = activeJobs;

            // If interpreter profile exists, improve recommendations
            if (interpreter != null)
            {
                // Prioritize jobs that match interpreter's location
                recommendedJobs = activeJobs
                    .OrderByDescending(j => j.WorkLocation.Contains(interpreter.WorkLocation))
                    .ThenByDescending(j => j.PostedDate)
                    .Take(10)
                    .ToList();
            }

            // Pass data to view
            ViewBag.UrgentJobs = urgentJobs;
            ViewBag.RecommendedJobs = recommendedJobs;
            ViewBag.UserLocation = interpreter?.WorkLocation ?? "Hà Nội";
            ViewBag.UserPoints = 4; // Placeholder, replace with actual points system

            // Return MobileDashboard view with job data
            return View("HomeMobile", activeJobs);
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
                .Include(i => i.Educations)
                .Include(i => i.WorkExperiences)
                .Include(i => i.Projects)
                .FirstOrDefaultAsync(m => m.UserId == user.Id);

            if (interpreter == null)
            {
                interpreter = new Interpreter { UserId = user.Id };
                ViewBag.HasProfile = false;
            }
            else
            {
                ViewBag.HasProfile = true;
                var applications = await _context.JobApplications
                    .Where(a => a.InterpreterId == interpreter.InterpreterId)
                    .Include(a => a.JobPosting)
                        .ThenInclude(jp => jp.Recruiter)
                    .OrderByDescending(a => a.ApplicationDate)
                    .Select(a => new {
                        JobApplicationId = a.JobApplicationId,
                        StatusText = a.Status.ToString(),
                        JobTitle = a.JobPosting.Title,
                        RecruiterName = a.JobPosting.Recruiter.RecruiterName,
                        ApplicationDate = a.ApplicationDate,
                        CVFilePath = a.CVFilePath,
                        HasFeedback = a.Status != ApplicationStatus.Pending
                    })
                    .ToListAsync();
                
                ViewBag.JobApplications = applications;

                var activeJobs = await _context.JobPostings
                .Include(j => j.Recruiter)
                .Where(jp => jp.Status == JobStatus.Active && jp.ExpiryDate >= DateTime.Now)
                .OrderByDescending(jp => jp.PostedDate)
                .ToListAsync();

                var recommendedJobs = activeJobs;
                ViewBag.RecommendedJobs = recommendedJobs;

            }



            // Tính toán số lượng view profile
            ViewBag.WeeklyViews = 0;
            ViewBag.MonthlyViews = 0;
            ViewBag.YearlyViews = 0;

            // Tính toán mức độ hoàn thiện hồ sơ
            ViewBag.ProfileCompletionPercentage = CalculateProfileCompletionPercentage(interpreter);

            return View("ProfileMobile", interpreter);
        }

        // Helper method to calculate profile completion percentage
        private int CalculateProfileCompletionPercentage(Interpreter interpreter)
        {
            if (interpreter == null) return 0;

            int totalFields = 8; // Base fields: Name, DOB, Gender, WorkLocation, DetailedAddress, YearsOfExperience, AvatarPath, Skills
            int completedFields = 0;

            // Check basic fields
            if (!string.IsNullOrEmpty(interpreter.InterpreterName)) completedFields++;
            if (interpreter.DateOfBirth != default) completedFields++;
            if (!string.IsNullOrEmpty(interpreter.Gender)) completedFields++;
            if (!string.IsNullOrEmpty(interpreter.WorkLocation)) completedFields++;
            if (!string.IsNullOrEmpty(interpreter.DetailedAddress)) completedFields++;
            // YearsOfExperience is an enum, so it's always set
            completedFields++;
            if (!string.IsNullOrEmpty(interpreter.AvatarPath)) completedFields++;
            if (!string.IsNullOrEmpty(interpreter.Skills)) completedFields++;

            // Add extra points for having associated data
            if (interpreter.Educations?.Count > 0) totalFields += 2; // Education worth more
            if (interpreter.Educations?.Count > 0) completedFields += 2;

            if (interpreter.WorkExperiences?.Count > 0) totalFields += 3; // Work experience worth more
            if (interpreter.WorkExperiences?.Count > 0) completedFields += 3;

            if (interpreter.Projects?.Count > 0) totalFields += 2; // Projects worth more
            if (interpreter.Projects?.Count > 0) completedFields += 2;

            return (int)((double)completedFields / totalFields * 100);
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
                    .Include(i => i.Educations)
                    .Include(i => i.WorkExperiences)
                    .Include(i => i.Projects)
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
                    existingInterpreter.Skills = interpreter.Skills;

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

        #region Education Management

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEducation(Education education)
        {
            // Xóa lỗi validation cho trường Interpreter nếu có
            if (ModelState.ContainsKey("Interpreter"))
            {
                ModelState.Remove("Interpreter");
            }

            // In ra log để debug model binding
            System.Diagnostics.Debug.WriteLine($"Received Education: ID={education.EducationId}, SchoolName={education.SchoolName}, StartDate={education.StartDate}, EndDate={education.EndDate}");

            // Xóa các lỗi validation liên quan đến EndDate
            if (ModelState.ContainsKey("EndDate"))
            {
                ModelState.Remove("EndDate");
            }

            // Kiểm tra xem yêu cầu có phải là AJAX
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            // Thiết lập InterpreterId
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null || user.Role != 1)
                {
                    if (isAjax)
                    {
                        return Json(new { success = false, message = "Vui lòng đăng nhập để thực hiện chức năng này" });
                    }
                    return RedirectToAction("Login", "Account");
                }

                var interpreter = await _context.Interpreters
                    .FirstOrDefaultAsync(i => i.UserId == user.Id);

                if (interpreter == null)
                {
                    if (isAjax)
                    {
                        return Json(new { success = false, message = "Không tìm thấy hồ sơ phiên dịch viên" });
                    }
                    return NotFound("Không tìm thấy hồ sơ phiên dịch viên");
                }

                education.InterpreterId = interpreter.InterpreterId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting interpreter: {ex.Message}");
                if (isAjax)
                {
                    return Json(new { success = false, message = "Có lỗi xảy ra khi xác thực người dùng" });
                }
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xác thực người dùng.";
                return RedirectToAction(nameof(Profile));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Điều chỉnh ngày kết thúc nếu cần
                    if (education.EndDate == DateTime.MinValue || education.EndDate == default)
                    {
                        education.EndDate = null;
                    }

                    _context.Educations.Add(education);
                    await _context.SaveChangesAsync();

                    if (isAjax)
                    {
                        // Lấy lại giá trị education với ID đã được tạo
                        var createdEducation = await _context.Educations
                            .FirstOrDefaultAsync(e => e.EducationId == education.EducationId);
                        
                        return Json(new { 
                            success = true, 
                            message = "Thêm thông tin học vấn thành công.",
                            item = new { 
                                educationId = createdEducation.EducationId,
                                schoolName = createdEducation.SchoolName,
                                degree = createdEducation.Degree,
                                major = createdEducation.Major,
                                startDate = createdEducation.StartDate,
                                endDate = createdEducation.EndDate,
                                description = createdEducation.Description
                            }
                        });
                    }
                    else
                    {
                        TempData["StatusMessage"] = "Thêm thông tin học vấn thành công.";
                        return RedirectToAction(nameof(Profile));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error adding education: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    
                    if (isAjax)
                    {
                        return Json(new { success = false, message = $"Không thể lưu dữ liệu: {ex.Message}" });
                    }
                    else
                    {
                        TempData["ErrorMessage"] = $"Không thể lưu dữ liệu: {ex.Message}";
                    }
                }
            }
            else
            {
                // Log lỗi validation để debug
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"ModelState Error: {error.ErrorMessage}");
                    }
                }
                
                if (isAjax)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại." });
                }
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
            }

            if (isAjax)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra, không thể lưu dữ liệu." });
            }
            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEducation(Education education)
        {
            // Xóa lỗi validation cho trường Interpreter nếu có
            if (ModelState.ContainsKey("Interpreter"))
            {
                ModelState.Remove("Interpreter");
            }

            // Kiểm tra xem yêu cầu có phải là AJAX
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user == null || user.Role != 1)
                    {
                        if (isAjax)
                        {
                            return Json(new { success = false, message = "Vui lòng đăng nhập để thực hiện chức năng này" });
                        }
                        return RedirectToAction("Login", "Account");
                    }

                    var existingEducation = await _context.Educations
                        .Include(e => e.Interpreter)
                        .FirstOrDefaultAsync(e => e.EducationId == education.EducationId);

                    if (existingEducation == null)
                    {
                        if (isAjax)
                        {
                            return Json(new { success = false, message = "Không tìm thấy thông tin học vấn" });
                        }
                        return NotFound("Không tìm thấy thông tin học vấn");
                    }

                    // Kiểm tra quyền sở hữu
                    if (existingEducation.Interpreter.UserId != user.Id)
                    {
                        if (isAjax)
                        {
                            return Json(new { success = false, message = "Bạn không có quyền chỉnh sửa thông tin này" });
                        }
                        return Forbid();
                    }

                    // Cập nhật các trường
                    existingEducation.SchoolName = education.SchoolName;
                    existingEducation.Major = education.Major;
                    existingEducation.Degree = education.Degree;
                    existingEducation.StartDate = education.StartDate;

                    // Xử lý EndDate đặc biệt để tránh lỗi DateTime.MinValue
                    if (education.EndDate == DateTime.MinValue)
                    {
                        existingEducation.EndDate = null;
                    }
                    else
                    {
                        existingEducation.EndDate = education.EndDate;
                    }

                    existingEducation.Description = education.Description;

                    _context.Update(existingEducation);
                    await _context.SaveChangesAsync();

                    if (isAjax)
                    {
                        return Json(new { 
                            success = true, 
                            message = "Cập nhật thông tin học vấn thành công",
                            item = new {
                                educationId = existingEducation.EducationId,
                                schoolName = existingEducation.SchoolName,
                                degree = existingEducation.Degree,
                                major = existingEducation.Major,
                                startDate = existingEducation.StartDate,
                                endDate = existingEducation.EndDate,
                                description = existingEducation.Description
                            }
                        });
                    }

                    TempData["StatusMessage"] = "Cập nhật thông tin học vấn thành công.";
                }
                catch (Exception ex)
                {
                    // Log lỗi để debug
                    System.Diagnostics.Debug.WriteLine($"Error editing education: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    
                    if (isAjax)
                    {
                        return Json(new { success = false, message = $"Không thể cập nhật thông tin học vấn: {ex.Message}" });
                    }
                    
                    TempData["ErrorMessage"] = $"Không thể cập nhật thông tin học vấn: {ex.Message}";
                }
            }
            else
            {
                // Log lỗi validation
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"ModelState Error: {error.ErrorMessage}");
                    }
                }
                
                if (isAjax)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại." });
                }
                
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
            }

            if (isAjax)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra, không thể cập nhật dữ liệu." });
            }
            
            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEducation(int id)
        {
            // Kiểm tra xem yêu cầu có phải là AJAX không
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            
            try
            {
                var education = await _context.Educations
                    .Include(e => e.Interpreter)
                    .FirstOrDefaultAsync(e => e.EducationId == id);

                if (education == null)
                {
                    if (isAjax)
                    {
                        return Json(new { success = false, message = "Không tìm thấy thông tin học vấn" });
                    }
                    return NotFound();
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null || education.Interpreter.UserId != user.Id)
                {
                    if (isAjax)
                    {
                        return Json(new { success = false, message = "Bạn không có quyền xóa thông tin này" });
                    }
                    return Forbid();
                }

                _context.Educations.Remove(education);
                await _context.SaveChangesAsync();

                if (isAjax)
                {
                    return Json(new { success = true, message = "Xóa thông tin học vấn thành công" });
                }

                TempData["StatusMessage"] = "Xóa thông tin học vấn thành công.";
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                // Log lỗi
                System.Diagnostics.Debug.WriteLine($"Error deleting education: {ex.Message}");
                
                if (isAjax)
                {
                    return Json(new { success = false, message = $"Không thể xóa thông tin học vấn: {ex.Message}" });
                }
                
                TempData["ErrorMessage"] = $"Không thể xóa thông tin học vấn: {ex.Message}";
                return RedirectToAction(nameof(Profile));
            }
        }

        #endregion

        #region Work Experience Management

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddWorkExperience(WorkExperience workExperience)
        {
            // Xóa lỗi validation cho trường Interpreter nếu có
            if (ModelState.ContainsKey("Interpreter"))
            {
                ModelState.Remove("Interpreter");
            }
            
            // Kiểm tra xem yêu cầu có phải là AJAX
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user == null || user.Role != 1)
                    {
                        if (isAjax)
                        {
                            return Json(new { success = false, message = "Vui lòng đăng nhập để thực hiện chức năng này" });
                        }
                        return RedirectToAction("Login", "Account");
                    }

                    var interpreter = await _context.Interpreters
                        .FirstOrDefaultAsync(i => i.UserId == user.Id);

                    if (interpreter == null)
                    {
                        if (isAjax)
                        {
                            return Json(new { success = false, message = "Không tìm thấy hồ sơ phiên dịch viên" });
                        }
                        return NotFound("Không tìm thấy hồ sơ phiên dịch viên");
                    }

                    workExperience.InterpreterId = interpreter.InterpreterId;

                    // Điều chỉnh ngày kết thúc nếu cần
                    if (workExperience.EndDate == DateTime.MinValue)
                    {
                        workExperience.EndDate = null;
                    }

                    _context.WorkExperiences.Add(workExperience);
                    await _context.SaveChangesAsync();
                    
                    if (isAjax)
                    {
                        // Lấy lại giá trị workExperience với ID đã được tạo
                        var createdExperience = await _context.WorkExperiences
                            .FirstOrDefaultAsync(e => e.WorkExperienceId == workExperience.WorkExperienceId);
                        
                        return Json(new { 
                            success = true, 
                            message = "Thêm kinh nghiệm làm việc thành công.",
                            item = new {
                                workExperienceId = createdExperience.WorkExperienceId,
                                jobTitle = createdExperience.JobTitle,
                                companyName = createdExperience.CompanyName,
                                startDate = createdExperience.StartDate,
                                endDate = createdExperience.EndDate,
                                jobDescription = createdExperience.JobDescription
                            }
                        });
                    }

                    TempData["StatusMessage"] = "Thêm kinh nghiệm làm việc thành công.";
                    return RedirectToAction(nameof(Profile));
                }
                catch (Exception ex)
                {
                    // Log lỗi để debug
                    System.Diagnostics.Debug.WriteLine($"Error adding work experience: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    
                    if (isAjax)
                    {
                        return Json(new { success = false, message = $"Không thể lưu dữ liệu: {ex.Message}" });
                    }
                    
                    ModelState.AddModelError("", $"Không thể lưu dữ liệu: {ex.Message}");
                    TempData["ErrorMessage"] = $"Không thể lưu dữ liệu: {ex.Message}";
                }
            }
            else
            {
                // Log lỗi validation để debug
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"ModelState Error: {error.ErrorMessage}");
                    }
                }
                
                if (isAjax)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại." });
                }
            }

            if (isAjax)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra, không thể lưu dữ liệu." });
            }
            
            // Nếu có lỗi, vẫn redirect để tránh hiển thị lỗi 500
            TempData["ErrorMessage"] = "Không thể thêm kinh nghiệm làm việc. Vui lòng kiểm tra dữ liệu nhập.";
            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditWorkExperience(WorkExperience workExperience)
        {
            // Xóa lỗi validation cho trường Interpreter nếu có
            if (ModelState.ContainsKey("Interpreter"))
            {
                ModelState.Remove("Interpreter");
            }
            
            // Kiểm tra xem yêu cầu có phải là AJAX
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user == null || user.Role != 1)
                    {
                        if (isAjax)
                        {
                            return Json(new { success = false, message = "Vui lòng đăng nhập để thực hiện chức năng này" });
                        }
                        return RedirectToAction("Login", "Account");
                    }

                    var existingExperience = await _context.WorkExperiences
                        .Include(e => e.Interpreter)
                        .FirstOrDefaultAsync(e => e.WorkExperienceId == workExperience.WorkExperienceId);

                    if (existingExperience == null)
                    {
                        if (isAjax)
                        {
                            return Json(new { success = false, message = "Không tìm thấy thông tin kinh nghiệm làm việc" });
                        }
                        return NotFound("Không tìm thấy thông tin kinh nghiệm làm việc");
                    }

                    // Kiểm tra quyền sở hữu
                    if (existingExperience.Interpreter.UserId != user.Id)
                    {
                        if (isAjax)
                        {
                            return Json(new { success = false, message = "Bạn không có quyền chỉnh sửa thông tin này" });
                        }
                        return Forbid();
                    }

                    // Cập nhật các trường
                    existingExperience.JobTitle = workExperience.JobTitle;
                    existingExperience.CompanyName = workExperience.CompanyName;
                    existingExperience.StartDate = workExperience.StartDate;

                    // Xử lý EndDate đặc biệt để tránh lỗi DateTime.MinValue
                    if (workExperience.EndDate == DateTime.MinValue)
                    {
                        existingExperience.EndDate = null;
                    }
                    else
                    {
                        existingExperience.EndDate = workExperience.EndDate;
                    }

                    existingExperience.JobDescription = workExperience.JobDescription;

                    _context.Update(existingExperience);
                    await _context.SaveChangesAsync();
                    
                    if (isAjax)
                    {
                        return Json(new { 
                            success = true, 
                            message = "Cập nhật kinh nghiệm làm việc thành công", 
                            item = new {
                                workExperienceId = existingExperience.WorkExperienceId,
                                jobTitle = existingExperience.JobTitle,
                                companyName = existingExperience.CompanyName,
                                startDate = existingExperience.StartDate,
                                endDate = existingExperience.EndDate,
                                jobDescription = existingExperience.JobDescription
                            }
                        });
                    }

                    TempData["StatusMessage"] = "Cập nhật kinh nghiệm làm việc thành công.";
                }
                catch (Exception ex)
                {
                    // Log lỗi để debug
                    System.Diagnostics.Debug.WriteLine($"Error editing work experience: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    
                    if (isAjax)
                    {
                        return Json(new { success = false, message = $"Không thể cập nhật kinh nghiệm làm việc: {ex.Message}" });
                    }
                    
                    TempData["ErrorMessage"] = $"Không thể cập nhật kinh nghiệm làm việc: {ex.Message}";
                }
            }
            else
            {
                // Log lỗi validation
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"ModelState Error: {error.ErrorMessage}");
                    }
                }
                
                if (isAjax)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại." });
                }
                
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
            }

            if (isAjax)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra, không thể cập nhật dữ liệu." });
            }
            
            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteWorkExperience(int id)
        {
            // Kiểm tra xem yêu cầu có phải là AJAX không
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            
            try
            {
                var workExperience = await _context.WorkExperiences
                    .Include(e => e.Interpreter)
                    .FirstOrDefaultAsync(e => e.WorkExperienceId == id);

                if (workExperience == null)
                {
                    if (isAjax)
                    {
                        return Json(new { success = false, message = "Không tìm thấy thông tin kinh nghiệm làm việc" });
                    }
                    return NotFound();
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null || workExperience.Interpreter.UserId != user.Id)
                {
                    if (isAjax)
                    {
                        return Json(new { success = false, message = "Bạn không có quyền xóa thông tin này" });
                    }
                    return Forbid();
                }

                _context.WorkExperiences.Remove(workExperience);
                await _context.SaveChangesAsync();

                if (isAjax)
                {
                    return Json(new { success = true, message = "Xóa kinh nghiệm làm việc thành công" });
                }

                TempData["StatusMessage"] = "Xóa kinh nghiệm làm việc thành công.";
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                // Log lỗi
                System.Diagnostics.Debug.WriteLine($"Error deleting work experience: {ex.Message}");
                
                if (isAjax)
                {
                    return Json(new { success = false, message = $"Không thể xóa kinh nghiệm làm việc: {ex.Message}" });
                }
                
                TempData["ErrorMessage"] = $"Không thể xóa kinh nghiệm làm việc: {ex.Message}";
                return RedirectToAction(nameof(Profile));
            }
        }

        #endregion

        #region Project/Achievement Management

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProject(Project project)
        {
            // Xóa lỗi validation cho trường Interpreter nếu có
            if (ModelState.ContainsKey("Interpreter"))
            {
                ModelState.Remove("Interpreter");
            }
            
            // Kiểm tra xem yêu cầu có phải là AJAX
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user == null || user.Role != 1)
                    {
                        if (isAjax)
                        {
                            return Json(new { success = false, message = "Vui lòng đăng nhập để thực hiện chức năng này" });
                        }
                        return RedirectToAction("Login", "Account");
                    }

                    var interpreter = await _context.Interpreters
                        .FirstOrDefaultAsync(i => i.UserId == user.Id);

                    if (interpreter == null)
                    {
                        if (isAjax)
                        {
                            return Json(new { success = false, message = "Không tìm thấy hồ sơ phiên dịch viên" });
                        }
                        return NotFound("Không tìm thấy hồ sơ phiên dịch viên");
                    }

                    project.InterpreterId = interpreter.InterpreterId;

                    // Điều chỉnh ngày kết thúc nếu cần
                    if (project.EndDate == DateTime.MinValue)
                    {
                        project.EndDate = null;
                    }

                    _context.Projects.Add(project);
                    await _context.SaveChangesAsync();
                    
                    if (isAjax)
                    {
                        // Lấy lại giá trị project với ID đã được tạo
                        var createdProject = await _context.Projects
                            .FirstOrDefaultAsync(p => p.ProjectId == project.ProjectId);
                        
                        return Json(new { 
                            success = true, 
                            message = "Thêm dự án/thành tựu thành công.",
                            item = new {
                                projectId = createdProject.ProjectId,
                                projectName = createdProject.ProjectName,
                                startDate = createdProject.StartDate,
                                endDate = createdProject.EndDate,
                                description = createdProject.Description
                            }
                        });
                    }

                    TempData["StatusMessage"] = "Thêm dự án/thành tựu thành công.";
                    return RedirectToAction(nameof(Profile));
                }
                catch (Exception ex)
                {
                    // Log lỗi để debug
                    System.Diagnostics.Debug.WriteLine($"Error adding project: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    
                    if (isAjax)
                    {
                        return Json(new { success = false, message = $"Không thể lưu dữ liệu: {ex.Message}" });
                    }
                    
                    ModelState.AddModelError("", $"Không thể lưu dữ liệu: {ex.Message}");
                    TempData["ErrorMessage"] = $"Không thể lưu dữ liệu: {ex.Message}";
                }
            }
            else
            {
                // Log lỗi validation để debug
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"ModelState Error: {error.ErrorMessage}");
                    }
                }
                
                if (isAjax)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại." });
                }
            }

            if (isAjax)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra, không thể lưu dữ liệu." });
            }
            
            // Nếu có lỗi, vẫn redirect để tránh hiển thị lỗi 500
            TempData["ErrorMessage"] = "Không thể thêm dự án/thành tựu. Vui lòng kiểm tra dữ liệu nhập.";
            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProject(Project project)
        {
            // Xóa lỗi validation cho trường Interpreter nếu có
            if (ModelState.ContainsKey("Interpreter"))
            {
                ModelState.Remove("Interpreter");
            }
            
            // Kiểm tra xem yêu cầu có phải là AJAX
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user == null || user.Role != 1)
                    {
                        if (isAjax)
                        {
                            return Json(new { success = false, message = "Vui lòng đăng nhập để thực hiện chức năng này" });
                        }
                        return RedirectToAction("Login", "Account");
                    }

                    var existingProject = await _context.Projects
                        .Include(p => p.Interpreter)
                        .FirstOrDefaultAsync(p => p.ProjectId == project.ProjectId);

                    if (existingProject == null)
                    {
                        if (isAjax)
                        {
                            return Json(new { success = false, message = "Không tìm thấy thông tin dự án/thành tựu" });
                        }
                        return NotFound("Không tìm thấy thông tin dự án/thành tựu");
                    }

                    // Kiểm tra quyền sở hữu
                    if (existingProject.Interpreter.UserId != user.Id)
                    {
                        if (isAjax)
                        {
                            return Json(new { success = false, message = "Bạn không có quyền chỉnh sửa thông tin này" });
                        }
                        return Forbid();
                    }

                    // Cập nhật các trường
                    existingProject.ProjectName = project.ProjectName;
                    existingProject.StartDate = project.StartDate;

                    // Xử lý EndDate đặc biệt để tránh lỗi DateTime.MinValue
                    if (project.EndDate == DateTime.MinValue)
                    {
                        existingProject.EndDate = null;
                    }
                    else
                    {
                        existingProject.EndDate = project.EndDate;
                    }

                    existingProject.Description = project.Description;

                    _context.Update(existingProject);
                    await _context.SaveChangesAsync();
                    
                    if (isAjax)
                    {
                        return Json(new { 
                            success = true, 
                            message = "Cập nhật dự án/thành tựu thành công", 
                            item = new {
                                projectId = existingProject.ProjectId,
                                projectName = existingProject.ProjectName,
                                startDate = existingProject.StartDate,
                                endDate = existingProject.EndDate,
                                description = existingProject.Description
                            }
                        });
                    }

                    TempData["StatusMessage"] = "Cập nhật dự án/thành tựu thành công.";
                }
                catch (Exception ex)
                {
                    // Log lỗi để debug
                    System.Diagnostics.Debug.WriteLine($"Error editing project: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    
                    if (isAjax)
                    {
                        return Json(new { success = false, message = $"Không thể cập nhật dự án/thành tựu: {ex.Message}" });
                    }
                    
                    TempData["ErrorMessage"] = $"Không thể cập nhật dự án/thành tựu: {ex.Message}";
                }
            }
            else
            {
                // Log lỗi validation
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"ModelState Error: {error.ErrorMessage}");
                    }
                }
                
                if (isAjax)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại." });
                }
                
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
            }

            if (isAjax)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra, không thể cập nhật dữ liệu." });
            }
            
            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProject(int id)
        {
            // Kiểm tra xem yêu cầu có phải là AJAX không
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            try
            {
                var project = await _context.Projects
                    .Include(p => p.Interpreter)
                    .FirstOrDefaultAsync(p => p.ProjectId == id);

                if (project == null)
                {
                    if (isAjax)
                    {
                        return Json(new { success = false, message = "Không tìm thấy thông tin dự án/thành tựu" });
                    }
                    return NotFound();
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null || project.Interpreter.UserId != user.Id)
                {
                    if (isAjax)
                    {
                        return Json(new { success = false, message = "Bạn không có quyền xóa thông tin này" });
                    }
                    return Forbid();
                }

                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();

                if (isAjax)
                {
                    return Json(new { success = true, message = "Xóa dự án/thành tựu thành công" });
                }

                TempData["StatusMessage"] = "Xóa dự án/thành tựu thành công.";
                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                // Log lỗi
                System.Diagnostics.Debug.WriteLine($"Error deleting project: {ex.Message}");
                
                if (isAjax)
                {
                    return Json(new { success = false, message = $"Không thể xóa dự án/thành tựu: {ex.Message}" });
                }
                
                TempData["ErrorMessage"] = $"Không thể xóa dự án/thành tựu: {ex.Message}";
                return RedirectToAction(nameof(Profile));
            }
        }

        #endregion

        #region Skills Management

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSkills(string skills)
        {
            // Kiểm tra xem yêu cầu có phải là AJAX
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null || user.Role != 1)
                {
                    if (isAjax)
                    {
                        return Json(new { success = false, message = "Vui lòng đăng nhập để thực hiện chức năng này" });
                    }
                    return RedirectToAction("Login", "Account");
                }

                var interpreter = await _context.Interpreters
                    .FirstOrDefaultAsync(i => i.UserId == user.Id);

                if (interpreter == null)
                {
                    if (isAjax)
                    {
                        return Json(new { success = false, message = "Không tìm thấy hồ sơ phiên dịch viên" });
                    }
                    return NotFound("Không tìm thấy hồ sơ phiên dịch viên");
                }

                // Cập nhật kỹ năng
                interpreter.Skills = skills;
                _context.Update(interpreter);
                await _context.SaveChangesAsync();
                
                if (isAjax)
                {
                    return Json(new { 
                        success = true, 
                        message = "Cập nhật kỹ năng thành công",
                        skills = skills
                    });
                }

                TempData["StatusMessage"] = "Cập nhật kỹ năng thành công.";
            }
            catch (Exception ex)
            {
                // Log lỗi để debug
                System.Diagnostics.Debug.WriteLine($"Error updating skills: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                if (isAjax)
                {
                    return Json(new { success = false, message = $"Không thể cập nhật kỹ năng: {ex.Message}" });
                }
                
                TempData["ErrorMessage"] = $"Không thể cập nhật kỹ năng: {ex.Message}";
            }

            return RedirectToAction(nameof(Profile));
        }

        #endregion

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBasicInfo(Interpreter interpreter)
        {
            // Xóa lỗi validation cho các trường không liên quan
            if (ModelState.ContainsKey("User"))
            {
                ModelState.Remove("User");
                ModelState.Remove("UserId");
            }
            
            // Kiểm tra xem yêu cầu có phải là AJAX
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            
            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _userManager.GetUserAsync(User);
                    if (user == null || user.Role != 1)
                    {
                        if (isAjax)
                        {
                            return Json(new { success = false, message = "Vui lòng đăng nhập để thực hiện chức năng này" });
                        }
                        return RedirectToAction("Login", "Account");
                    }
                    
                    // Lấy thông tin interpreter hiện tại
                    var existingInterpreter = await _context.Interpreters
                        .FirstOrDefaultAsync(i => i.UserId == user.Id);
                        
                    if (existingInterpreter == null)
                    {
                        if (isAjax)
                        {
                            return Json(new { success = false, message = "Không tìm thấy hồ sơ phiên dịch viên" });
                        }
                        return NotFound("Không tìm thấy hồ sơ phiên dịch viên");
                    }
                    
                    // Cập nhật các trường thông tin cơ bản
                    existingInterpreter.InterpreterName = interpreter.InterpreterName;
                    existingInterpreter.DateOfBirth = interpreter.DateOfBirth;
                    existingInterpreter.Gender = interpreter.Gender;
                    existingInterpreter.Field = interpreter.Field;
                    existingInterpreter.WorkLocation = interpreter.WorkLocation;
                    existingInterpreter.DetailedAddress = interpreter.DetailedAddress;
                    existingInterpreter.YearsOfExperience = interpreter.YearsOfExperience;
                    
                    _context.Update(existingInterpreter);
                    await _context.SaveChangesAsync();
                    
                    // Tính toán mức độ hoàn thiện hồ sơ
                    var profileCompletionPercentage = CalculateProfileCompletionPercentage(existingInterpreter);
                    
                    // Chuyển ExperienceLevel thành văn bản 
                    string experienceText;
                    switch (existingInterpreter.YearsOfExperience)
                    {
                        case ExperienceLevel.LessThanOneYear: experienceText = "Dưới 1 năm"; break;
                        case ExperienceLevel.OneYear: experienceText = "1 năm"; break;
                        case ExperienceLevel.TwoYears: experienceText = "2 năm"; break;
                        case ExperienceLevel.ThreeYears: experienceText = "3 năm"; break;
                        case ExperienceLevel.FourYears: experienceText = "4 năm"; break;
                        case ExperienceLevel.FiveYears: experienceText = "5 năm"; break;
                        case ExperienceLevel.MoreThanFiveYears: experienceText = "Hơn 5 năm"; break;
                        default: experienceText = "Không xác định"; break;
                    }
                    
                    // Tính tuổi
                    int age = DateTime.Now.Year - existingInterpreter.DateOfBirth.Year;
                    if (DateTime.Now.DayOfYear < existingInterpreter.DateOfBirth.DayOfYear)
                    {
                        age--;
                    }
                    
                    if (isAjax)
                    {
                        return Json(new { 
                            success = true, 
                            message = "Cập nhật thông tin cơ bản thành công",
                            item = new {
                                interpreterId = existingInterpreter.InterpreterId,
                                interpreterName = existingInterpreter.InterpreterName,
                                gender = existingInterpreter.Gender,
                                age = age,
                                field = existingInterpreter.Field,
                                workLocation = existingInterpreter.WorkLocation,
                                detailedAddress = existingInterpreter.DetailedAddress,
                                experienceText = experienceText,
                                profileCompletionPercentage = profileCompletionPercentage
                            }
                        });
                    }
                    
                    TempData["StatusMessage"] = "Cập nhật thông tin cơ bản thành công.";
                    return RedirectToAction(nameof(Profile));
                }
                catch (Exception ex)
                {
                    // Log lỗi để debug
                    System.Diagnostics.Debug.WriteLine($"Error updating basic info: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    
                    if (isAjax)
                    {
                        return Json(new { success = false, message = $"Không thể cập nhật thông tin cơ bản: {ex.Message}" });
                    }
                    
                    TempData["ErrorMessage"] = $"Không thể cập nhật thông tin cơ bản: {ex.Message}";
                }
            }
            else
            {
                // Log lỗi validation
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"ModelState Error: {error.ErrorMessage}");
                    }
                }
                
                if (isAjax)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại." });
                }
                
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
            }
            
            return RedirectToAction(nameof(Profile));
        }

        // API để tải xuống profile dưới dạng PDF
        public async Task<IActionResult> DownloadProfilePdf()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != 1)
            {
                return RedirectToAction("Login", "Account");
            }

            var interpreter = await _context.Interpreters
                .Include(i => i.Educations)
                .Include(i => i.WorkExperiences)
                .Include(i => i.Projects)
                .FirstOrDefaultAsync(i => i.UserId == user.Id);

            if (interpreter == null)
            {
                return NotFound("Không tìm thấy hồ sơ phiên dịch viên");
            }

            // TODO: Implement PDF generation
            // This would require a PDF library like iTextSharp, DinkToPdf, or Syncfusion.Pdf

            // Placeholder response
            TempData["StatusMessage"] = "Tính năng đang được phát triển.";
            return RedirectToAction(nameof(Profile));
        }
    }
}
