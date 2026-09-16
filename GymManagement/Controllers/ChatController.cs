using GymManagement.Helpers;
using GymManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly GymDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatController(GymDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ── GET /Chat  — Inbox tổng ──────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var conversations = await _db.ChatConversations
                .Where(c => c.MemberId == userId)
                .Include(c => c.Gym)
                .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();

            // Lấy danh sách các phòng gym user là hội viên (có Membership)
            var myGyms = await _db.MemberMemberships
                .Where(m => m.MemberId == userId)
                .Select(m => m.Gym)
                .Where(g => g != null)
                .Distinct()
                .ToListAsync();

            var memberGymIds = myGyms.Select(g => g.Id).ToList();
            var convGymIds   = conversations.Select(c => c.GymId).ToList();
            var allGymIds    = memberGymIds.Union(convGymIds).Distinct().ToList();

            // Broadcast mới nhất từ các gym user là hội viên hoặc đã từng trò chuyện
            var latestBroadcasts = await _db.ChatMessages
                .Where(m => m.MessageType == "Broadcast" && m.GymId.HasValue && allGymIds.Contains(m.GymId.Value))
                .Include(m => m.Gym)
                .Include(m => m.Sender)
                .OrderByDescending(m => m.CreatedAt)
                .Take(20)
                .ToListAsync();

            ViewBag.Conversations    = conversations;
            ViewBag.MyGyms           = myGyms;
            ViewBag.LatestBroadcasts = latestBroadcasts;
            ViewBag.TotalUnread      = conversations.Sum(c => c.MemberUnread);
            ViewBag.CurrentUserId    = userId;

            return View();
        }

        // ── GET /Chat/Conversation/{id}  — Chat 1-1 ──────────────────
        [HttpGet]
        public async Task<IActionResult> Conversation(int? id, int? gymId)
        {
            int targetGymId = id ?? gymId ?? 0;
            if (targetGymId <= 0) return NotFound();

            var userId = _userManager.GetUserId(User);

            var gym = await _db.Gyms
                .Include(g => g.Owner)
                .FirstOrDefaultAsync(g => g.Id == targetGymId);

            if (gym == null) return NotFound();

            // Lấy hoặc tạo conversation
            var conv = await _db.ChatConversations
                .FirstOrDefaultAsync(c => c.MemberId == userId && c.GymId == targetGymId);

            if (conv == null)
            {
                conv = new ChatConversation
                {
                    MemberId      = userId!,
                    GymId         = targetGymId,
                    LastMessageAt = VnTime.Now,
                    CreatedAt     = VnTime.Now
                };
                _db.ChatConversations.Add(conv);
                await _db.SaveChangesAsync();
            }
            else
            {
                // Đánh dấu đã đọc khi mở conversation
                if (conv.MemberUnread > 0)
                {
                    conv.MemberUnread = 0;
                    await _db.SaveChangesAsync();
                }
            }

            // Load 50 tin nhắn gần nhất
            var messages = await _db.ChatMessages
                .Where(m => m.ConversationId == conv.Id)
                .Include(m => m.Sender)
                .OrderByDescending(m => m.CreatedAt)
                .Take(50)
                .ToListAsync();

            messages.Reverse(); // hiển thị cũ → mới

            ViewBag.Gym            = gym;
            ViewBag.Conversation   = conv;
            ViewBag.Messages       = messages;
            ViewBag.CurrentUserId  = userId;

            return View();
        }

        // ── GET /Chat/GetHistory  — JSON API phân trang ───────────────
        [HttpGet]
        public async Task<IActionResult> GetHistory(int conversationId, int beforeId = 0, int pageSize = 30)
        {
            var userId = _userManager.GetUserId(User);

            var conv = await _db.ChatConversations
                .FirstOrDefaultAsync(c => c.Id == conversationId && c.MemberId == userId);

            if (conv == null) return Forbid();

            var query = _db.ChatMessages
                .Where(m => m.ConversationId == conversationId);

            if (beforeId > 0) query = query.Where(m => m.Id < beforeId);

            var messages = await query
                .Include(m => m.Sender)
                .OrderByDescending(m => m.CreatedAt)
                .Take(pageSize)
                .Select(m => new
                {
                    id         = m.Id,
                    senderId   = m.SenderId,
                    senderName = m.Sender.FullName,
                    senderRole = m.SenderRole,
                    content    = m.Content,
                    imageUrl   = m.ImageUrl,
                    createdAt  = m.CreatedAt.ToString("HH:mm dd/MM/yyyy"),
                    isOwn      = m.SenderId == userId
                })
                .ToListAsync();

            return Json(messages.AsEnumerable().Reverse());
        }

        // ── GET /Chat/GetConversations  — JSON unread counts ─────────
        [HttpGet]
        public async Task<IActionResult> GetConversations()
        {
            var userId = _userManager.GetUserId(User);

            var data = await _db.ChatConversations
                .Where(c => c.MemberId == userId)
                .Select(c => new
                {
                    id         = c.Id,
                    gymId      = c.GymId,
                    gymName    = c.Gym.Name,
                    unread     = c.MemberUnread,
                    lastAt     = c.LastMessageAt
                })
                .OrderByDescending(c => c.lastAt)
                .ToListAsync();

            return Json(data);
        }
    }
}
