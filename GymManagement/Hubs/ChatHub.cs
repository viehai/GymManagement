using GymManagement.Helpers;
using GymManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Hubs
{
    /// <summary>
    /// SignalR Hub cho hệ thống chat real-time.
    /// Groups:
    ///   - "user-{userId}"  → gửi tin nhắn đến đúng người
    ///   - "gym-{gymId}"    → broadcast của Owner đến toàn bộ member gym
    /// </summary>
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly GymDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatHub(GymDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ────────────────────────────────────────────────────────────────
        // CONNECTION LIFECYCLE
        // ────────────────────────────────────────────────────────────────
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier
                ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                // Join personal group để nhận tin nhắn cá nhân
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");

                // Join group của tất cả gym mình là member hoặc đã có hội thoại để nhận broadcast
                var memberGymIds = await _db.MemberMemberships
                    .Where(m => m.MemberId == userId)
                    .Select(m => m.GymId)
                    .ToListAsync();

                var convGymIds = await _db.ChatConversations
                    .Where(c => c.MemberId == userId)
                    .Select(c => c.GymId)
                    .ToListAsync();

                var allGymIds = memberGymIds.Union(convGymIds).Distinct().ToList();

                foreach (var gymId in allGymIds)
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"gym-{gymId}");

                // Nếu là Owner: join group gym-{gymId} của gym mình
                var ownedGymIds = await _db.Gyms
                    .Where(g => g.OwnerId == userId && g.Status == "Approved")
                    .Select(g => g.Id)
                    .ToListAsync();

                foreach (var gymId in ownedGymIds)
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"gym-{gymId}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }

        // ────────────────────────────────────────────────────────────────
        // CLIENT → SERVER: Gửi tin nhắn 1-1
        // ────────────────────────────────────────────────────────────────
        public async Task SendMessage(int conversationId, string content, string? imageUrl = null)
        {
            var senderId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(senderId) || string.IsNullOrWhiteSpace(content)) return;
            if (content.Length > 2000) content = content[..2000];

            var conv = await _db.ChatConversations
                .Include(c => c.Gym)
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conv == null) return;

            // Xác định role của sender
            var isMember = conv.MemberId == senderId;
            var isOwner  = conv.Gym.OwnerId == senderId;
            if (!isMember && !isOwner) return; // bảo mật: chỉ 2 người trong conversation

            var sender = await _userManager.FindByIdAsync(senderId);
            var senderRole = isMember ? "Member" : "Owner";

            var msg = new ChatMessage
            {
                ConversationId = conversationId,
                GymId          = conv.GymId,
                SenderId       = senderId,
                Content        = content.Trim(),
                ImageUrl       = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim(),
                SenderRole     = senderRole,
                MessageType    = "Chat",
                IsRead         = false,
                CreatedAt      = VnTime.Now
            };

            _db.ChatMessages.Add(msg);

            // Cập nhật conversation
            conv.LastMessageAt = VnTime.Now;
            if (isMember) conv.OwnerUnread++;
            else          conv.MemberUnread++;

            await _db.SaveChangesAsync();

            // Payload gửi đến client
            var payload = new
            {
                id             = msg.Id,
                conversationId = msg.ConversationId,
                senderId       = msg.SenderId,
                senderName     = sender?.FullName ?? "Người dùng",
                senderRole     = msg.SenderRole,
                content        = msg.Content,
                imageUrl       = msg.ImageUrl,
                createdAt      = msg.CreatedAt.ToString("HH:mm dd/MM"),
                isOwn          = false // sẽ override ở client
            };

            // Gửi đến người nhận
            string recipientId = isMember ? conv.Gym.OwnerId : conv.MemberId;
            await Clients.Group($"user-{recipientId}").SendAsync("ReceiveMessage", payload);

            // Gửi lại cho chính người gửi (confirm + sync nhiều tab)
            await Clients.Caller.SendAsync("MessageSent", payload);
        }

        // ────────────────────────────────────────────────────────────────
        // CLIENT → SERVER: Owner broadcast tới toàn bộ member gym
        // ────────────────────────────────────────────────────────────────
        public async Task SendBroadcast(int gymId, string content, string? imageUrl = null)
        {
            var senderId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(senderId) || string.IsNullOrWhiteSpace(content)) return;
            if (content.Length > 2000) content = content[..2000];

            // Chỉ Owner của gym đó mới được broadcast
            var gym = await _db.Gyms.FirstOrDefaultAsync(g => g.Id == gymId && g.OwnerId == senderId);
            if (gym == null) return;

            var sender = await _userManager.FindByIdAsync(senderId);

            var msg = new ChatMessage
            {
                ConversationId = null,
                GymId          = gymId,
                SenderId       = senderId,
                Content        = content.Trim(),
                ImageUrl       = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim(),
                SenderRole     = "Owner",
                MessageType    = "Broadcast",
                IsRead         = false,
                CreatedAt      = VnTime.Now
            };

            _db.ChatMessages.Add(msg);
            await _db.SaveChangesAsync();

            var payload = new
            {
                id         = msg.Id,
                gymId,
                gymName    = gym.Name,
                senderId   = msg.SenderId,
                senderName = sender?.FullName ?? "Chủ phòng gym",
                content    = msg.Content,
                imageUrl   = msg.ImageUrl,
                createdAt  = msg.CreatedAt.ToString("HH:mm dd/MM")
            };

            // Broadcast đến tất cả member của gym (qua group "gym-{gymId}")
            // Caller (Owner) cũng trong group nên dùng SendAsync thay vì OthersInGroup
            await Clients.Group($"gym-{gymId}").SendAsync("ReceiveBroadcast", payload);
        }

        // ────────────────────────────────────────────────────────────────
        // CLIENT → SERVER: Đánh dấu đã đọc
        // ────────────────────────────────────────────────────────────────
        public async Task MarkRead(int conversationId)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId)) return;

            var conv = await _db.ChatConversations
                .Include(c => c.Gym)
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conv == null) return;

            if (conv.MemberId == userId)
                conv.MemberUnread = 0;
            else if (conv.Gym.OwnerId == userId)
                conv.OwnerUnread = 0;

            await _db.SaveChangesAsync();
        }
    }
}
