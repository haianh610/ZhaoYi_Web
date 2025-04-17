using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ZhaoYi_Test2.Data; // Giả sử bạn có DbContext
using ZhaoYi_Test2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ZhaoYi_Test2.Controllers
{
    [Authorize] // Yêu cầu đăng nhập
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext _context; // Thêm nếu cần truy vấn DB
        private readonly UserManager<ApplicationUser> _userManager;

        public MessagesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Messages/Index hoặc /Messages
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge(); // Hoặc Redirect tới Login
            }

            // --- Lấy dữ liệu thực tế ở đây ---
            // Ví dụ: Lấy danh sách cuộc trò chuyện của người dùng từ DB
            // var conversations = await _context.Conversations
            //    .Where(c => c.Participants.Any(p => p.UserId == user.Id))
            //    .Include(c => c.Messages.OrderByDescending(m => m.Timestamp).Take(1)) // Lấy tin nhắn cuối
            //    .Include(c => c.Participants)
            //    .ToListAsync();

            // --- Tạm thời dùng dữ liệu giả từ View cũ ---
            var messages = new List<object> {
                new { Id = 1, Sender = "Nguyễn Chính", Snippet = "Tin nhắn", Timestamp = "2 phút trước", UnreadCount = 3, IsGroup = false, AvatarUrl = "~/images/user.png" },
                new { Id = 2, Sender = "Nhóm phiên dịch...", Snippet = "Thông báo mới", Timestamp = "3 phút trước", UnreadCount = 4, IsGroup = true, GroupAvatarUrls = new List<string> { "~/images/user.png", "~/images/user_2.png", "~/images/user.png" } },
                new { Id = 3, Sender = "Họ tên", Snippet = "Tin nhắn A", Timestamp = "2 phút trước", UnreadCount = 0, IsGroup = false, AvatarUrl = "~/images/user_2.png" },
                new { Id = 4, Sender = "Cient Company", Snippet = "How are you today?", Timestamp = "2 phút trước", UnreadCount = 0, IsGroup = false, AvatarUrl = "~/images/user.png" },
                new { Id = 5, Sender = "Nguyễn Chính", Snippet = "Have a good day 🌸", Timestamp = "3 ngày trước", UnreadCount = 0, IsGroup = false, AvatarUrl = "~/images/user_2.png" },
            };
             ViewBag.Messages = messages;

             var stories = new List<object> {
                new { Id = "add", Name = "Trạng thái...", IsAdd = true, AvatarUrl = "~/images/user.png", BgClass="" },
                new { Id = 101, Name = "Họ tên", IsAdd = false, AvatarUrl = "~/images/user.png", BgClass="yellow-bg" },
                new { Id = 102, Name = "Họ tên", IsAdd = false, AvatarUrl = "~/images/user_2.png", BgClass="pink-bg" },
                new { Id = 103, Name = "Họ tên", IsAdd = false, AvatarUrl = "~/images/user.png", BgClass="light-bg" },
                new { Id = 104, Name = "Họ tên", IsAdd = false, AvatarUrl = "~/images/user_2.png", BgClass="dark-bg" },
            };
            ViewBag.Stories = stories;

            var peopleSearchResults = new List<object> {
                new { Id = 1, Name = "Họ tên", Status = "Be your own hero 💪", AvatarUrl = "~/images/user.png" },
                new { Id = 2, Name = "Họ tên", Status = "Keep working 💻", AvatarUrl = "~/images/user_2.png" },
                new { Id = 3, Name = "Họ tên", Status = "Make yourself proud 🏆", AvatarUrl = "~/images/user.png" }
            };
             ViewBag.PeopleSearchResults = peopleSearchResults;

            var groupSearchResults = new List<object> {
                new { Id = 1, Name = "Nhóm phiên dịch", ParticipantCount = 4, AvatarUrl = "~/images/user.png" },
                new { Id = 2, Name = "Team Viettrans", ParticipantCount = 8, AvatarUrl = "~/images/user_2.png" }
            };
            ViewBag.GroupSearchResults = groupSearchResults;


            // Lấy avatar người dùng hiện tại
            string currentUserAvatarUrl = "/images/user.png"; // Default
             if (user.Role == 1) { // Interpreter
                 var interpreter = await _context.Interpreters.FirstOrDefaultAsync(i => i.UserId == user.Id);
                 if (interpreter != null && !string.IsNullOrEmpty(interpreter.AvatarPath)) {
                     currentUserAvatarUrl = $"/uploads/avatars/{interpreter.AvatarPath}";
                 }
             } else if (user.Role == 2) { // Recruiter
                 var recruiter = await _context.Recruiters.FirstOrDefaultAsync(r => r.UserId == user.Id);
                 if (recruiter != null && !string.IsNullOrEmpty(recruiter.AvatarPath)) {
                     currentUserAvatarUrl = $"/uploads/avatars/{recruiter.AvatarPath}";
                 }
             }
             ViewBag.CurrentUserAvatarUrl = currentUserAvatarUrl;

            // Xác định vai trò người dùng cho layout
            ViewData["UserRole"] = user.Role == 1 ? "Interpreter" : (user.Role == 2 ? "Recruiter" : "User");
            ViewData["ShowBottomNav"] = true;
            ViewData["Title"] = "Tin nhắn";

            return View(); // Trả về View Index.cshtml trong thư mục Views/Messages
        }

         // Action cho trang Chat chi tiết (sẽ cần tạo View riêng)
         // GET: /Messages/Chat/{id}
         public IActionResult Chat(int id)
         {
             // TODO: Lấy thông tin cuộc trò chuyện và tin nhắn theo id
             ViewData["ChatId"] = id; // Truyền ID vào View
             ViewData["ShowBottomNav"] = false; // Thường ẩn nav khi chat
             ViewData["Title"] = "Chat"; // Đặt tiêu đề động sau

             // return View("Chat"); // Trả về View Chat.cshtml
             return Content($"Trang chat cho ID: {id} (Chưa tạo View)"); // Placeholder
         }

        // Action cho tìm kiếm (API endpoint) - Sẽ cần tạo sau
        // [HttpGet("api/messages/search")]
        // public async Task<IActionResult> Search(string query, string type = "people") { ... }
    }
}