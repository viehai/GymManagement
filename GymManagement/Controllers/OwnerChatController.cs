using GymManagement.Helpers;
using GymManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Controllers
{
    [Authorize(Roles = "Owner")]
    public class OwnerChatController : Controller
    {
        private readonly GymDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public OwnerChatController(GymDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // Lấy gym của Owner hiện tại (chỉ lấy Approved)
        private async Task<Gym?> GetOwnerGymAsync(string ownerId)
        {
            return await _db.Gyms
                .FirstOrDefaultAsync(g => g.OwnerId == ownerId && g.Status == "Approved");
        }

        // ── GET /OwnerChat  — Inbox tổng ─────────────────────────────
        public async Task<IActionResult> Index()
        {
            var ownerId = _userManager.GetUserId(User);
            var gym = await GetOwnerGymAsync(ownerId!);
            if (gym == null) return RedirectToAction("Index", "OwnerDashboard");

            var conversations = await _db.ChatConversations
                .Where(c => c.GymId == gym.Id)
                .Include(c => c.Member)
                .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
                .OrderByDescending(c => c.LastMessageAt)
                .ToListAsync();

            int totalUnread = conversations.Sum(c => c.OwnerUnread);

            // Lịch sử broadcast gần nhất
            var broadcasts = await _db.ChatMessages
                .Where(m => m.GymId == gym.Id && m.MessageType == "Broadcast")
                .OrderByDescending(m => m.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.Gym           = gym;
            ViewBag.Conversations = conversations;
            ViewBag.TotalUnread   = totalUnread;
            ViewBag.Broadcasts    = broadcasts;

            return View();
        }

        // ── GET /OwnerChat/Conversation/{id}  — Chat 1-1 ─────────────
        [HttpGet]
        public async Task<IActionResult> Conversation(string? id, string? memberId)
        {
            var targetMemberId = id ?? memberId;
            if (string.IsNullOrEmpty(targetMemberId)) return NotFound();

            var ownerId = _userManager.GetUserId(User);
            var gym = await GetOwnerGymAsync(ownerId!);
            if (gym == null) return RedirectToAction("Index");

            var member = await _userManager.FindByIdAsync(targetMemberId);
            if (member == null) return NotFound();

            // Lấy hoặc tạo conversation
            var conv = await _db.ChatConversations
                .FirstOrDefaultAsync(c => c.MemberId == targetMemberId && c.GymId == gym.Id);

            if (conv == null)
            {
                conv = new ChatConversation
                {
                    MemberId      = memberId,
                    GymId         = gym.Id,
                    LastMessageAt = VnTime.Now,
                    CreatedAt     = VnTime.Now
                };
                _db.ChatConversations.Add(conv);
                await _db.SaveChangesAsync();
            }
            else if (conv.OwnerUnread > 0)
            {
                conv.OwnerUnread = 0;
                await _db.SaveChangesAsync();
            }

            // Load 50 tin nhắn gần nhất
            var messages = await _db.ChatMessages
                .Where(m => m.ConversationId == conv.Id)
                .Include(m => m.Sender)
                .OrderByDescending(m => m.CreatedAt)
                .Take(50)
                .ToListAsync();

            messages.Reverse();

            ViewBag.Gym            = gym;
            ViewBag.Member         = member;
            ViewBag.Conversation   = conv;
            ViewBag.Messages       = messages;
            ViewBag.CurrentUserId  = ownerId;

            return View();
        }

        // ── GET /OwnerChat/Broadcast  — Trang gửi thông báo hàng loạt
        public async Task<IActionResult> Broadcast()
        {
            var ownerId = _userManager.GetUserId(User);
            var gym = await GetOwnerGymAsync(ownerId!);
            if (gym == null) return RedirectToAction("Index");

            // Lịch sử broadcast
            var history = await _db.ChatMessages
                .Where(m => m.GymId == gym.Id && m.MessageType == "Broadcast")
                .OrderByDescending(m => m.CreatedAt)
                .Take(30)
                .ToListAsync();

            // Tổng số member active
            var memberCount = await _db.MemberMemberships
                .Where(m => m.GymId == gym.Id && m.EndDate >= DateTime.UtcNow)
                .Select(m => m.MemberId)
                .Distinct()
                .CountAsync();

            ViewBag.Gym         = gym;
            ViewBag.History     = history;
            ViewBag.MemberCount = memberCount;

            return View();
        }

        // ── POST /OwnerChat/Broadcast  — Gửi thông báo hàng loạt qua HTTP POST ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Broadcast(string content, string? imageUrl)
        {
            var ownerId = _userManager.GetUserId(User);
            var gym = await GetOwnerGymAsync(ownerId!);
            if (gym == null) return RedirectToAction("Index");

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["Error"] = "Nội dung thông báo không được để trống.";
                return RedirectToAction(nameof(Broadcast));
            }

            if (content.Length > 2000) content = content[..2000];

            var msg = new ChatMessage
            {
                GymId       = gym.Id,
                SenderId    = ownerId!,
                Content     = content.Trim(),
                ImageUrl    = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim(),
                SenderRole  = "Owner",
                MessageType = "Broadcast",
                IsRead      = false,
                CreatedAt   = VnTime.Now
            };

            _db.ChatMessages.Add(msg);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Đã gửi thông báo đến toàn bộ hội viên thành công!";
            return RedirectToAction(nameof(Broadcast));
        }

        // ── GET /OwnerChat/GetHistory — JSON lịch sử tin nhắn ────────
        [HttpGet]
        public async Task<IActionResult> GetHistory(int conversationId, int beforeId = 0, int pageSize = 30)
        {
            var ownerId = _userManager.GetUserId(User);
            var gym = await GetOwnerGymAsync(ownerId!);
            if (gym == null) return Forbid();

            var conv = await _db.ChatConversations
                .FirstOrDefaultAsync(c => c.Id == conversationId && c.GymId == gym.Id);

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
                    isOwn      = m.SenderId == ownerId
                })
                .ToListAsync();

            return Json(messages.AsEnumerable().Reverse());
        }
    }
}
