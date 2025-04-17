using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using ZhaoYi_Test2.Data;
using ZhaoYi_Test2.Models;

namespace ZhaoYi_Test2.Controllers
{
    [Authorize]
    public class JobPostingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public JobPostingsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: JobPostings
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Chỉ cho phép nhà tuyển dụng (Role = 2) truy cập
            if (user.Role != 2)
            {
                return RedirectToAction("Index", "Home");
            }

            // Lấy thông tin nhà tuyển dụng
            var recruiter = await _context.Recruiters
                .FirstOrDefaultAsync(r => r.UserId == user.Id);

            if (recruiter == null)
            {
                TempData["ErrorMessage"] = "Bạn cần tạo hồ sơ nhà tuyển dụng trước khi đăng tin tuyển dụng.";
                return RedirectToAction("Profile", "Recruiters");
            }

            // Lấy các bài đăng của nhà tuyển dụng này
            var jobPostings = await _context.JobPostings
                .Where(jp => jp.RecruiterId == recruiter.RecruiterId)
                .OrderByDescending(jp => jp.PostedDate)
                .ToListAsync();

            return View(jobPostings);
        }

        // GET: JobPostings/Details/5
        public async Task<IActionResult> Details(int? id)
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
                return RedirectToAction("Profile", "Recruiters");
            }

            var jobPosting = await _context.JobPostings
                .Include(j => j.Recruiter)
                .FirstOrDefaultAsync(m => m.JobPostingId == id && m.RecruiterId == recruiter.RecruiterId);

            if (jobPosting == null)
            {
                return NotFound();
            }

            // Lấy danh sách đơn ứng tuyển cho bài đăng này kèm thông tin interpreter
            var applications = await _context.JobApplications
                .Where(a => a.JobPostingId == id)
                .Include(a => a.Interpreter)
                .OrderByDescending(a => a.ApplicationDate)
                .ToListAsync();

            ViewBag.Applications = applications;

            return View(jobPosting);
        }



        // GET: JobPostings/Create
        public async Task<IActionResult> Create()
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
                TempData["ErrorMessage"] = "Bạn cần tạo hồ sơ nhà tuyển dụng trước khi đăng tin tuyển dụng.";
                return RedirectToAction("Profile", "Recruiters");
            }

            // Khởi tạo JobPosting với UserId đã được gán
            var jobPosting = new JobPosting
            {
                UserId = user.Id,
                RecruiterId = recruiter.RecruiterId,
                PostedDate = DateTime.Now,
                Status = JobStatus.Active,
                ExpiryDate = DateTime.Now.AddDays(30)
            };

            return View(jobPosting);
        }


        // POST: JobPostings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JobPosting jobPosting)
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
                TempData["ErrorMessage"] = "Bạn cần tạo hồ sơ nhà tuyển dụng trước khi đăng tin tuyển dụng.";
                return RedirectToAction("Profile", "Recruiters");
            }

            // Gán UserId trước khi kiểm tra ModelState
            jobPosting.UserId = user.Id;
            jobPosting.RecruiterId = recruiter.RecruiterId;

            // Xóa validation cho User và Recruiter
            ModelState.Remove("User");
            ModelState.Remove("Recruiter");

            if (ModelState.IsValid)
            {
                try
                {
                    // Cập nhật các giá trị mặc định
                    jobPosting.PostedDate = DateTime.Now;
                    jobPosting.Status = JobStatus.Pending;
                    jobPosting.ViewCount = 0;

                    _context.Add(jobPosting);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Bài đăng việc làm đã được tạo và đang chờ duyệt.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Lỗi khi tạo bài đăng: {ex.Message}");
                }
            }
            else
            {
                // Log lỗi validation để debug
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    System.Diagnostics.Debug.WriteLine($"Validation error: {error.ErrorMessage}");
                }
            }

            return View(jobPosting);
        }


        // GET: JobPostings/Edit/5
        public async Task<IActionResult> Edit(int? id)
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
                return RedirectToAction("Profile", "Recruiters");
            }

            var jobPosting = await _context.JobPostings
                .FirstOrDefaultAsync(jp => jp.JobPostingId == id && jp.RecruiterId == recruiter.RecruiterId);

            if (jobPosting == null)
            {
                return NotFound();
            }

            return View(jobPosting);
        }

        // POST: JobPostings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, JobPosting jobPosting)
        {
            if (id != jobPosting.JobPostingId)
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
                return RedirectToAction("Profile", "Recruiters");
            }

            // Kiểm tra xem bài đăng có thuộc về nhà tuyển dụng này không
            var existingJob = await _context.JobPostings
                .FirstOrDefaultAsync(jp => jp.JobPostingId == id && jp.RecruiterId == recruiter.RecruiterId);

            if (existingJob == null)
            {
                return NotFound();
            }

            // Xóa validation cho User và Recruiter nếu có
            if (ModelState.ContainsKey("User"))
            {
                ModelState.Remove("User");
            }
            if (ModelState.ContainsKey("Recruiter"))
            {
                ModelState.Remove("Recruiter");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Cập nhật các trường có thể chỉnh sửa
                    existingJob.Title = jobPosting.Title;
                    existingJob.JobDescription = jobPosting.JobDescription;
                    existingJob.MinSalary = jobPosting.MinSalary;
                    existingJob.MaxSalary = jobPosting.MaxSalary;
                    existingJob.WorkLocation = jobPosting.WorkLocation;
                    existingJob.Field = jobPosting.Field;
                    existingJob.RequiredExperience = jobPosting.RequiredExperience;
                    existingJob.GenderRequirement = jobPosting.GenderRequirement;
                    existingJob.HiringCount = jobPosting.HiringCount;
                    existingJob.EducationLevel = jobPosting.EducationLevel;
                    existingJob.ExpiryDate = jobPosting.ExpiryDate;
                    
                    // Cập nhật trạng thái và ngày cập nhật
                    existingJob.Status = JobStatus.Active; // Đưa về trạng thái chờ duyệt sau khi sửa, tạm thời auto duyệt
                    existingJob.UpdatedDate = DateTime.Now;

                    _context.Update(existingJob);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Bài đăng việc làm đã được cập nhật và đang chờ duyệt lại.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!JobPostingExists(jobPosting.JobPostingId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(jobPosting);
        }

        // GET: JobPostings/Delete/5
        public async Task<IActionResult> Delete(int? id)
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
                return RedirectToAction("Profile", "Recruiters");
            }

            var jobPosting = await _context.JobPostings
                .Include(j => j.Recruiter)
                .FirstOrDefaultAsync(m => m.JobPostingId == id && m.RecruiterId == recruiter.RecruiterId);

            if (jobPosting == null)
            {
                return NotFound();
            }

            return View(jobPosting);
        }

        // POST: JobPostings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
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
                return RedirectToAction("Profile", "Recruiters");
            }

            var jobPosting = await _context.JobPostings
                .FirstOrDefaultAsync(jp => jp.JobPostingId == id && jp.RecruiterId == recruiter.RecruiterId);

            if (jobPosting == null)
            {
                return NotFound();
            }

            _context.JobPostings.Remove(jobPosting);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Bài đăng việc làm đã được xóa.";
            return RedirectToAction(nameof(Index));
        }

        private bool JobPostingExists(int id)
        {
            return _context.JobPostings.Any(e => e.JobPostingId == id);
        }

        // GET: JobPostings/UpdateJobStatus
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
                return RedirectToAction("Profile", "Recruiters");
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
                return RedirectToAction(nameof(Details), new { id });
            }

            newStatus = (JobStatus)status;

            try
            {
                // Xử lý chuyển trạng thái tùy thuộc vào trạng thái hiện tại
                switch (newStatus)
                {
                    case JobStatus.InProgress:
                        // Kiểm tra xem còn ứng viên chưa phản hồi không
                        var pendingApplications = await _context.JobApplications
                            .AnyAsync(a => a.JobPostingId == id && a.Status == ApplicationStatus.Pending);

                        if (pendingApplications)
                        {
                            TempData["ErrorMessage"] = "Vẫn còn ứng viên đang chờ phản hồi. Vui lòng phản hồi tất cả ứng viên trước khi bắt đầu công việc.";
                            return RedirectToAction(nameof(Details), new { id });
                        }

                        // Kiểm tra xem có ít nhất một ứng viên được tuyển không
                        var hasAcceptedApplications = await _context.JobApplications
                            .AnyAsync(a => a.JobPostingId == id && a.Status == ApplicationStatus.WaitingToStart);

                        if (!hasAcceptedApplications)
                        {
                            TempData["ErrorMessage"] = "Chưa có ứng viên nào được tuyển. Vui lòng chấp nhận ít nhất một ứng viên trước khi bắt đầu công việc.";
                            return RedirectToAction(nameof(Details), new { id });
                        }

                        // Cập nhật trạng thái bài đăng
                        jobPosting.Status = JobStatus.InProgress;
                        jobPosting.UpdatedDate = DateTime.Now;
                        _context.Update(jobPosting);

                        // Cập nhật trạng thái của ứng viên đã chấp nhận thành "Đang thực hiện"
                        var acceptedApplications = await _context.JobApplications
                            .Where(a => a.JobPostingId == id && a.Status == ApplicationStatus.WaitingToStart)
                            .ToListAsync();

                        foreach (var app in acceptedApplications)
                        {
                            app.Status = ApplicationStatus.InProgress;
                            _context.Update(app);
                        }

                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Đã chuyển trạng thái sang Đang thực hiện.";
                        break;

                    case JobStatus.Completed:
                        // Cập nhật trạng thái bài đăng thành "Đã hoàn thành"
                        jobPosting.Status = JobStatus.Completed;
                        jobPosting.UpdatedDate = DateTime.Now;
                        _context.Update(jobPosting);

                        // Cập nhật trạng thái của ứng viên thành "Hoàn thành"
                        var inProgressApplications = await _context.JobApplications
                            .Where(a => a.JobPostingId == id && a.Status == ApplicationStatus.InProgress)
                            .ToListAsync();

                        foreach (var app in inProgressApplications)
                        {
                            app.Status = ApplicationStatus.Completed;
                            _context.Update(app);
                        }

                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Đã hoàn thành công việc.";
                        break;

                    case JobStatus.Cancelled:
                        // Cập nhật trạng thái bài đăng thành "Đã hủy"
                        jobPosting.Status = JobStatus.Cancelled;
                        jobPosting.UpdatedDate = DateTime.Now;
                        _context.Update(jobPosting);

                        // Cập nhật trạng thái của tất cả ứng viên đang thực hiện hoặc chờ bắt đầu
                        var activeApplications = await _context.JobApplications
                            .Where(a => a.JobPostingId == id &&
                                   (a.Status == ApplicationStatus.WaitingToStart || a.Status == ApplicationStatus.InProgress))
                            .ToListAsync();

                        foreach (var app in activeApplications)
                        {
                            app.Status = ApplicationStatus.Canceled;
                            _context.Update(app);
                        }

                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Đã hủy công việc.";
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

            return RedirectToAction(nameof(Details), new { id });
        }


        // GET: JobPostings/UpdateApplicationStatus
        public async Task<IActionResult> UpdateApplicationStatus(int id, int status, int jobPostingId)
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
                return RedirectToAction("Profile", "Recruiters");
            }

            // Kiểm tra xem bài đăng có thuộc về nhà tuyển dụng này không
            var jobPosting = await _context.JobPostings
                .FirstOrDefaultAsync(jp => jp.JobPostingId == jobPostingId && jp.RecruiterId == recruiter.RecruiterId);

            if (jobPosting == null)
            {
                return NotFound();
            }

            // Lấy đơn ứng tuyển cần cập nhật
            var application = await _context.JobApplications
                .FirstOrDefaultAsync(a => a.JobApplicationId == id && a.JobPostingId == jobPostingId);

            if (application == null)
            {
                return NotFound();
            }

            try
            {
                // Kiểm tra trạng thái hợp lệ
                if (Enum.IsDefined(typeof(ApplicationStatus), status))
                {
                    // Cập nhật trạng thái
                    application.Status = (ApplicationStatus)status;
                    _context.Update(application);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Đã cập nhật trạng thái ứng tuyển.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Trạng thái không hợp lệ.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi khi cập nhật trạng thái: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id = jobPostingId });
        }



    }
}
