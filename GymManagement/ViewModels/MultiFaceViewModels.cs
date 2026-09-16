using System;
using System.Collections.Generic;

namespace GymManagement.ViewModels
{
    /// <summary>
    /// Payload gửi lên từ Camera chứa danh sách nhiều khuôn mặt xuất hiện đồng thời trong 1 khung hình.
    /// </summary>
    public class VerifyMultiFaceRequestDto
    {
        public int GymId { get; set; }

        /// <summary>
        /// Chế độ cổng: "Auto" (Tự động thông minh), "InOnly" (Chỉ cổng vào), "OutOnly" (Chỉ cổng ra)
        /// </summary>
        public string GateMode { get; set; } = "Auto";

        /// <summary>
        /// Danh sách vector 128D của các khuôn mặt phát hiện trong khung hình.
        /// </summary>
        public List<MultiFaceItemDto> Faces { get; set; } = new();
    }

    public class MultiFaceItemDto
    {
        public int FaceIndex { get; set; }
        public float[] Descriptor { get; set; } = Array.Empty<float>();
        public double BoxX { get; set; }
        public double BoxY { get; set; }
        public double BoxWidth { get; set; }
        public double BoxHeight { get; set; }
    }

    /// <summary>
    /// Kết quả trả về cho từng khuôn mặt được xử lý.
    /// </summary>
    public class MultiFaceProcessResultDto
    {
        public int FaceIndex { get; set; }
        public bool FoundMatch { get; set; }
        public string? MemberId { get; set; }
        public string? FullName { get; set; }
        public string? VipTierName { get; set; }
        public string? VipBadgeColor { get; set; }
        public string? PackageName { get; set; }
        public string ActionType { get; set; } = "None"; // "Checkin", "Checkout", "Denied", "AlreadyProcessed"
        public string Message { get; set; } = string.Empty;
        public double ConfidenceScore { get; set; }
        public int MinutesInside { get; set; }
        public double BoxX { get; set; }
        public double BoxY { get; set; }
        public double BoxWidth { get; set; }
        public double BoxHeight { get; set; }
    }

    public class VerifyMultiFaceResponseDto
    {
        public bool Success { get; set; }
        public List<MultiFaceProcessResultDto> Results { get; set; } = new();
        public int ActiveCount { get; set; }
        public int MaxCapacity { get; set; }
        public int CrowdPercentage { get; set; }
        public string CrowdStatusText { get; set; } = string.Empty;
        public string CrowdStatusColor { get; set; } = string.Empty;
        public string CrowdStatusIcon { get; set; } = string.Empty;
        public string CrowdRecommendation { get; set; } = string.Empty;
    }
}
