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
        // Trong MessagesController.cs

        // GET: /Messages/Chat/{id}
        [HttpGet]
        public async Task<IActionResult> Chat(int id) // Thêm async Task
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // --- TODO: Lấy dữ liệu chat thực tế dựa vào 'id' (ConversationId) ---
            // Ví dụ: Lấy thông tin người/nhóm đang chat, danh sách tin nhắn...
            // var conversation = await _context.Conversations
            //     .Include(c => c.Participants.Where(p => p.UserId != user.Id).Select(p => p.User)) // Lấy người chat cùng
            //     .Include(c => c.Messages.OrderBy(m => m.Timestamp)) // Lấy tin nhắn
            //     .FirstOrDefaultAsync(c => c.ConversationId == id && c.Participants.Any(p => p.UserId == user.Id));

            // if (conversation == null) return NotFound();

            // var otherParticipant = conversation.Participants.FirstOrDefault(p => p.UserId != user.Id)?.User;
            // string contactName = conversation.IsGroup ? conversation.GroupName : otherParticipant?.UserName ?? "Người lạ";
            // string contactAvatar = conversation.IsGroup ? conversation.GroupAvatar : (Logic lấy avatar của otherParticipant);
            // string contactStatus = "Đang hoạt động"; // Lấy status thực tế

            // var chatMessages = conversation.Messages.Select(m => new { ... }); // Map sang dynamic hoặc ViewModel

            // --- Tạm thời dùng dữ liệu giả ---
            ViewBag.ChatId = id;
            ViewBag.ContactName = "Lê Minh Nghĩa"; // Giả
            ViewBag.ContactStatus = "Đang hoạt động"; // Giả
            ViewBag.ContactAvatarUrl = "~/images/user_2.png"; // Giả

            var chatMessages = new List<dynamic> {
        new { Id = 1, Sender = "You", Text = "Xin chào Minh Nghĩa", Timestamp = "09:25 AM", IsOutgoing = true, ShowAvatar = false, AvatarUrl = "", IsFirstInSequence = true, IsLastInSequence = true },
        new { Id = 2, Sender = "Lê Minh Nghĩa", Text = "Xin chào bạn, bạn là ai?", Timestamp = "09:25 AM", IsOutgoing = false, ShowAvatar = true, AvatarUrl = "~/images/user_2.png", IsFirstInSequence = true, IsLastInSequence = true },
        new { Id = 3, Sender = "You", Text = "Tôi muốn apply vào vị trí phiên dịch viên", Timestamp = "09:25 AM", IsOutgoing = true, ShowAvatar = false, AvatarUrl = "", IsFirstInSequence = true, IsLastInSequence = true },
        new { Id = 4, Sender = "Lê Minh Nghĩa", Text = "Bạn gửi mình CV để mình xem nhé", Timestamp = "09:25 AM", IsOutgoing = false, ShowAvatar = true, AvatarUrl = "~/images/user_2.png", IsFirstInSequence = true, IsLastInSequence = false },
        new { Id = 5, Sender = "Lê Minh Nghĩa", Text = "Để mình xem sao", Timestamp = "09:25 AM", IsOutgoing = false, ShowAvatar = false, AvatarUrl = "", IsFirstInSequence = false, IsLastInSequence = true }
    };
            ViewBag.ChatMessages = chatMessages;
            // --------------------------------

            ViewData["Title"] = ViewBag.ContactName; // Đặt tiêu đề là tên người chat
            ViewData["UserRole"] = user.Role == 1 ? "Interpreter" : (user.Role == 2 ? "Recruiter" : "User");
            ViewData["ShowBottomNav"] = false; // Ẩn nav khi đang chat

            return View("Chat"); // Trả về View Chat.cshtml
        }
    }
}