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

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");

            using var client = new SmtpClient(smtpSettings["Host"], int.Parse(smtpSettings["Port"]))
            {
                Credentials = new NetworkCredential(smtpSettings["SenderEmail"], smtpSettings["Password"]),
                EnableSsl = bool.Parse(smtpSettings["EnableSsl"])
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpSettings["SenderEmail"], smtpSettings["SenderName"]),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
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
    }
}