using System.Net;
using System.Net.Mail;

namespace GymManagement.Helpers
{
    public class EmailHelper
    {
        private readonly IConfiguration _configuration;

        // Đăng ký DI trong Program.cs: builder.Services.AddScoped<EmailHelper>();
        public EmailHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("SmtpSettings");
                var senderEmail = smtpSettings["SenderEmail"];
                var password = smtpSettings["Password"];
                var host = smtpSettings["Host"];
                var portStr = smtpSettings["Port"];

                if (string.IsNullOrEmpty(senderEmail) || senderEmail.Contains("your-email") ||
                    string.IsNullOrEmpty(password) || password.Contains("your-app-password"))
                {
                    return false;
                }

                int port = int.TryParse(portStr, out int p) ? p : 587;
                bool enableSsl = bool.TryParse(smtpSettings["EnableSsl"], out bool ssl) ? ssl : true;

                using var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(senderEmail, password),
                    EnableSsl = enableSsl
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, smtpSettings["SenderName"] ?? "Gym Management System"),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Template riêng cho email OTP, tránh lặp code HTML ở Controller
        public async Task SendOtpEmailAsync(string toEmail, string otpCode)
        {
            string subject = "Mã xác nhận đặt lại mật khẩu - Gym Management";
            string body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 500px; margin: auto;'>
                    <h2>Đặt lại mật khẩu</h2>
                    <p>Mã OTP của bạn là:</p>
                    <h1 style='letter-spacing: 5px; color: #2563eb;'>{otpCode}</h1>
                    <p>Mã có hiệu lực trong <strong>5 phút</strong>. Vui lòng không chia sẻ mã này với bất kỳ ai.</p>
                    <p>Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này.</p>
                </div>";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendSuspensionEmailAsync(string toEmail, string memberName, string gymName, string suspensionType, DateTime? endDate, string reason)
        {
            string durText = suspensionType == "Permanent"
                ? "Vĩnh viễn (Không thời hạn)"
                : $"Từ {VnTime.Now:dd/MM/yyyy} đến hết {endDate:dd/MM/yyyy}";

            string subject = $"[GymPro] Thông báo đình chỉ quyền sử dụng dịch vụ tại {gymName}";
            string body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 24px; border: 1px solid #e5e7eb; border-radius: 8px;'>
                    <h2 style='color: #dc2626; margin-top: 0;'>Thông Báo Đình Chỉ Hội Viên</h2>
                    <p>Kính gửi quý hội viên <strong>{memberName}</strong>,</p>
                    <p>Chúng tôi rất tiếc phải thông báo rằng tài khoản hội viên của bạn tại cơ sở <strong>{gymName}</strong> đã bị đình chỉ sử dụng dịch vụ do vi phạm quy định cơ sở.</p>
                    <div style='background-color: #fef2f2; border-left: 4px solid #dc2626; padding: 14px; margin: 18px 0;'>
                        <p style='margin: 4px 0;'><strong>Hình thức:</strong> {(suspensionType == "Permanent" ? "Đình chỉ vĩnh viễn" : "Đình chỉ có thời hạn")}</p>
                        <p style='margin: 4px 0;'><strong>Thời hạn:</strong> {durText}</p>
                        <p style='margin: 4px 0;'><strong>Lý do:</strong> {reason}</p>
                    </div>
                    <p>Trong thời gian bị đình chỉ, bạn sẽ không thể mua vé mới hoặc gia hạn thẻ tập tại phòng Gym này.</p>
                    <p>Mọi thắc mắc hoặc yêu cầu giải quyết khiếu nại, vui lòng liên hệ trực tiếp với Ban quản lý cơ sở <strong>{gymName}</strong>.</p>
                    <hr style='border: none; border-top: 1px solid #e5e7eb; margin: 20px 0;' />
                    <p style='font-size: 12px; color: #6b7280;'>GymPro Management System - Thông báo tự động, vui lòng không phản hồi thư này.</p>
                </div>";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendSuspensionLiftedEmailAsync(string toEmail, string memberName, string gymName, string? liftedReason)
        {
            string subject = $"[GymPro] Thông báo gỡ bỏ đình chỉ dịch vụ tại {gymName}";
            string body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 24px; border: 1px solid #e5e7eb; border-radius: 8px;'>
                    <h2 style='color: #16a34a; margin-top: 0;'>Thông Báo Khôi Phục Quyền Lợi Hội Viên</h2>
                    <p>Kính gửi quý hội viên <strong>{memberName}</strong>,</p>
                    <p>Ban quản lý cơ sở <strong>{gymName}</strong> xin thông báo quyết định gỡ bỏ đình chỉ đối với tài khoản hội viên của bạn.</p>
                    <div style='background-color: #f0fdf4; border-left: 4px solid #16a34a; padding: 14px; margin: 18px 0;'>
                        <p style='margin: 4px 0;'><strong>Thời gian gỡ:</strong> {VnTime.Now:dd/MM/yyyy HH:mm}</p>
                        {(string.IsNullOrWhiteSpace(liftedReason) ? "" : $"<p style='margin: 4px 0;'><strong>Ghi chú:</strong> {liftedReason}</p>")}
                    </div>
                    <p>Quyền lợi hội viên của bạn đã được khôi phục. Bạn hiện có thể tiếp tục sử dụng dịch vụ và đăng ký/gia hạn gói tập bình thường tại cơ sở.</p>
                    <hr style='border: none; border-top: 1px solid #e5e7eb; margin: 20px 0;' />
                    <p style='font-size: 12px; color: #6b7280;'>GymPro Management System - Chúc bạn có những buổi tập hiệu quả!</p>
                </div>";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task SendVipPromotionEmailAsync(string toEmail, string memberName, string gymName, string tierName, decimal? discountPercent, string? benefitDescription)
        {
            string subject = $"👑 [GymPro] Chúc mừng bạn đã thăng hạng VIP {tierName} tại {gymName}!";
            string discountText = discountPercent.HasValue && discountPercent.Value > 0
                ? $"<p style='margin: 6px 0;'><strong>Ưu đãi giảm giá:</strong> Giảm ngay <span style='color: #dc2626; font-weight: 700;'>{discountPercent:0.#}%</span> cho mọi lượt mua vé & gia hạn tiếp theo!</p>"
                : "";

            string benefitText = !string.IsNullOrWhiteSpace(benefitDescription)
                ? $"<p style='margin: 6px 0;'><strong>Đặc quyền bổ sung:</strong> {benefitDescription}</p>"
                : "";

            string body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 28px; border: 1px solid #e5e7eb; border-radius: 12px; background: #ffffff;'>
                    <div style='text-align: center; margin-bottom: 20px;'>
                        <span style='font-size: 40px;'>👑</span>
                        <h2 style='color: #111827; margin: 8px 0 4px; font-family: Montserrat, Arial, sans-serif;'>CHÚC MỪNG THĂNG HẠNG VIP!</h2>
                        <p style='color: #6b7280; font-size: 14px; margin: 0;'>Chương trình Khách Hàng Thân Thiết GymPro</p>
                    </div>

                    <p>Xin chào <strong>{memberName}</strong>,</p>
                    <p>Ban quản lý cơ sở <strong>{gymName}</strong> xin trân trọng chúc mừng bạn đã chính thức đạt danh hiệu:</p>

                    <div style='background: linear-gradient(135deg, #111827, #374151); color: #ffffff; border-radius: 10px; padding: 20px; text-align: center; margin: 20px 0;'>
                        <div style='font-size: 12px; text-transform: uppercase; letter-spacing: 2px; color: #9ca3af;'>Hạng Thành Viên</div>
                        <div style='font-size: 24px; font-weight: 900; color: #fbbf24; margin: 6px 0; letter-spacing: 1px;'>{tierName.ToUpper()}</div>
                        <div style='font-size: 13px; color: #e5e7eb;'>Áp dụng tại: <strong>{gymName}</strong></div>
                    </div>

                    <div style='background: #f9fafb; border: 1px solid #e5e7eb; border-radius: 8px; padding: 16px; margin: 20px 0;'>
                        <h4 style='margin: 0 0 10px; color: #111827; font-size: 14px;'>ĐẶC QUYỀN VIP CỦA BẠN:</h4>
                        {discountText}
                        {benefitText}
                        <p style='margin: 6px 0; font-size: 13px; color: #4b5563;'>Hệ thống sẽ tự động áp dụng ưu đãi này mỗi khi bạn đăng ký hoặc gia hạn gói tập tại {gymName}.</p>
                    </div>

                    <p style='font-size: 14px; color: #374151;'>Cảm ơn bạn đã luôn đồng hành và tập luyện chăm chỉ cùng chúng tôi. Chúc bạn luôn tràn đầy năng lượng và đạt được những mục tiêu thể hình tuyệt vời!</p>

                    <hr style='border: none; border-top: 1px solid #e5e7eb; margin: 24px 0;' />
                    <p style='font-size: 12px; color: #9ca3af; text-align: center; margin: 0;'>GymPro Management System • Thông báo tự động từ cơ sở đối tác</p>
                </div>";

            await SendEmailAsync(toEmail, subject, body);
        }
    }
}