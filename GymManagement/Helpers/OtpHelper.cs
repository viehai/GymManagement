namespace GymManagement.Helpers
{
    public static class OtpHelper
    {
        // Sinh mã OTP ngẫu nhiên 6 chữ số, đảm bảo luôn đủ 6 ký tự (VD: 007321, không bị mất số 0 đầu)
        public static string GenerateOtp()
        {
            var random = new Random();
            int number = random.Next(0, 1000000); // 0 -> 999999
            return number.ToString("D6");
        }

        // Thời hạn hiệu lực của OTP: 5 phút
        public static DateTime GetExpiryTime()
        {
            return DateTime.Now.AddMinutes(5);
        }
    }
}