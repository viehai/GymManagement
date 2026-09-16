using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GymManagement.Helpers;

namespace GymManagement.Models
{
    /// <summary>
    /// Conversation 1-1 giữa một Member và một Gym (Owner).
    /// </summary>
    public class ChatConversation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string MemberId { get; set; } = string.Empty;

        [Required]
        public int GymId { get; set; }

        /// <summary>Thời điểm tin nhắn cuối cùng (để sort inbox).</summary>
        public DateTime LastMessageAt { get; set; } = VnTime.Now;

        /// <summary>Số tin chưa đọc phía Member.</summary>
        public int MemberUnread { get; set; } = 0;

        /// <summary>Số tin chưa đọc phía Owner.</summary>
        public int OwnerUnread { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = VnTime.Now;

        // ── Navigation ────────────────────────────────────────────────
        [ForeignKey(nameof(MemberId))]
        public ApplicationUser Member { get; set; } = null!;

        [ForeignKey(nameof(GymId))]
        public Gym Gym { get; set; } = null!;

        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
