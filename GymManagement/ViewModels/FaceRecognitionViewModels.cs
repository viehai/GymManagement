using System;
using System.Collections.Generic;

namespace GymManagement.ViewModels
{
    /// <summary>
    /// Payload nhận từ client khi Hội viên đăng ký hoặc cập nhật Face ID.
    /// </summary>
    public class RegisterFaceRequestDto
    {
        /// <summary>
        /// Mảng 128 số float trích xuất từ FaceNet trên trình duyệt.
        /// </summary>
        public float[] Descriptor { get; set; } = Array.Empty<float>();

        /// <summary>
        /// Ảnh thumbnail crop khuôn mặt (Base64 JPEG/PNG).
        /// </summary>
        public string? ImageBase64 { get; set; }

        /// <summary>
        /// Điểm chất lượng ảnh khi chụp (0.0 - 1.0).
        /// </summary>
        public double QualityScore { get; set; } = 1.0;
    }

    /// <summary>
    /// Payload gửi lên từ Camera lễ tân / Kiosk để tra cứu danh tính hội viên.
    /// </summary>
    public class VerifyFaceRequestDto
    {
        public int GymId { get; set; }

        /// <summary>
        /// Vector 128D của khuôn mặt đang đứng trước webcam.
        /// </summary>
        public float[] Descriptor { get; set; } = Array.Empty<float>();
    }

    /// <summary>
    /// Kết quả trả về sau khi server so khớp vector khuôn mặt.
    /// </summary>
    public class VerifyFaceResultDto
    {
        public bool Success { get; set; }
        public bool FoundMatch { get; set; }
        public string Message { get; set; } = string.Empty;

        // Thông tin hội viên
        public string? MemberId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }

        // Loyalty & Gói tập
        public string? VipTierName { get; set; }
        public string? VipBadgeColor { get; set; }
        public int? MembershipId { get; set; }
        public string? PackageName { get; set; }
        public int DaysRemaining { get; set; }

        // Trạng thái tập luyện
        public bool IsSuspended { get; set; }
        public string? SuspensionReason { get; set; }
        public bool HasActiveMembership { get; set; }
        public bool CanCheckin { get; set; }

        /// <summary>
        /// True nếu hội viên hiện đang trong phòng tập (chưa checkout và check-in trong vòng 2h).
        /// Cho phép tự động chuyển sang luồng CHECK-OUT khi hội viên bước ra cửa.
        /// </summary>
        public bool IsAlreadyInside { get; set; }
        public int? ActiveCheckinId { get; set; }
        public int MinutesInside { get; set; }
        public DateTime? CheckinTime { get; set; }

        // Điểm số FaceNet
        public double Distance { get; set; }
        public double ConfidenceScore { get; set; }
    }

    /// <summary>
    /// ViewModel cho trang quản lý Face ID cá nhân của Member.
    /// </summary>
    public class MemberFaceIdViewModel
    {
        public bool HasFaceRegistered { get; set; }
        public string? SampleImageUrl { get; set; }
        public DateTime? RegisteredAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public double QualityScore { get; set; }
        public bool IsActive { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
