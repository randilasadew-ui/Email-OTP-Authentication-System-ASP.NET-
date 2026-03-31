namespace EmailOTP_Core.Models
{
    public static class OtpHelper
    {
        public static string GenerateOTP(int length = 6)
        {
            var rng = new Random();
            return string.Concat(Enumerable.Range(0, length).Select(_ => rng.Next(0, 10).ToString()));
        }
    }

    public class OtpViewModel
    {
        public string? Email { get; set; }
        public string? EnteredOtp { get; set; }
        public string? Message { get; set; }
        public bool IsSuccess { get; set; }
    }
}
