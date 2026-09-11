using System.Text;
using System.Text.RegularExpressions;

namespace GymManagement.Helpers
{
    /// <summary>
    /// Helper xử lý chuẩn hóa và kiểm tra từ ngữ cấm (Profanity Filter) trong bình luận, đánh giá.
    /// </summary>
    public static class ContentFilterHelper
    {
        /// <summary>
        /// Danh sách từ cấm mặc định phổ biến (tiếng Việt & biến thể tục tĩu/lừa đảo).
        /// </summary>
        public static readonly List<(string Word, string Category)> DefaultBannedWords = new()
        {
            ("đm", "Thô tục"),
            ("dm", "Thô tục"),
            ("đcm", "Thô tục"),
            ("dcm", "Thô tục"),
            ("đmm", "Thô tục"),
            ("dmm", "Thô tục"),
            ("vcl", "Thô tục"),
            ("vcc", "Thô tục"),
            ("đĩ", "Thô tục"),
            ("đụ", "Thô tục"),
            ("chó chết", "Xúc phạm"),
            ("khốn nạn", "Xúc phạm"),
            ("thằng chó", "Xúc phạm"),
            ("đồ chó", "Xúc phạm"),
            ("lừa đảo", "Lừa đảo"),
            ("scam", "Lừa đảo"),
            ("đa cấp lừa", "Lừa đảo"),
            ("bố mày", "Xúc phạm"),
            ("mẹ mày", "Xúc phạm"),
            ("ngu như chó", "Xúc phạm"),
            ("cút", "Xúc phạm")
        };

        /// <summary>
        /// Kiểm tra xem chuỗi văn bản có chứa bất kỳ từ cấm nào trong danh sách không.
        /// Sử dụng so sánh không phân biệt hoa thường và kiểm tra ranh giới từ hoặc cụm từ.
        /// </summary>
        /// <param name="content">Nội dung cần kiểm tra</param>
        /// <param name="bannedWords">Danh sách từ cấm</param>
        /// <param name="matchedWords">Danh sách các từ cấm bị phát hiện trong nội dung</param>
        /// <returns>True nếu có từ cấm, False nếu sạch</returns>
        public static bool CheckForBannedWords(string? content, IEnumerable<string> bannedWords, out List<string> matchedWords)
        {
            matchedWords = new List<string>();
            if (string.IsNullOrWhiteSpace(content)) return false;

            var normalizedContent = NormalizeText(content);

            foreach (var rawWord in bannedWords)
            {
                if (string.IsNullOrWhiteSpace(rawWord)) continue;
                var normalizedWord = NormalizeText(rawWord.Trim());
                if (string.IsNullOrWhiteSpace(normalizedWord)) continue;

                // Tạo pattern tìm kiếm:
                // Nếu từ có khoảng trắng (cụm từ), tìm kiếm trực tiếp
                // Nếu từ đơn, tìm theo ranh giới ký tự hoặc từ
                string pattern;
                if (normalizedWord.Contains(' '))
                {
                    pattern = @"(?i)\b" + Regex.Escape(normalizedWord) + @"\b";
                }
                else
                {
                    pattern = @"(?i)(?<=^|[\s\p{P}])" + Regex.Escape(normalizedWord) + @"(?=$|[\s\p{P}])";
                }

                if (Regex.IsMatch(normalizedContent, pattern) || Regex.IsMatch(content, pattern))
                {
                    if (!matchedWords.Contains(rawWord))
                    {
                        matchedWords.Add(rawWord);
                    }
                }
            }

            return matchedWords.Count > 0;
        }

        /// <summary>
        /// Chuẩn hóa chuỗi văn bản: loại bỏ dấu câu thừa, chuyển chữ thường để dễ so sánh.
        /// </summary>
        private static string NormalizeText(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return input.Trim().ToLowerInvariant();
        }
    }
}
