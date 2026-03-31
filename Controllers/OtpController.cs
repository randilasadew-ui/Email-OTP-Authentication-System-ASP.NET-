using EmailOTP_Core.Models;
using EmailOTP_Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace EmailOTP_Core.Controllers
{
    public class OtpController : Controller
    {
        private readonly EmailService _emailService;

        public OtpController(EmailService emailService)
        {
            _emailService = emailService;
        }

        // GET /Otp/Send
        [HttpGet]
        public IActionResult Send() => View(new OtpViewModel());

        // POST /Otp/Send
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(OtpViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                model.Message = "Please enter a valid email address.";
                return View(model);
            }

            try
            {
                var otp = OtpHelper.GenerateOTP();

                HttpContext.Session.SetString("OTP", otp);
                HttpContext.Session.SetString("OTPExpiry", DateTime.Now.AddMinutes(5).ToString("o"));
                HttpContext.Session.SetString("OTPEmail", model.Email);
                HttpContext.Session.SetInt32("OTPAttempts", 0);

                await _emailService.SendOTPAsync(model.Email, otp);

                TempData["Email"]   = model.Email;
                TempData["Success"] = $"OTP sent to {model.Email}";
                return RedirectToAction("Verify");
            }
            catch (Exception ex)
            {
                model.Message   = $"Failed to send OTP: {ex.Message}";
                model.IsSuccess = false;
                return View(model);
            }
        }

        // GET /Otp/Verify
        [HttpGet]
        public IActionResult Verify()
        {
            return View(new OtpViewModel
            {
                Email     = TempData["Email"]?.ToString(),
                Message   = TempData["Success"]?.ToString(),
                IsSuccess = TempData["Success"] != null
            });
        }

        // POST /Otp/Verify
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Verify(OtpViewModel model)
        {
            var storedOtp   = HttpContext.Session.GetString("OTP");
            var storedEmail = HttpContext.Session.GetString("OTPEmail");
            var expiryStr   = HttpContext.Session.GetString("OTPExpiry");
            var attempts    = HttpContext.Session.GetInt32("OTPAttempts") ?? 0;

            if (attempts >= 3)
            {
                HttpContext.Session.Remove("OTP");
                model.Message = "Too many failed attempts. Please request a new OTP.";
                return View(model);
            }

            if (expiryStr == null || DateTime.Parse(expiryStr) < DateTime.Now)
            {
                model.Message = "OTP has expired. Please request a new one.";
                return View(model);
            }

            if (model.EnteredOtp?.Trim() == storedOtp)
            {
                HttpContext.Session.Clear();
                model.IsSuccess = true;
                model.Email     = storedEmail;
                model.Message   = "OTP verified successfully!";
                return View("Success", model);
            }

            HttpContext.Session.SetInt32("OTPAttempts", attempts + 1);
            model.Message   = $"Invalid OTP. {2 - attempts} attempt(s) remaining.";
            model.Email     = storedEmail;
            return View(model);
        }

        // POST /Otp/Resend
        [HttpPost]
        public async Task<IActionResult> Resend()
        {
            var email = HttpContext.Session.GetString("OTPEmail");
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Send");

            try
            {
                var otp = OtpHelper.GenerateOTP();
                HttpContext.Session.SetString("OTP", otp);
                HttpContext.Session.SetString("OTPExpiry", DateTime.Now.AddMinutes(5).ToString("o"));
                HttpContext.Session.SetInt32("OTPAttempts", 0);
                await _emailService.SendOTPAsync(email, otp);
                TempData["Success"] = $"New OTP sent to {email}";
                TempData["Email"]   = email;
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Resend failed: {ex.Message}";
            }

            return RedirectToAction("Verify");
        }
    }
}
