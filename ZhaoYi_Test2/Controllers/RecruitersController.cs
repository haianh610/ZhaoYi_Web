using Microsoft.AspNetCore.Authorization; // Thêm dòng này
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZhaoYi_Test2.Data;
using ZhaoYi_Test2.Models;
using System.Reflection; 
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic; // Thêm dòng này
using System.Linq; // Thêm dòng này
using System.ComponentModel.DataAnnotations; // Thêm dòng này để sử dụng DisplayAttribute

namespace ZhaoYi_Test2.Controllers
{
    [Authorize] // Yêu cầu đăng nhập cho tất cả các action trong controller này
    public class RecruitersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public RecruitersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Recruiters/Dashboard
        public async Task<IActionResult> Dashboard(string searchTerm, string location, ExperienceLevel? experience)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != 2) // Chỉ cho phép Recruiter (Role = 2)
            {
                // Có thể chuyển hướng đến trang chọn vai trò nếu chưa có
                if (user != null && user.Role == 0)
                {
                    return RedirectToAction("ChooseRole", "Role");
                }
                // Hoặc chuyển về trang chủ nếu không phải recruiter
                return RedirectToAction("Index", "Home");
            }

            // Lấy danh sách tất cả phiên dịch viên
            var interpretersQuery = _context.Interpreters.AsQueryable();

            // --- Logic lọc (Tương tự trang tìm kiếm của phiên dịch viên) ---
            if (!string.IsNullOrEmpty(searchTerm))
            {
                interpretersQuery = interpretersQuery.Where(i => i.InterpreterName.Contains(searchTerm) ||
                                                               (i.Skills != null && i.Skills.Contains(searchTerm)) ||
                                                               (i.Field != null && i.Field.Contains(searchTerm)));
                ViewBag.SearchTerm = searchTerm;
            }

            if (!string.IsNullOrEmpty(location))
            {
                interpretersQuery = interpretersQuery.Where(i => i.WorkLocation.Contains(location) || i.DetailedAddress.Contains(location));
                ViewBag.Location = location;
            }

            if (experience.HasValue)
            {
                interpretersQuery = interpretersQuery.Where(i => i.YearsOfExperience == experience.Value);
                ViewBag.Experience = experience.Value;
            }
            // --- Kết thúc Logic lọc ---

            var interpreters = await interpretersQuery
                                    .OrderBy(i => i.InterpreterName) // Sắp xếp theo tên hoặc tiêu chí khác
                                    .ToListAsync();

            // Lấy danh sách ID các interpreter đã được recruiter hiện tại bookmark (logic này cần bổ sung sau)
            ViewBag.BookmarkedInterpreterIds = new List<int>(); // Tạm thời để trống

            return View("Dashboard", interpreters); // Trả về view Dashboard với danh sách interpreters
        }



        // GET: Recruiters/InterpreterDetails/{id}
        public async Task<IActionResult> InterpreterDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != 2) // Chỉ cho phép Recruiter
            {
                // Chuyển hướng nếu không phải Recruiter
                return RedirectToAction("Index", "Home");
            }

            var interpreter = await _context.Interpreters
                .Include(i => i.Educations)        // Include Học vấn
                .Include(i => i.WorkExperiences) // Include Kinh nghiệm làm việc
                .Include(i => i.Projects)        // Include Dự án
                .FirstOrDefaultAsync(m => m.InterpreterId == id);

            if (interpreter == null)
            {
                return NotFound();
            }

            // TODO: Lấy trạng thái bookmark thực tế của recruiter này với interpreter này
            ViewBag.IsBookmarked = false; // Placeholder - Cần logic thực tế

            // Đặt tiêu đề cho trang
            ViewData["Title"] = $"Hồ sơ: {interpreter.InterpreterName}";
            ViewData["UserRole"] = "Recruiter"; // Xác định vai trò cho layout
            ViewData["ShowBottomNav"] = false; // Thường trang chi tiết không có nav dưới

            return View("InterpreterProfileView", interpreter);
        }


        // --- Các Action Profile, CreateJob, Edit, Delete,... của Recruiter giữ nguyên như code cũ của bạn ---
        // GET: Recruiters/Profile
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Chỉ cho phép người dùng có vai trò nhà tuyển dụng (Role = 2) truy cập trang này
            if (user.Role != 2)
            {
                // Nếu chưa chọn vai trò, chuyển hướng đến trang chọn
                if (user.Role == 0)
                {
                    return RedirectToAction("ChooseRole", "Role");
                }
                // Nếu là vai trò khác, về trang chủ
                return RedirectToAction("Index", "Home");
            }

            // Kiểm tra xem người dùng đã có hồ sơ nhà tuyển dụng chưa
            var recruiter = await _context.Recruiters
                .FirstOrDefaultAsync(m => m.UserId == user.Id);

            if (recruiter == null)
            {
                TempData["InfoMessage"] = "Vui lòng hoàn thiện hồ sơ nhà tuyển dụng của bạn.";
                return RedirectToAction(nameof(CreateProfile));
            }
            else
            {
                // Nếu đã có hồ sơ
                ViewBag.HasProfile = true;

                // Lấy các bài đăng của nhà tuyển dụng này
                var jobPostings = await _context.JobPostings
                    .Where(jp => jp.RecruiterId == recruiter.RecruiterId)
                    .OrderByDescending(jp => jp.PostedDate)
                    .ToListAsync();
                ViewBag.JobPostings = jobPostings;

                // Lấy các đơn ứng tuyển gần đây cho các bài đăng của recruiter này
                var recentApplications = await _context.JobApplications
                    .Where(a => a.JobPosting.RecruiterId == recruiter.RecruiterId)
                    .Include(a => a.Interpreter) // Lấy thông tin Interpreter
                    .Include(a => a.JobPosting) // Lấy thông tin JobPosting (nếu cần title)
                    .OrderByDescending(a => a.ApplicationDate)
                    .Take(5) // Lấy 5 đơn gần nhất để hiển thị ví dụ
                    .ToListAsync();
                ViewBag.Applications = recentApplications;

                // Placeholder cho Following/Followers/Rank (cần logic thực tế nếu có)
                ViewBag.FollowingCount = 150; // Ví dụ
                ViewBag.FollowersCount = 280; // Ví dụ
                ViewBag.Rank = "Bạc"; // Ví dụ
            }

            // Trả về view profile tương ứng cho Recruiter
            return View("ProfileMobile", recruiter);
        }


        // Trong Controllers/RecruitersController.cs

        // POST: Recruiters/Profile (Chỉ để UPDATE hồ sơ đã có)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(Recruiter recruiter, IFormFile avatarFile, bool removeAvatar = false)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != 2) // Đảm bảo là Recruiter và đã đăng nhập
            {
                // Nếu chưa đăng nhập, chuyển về trang login
                if (user == null) return RedirectToAction("Login", "Account", new { area = "Identity" });
                // Nếu không phải role Recruiter, về trang chủ
                return RedirectToAction("Index", "Home");
            }

            // --- Lấy hồ sơ hiện có từ DB để cập nhật ---
            var existingRecruiter = await _context.Recruiters.FirstOrDefaultAsync(r => r.UserId == user.Id);

            // Nếu không tìm thấy hồ sơ hiện có để cập nhật
            if (existingRecruiter == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hồ sơ nhà tuyển dụng để cập nhật. Vui lòng tạo hồ sơ trước.";
                return RedirectToAction(nameof(CreateProfile)); // Chuyển hướng đến trang tạo mới
            }

            // --- QUAN TRỌNG: Gán lại ID và UserId từ bản ghi hiện có ---
            // Ngăn chặn việc cố gắng thay đổi ID qua form
            recruiter.RecruiterId = existingRecruiter.RecruiterId;
            recruiter.UserId = existingRecruiter.UserId;

            // --- Xử lý Avatar ---
            string currentAvatarPath = existingRecruiter.AvatarPath; // Lấy đường dẫn avatar hiện tại
            string uniqueFileName = null; // Tên file mới (nếu có)

            try // Bọc xử lý file trong try-catch để xử lý lỗi I/O
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
                        System.Diagnostics.Debug.WriteLine($"Deleted avatar for remove request: {oldFilePath}");
                    }
                    recruiter.AvatarPath = null; // Đặt AvatarPath thành null cho model sẽ validate
                    currentAvatarPath = null; // Cập nhật biến tạm để không giữ lại path cũ ở bước sau
                }
                // 2. Xử lý upload avatar mới
                else if (avatarFile != null && avatarFile.Length > 0)
                {
                    // Kiểm tra kích thước và loại file (giống CreateProfile)
                    if (avatarFile.Length > 2 * 1024 * 1024)
                    {
                        ModelState.AddModelError("avatarFile", "Kích thước file quá lớn (tối đa 2MB).");
                    }
                    else
                    {
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                        var fileExtension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            ModelState.AddModelError("avatarFile", "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif).");
                        }
                        else
                        {
                            // Tạo tên file và lưu
                            uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await avatarFile.CopyToAsync(fileStream);
                            }
                            recruiter.AvatarPath = uniqueFileName; // Gán path mới cho model sẽ validate

                            // Xóa avatar cũ nếu có và khác avatar mới
                            if (!string.IsNullOrEmpty(currentAvatarPath) && currentAvatarPath != uniqueFileName)
                            {
                                var oldFilePath = Path.Combine(uploadsFolder, currentAvatarPath);
                                if (System.IO.File.Exists(oldFilePath))
                                {
                                    try { System.IO.File.Delete(oldFilePath); } catch (IOException ioEx) { System.Diagnostics.Debug.WriteLine($"IO Error deleting old avatar: {ioEx.Message}"); }
                                }
                            }
                            currentAvatarPath = uniqueFileName; // Cập nhật biến tạm
                        }
                    }
                }
                // 3. Giữ avatar cũ (nếu không xóa và không upload mới)
                else if (!removeAvatar)
                {
                    recruiter.AvatarPath = currentAvatarPath; // Gán lại path cũ cho model sẽ validate
                }

            }
            catch (IOException ioEx)
            {
                System.Diagnostics.Debug.WriteLine($"IO Error during avatar processing: {ioEx.Message}");
                ModelState.AddModelError("avatarFile", "Đã xảy ra lỗi khi xử lý ảnh đại diện.");
                // Không dừng ở đây, vẫn tiếp tục kiểm tra ModelState chung
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"General Error during avatar processing: {ex.Message}");
                ModelState.AddModelError("", "Đã xảy ra lỗi không mong muốn khi xử lý ảnh.");
                // Không dừng ở đây, vẫn tiếp tục kiểm tra ModelState chung
            }


            // --- Chuẩn bị và Kiểm tra ModelState ---
            ModelState.Remove("User"); // Bỏ qua validation cho navigation property
            if (avatarFile == null || avatarFile.Length == 0)
            {
                ModelState.Remove("avatarFile"); // Không yêu cầu avatar khi cập nhật
            }


            if (ModelState.IsValid)
            {
                try
                {
                    // --- Cập nhật các trường của bản ghi hiện có ---
                    existingRecruiter.RecruiterName = recruiter.RecruiterName;
                    existingRecruiter.WorkLocation = recruiter.WorkLocation;
                    existingRecruiter.DetailedAddress = recruiter.DetailedAddress;
                    // Cập nhật AvatarPath từ model 'recruiter' đã được xử lý ở trên
                    existingRecruiter.AvatarPath = recruiter.AvatarPath;

                    _context.Update(existingRecruiter); // Đánh dấu bản ghi là đã thay đổi
                    await _context.SaveChangesAsync(); // Lưu thay đổi vào DB

                    TempData["StatusMessage"] = "Hồ sơ nhà tuyển dụng đã được cập nhật thành công.";
                    System.Diagnostics.Debug.WriteLine("Recruiter Profile updated successfully.");
                    return RedirectToAction(nameof(Profile)); // Quay lại trang Profile sau khi cập nhật thành công
                }
                catch (DbUpdateConcurrencyException)
                {
                    ModelState.AddModelError("", "Lỗi xung đột dữ liệu. Hồ sơ có thể đã bị thay đổi bởi người khác.");
                    // Load lại dữ liệu mới nhất để hiển thị nếu muốn
                    // existingRecruiter = await _context.Recruiters.AsNoTracking().FirstOrDefaultAsync(r => r.UserId == user.Id);
                    // if (existingRecruiter != null) recruiter = existingRecruiter; // Hiển thị dữ liệu mới nhất
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating recruiter profile in DB: {ex.Message}\n{ex.StackTrace}");
                    ModelState.AddModelError("", $"Không thể cập nhật hồ sơ: {ex.Message}");
                }
            }
            else
            {
                // Log lỗi validation nếu cần
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"ModelState Error on Update: {error.ErrorMessage}");
                    }
                }
            }

            // --- Nếu ModelState không hợp lệ hoặc có lỗi khi lưu DB, quay lại View Profile ---
            // Cần truyền lại các ViewBag cần thiết cho trang Profile (giống action GET)
            ViewBag.HasProfile = true; // Chắc chắn đã có profile vì đang ở action Update
                                       // Lấy lại job postings và applications cho view
            var jobPostings = await _context.JobPostings.Where(jp => jp.RecruiterId == existingRecruiter.RecruiterId).OrderByDescending(jp => jp.PostedDate).ToListAsync();
            ViewBag.JobPostings = jobPostings;
            var recentApplications = await _context.JobApplications.Where(a => a.JobPosting.RecruiterId == existingRecruiter.RecruiterId).Include(a => a.Interpreter).Include(a => a.JobPosting).OrderByDescending(a => a.ApplicationDate).Take(5).ToListAsync();
            ViewBag.Applications = recentApplications;
            // Lấy các giá trị ViewBag khác (ví dụ: stats, rank)
            ViewBag.FollowingCount = 150; // Placeholder
            ViewBag.FollowersCount = 280; // Placeholder
            ViewBag.Rank = "Bạc"; // Placeholder

            ViewData["UserRole"] = "Recruiter";
            ViewData["ShowBottomNav"] = true;
            // Truyền lại model 'recruiter' (chứa dữ liệu lỗi từ form) để hiển thị validation errors
            return View("RecruiterProfileMobile", recruiter);
        }

        // GET: Recruiters/CreateProfile
        [HttpGet]
        public async Task<IActionResult> CreateProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != 2)
            {
                // Không phải Recruiter hoặc chưa đăng nhập, chuyển về trang chủ hoặc login
                return RedirectToAction("Index", "Home");
            }

            // Kiểm tra xem đã có hồ sơ chưa, nếu có rồi thì chuyển về trang Profile
            var existingRecruiter = await _context.Recruiters.FirstOrDefaultAsync(r => r.UserId == user.Id);
            if (existingRecruiter != null)
            {
                return RedirectToAction(nameof(Profile));
            }

            // Khởi tạo model trống để View sử dụng
            var recruiter = new Recruiter
            {
                UserId = user.Id // Gán sẵn UserId
            };

            ViewData["Title"] = "Hoàn thiện Hồ sơ Nhà tuyển dụng";
            ViewData["UserRole"] = "Recruiter";
            ViewData["ShowBottomNav"] = false; // Không hiển thị nav khi tạo profile

            return View("CreateProfile", recruiter); // Trả về View CreateProfile.cshtml
        }

        // POST: Recruiters/CreateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProfile(Recruiter recruiter, IFormFile avatarFile) // Thêm IFormFile avatarFile
        {
            var user = await _userManager.GetUserAsync(User);
            // Kiểm tra lại user và role
            if (user == null || user.Role != 2)
            {
                return Unauthorized();
            }
            // Đảm bảo UserId được gán đúng từ user đang đăng nhập
            recruiter.UserId = user.Id;

            // Kiểm tra xem hồ sơ đã tồn tại chưa (tránh tạo trùng)
            var existingRecruiterCheck = await _context.Recruiters.FirstOrDefaultAsync(r => r.UserId == user.Id);
            if (existingRecruiterCheck != null)
            {
                ModelState.AddModelError("", "Hồ sơ nhà tuyển dụng đã tồn tại.");
                // Có thể chuyển hướng về Profile thay vì báo lỗi
                // return RedirectToAction(nameof(Profile));
            }


            // Xóa validation properties không cần thiết trước khi kiểm tra ModelState
            ModelState.Remove("User");
            ModelState.Remove("UserId"); // Đã gán lại từ server
            ModelState.Remove("avatarFile"); // Không bắt buộc avatar khi tạo

            // Xử lý upload Avatar
            if (avatarFile != null && avatarFile.Length > 0)
            {
                // Kiểm tra kích thước file
                if (avatarFile.Length > 2 * 1024 * 1024) // 2MB
                {
                    ModelState.AddModelError("avatarFile", "Kích thước file quá lớn (tối đa 2MB).");
                    TempData["ErrorMessage"] = "Kích thước file quá lớn (tối đa 2MB).";
                    ViewData["Title"] = "Hoàn thiện Hồ sơ Nhà tuyển dụng";
                    ViewData["UserRole"] = "Recruiter";
                    ViewData["ShowBottomNav"] = false;
                    return View(recruiter);
                }

                // Kiểm tra định dạng file
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("avatarFile", "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif).");
                    TempData["ErrorMessage"] = "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif).";
                    ViewData["Title"] = "Hoàn thiện Hồ sơ Nhà tuyển dụng";
                    ViewData["UserRole"] = "Recruiter";
                    ViewData["ShowBottomNav"] = false;
                    return View(recruiter);
                }

                try
                {
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "avatars");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Tạo tên file duy nhất và lưu
                    var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await avatarFile.CopyToAsync(fileStream);
                    }
                    recruiter.AvatarPath = uniqueFileName;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("avatarFile", $"Lỗi khi xử lý file: {ex.Message}");
                    TempData["ErrorMessage"] = $"Lỗi khi xử lý file: {ex.Message}";
                    ViewData["Title"] = "Hoàn thiện Hồ sơ Nhà tuyển dụng";
                    ViewData["UserRole"] = "Recruiter";
                    ViewData["ShowBottomNav"] = false;
                    return View(recruiter);
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Recruiters.Add(recruiter);
                    await _context.SaveChangesAsync();

                    TempData["StatusMessage"] = "Hồ sơ nhà tuyển dụng đã được tạo thành công.";
                    return RedirectToAction(nameof(Dashboard)); // Chuyển đến trang Dashboard sau khi tạo xong
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error creating recruiter profile: {ex.Message}");
                    ModelState.AddModelError("", $"Không thể tạo hồ sơ: {ex.Message}");
                }
            }

            // Nếu ModelState không hợp lệ hoặc có lỗi, quay lại form
            ViewData["Title"] = "Hoàn thiện Hồ sơ Nhà tuyển dụng";
            ViewData["UserRole"] = "Recruiter";
            ViewData["ShowBottomNav"] = false;
            return View("CreateProfile", recruiter);
        }

        // Trong RecruitersController.cs

        // GET: Recruiters/EditProfile
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != 2)
            {
                // Redirect nếu chưa đăng nhập hoặc không phải Recruiter
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Lấy hồ sơ hiện tại để chỉnh sửa
            var recruiter = await _context.Recruiters.FirstOrDefaultAsync(r => r.UserId == user.Id);

            // Nếu không tìm thấy hồ sơ (trường hợp hiếm), chuyển đến trang tạo
            if (recruiter == null)
            {
                TempData["InfoMessage"] = "Vui lòng tạo hồ sơ nhà tuyển dụng trước khi chỉnh sửa.";
                return RedirectToAction(nameof(CreateProfile));
            }

            ViewData["Title"] = "Chỉnh sửa Hồ sơ Nhà tuyển dụng";
            ViewData["UserRole"] = "Recruiter";
            ViewData["ShowBottomNav"] = false; // Không hiển thị nav khi chỉnh sửa

            return View("EditProfile", recruiter); // Trả về View EditProfile.cshtml với dữ liệu hiện tại
        }

        // POST: Recruiters/EditProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(Recruiter recruiter, IFormFile avatarFile, bool removeAvatar = false)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != 2) // Đảm bảo là Recruiter và đã đăng nhập
            {
                if (user == null) return RedirectToAction("Login", "Account", new { area = "Identity" });
                return RedirectToAction("Index", "Home");
            }

            // Kiểm tra file ảnh trước khi xử lý
            if (avatarFile != null)
            {
                // Kiểm tra kích thước
                if (avatarFile.Length > 2 * 1024 * 1024) // 2MB
                {
                    TempData["ErrorMessage"] = "Kích thước file quá lớn (tối đa 2MB).";
                    return RedirectToAction(nameof(EditProfile));
                }

                // Kiểm tra loại file
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    TempData["ErrorMessage"] = "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif).";
                    return RedirectToAction(nameof(EditProfile));
                }
            }

            // --- Lấy hồ sơ hiện có từ DB để cập nhật ---
            var existingRecruiter = await _context.Recruiters.FirstOrDefaultAsync(r => r.UserId == user.Id);

            // Nếu không tìm thấy hồ sơ hiện có để cập nhật
            if (existingRecruiter == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hồ sơ nhà tuyển dụng để cập nhật. Vui lòng tạo hồ sơ trước.";
                return RedirectToAction(nameof(CreateProfile)); // Chuyển hướng đến trang tạo mới
            }

            // --- QUAN TRỌNG: Gán lại ID và UserId từ bản ghi hiện có ---
            // Ngăn chặn việc cố gắng thay đổi ID qua form
            recruiter.RecruiterId = existingRecruiter.RecruiterId;
            recruiter.UserId = existingRecruiter.UserId;

            // --- Xử lý Avatar ---
            string currentAvatarPath = existingRecruiter.AvatarPath; // Lấy đường dẫn avatar hiện tại
            string uniqueFileName = null; // Tên file mới (nếu có)

            try // Bọc xử lý file trong try-catch để xử lý lỗi I/O
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
                        System.Diagnostics.Debug.WriteLine($"Deleted avatar for remove request: {oldFilePath}");
                    }
                    recruiter.AvatarPath = null; // Đặt AvatarPath thành null cho model sẽ validate
                    currentAvatarPath = null; // Cập nhật biến tạm để không giữ lại path cũ ở bước sau
                }
                // 2. Xử lý upload avatar mới
                else if (avatarFile != null && avatarFile.Length > 0)
                {
                    // Kiểm tra kích thước và loại file (giống CreateProfile)
                    if (avatarFile.Length > 2 * 1024 * 1024)
                    {
                        ModelState.AddModelError("avatarFile", "Kích thước file quá lớn (tối đa 2MB).");
                    }
                    else
                    {
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                        var fileExtension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            ModelState.AddModelError("avatarFile", "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif).");
                        }
                        else
                        {
                            // Tạo tên file và lưu
                            uniqueFileName = Guid.NewGuid().ToString() + fileExtension;
                            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await avatarFile.CopyToAsync(fileStream);
                            }
                            recruiter.AvatarPath = uniqueFileName; // Gán path mới cho model sẽ validate

                            // Xóa avatar cũ nếu có và khác avatar mới
                            if (!string.IsNullOrEmpty(currentAvatarPath) && currentAvatarPath != uniqueFileName)
                            {
                                var oldFilePath = Path.Combine(uploadsFolder, currentAvatarPath);
                                if (System.IO.File.Exists(oldFilePath))
                                {
                                    try { System.IO.File.Delete(oldFilePath); } catch (IOException ioEx) { System.Diagnostics.Debug.WriteLine($"IO Error deleting old avatar: {ioEx.Message}"); }
                                }
                            }
                            currentAvatarPath = uniqueFileName; // Cập nhật biến tạm
                        }
                    }
                }
                // 3. Giữ avatar cũ (nếu không xóa và không upload mới)
                else if (!removeAvatar)
                {
                    recruiter.AvatarPath = currentAvatarPath; // Gán lại path cũ cho model sẽ validate
                }

            }
            catch (IOException ioEx)
            {
                System.Diagnostics.Debug.WriteLine($"IO Error during avatar processing: {ioEx.Message}");
                ModelState.AddModelError("avatarFile", "Đã xảy ra lỗi khi xử lý ảnh đại diện.");
                // Không dừng ở đây, vẫn tiếp tục kiểm tra ModelState chung
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"General Error during avatar processing: {ex.Message}");
                ModelState.AddModelError("", "Đã xảy ra lỗi không mong muốn khi xử lý ảnh.");
                // Không dừng ở đây, vẫn tiếp tục kiểm tra ModelState chung
            }


            // --- Chuẩn bị và Kiểm tra ModelState ---
            ModelState.Remove("User"); // Bỏ qua validation cho navigation property
            if (avatarFile == null || avatarFile.Length == 0)
            {
                ModelState.Remove("avatarFile"); // Không yêu cầu avatar khi cập nhật
            }


            if (ModelState.IsValid)
            {
                try
                {
                    // --- Cập nhật các trường của bản ghi hiện có ---
                    existingRecruiter.RecruiterName = recruiter.RecruiterName;
                    existingRecruiter.WorkLocation = recruiter.WorkLocation;
                    existingRecruiter.DetailedAddress = recruiter.DetailedAddress;
                    // Cập nhật AvatarPath từ model 'recruiter' đã được xử lý ở trên
                    existingRecruiter.AvatarPath = recruiter.AvatarPath;

                    _context.Update(existingRecruiter); // Đánh dấu bản ghi là đã thay đổi
                    await _context.SaveChangesAsync(); // Lưu thay đổi vào DB

                    TempData["StatusMessage"] = "Hồ sơ nhà tuyển dụng đã được cập nhật thành công.";
                    System.Diagnostics.Debug.WriteLine("Recruiter Profile updated successfully.");
                    return RedirectToAction(nameof(Profile)); // Quay lại trang Profile sau khi cập nhật thành công
                }
                catch (DbUpdateConcurrencyException)
                {
                    ModelState.AddModelError("", "Lỗi xung đột dữ liệu. Hồ sơ có thể đã bị thay đổi bởi người khác.");
                    // Load lại dữ liệu mới nhất để hiển thị nếu muốn
                    // existingRecruiter = await _context.Recruiters.AsNoTracking().FirstOrDefaultAsync(r => r.UserId == user.Id);
                    // if (existingRecruiter != null) recruiter = existingRecruiter; // Hiển thị dữ liệu mới nhất
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating recruiter profile in DB: {ex.Message}\n{ex.StackTrace}");
                    ModelState.AddModelError("", $"Không thể cập nhật hồ sơ: {ex.Message}");
                }
            }
            else
            {
                // Log lỗi validation nếu cần
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"ModelState Error on Update: {error.ErrorMessage}");
                    }
                }
            }

            // --- Nếu ModelState không hợp lệ hoặc có lỗi khi lưu DB, quay lại View Profile ---
            // Cần truyền lại các ViewBag cần thiết cho trang Profile (giống action GET)
            ViewBag.HasProfile = true; // Chắc chắn đã có profile vì đang ở action Update
                                       // Lấy lại job postings và applications cho view
            var jobPostings = await _context.JobPostings.Where(jp => jp.RecruiterId == existingRecruiter.RecruiterId).OrderByDescending(jp => jp.PostedDate).ToListAsync();
            ViewBag.JobPostings = jobPostings;
            var recentApplications = await _context.JobApplications.Where(a => a.JobPosting.RecruiterId == existingRecruiter.RecruiterId).Include(a => a.Interpreter).Include(a => a.JobPosting).OrderByDescending(a => a.ApplicationDate).Take(5).ToListAsync();
            ViewBag.Applications = recentApplications;
            // Lấy các giá trị ViewBag khác (ví dụ: stats, rank)
            ViewBag.FollowingCount = 150; // Placeholder
            ViewBag.FollowersCount = 280; // Placeholder
            ViewBag.Rank = "Bạc"; // Placeholder

            ViewData["UserRole"] = "Recruiter";
            ViewData["ShowBottomNav"] = true;
            // Truyền lại model 'recruiter' (chứa dữ liệu lỗi từ form) để hiển thị validation errors
            return View("RecruiterProfileMobile", recruiter);
        }

        // GET: Recruiters/CreateJob
        [HttpGet] // Thêm HttpGet để rõ ràng hơn
        public async Task<IActionResult> CreateJob()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != 2)
            {
                return RedirectToAction("Index", "Home"); // Hoặc trang không có quyền
            }

            var recruiter = await _context.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == user.Id);

            if (recruiter == null)
            {
                TempData["ErrorMessage"] = "Bạn cần tạo hồ sơ nhà tuyển dụng trước khi đăng tin tuyển dụng.";
                return RedirectToAction("Profile", "Recruiters");
            }

            // Khởi tạo JobPosting với UserId, RecruiterId và giá trị mặc định
            var jobPosting = new JobPosting
            {
                UserId = user.Id,
                RecruiterId = recruiter.RecruiterId,
                ExpiryDate = DateTime.Now.AddDays(30) // Mặc định hết hạn sau 30 ngày
            };

            // Đặt ViewData cho layout
            ViewData["UserRole"] = "Recruiter";
            ViewData["ShowBottomNav"] = false;

            return View(jobPosting);
        }

       // POST: Recruiters/CreateJob
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateJob(JobPosting jobPosting)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != 2)
            {
                return Unauthorized(); // Hoặc trang không có quyền
            }

            var recruiter = await _context.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == user.Id);

            if (recruiter == null)
            {
                ModelState.AddModelError("", "Không tìm thấy thông tin nhà tuyển dụng.");
                // Đặt lại ViewData nếu cần quay lại View
                ViewData["UserRole"] = "Recruiter";
                ViewData["ShowBottomNav"] = false;
                return View(jobPosting);
            }

            // Gán UserId và RecruiterId lại từ server để đảm bảo an toàn
            jobPosting.UserId = user.Id;
            jobPosting.RecruiterId = recruiter.RecruiterId;

            // Xóa validation cho navigation properties trước khi kiểm tra ModelState
            ModelState.Remove("User");
            ModelState.Remove("UserId");
            ModelState.Remove("Recruiter");

            if (ModelState.IsValid)
            {
                try
                {
                    // Đặt các giá trị mặc định/server-side khác
                    jobPosting.PostedDate = DateTime.Now;
                    jobPosting.Status = JobStatus.Pending; // Luôn chờ duyệt khi tạo mới
                    jobPosting.ViewCount = 0;
                    jobPosting.UpdatedDate = null;

                    _context.JobPostings.Add(jobPosting);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Bài đăng việc làm đã được tạo thành công và đang chờ duyệt.";
                    // Chuyển hướng đến trang quản lý bài đăng sau khi tạo thành công
                    return RedirectToAction(nameof(Profile)); // Hoặc Dashboard
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error creating job posting: {ex.Message}");
                    ModelState.AddModelError("", $"Lỗi khi lưu bài đăng: {ex.Message}");
                }
            }
            else
            {
                // Log lỗi validation nếu cần
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    System.Diagnostics.Debug.WriteLine($"Validation error: {error.ErrorMessage}");
                }
            }

            // Nếu ModelState không hợp lệ hoặc có lỗi, quay lại form với dữ liệu đã nhập
            ViewData["UserRole"] = "Recruiter";
            ViewData["ShowBottomNav"] = false;
            return View(jobPosting);
        }

         // GET: Recruiters/ManageJobs - Trang quản lý các bài đăng
        public async Task<IActionResult> ManageJobs()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != 2)
            {
                return RedirectToAction("Index", "Home");
            }

            var recruiter = await _context.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == user.Id);

            if (recruiter == null)
            {
                TempData["ErrorMessage"] = "Bạn cần tạo hồ sơ nhà tuyển dụng.";
                return RedirectToAction("Profile");
            }

            var jobPostings = await _context.JobPostings
                .Where(jp => jp.RecruiterId == recruiter.RecruiterId)
                .OrderByDescending(jp => jp.PostedDate)
                .ToListAsync();

             // Tạo View Views/Recruiters/ManageJobs.cshtml để hiển thị danh sách này
             return View(jobPostings);
        }


        // GET: Recruiters/JobDetails/{id}
        [HttpGet]
        public async Task<IActionResult> JobDetails(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != 2)
            {
                return RedirectToAction("Index", "Home");
            }

            var recruiter = await _context.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == user.Id);

            if (recruiter == null)
            {
                // Có thể chuyển hướng đến trang tạo profile nếu chưa có
                TempData["ErrorMessage"] = "Vui lòng tạo hồ sơ nhà tuyển dụng.";
                return RedirectToAction("Profile");
            }

            var jobPosting = await _context.JobPostings
                // Include Recruiter nếu bạn muốn hiển thị tên công ty/recruiter trên trang chi tiết
                // .Include(j => j.Recruiter)
                .FirstOrDefaultAsync(m => m.JobPostingId == id && m.RecruiterId == recruiter.RecruiterId);

            if (jobPosting == null)
            {
                return NotFound("Không tìm thấy bài đăng hoặc bạn không có quyền truy cập.");
            }

            // Lấy danh sách ứng viên đã ứng tuyển (tùy chọn, có thể hiển thị ở trang riêng)
            var applications = await _context.JobApplications
                .Where(a => a.JobPostingId == id)
                .Include(a => a.Interpreter) // Lấy thông tin Interpreter
                .OrderByDescending(a => a.ApplicationDate)
                .ToListAsync();

            ViewBag.Applications = applications; // Truyền danh sách ứng viên qua ViewBag

            ViewData["Title"] = $"Chi tiết: {jobPosting.Title}";
            ViewData["UserRole"] = "Recruiter";
            ViewData["ShowBottomNav"] = false; // Không hiển thị nav dưới

            return View("JobDetails", jobPosting); // Trả về View mới JobDetails.cshtml
        }

        // GET: Recruiters/EditJob/{id}
        [HttpGet]
        public async Task<IActionResult> EditJob(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != 2)
            {
                return RedirectToAction("Index", "Home");
            }

            var recruiter = await _context.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == user.Id);

            if (recruiter == null)
            {
                return RedirectToAction("Profile");
            }

            var jobPosting = await _context.JobPostings
                .FirstOrDefaultAsync(jp => jp.JobPostingId == id && jp.RecruiterId == recruiter.RecruiterId);

            if (jobPosting == null)
            {
                return NotFound();
            }

            ViewData["Title"] = $"Chỉnh sửa: {jobPosting.Title}";
            ViewData["UserRole"] = "Recruiter";
            ViewData["ShowBottomNav"] = false;

            // Tạo View Views/Recruiters/EditJob.cshtml
            return View("EditJob", jobPosting);
        }


      // POST: Recruiters/EditJob/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditJob(int id, JobPosting jobPosting)
        {
            if (id != jobPosting.JobPostingId)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != 2)
            {
                return Unauthorized();
            }

            var recruiter = await _context.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == user.Id);

            if (recruiter == null)
            {
                ModelState.AddModelError("", "Không tìm thấy thông tin nhà tuyển dụng.");
                ViewData["UserRole"] = "Recruiter"; ViewData["ShowBottomNav"] = false; // Pass ViewData back
                return View("EditJob", jobPosting);
            }

            // Kiểm tra xem bài đăng có thuộc về nhà tuyển dụng này không
            var existingJob = await _context.JobPostings
                .AsNoTracking() // Quan trọng: Dùng AsNoTracking để tránh xung đột Entity Tracking
                .FirstOrDefaultAsync(jp => jp.JobPostingId == id && jp.RecruiterId == recruiter.RecruiterId);

            if (existingJob == null)
            {
                return NotFound();
            }

            // Gán lại các giá trị không cho phép sửa đổi từ DB
            jobPosting.UserId = existingJob.UserId;
            jobPosting.RecruiterId = existingJob.RecruiterId;
            jobPosting.PostedDate = existingJob.PostedDate;
            jobPosting.ViewCount = existingJob.ViewCount;
            jobPosting.Status = existingJob.Status; // Không cho sửa status trực tiếp qua form này
                                                    // Nếu muốn reset về Pending thì đặt jobPosting.Status = JobStatus.Pending;

            // Xóa validation cho navigation properties
            ModelState.Remove("User");
            ModelState.Remove("Recruiter");

            if (ModelState.IsValid)
            {
                try
                {
                    // Chỉ cập nhật các trường cho phép sửa đổi
                    jobPosting.UpdatedDate = DateTime.Now; // Cập nhật ngày sửa đổi

                    _context.Update(jobPosting); // EF Core sẽ chỉ cập nhật các trường đã thay đổi
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Bài đăng việc làm đã được cập nhật thành công.";
                    return RedirectToAction(nameof(JobDetails), new { id = jobPosting.JobPostingId }); // Chuyển về trang chi tiết sau khi sửa
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!JobPostingExists(jobPosting.JobPostingId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        ModelState.AddModelError("", "Lỗi xung đột dữ liệu. Bài đăng có thể đã bị thay đổi bởi người khác. Vui lòng thử lại.");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error editing job posting: {ex.Message}");
                    ModelState.AddModelError("", $"Lỗi khi cập nhật bài đăng: {ex.Message}");
                }
            }

            // Nếu ModelState không hợp lệ hoặc có lỗi, quay lại view EditJob
            ViewData["UserRole"] = "Recruiter"; ViewData["ShowBottomNav"] = false;
            return View("EditJob", jobPosting);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // Giữ nguyên trả về JSON cho DeleteJob để xử lý bằng AJAX
        public async Task<IActionResult> DeleteJob(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != 2)
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var recruiter = await _context.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == user.Id);

            if (recruiter == null)
            {
                return Json(new { success = false, message = "Recruiter profile not found" });
            }

            var jobPosting = await _context.JobPostings
                .FirstOrDefaultAsync(jp => jp.JobPostingId == id && jp.RecruiterId == recruiter.RecruiterId);

            if (jobPosting == null)
            {
                return Json(new { success = false, message = "Job posting not found or not owned by you" });
            }

            try
            {
                var applications = await _context.JobApplications.Where(a => a.JobPostingId == id).ToListAsync();
                _context.JobApplications.RemoveRange(applications);

                _context.JobPostings.Remove(jobPosting);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Bài đăng việc làm đã được xóa."; // Dùng TempData để hiển thị sau redirect
                // Trả về URL để JS redirect
                return Json(new { success = true, redirectUrl = Url.Action("Profile") });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting job posting: {ex.Message}");
                return Json(new { success = false, message = $"Lỗi khi xóa bài đăng: {ex.Message}" });
            }
        }


         // GET: JobPostings/UpdateJobStatus (Di chuyển vào RecruitersController)
         public async Task<IActionResult> UpdateJobStatus(int id, int status)
         {
             var user = await _userManager.GetUserAsync(User);
             if (user == null || user.Role != 2)
             {
                 return RedirectToAction("Index", "Home");
             }

             var recruiter = await _context.Recruiters
                 .FirstOrDefaultAsync(r => r.UserId == user.Id);

             if (recruiter == null)
             {
                 return RedirectToAction("Profile");
             }

             // Kiểm tra xem bài đăng có thuộc về nhà tuyển dụng này không
             var jobPosting = await _context.JobPostings
                 .FirstOrDefaultAsync(jp => jp.JobPostingId == id && jp.RecruiterId == recruiter.RecruiterId);

             if (jobPosting == null)
             {
                 return NotFound();
             }

             // Chuyển đổi giá trị status thành enum JobStatus
             JobStatus newStatus;
             if (!Enum.IsDefined(typeof(JobStatus), status))
             {
                 TempData["ErrorMessage"] = "Trạng thái không hợp lệ.";
                 return RedirectToAction(nameof(JobDetails), new { id });
             }

             newStatus = (JobStatus)status;

             try
             {
                 // Xử lý chuyển trạng thái tùy thuộc vào trạng thái hiện tại
                 switch (newStatus)
                 {
                     case JobStatus.InProgress:
                         // Logic kiểm tra ứng viên chờ, tuyển...
                         var pendingApplications = await _context.JobApplications
                            .AnyAsync(a => a.JobPostingId == id && a.Status == ApplicationStatus.Pending);

                        if (pendingApplications)
                        {
                            TempData["ErrorMessage"] = "Vẫn còn ứng viên đang chờ phản hồi. Vui lòng phản hồi tất cả ứng viên trước khi bắt đầu công việc.";
                            return RedirectToAction(nameof(JobDetails), new { id });
                        }
                         var hasAcceptedApplications = await _context.JobApplications
                            .AnyAsync(a => a.JobPostingId == id && a.Status == ApplicationStatus.WaitingToStart);

                        if (!hasAcceptedApplications)
                        {
                            TempData["ErrorMessage"] = "Chưa có ứng viên nào được tuyển. Vui lòng chấp nhận ít nhất một ứng viên trước khi bắt đầu công việc.";
                            return RedirectToAction(nameof(JobDetails), new { id });
                        }

                         jobPosting.Status = JobStatus.InProgress;
                         jobPosting.UpdatedDate = DateTime.Now;
                         _context.Update(jobPosting);

                         var acceptedApplications = await _context.JobApplications
                            .Where(a => a.JobPostingId == id && a.Status == ApplicationStatus.WaitingToStart)
                            .ToListAsync();
                         foreach (var app in acceptedApplications) { app.Status = ApplicationStatus.InProgress; _context.Update(app); }
                         await _context.SaveChangesAsync();
                         TempData["SuccessMessage"] = "Đã chuyển trạng thái sang Đang thực hiện.";
                         break;

                     case JobStatus.Completed:
                         jobPosting.Status = JobStatus.Completed;
                         jobPosting.UpdatedDate = DateTime.Now;
                         _context.Update(jobPosting);

                         var inProgressApplications = await _context.JobApplications
                            .Where(a => a.JobPostingId == id && a.Status == ApplicationStatus.InProgress)
                            .ToListAsync();
                         foreach (var app in inProgressApplications) { app.Status = ApplicationStatus.Completed; _context.Update(app); }
                         await _context.SaveChangesAsync();
                         TempData["SuccessMessage"] = "Đã hoàn thành công việc.";
                         break;

                     case JobStatus.Cancelled:
                         jobPosting.Status = JobStatus.Cancelled;
                         jobPosting.UpdatedDate = DateTime.Now;
                         _context.Update(jobPosting);

                         var activeApplications = await _context.JobApplications
                            .Where(a => a.JobPostingId == id &&
                                   (a.Status == ApplicationStatus.WaitingToStart || a.Status == ApplicationStatus.InProgress))
                            .ToListAsync();
                         foreach (var app in activeApplications) { app.Status = ApplicationStatus.Canceled; _context.Update(app); }
                         await _context.SaveChangesAsync();
                         TempData["SuccessMessage"] = "Đã hủy công việc.";
                         break;
                    case JobStatus.Active: // Cho phép kích hoạt lại bài đăng (nếu cần)
                        if (jobPosting.ExpiryDate < DateTime.Now)
                        {
                             TempData["ErrorMessage"] = "Bài đăng đã hết hạn. Vui lòng gia hạn trước khi kích hoạt lại.";
                             return RedirectToAction(nameof(JobDetails), new { id });
                        }
                         jobPosting.Status = JobStatus.Active;
                         jobPosting.UpdatedDate = DateTime.Now;
                         _context.Update(jobPosting);
                         await _context.SaveChangesAsync();
                         TempData["SuccessMessage"] = "Đã kích hoạt lại bài đăng.";
                        break;

                     default:
                         TempData["ErrorMessage"] = "Không thể chuyển sang trạng thái này.";
                         break;
                 }
             }
             catch (Exception ex)
             {
                 TempData["ErrorMessage"] = $"Lỗi khi cập nhật trạng thái: {ex.Message}";
             }

             return RedirectToAction(nameof(JobDetails), new { id });
         }

         // UpdateApplicationStatus giữ nguyên trả về JSON
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateApplicationStatus(int applicationId, ApplicationStatus status, int jobPostingId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.Role != 2)
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var recruiter = await _context.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == user.Id);

            if (recruiter == null)
            {
                return Json(new { success = false, message = "Recruiter profile not found" });
            }

            var jobPostingExists = await _context.JobPostings
                .AnyAsync(jp => jp.JobPostingId == jobPostingId && jp.RecruiterId == recruiter.RecruiterId);

            if (!jobPostingExists)
            {
                return Json(new { success = false, message = "Job posting not found or not owned by you" });
            }

            var application = await _context.JobApplications
                .Include(a => a.Interpreter) // Include Interpreter để trả về thông tin nếu cần
                .FirstOrDefaultAsync(a => a.JobApplicationId == applicationId && a.JobPostingId == jobPostingId);

            if (application == null)
            {
                return Json(new { success = false, message = "Application not found" });
            }

            try
            {
                // Chỉ cho phép cập nhật nếu trạng thái hiện tại là Pending
                if (application.Status == ApplicationStatus.Pending)
                {
                    if (status == ApplicationStatus.WaitingToStart || status == ApplicationStatus.Rejected)
                    {
                        application.Status = status;
                        _context.Update(application);
                        await _context.SaveChangesAsync();

                        // Trả về thông tin mới để cập nhật UI
                        return Json(new {
                            success = true,
                            message = "Đã cập nhật trạng thái ứng tuyển.",
                            applicationId = application.JobApplicationId,
                            newStatus = (int)application.Status,
                            newStatusText = GetAppStatusDisplayNameFromEnum(application.Status), // Cần hàm helper hoặc định nghĩa lại
                            newStatusClass = GetAppStatusClassFromEnum(application.Status) // Cần hàm helper hoặc định nghĩa lại
                        });
                    }
                    else
                    {
                        return Json(new { success = false, message = "Trạng thái cập nhật không hợp lệ từ Pending." });
                    }
                }
                else
                {
                    return Json(new { success = false, message = $"Không thể thay đổi trạng thái từ {application.Status}." });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating application status: {ex.Message}");
                return Json(new { success = false, message = $"Lỗi khi cập nhật trạng thái: {ex.Message}" });
            }
        }

        // --- Thêm các hàm helper lấy tên và class cho status Enum ---
        private string GetAppStatusDisplayNameFromEnum(ApplicationStatus status) {
            var field = status.GetType().GetField(status.ToString());
            var displayAttribute = field?.GetCustomAttribute<DisplayAttribute>();
            return displayAttribute?.Name ?? status.ToString();
        }

            private string GetAppStatusClassFromEnum(ApplicationStatus status) {
            return status switch {
                ApplicationStatus.Pending => "pending",
                ApplicationStatus.WaitingToStart => "waitingtostart", // Hoặc dùng tên class khác nếu muốn
                ApplicationStatus.InProgress => "inprogress",
                ApplicationStatus.Completed => "completed",
                ApplicationStatus.Canceled => "canceled",
                ApplicationStatus.Rejected => "rejected",
                _ => "secondary"
            };
        }

        private bool JobPostingExists(int id)
        {
            return _context.JobPostings.Any(e => e.JobPostingId == id);
        }
    }
}
