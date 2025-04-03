using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ZhaoYi_Test2.Data;
using ZhaoYi_Test2.Models;
using System;
using System.Threading.Tasks;

namespace ZhaoYi_Test2.Controllers
{
    public class RecruitersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RecruitersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Hi?n th? trang h? s? nh� tuy?n d?ng
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Ch? cho ph�p ng??i d�ng c� vai tr� nh� tuy?n d?ng (Role = 2) truy c?p trang n�y
            if (user.Role != 2)
            {
                return RedirectToAction("Index", "Home");
            }

            // Ki?m tra xem ng??i d�ng ?� c� h? s? nh� tuy?n d?ng ch?a
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

        // X? l� y�u c?u POST ?? l?u ho?c c?p nh?t h? s?
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(Recruiter recruiter)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // In ra th�ng tin ?? debug
            System.Diagnostics.Debug.WriteLine($"Received data: Name={recruiter.RecruiterName}, Location={recruiter.WorkLocation}");

            // X�a l?i validation cho tr??ng User n?u c�
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

                ViewBag.HasProfile = await _context.Recruiters.AnyAsync(r => r.UserId == user.Id);
                return View(recruiter);
            }

            try
            {
                // Ki?m tra xem ?ang c?p nh?t hay t?o m?i
                var existingRecruiter = await _context.Recruiters
                    .FirstOrDefaultAsync(m => m.UserId == user.Id);

                if (existingRecruiter == null)
                {
                    // T?o m?i h? s?
                    recruiter.UserId = user.Id;

                    System.Diagnostics.Debug.WriteLine($"Creating new recruiter: {recruiter.RecruiterName} for user {user.Id}");

                    _context.Recruiters.Add(recruiter);
                    await _context.SaveChangesAsync();

                    TempData["StatusMessage"] = "H? s? nh� tuy?n d?ng ?� ???c t?o th�nh c�ng.";
                    System.Diagnostics.Debug.WriteLine("Profile created successfully");
                }
                else
                {
                    // C?p nh?t h? s? hi?n c�
                    existingRecruiter.RecruiterName = recruiter.RecruiterName;
                    existingRecruiter.WorkLocation = recruiter.WorkLocation;
                    existingRecruiter.DetailedAddress = recruiter.DetailedAddress;

                    System.Diagnostics.Debug.WriteLine($"Updating recruiter: {existingRecruiter.RecruiterName}");

                    _context.Update(existingRecruiter);
                    await _context.SaveChangesAsync();

                    TempData["StatusMessage"] = "H? s? nh� tuy?n d?ng ?� ???c c?p nh?t th�nh c�ng.";
                    System.Diagnostics.Debug.WriteLine("Profile updated successfully");
                }

                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                // B?t l?i v� hi?n th?
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
                ModelState.AddModelError("", $"Kh�ng th? l?u d? li?u: {ex.Message}");
                ViewBag.HasProfile = await _context.Recruiters.AnyAsync(r => r.UserId == user.Id);
                return View(recruiter);
            }
        }
    }
}
