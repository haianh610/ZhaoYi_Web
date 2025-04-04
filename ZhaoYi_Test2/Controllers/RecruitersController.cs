using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZhaoYi_Test2.Data;
using ZhaoYi_Test2.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace ZhaoYi_Test2.Controllers
{
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

        // Hiển thị trang hồ sơ nhà tuyển dụng
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
                return RedirectToAction("Index", "Home");
            }

            // Kiểm tra xem người dùng đã có hồ sơ nhà tuyển dụng chưa
            var recruiter = await _context.Recruiters
                .FirstOrDefaultAsync(m => m.UserId == user.Id);

            if (recruiter == null)
            {
                recruiter = new Recruiter { UserId = user.Id };
                ViewBag.HasProfile = false;
            }
            else
            {
                ViewBag.HasProfile = true;
            }

            return View(recruiter);
        }

        // Xử lý yêu cầu POST để lưu hoặc cập nhật hồ sơ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(Recruiter recruiter, IFormFile avatarFile, bool removeAvatar = false)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // In ra thông tin để debug
            System.Diagnostics.Debug.WriteLine($"Received data: Name={recruiter.RecruiterName}, Location={recruiter.WorkLocation}");
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

                ViewBag.HasProfile = await _context.Recruiters.AnyAsync(r => r.UserId == user.Id);
                return View(recruiter);
            }

            try
            {
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "avatars");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Xử lý trường hợp xóa ảnh đại diện
                if (removeAvatar && !string.IsNullOrEmpty(recruiter.AvatarPath))
                {
                    var oldFilePath = Path.Combine(uploadsFolder, recruiter.AvatarPath);
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
                    recruiter.AvatarPath = null; // Đặt lại đường dẫn ảnh
                }
                // Xử lý upload ảnh đại diện
                else if (avatarFile != null && avatarFile.Length > 0)
                {
                    // Kiểm tra kích thước file (tối đa 2MB)
                    if (avatarFile.Length > 2 * 1024 * 1024)
                    {
                        ModelState.AddModelError("", "Kích thước file quá lớn (tối đa 2MB).");
                        ViewBag.HasProfile = await _context.Recruiters.AnyAsync(r => r.UserId == user.Id);
                        return View(recruiter);
                    }

                    // Kiểm tra loại file (chỉ cho phép ảnh)
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var fileExtension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("", "Chỉ chấp nhận file ảnh (jpg, jpeg, png, gif).");
                        ViewBag.HasProfile = await _context.Recruiters.AnyAsync(r => r.UserId == user.Id);
                        return View(recruiter);
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
                    if (!string.IsNullOrEmpty(recruiter.AvatarPath) && recruiter.AvatarPath != uniqueFileName)
                    {
                        var oldFilePath = Path.Combine(uploadsFolder, recruiter.AvatarPath);
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
                    recruiter.AvatarPath = uniqueFileName;
                }

                // Kiểm tra xem đang cập nhật hay tạo mới
                var existingRecruiter = await _context.Recruiters
                    .FirstOrDefaultAsync(m => m.UserId == user.Id);

                if (existingRecruiter == null)
                {
                    // Tạo mới hồ sơ
                    recruiter.UserId = user.Id;

                    System.Diagnostics.Debug.WriteLine($"Creating new recruiter: {recruiter.RecruiterName} for user {user.Id}");

                    _context.Recruiters.Add(recruiter);
                    await _context.SaveChangesAsync();

                    TempData["StatusMessage"] = "Hồ sơ nhà tuyển dụng đã được tạo thành công.";
                    System.Diagnostics.Debug.WriteLine("Profile created successfully");
                }
                else
                {
                    // Cập nhật hồ sơ hiện có
                    existingRecruiter.RecruiterName = recruiter.RecruiterName;
                    existingRecruiter.WorkLocation = recruiter.WorkLocation;
                    existingRecruiter.DetailedAddress = recruiter.DetailedAddress;

                    // Cập nhật AvatarPath nếu có upload ảnh mới hoặc xóa ảnh
                    if (avatarFile != null && avatarFile.Length > 0 || removeAvatar)
                    {
                        existingRecruiter.AvatarPath = recruiter.AvatarPath;
                    }

                    System.Diagnostics.Debug.WriteLine($"Updating recruiter: {existingRecruiter.RecruiterName}");

                    _context.Update(existingRecruiter);
                    await _context.SaveChangesAsync();

                    TempData["StatusMessage"] = "Hồ sơ nhà tuyển dụng đã được cập nhật thành công.";
                    System.Diagnostics.Debug.WriteLine("Profile updated successfully");
                }

                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                // Bắt lỗi và hiển thị
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
                ModelState.AddModelError("", $"Không thể lưu dữ liệu: {ex.Message}");
                ViewBag.HasProfile = await _context.Recruiters.AnyAsync(r => r.UserId == user.Id);
                return View(recruiter);
            }
        }


    }
}
