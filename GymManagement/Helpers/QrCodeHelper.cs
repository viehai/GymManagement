using System;
using System.Security.Cryptography;
using System.Text;
using GymManagement.Models;

namespace GymManagement.Helpers
{
    public static class QrCodeHelper
    {
        private const string SecretSalt = "GymPro_Qr_Salt_2026_Secured";

        /// <summary>
        /// Tạo chuỗi Payload mã QR bảo mật cho Hội viên:
        /// - Mã QR Đa năng: GYMPRO:MEMBER:{MemberId}:{Token}
        /// - Mã QR Đích danh gói: GYMPRO:MEMBER:{MemberId}:{Token}:{MembershipId}
        /// </summary>
        public static string GenerateMemberQrPayload(ApplicationUser member, int? membershipId = null)
        {
            if (member == null) return string.Empty;

            // Token được tạo từ MemberId + SecurityStamp + Salt bí mật
            string raw = $"{member.Id}_{member.SecurityStamp}_{SecretSalt}";
            using var sha = SHA256.Create();
            byte[] hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            string token = Convert.ToHexString(hashBytes).Substring(0, 16); // Lấy 16 ký tự hexa

            if (membershipId.HasValue && membershipId.Value > 0)
            {
                return $"GYMPRO:MEMBER:{member.Id}:{token}:{membershipId.Value}";
            }

            return $"GYMPRO:MEMBER:{member.Id}:{token}";
        }

        /// <summary>
        /// Xác thực chuỗi Payload quét được từ QR có đúng định dạng và khớp với Member hay không
        /// Hỗ trợ cả định dạng 4 phần (QR đa năng) và 5 phần (QR đích danh gói).
        /// </summary>
        public static bool ValidateMemberQrPayload(string payload, ApplicationUser member, out string memberId)
        {
            memberId = string.Empty;
            if (string.IsNullOrWhiteSpace(payload) || member == null) return false;

            // Format: GYMPRO:MEMBER:{memberId}:{token} HOẶC GYMPRO:MEMBER:{memberId}:{token}:{membershipId}
            var parts = payload.Trim().Split(':');
            if ((parts.Length != 4 && parts.Length != 5) || parts[0] != "GYMPRO" || parts[1] != "MEMBER")
            {
                // Fallback: nếu chuỗi quét trực tiếp là MemberId (hoặc SĐT/Email)
                if (payload.Trim() == member.Id)
                {
                    memberId = member.Id;
                    return true;
                }
                return false;
            }

            string payloadMemberId = parts[2];
            string payloadToken = parts[3];

            if (payloadMemberId != member.Id) return false;

            string expectedRaw = $"{member.Id}_{member.SecurityStamp}_{SecretSalt}";
            using var sha = SHA256.Create();
            byte[] hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(expectedRaw));
            string expectedToken = Convert.ToHexString(hashBytes).Substring(0, 16);

            if (string.Equals(payloadToken, expectedToken, StringComparison.OrdinalIgnoreCase))
            {
                memberId = member.Id;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tách trích xuất MemberId từ chuỗi quét (nếu là QR GYMPRO)
        /// </summary>
        public static string? ExtractMemberIdFromPayload(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) return null;

            var parts = payload.Trim().Split(':');
            if ((parts.Length == 4 || parts.Length == 5) && parts[0] == "GYMPRO" && parts[1] == "MEMBER")
            {
                return parts[2];
            }

            return null;
        }

        /// <summary>
        /// Tách trích xuất MembershipId từ chuỗi quét (nếu là QR đích danh gói tập)
        /// </summary>
        public static int? ExtractMembershipIdFromPayload(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) return null;

            var parts = payload.Trim().Split(':');
            if (parts.Length == 5 && parts[0] == "GYMPRO" && parts[1] == "MEMBER")
            {
                if (int.TryParse(parts[4], out int membershipId) && membershipId > 0)
                {
                    return membershipId;
                }
            }

            return null;
        }
    }
}
