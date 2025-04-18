using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZhaoYi_Test2.Data;
using ZhaoYi_Test2.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

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
        // Removed [Authorize] attribute to allow anonymous access
        public async Task<IActionResult> Dashboard()
        {
            // Đặt thông tin mặc định cho người dùng chưa đăng nhập
            string userId = null;
            Interpreter interpreter = null;
            int userRole = 0; // Default role for unauthenticated users
            
            // Kiểm tra người dùng đã đăng nhập hay chưa
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    userId = user.Id;
                    userRole = user.Role;
                    
                    // Chỉ lấy thông tin phiên dịch viên nếu là phiên dịch viên
                    if (user.Role == 1)
                    {
                        interpreter = await _context.Interpreters
                            .FirstOrDefaultAsync(i => i.UserId == user.Id);
                    }
                }
            }

            // Get active jobs
            var activeJobs = await _context.JobPostings
                .Include(j => j.Recruiter)
                .Where(jp => jp.Status == JobStatus.Active && jp.ExpiryDate >= DateTime.Now)
                .OrderByDescending(jp => jp.PostedDate)
                .ToListAsync();

            // Identify urgent jobs based on IsUrgent flag instead of expiry time
            var urgentJobs = activeJobs
                .Where(j => j.IsUrgent)
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
            ViewBag.UserRole = userRole; // Pass user role to view
            ViewBag.IsAuthenticated = User.Identity.IsAuthenticated; // Pass authentication status

            // Return MobileDashboard view with job data
            return View("HomeMobile", activeJobs);
        }

        // GET: Interpreters/Profile
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }
            if (user.Role != 1) // Chỉ cho Interpreter
            {
                if (user.Role == 0) return RedirectToAction("ChooseRole", "Role");
                return RedirectToAction("Index", "Home");
            }

            // Kiểm tra xem người dùng đã có hồ sơ phiên dịch viên chưa
            var interpreter = await _context.Interpreters
                .Include(i => i.Educations)
                .Include(i => i.WorkExperiences)
                .Include(i => i.Projects)
                .FirstOrDefaultAsync(m => m.UserId == user.Id);

            // *** THAY ĐỔI Ở ĐÂY: Nếu chưa có profile, chuyển đến trang tạo ***
            if (interpreter == null)
            {
                TempData["InfoMessage"] = "Vui lòng hoàn thiện hồ sơ phiên dịch viên của bạn.";
                return RedirectToAction(nameof(CreateProfile));
            }

            // --- Phần còn lại giữ nguyên ---
            ViewBag.HasProfile = true; // Đã có profile
                                       // Lấy Job Applications
            var applications = await _context.JobApplications
                .Where(a => a.InterpreterId == interpreter.InterpreterId)
                .Include(a => a.JobPosting)
                    .ThenInclude(jp => jp.Recruiter)
                .OrderByDescending(a => a.ApplicationDate)
                .Select(a => new { // Chọn các trường cần thiết
                    JobApplicationId = a.JobApplicationId,
                    StatusText = GetAppStatusDisplayNameStatic(a.Status), // Sử dụng hàm static helper nếu có
                    JobTitle = a.JobPosting.Title,
                    RecruiterName = a.JobPosting.Recruiter.RecruiterName,
                    ApplicationDate = a.ApplicationDate,
                    CVFilePath = a.CVFilePath,
                    HasFeedback = a.Status != ApplicationStatus.Pending
                })
                .ToListAsync();
            ViewBag.JobApplications = applications;

            // Lấy Recommended Jobs
            var activeJobs = await _context.JobPostings
                .Include(j => j.Recruiter)
                .Where(jp => jp.Status == JobStatus.Active && jp.ExpiryDate >= DateTime.Now)
                .OrderByDescending(jp => jp.PostedDate)
                .ToListAsync();

            var recommendedJobs = activeJobs
                 .OrderByDescending(j => j.WorkLocation.Contains(interpreter.WorkLocation)) // Ưu tiên vị trí
                 .ThenByDescending(j => j.PostedDate)
                 .Take(5) // Lấy 5 job gợi ý
                 .ToList();
            ViewBag.RecommendedJobs = recommendedJobs;


            // Tính toán số lượng view profile (Placeholder)
            ViewBag.WeeklyViews = 15; ViewBag.MonthlyViews = 62; ViewBag.YearlyViews = 310;
            // Tính toán mức độ hoàn thiện hồ sơ
            ViewBag.ProfileCompletionPercentage = CalculateProfileCompletionPercentage(interpreter);

            ViewData["UserRole"] = "Interpreter";
            ViewData["ShowBottomNav"] = true;
            return View("ProfileMobile", interpreter);
        }

        // Hàm static helper để lấy display name (có thể đặt ở class khác)
        public static string GetAppStatusDisplayNameStatic(ApplicationStatus status)
        {
            var field = status.GetType().GetField(status.ToString());
            var displayAttribute = field?.GetCustomAttribute<DisplayAttribute>();
            return displayAttribute?.Name ?? status.ToString();
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

        // POST: Interpreters/Profile (Chỉ để UPDATE)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(Interpreter interpreter, IFormFile? avatarFile, bool removeAvatar = false)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != 1)
            {
                return RedirectToAction("Index", "Home");
            }

            // --- Lấy hồ sơ Interpreter hiện có ---
            var existingInterpreter = await _context.Interpreters
                .Include(i => i.Educations) // Include nếu cần kiểm tra gì đó liên quan
                .Include(i => i.WorkExperiences)
                .Include(i => i.Projects)
                .FirstOrDefaultAsync(m => m.UserId == user.Id);

            // Nếu không tìm thấy hồ sơ để cập nhật
            if (existingInterpreter == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hồ sơ để cập nhật.";
                return RedirectToAction(nameof(CreateProfile)); // Chuyển về trang tạo
            }

            // --- Gán lại ID và UserId từ bản ghi hiện có ---
            interpreter.InterpreterId = existingInterpreter.InterpreterId;
            interpreter.UserId = existingInterpreter.UserId;

            // --- Xử lý Avatar (tương tự Recruiter) ---
            string currentAvatarPath = existingInterpreter.AvatarPath;
            string uniqueFileName = null;
            try
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "avatars");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                if (removeAvatar && !string.IsNullOrEmpty(currentAvatarPath))
                {
                    var oldFilePath = Path.Combine(uploadsFolder, currentAvatarPath);
                    if (System.IO.File.Exists(oldFilePath)) System.IO.File.Delete(oldFilePath);
                    interpreter.AvatarPath = null;
                    currentAvatarPath = null;
                }
                else if (avatarFile != null && avatarFile.Length > 0)
                {
                    // Check size/type
                    if (avatarFile.Length > 2 * 1024 * 1024) { ModelState.AddModelError("avatarFile", "Ảnh quá lớn (> 2MB)."); }
                    else
                    {
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                        var fileExtension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
                        if (!allowedExtensions.Contains(fileExtension)) { ModelState.AddModelError("avatarFile", "Chỉ chấp nhận file ảnh."); }
                        else
                        {
                            uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                            using (var fs = new FileStream(filePath, FileMode.Create)) { await avatarFile.CopyToAsync(fs); }
                            interpreter.AvatarPath = uniqueFileName;
                            // Delete old if different
                            if (!string.IsNullOrEmpty(currentAvatarPath) && currentAvatarPath != uniqueFileName)
                            {
                                var oldFilePath = Path.Combine(uploadsFolder, currentAvatarPath);
                                if (System.IO.File.Exists(oldFilePath)) { try { System.IO.File.Delete(oldFilePath); } catch { } }
                            }
                            currentAvatarPath = uniqueFileName;
                        }
                    }
                }
                else if (!removeAvatar)
                {
                    interpreter.AvatarPath = currentAvatarPath; // Keep old
                }

                // Nếu có lỗi avatar, phải return view với đầy đủ ViewBag
                if (!ModelState.IsValid)
                {
                    // Load lại ViewBag cần thiết cho ProfileMobile
                    ViewBag.HasProfile = true;
                    ViewBag.JobApplications = await _context.JobApplications.Where(a => a.InterpreterId == existingInterpreter.InterpreterId).Include(a => a.JobPosting.Recruiter).Select(a => new { /*...*/ }).ToListAsync();
                    ViewBag.RecommendedJobs = await _context.JobPostings.Include(j => j.Recruiter).Where(jp => jp.Status == JobStatus.Active && jp.ExpiryDate >= DateTime.Now).OrderByDescending(jp => jp.PostedDate).Take(5).ToListAsync();
                    ViewBag.WeeklyViews = 15; ViewBag.MonthlyViews = 62; ViewBag.YearlyViews = 310;
                    ViewBag.ProfileCompletionPercentage = CalculateProfileCompletionPercentage(interpreter); // Tính với model lỗi
                    ViewData["UserRole"] = "Interpreter"; ViewData["ShowBottomNav"] = true;
                    return View("ProfileMobile", interpreter); // Truyền model lỗi
                }

            }
            catch (Exception ex)
            {
                ModelState.AddModelError("avatarFile", "Lỗi xử lý ảnh.");
                System.Diagnostics.Debug.WriteLine($"Avatar processing error on update: {ex.Message}");
                // Load lại ViewBag và return view lỗi (như trên)
                ViewBag.HasProfile = true; ViewBag.JobApplications = await _context.JobApplications.Where(a => a.InterpreterId == existingInterpreter.InterpreterId).Include(a => a.JobPosting.Recruiter).Select(a => new { /*...*/ }).ToListAsync(); ViewBag.RecommendedJobs = await _context.JobPostings.Include(j => j.Recruiter).Where(jp => jp.Status == JobStatus.Active && jp.ExpiryDate >= DateTime.Now).OrderByDescending(jp => jp.PostedDate).Take(5).ToListAsync(); ViewBag.WeeklyViews = 15; ViewBag.MonthlyViews = 62; ViewBag.YearlyViews = 310; ViewBag.ProfileCompletionPercentage = CalculateProfileCompletionPercentage(interpreter); ViewData["UserRole"] = "Interpreter"; ViewData["ShowBottomNav"] = true;
                return View("ProfileMobile", interpreter);
            }


            // --- Kiểm tra ModelState cho các trường khác ---
            ModelState.Remove("User");
            ModelState.Remove("Educations");
            ModelState.Remove("WorkExperiences");
            ModelState.Remove("Projects");
            if (avatarFile == null || avatarFile.Length == 0)
            {
                ModelState.Remove("avatarFile");
            }


            if (ModelState.IsValid)
            {
                try
                {
                    // --- Cập nhật các trường của existingInterpreter ---
                    existingInterpreter.InterpreterName = interpreter.InterpreterName;
                    existingInterpreter.DateOfBirth = interpreter.DateOfBirth;
                    existingInterpreter.Gender = interpreter.Gender;
                    existingInterpreter.WorkLocation = interpreter.WorkLocation;
                    existingInterpreter.DetailedAddress = interpreter.DetailedAddress;
                    existingInterpreter.YearsOfExperience = interpreter.YearsOfExperience;
                    existingInterpreter.Field = interpreter.Field;
                    existingInterpreter.Skills = interpreter.Skills; // Cập nhật skills
                    existingInterpreter.AvatarPath = interpreter.AvatarPath; // Cập nhật avatar đã xử lý


                    _context.Update(existingInterpreter);
                    await _context.SaveChangesAsync();

                    TempData["StatusMessage"] = "Hồ sơ phiên dịch viên đã được cập nhật thành công.";
                    System.Diagnostics.Debug.WriteLine("Interpreter Profile updated successfully");
                    return RedirectToAction(nameof(Profile)); // Quay về trang Profile
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating interpreter profile DB: {ex.Message}");
                    ModelState.AddModelError("", $"Không thể cập nhật hồ sơ: {ex.Message}");
                }
            }
            else
            {
                // Log lỗi validation
                foreach (var modelState in ModelState.Values) { foreach (var error in modelState.Errors) { System.Diagnostics.Debug.WriteLine($"ModelState Error on Update: {error.ErrorMessage}"); } }
            }

            // --- Nếu ModelState không hợp lệ hoặc có lỗi DB, quay lại View ProfileMobile ---
            // Load lại ViewBag cần thiết
            ViewBag.HasProfile = true;
            ViewBag.JobApplications = await _context.JobApplications.Where(a => a.InterpreterId == existingInterpreter.InterpreterId).Include(a => a.JobPosting.Recruiter).Select(a => new { /*...*/ }).ToListAsync(); // Lấy lại data cho view
            ViewBag.RecommendedJobs = await _context.JobPostings.Include(j => j.Recruiter).Where(jp => jp.Status == JobStatus.Active && jp.ExpiryDate >= DateTime.Now).OrderByDescending(jp => jp.PostedDate).Take(5).ToListAsync();
            ViewBag.WeeklyViews = 15; ViewBag.MonthlyViews = 62; ViewBag.YearlyViews = 310; // Placeholder
            ViewBag.ProfileCompletionPercentage = CalculateProfileCompletionPercentage(interpreter); // Tính với model lỗi

            ViewData["UserRole"] = "Interpreter";
            ViewData["ShowBottomNav"] = true;
            // Truyền lại model 'interpreter' (chứa dữ liệu lỗi từ form)
            return View("ProfileMobile", interpreter);
        }


        // GET: Interpreters/CreateProfile
        [HttpGet]
        public async Task<IActionResult> CreateProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != 1) // Chỉ cho Interpreter (Role = 1)
            {
                // Không phải Interpreter hoặc chưa đăng nhập
                return RedirectToAction("Index", "Home");
            }

            // Kiểm tra xem đã có hồ sơ chưa, nếu có rồi thì chuyển về trang Profile
            var existingInterpreter = await _context.Interpreters.FirstOrDefaultAsync(i => i.UserId == user.Id);
            if (existingInterpreter != null)
            {
                return RedirectToAction(nameof(Profile)); // Đã có profile, xem profile
            }

            // Khởi tạo model trống
            var interpreter = new Interpreter
            {
                UserId = user.Id // Gán sẵn UserId
            };

            ViewData["Title"] = "Hoàn thiện Hồ sơ Phiên dịch viên";
            ViewData["UserRole"] = "Interpreter";
            ViewData["ShowBottomNav"] = false; // Không hiển thị nav khi tạo

            return View("CreateProfile", interpreter); // Trả về View CreateProfile.cshtml
        }

        // POST: Interpreters/CreateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProfile(Interpreter interpreter, IFormFile? avatarFile) // Thêm IFormFile? avatarFile (nullable)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != 1)
            {
                return Unauthorized();
            }
            // Gán lại UserId từ user đang đăng nhập
            interpreter.UserId = user.Id;

            // Kiểm tra lại xem hồ sơ đã tồn tại chưa
            var existingInterpreterCheck = await _context.Interpreters.FirstOrDefaultAsync(i => i.UserId == user.Id);
            if (existingInterpreterCheck != null)
            {
                ModelState.AddModelError("", "Hồ sơ phiên dịch viên đã tồn tại.");
                // return RedirectToAction(nameof(Profile));
            }

            // Xóa validation không cần thiết
            ModelState.Remove("User");
            ModelState.Remove("Educations"); // Không tạo các list này ở bước này
            ModelState.Remove("WorkExperiences");
            ModelState.Remove("Projects");
            if (avatarFile == null || avatarFile.Length == 0)
            {
                ModelState.Remove("avatarFile"); // Không bắt buộc avatar khi tạo
                ModelState.Remove("AvatarPath"); // Cũng bỏ qua nếu ko có file
            }


            if (ModelState.IsValid)
            {
                try
                {
                    // Xử lý upload Avatar (tương tự Recruiter)
                    if (avatarFile != null && avatarFile.Length > 0)
                    {
                        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "avatars");
                        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                        // Kiểm tra size/type
                        if (avatarFile.Length > 2 * 1024 * 1024) { ModelState.AddModelError("avatarFile", "Kích thước file quá lớn (tối đa 2MB)."); }
                        else
                        {
                            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                            var fileExtension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
                            if (!allowedExtensions.Contains(fileExtension)) { ModelState.AddModelError("avatarFile", "Chỉ chấp nhận file ảnh."); }
                            else
                            {
                                // Lưu file mới
                                var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                                using (var fileStream = new FileStream(filePath, FileMode.Create)) { await avatarFile.CopyToAsync(fileStream); }
                                interpreter.AvatarPath = uniqueFileName; // Gán path mới
                            }
                        }
                        // Nếu có lỗi avatar, quay lại form
                        if (!ModelState.IsValid)
                        {
                            ViewData["Title"] = "Hoàn thiện Hồ sơ Phiên dịch viên"; ViewData["UserRole"] = "Interpreter"; ViewData["ShowBottomNav"] = false;
                            return View("CreateProfile", interpreter);
                        }
                    }
                    else
                    {
                        interpreter.AvatarPath = null; // Không có avatar
                    }

                    // Khởi tạo các List rỗng để tránh lỗi null reference
                    interpreter.Educations = new List<Education>();
                    interpreter.WorkExperiences = new List<WorkExperience>();
                    interpreter.Projects = new List<Project>();


                    _context.Interpreters.Add(interpreter);
                    await _context.SaveChangesAsync();

                    TempData["StatusMessage"] = "Hồ sơ phiên dịch viên đã được tạo thành công.";
                    // Sau khi tạo thành công, chuyển đến trang Dashboard chính để họ có thể thêm Học vấn, Kinh nghiệm...
                    return RedirectToAction(nameof(Dashboard));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error creating interpreter profile: {ex.Message}");
                    ModelState.AddModelError("", $"Không thể tạo hồ sơ: {ex.Message}");
                }
            }
            else
            {
                // Log lỗi validation nếu cần
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"ModelState Error on Create: {error.ErrorMessage}");
                    }
                }
            }

            // Nếu ModelState không hợp lệ hoặc có lỗi, quay lại form CreateProfile
            ViewData["Title"] = "Hoàn thiện Hồ sơ Phiên dịch viên";
            ViewData["UserRole"] = "Interpreter";
            ViewData["ShowBottomNav"] = false;
            return View("CreateProfile", interpreter);
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
        public async Task<IActionResult> UpdateBasicInfo(Interpreter interpreter, IFormFile? avatarFile, bool removeAvatar = false)
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

                    // --- Xử lý Avatar ---
                    string currentAvatarPath = existingInterpreter.AvatarPath;
                    string uniqueFileName = null;

                    try
                    {
                        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "avatars");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        // 1. Xử lý xóa avatar
                        if (removeAvatar && !string.IsNullOrEmpty(currentAvatarPath))
                        {
                            var oldFilePath = Path.Combine(uploadsFolder, currentAvatarPath);
                            if (System.IO.File.Exists(oldFilePath))
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                            existingInterpreter.AvatarPath = null;
                            currentAvatarPath = null;
                        }
                        // 2. Xử lý upload avatar mới
                        else if (avatarFile != null && avatarFile.Length > 0)
                        {
                            // Kiểm tra kích thước file
                            if (avatarFile.Length > 2 * 1024 * 1024)
                            {
                                if (isAjax)
                                {
                                    return Json(new { success = false, message = "Kích thước file quá lớn (tối đa 2MB)" });
                                }
                                ModelState.AddModelError("avatarFile", "Kích thước file quá lớn (tối đa 2MB)");
                                return View("ProfileMobile", interpreter);
                            }

                            // Kiểm tra định dạng file
                            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                            var fileExtension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
                            if (!allowedExtensions.Contains(fileExtension))
                            {
                                if (isAjax)
                                {
                                    return Json(new { success = false, message = "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif)" });
                                }
                                ModelState.AddModelError("avatarFile", "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif)");
                                return View("ProfileMobile", interpreter);
                            }

                            // Tạo tên file mới và lưu
                            uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await avatarFile.CopyToAsync(fileStream);
                            }

                            // Xóa avatar cũ nếu có
                            if (!string.IsNullOrEmpty(currentAvatarPath) && currentAvatarPath != uniqueFileName)
                            {
                                var oldFilePath = Path.Combine(uploadsFolder, currentAvatarPath);
                                if (System.IO.File.Exists(oldFilePath))
                                {
                                    try { System.IO.File.Delete(oldFilePath); } catch { }
                                }
                            }

                            existingInterpreter.AvatarPath = uniqueFileName;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (isAjax)
                        {
                            return Json(new { success = false, message = $"Lỗi xử lý file: {ex.Message}" });
                        }
                        ModelState.AddModelError("", $"Lỗi xử lý file: {ex.Message}");
                        return View("ProfileMobile", interpreter);
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
                    string experienceText = GetExperienceText(existingInterpreter.YearsOfExperience);

                    // Tính tuổi
                    int age = DateTime.Now.Year - existingInterpreter.DateOfBirth.Year;
                    if (DateTime.Now.DayOfYear < existingInterpreter.DateOfBirth.DayOfYear)
                    {
                        age--;
                    }

                    if (isAjax)
                    {
                        return Json(new
                        {
                            success = true,
                            message = "Cập nhật thông tin cơ bản thành công",
                            item = new
                            {
                                interpreterId = existingInterpreter.InterpreterId,
                                interpreterName = existingInterpreter.InterpreterName,
                                gender = existingInterpreter.Gender,
                                age = age,
                                field = existingInterpreter.Field,
                                workLocation = existingInterpreter.WorkLocation,
                                detailedAddress = existingInterpreter.DetailedAddress,
                                experienceText = experienceText,
                                profileCompletionPercentage = profileCompletionPercentage,
                                avatarPath = existingInterpreter.AvatarPath
                            }
                        });
                    }

                    TempData["StatusMessage"] = "Cập nhật thông tin cơ bản thành công.";
                    return RedirectToAction(nameof(Profile));
                }
                catch (Exception ex)
                {
                    if (isAjax)
                    {
                        return Json(new { success = false, message = $"Không thể cập nhật thông tin cơ bản: {ex.Message}" });
                    }

                    ModelState.AddModelError("", $"Không thể cập nhật thông tin cơ bản: {ex.Message}");
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
            }

            return RedirectToAction(nameof(Profile));
        }

        // Helper method to convert ExperienceLevel to display text
        private string GetExperienceText(ExperienceLevel experience)
        {
            return experience switch
            {
                ExperienceLevel.LessThanOneYear => "Dưới 1 năm",
                ExperienceLevel.OneYear => "1 năm",
                ExperienceLevel.TwoYears => "2 năm",
                ExperienceLevel.ThreeYears => "3 năm",
                ExperienceLevel.FourYears => "4 năm",
                ExperienceLevel.FiveYears => "5 năm",
                ExperienceLevel.MoreThanFiveYears => "Hơn 5 năm",
                _ => "Không xác định"
            };
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
