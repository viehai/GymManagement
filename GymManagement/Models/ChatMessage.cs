using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GymManagement.Helpers;

namespace GymManagement.Models
{
    /// <summary>
    /// Một tin nhắn đơn — có thể là chat 1-1 hoặc broadcast từ Owner tới toàn bộ gym.
    /// </summary>
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Null nếu là Broadcast (không gắn với conversation cụ thể).</summary>
        public int? ConversationId { get; set; }

        /// <summary>Gym liên quan (dùng cho Broadcast và để trace ngữ cảnh).</summary>
        public int? GymId { get; set; }

        [Required]
        public string SenderId { get; set; } = string.Empty;

        /// <summary>Nội dung văn bản của tin nhắn.</summary>
        [Required]
        [StringLength(2000)]
        public string Content { get; set; } = string.Empty;

        /// <summary>URL ảnh đính kèm (nullable).</summary>
        [StringLength(500)]
        public string? ImageUrl { get; set; }

        /// <summary>"Member" hoặc "Owner".</summary>
        [Required]
        [StringLength(10)]
        public string SenderRole { get; set; } = "Member";

        /// <summary>"Chat" (1-1) hoặc "Broadcast" (hàng loạt).</summary>
        [Required]
        [StringLength(15)]
        public string MessageType { get; set; } = "Chat";

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = VnTime.Now;

        // ── Navigation ────────────────────────────────────────────────
        [ForeignKey(nameof(ConversationId))]
        public ChatConversation? Conversation { get; set; }

        [ForeignKey(nameof(GymId))]
        public Gym? Gym { get; set; }

        [ForeignKey(nameof(SenderId))]
        public ApplicationUser Sender { get; set; } = null!;
    }
}
