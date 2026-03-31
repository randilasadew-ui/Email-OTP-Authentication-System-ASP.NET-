using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace EmailOTP_Core.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendOTPAsync(string toEmail, string otp)
        {
            var host       = _config["Gmail:Host"] ?? "smtp.gmail.com";
            var port       = int.Parse(_config["Gmail:Port"] ?? "587");
            var fromEmail  = _config["Gmail:Email"]!;
            var password   = _config["Gmail:Password"]!;
            var senderName = _config["Gmail:SenderName"] ?? "OTP Service";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, fromEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = "Your OTP Code – Expires in 5 Minutes";

            message.Body = new TextPart("html")
            {
                Text = BuildEmailBody(otp, toEmail)
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(fromEmail, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        private static string BuildEmailBody(string otp, string toEmail) => $@"
<!DOCTYPE html>
<html>
<head><meta charset='UTF-8'/>
<style>
  body{{font-family:'Segoe UI',sans-serif;background:#f4f7fa;margin:0;padding:0}}
  .wrap{{max-width:480px;margin:40px auto;background:#fff;border-radius:12px;
         box-shadow:0 4px 24px rgba(0,0,0,.08);overflow:hidden}}
  .hdr{{background:linear-gradient(135deg,#1a1a2e,#16213e);padding:36px 32px;text-align:center}}
  .hdr h1{{color:#e94560;margin:0;font-size:28px;letter-spacing:2px}}
  .hdr p{{color:#aaa;margin:6px 0 0;font-size:13px}}
  .body{{padding:36px 32px}}
  .otp-box{{background:#f4f7fa;border:2px dashed #e94560;border-radius:10px;
            text-align:center;padding:24px;margin:24px 0}}
  .otp{{font-size:42px;font-weight:900;letter-spacing:12px;color:#1a1a2e;font-family:monospace}}
  .lbl{{font-size:12px;color:#888;margin-top:6px;text-transform:uppercase}}
  .info{{font-size:13px;color:#555;line-height:1.7}}
  .warn{{background:#fff8f0;border-left:4px solid #f0a500;padding:12px 16px;
         border-radius:0 8px 8px 0;font-size:13px;color:#7a5500;margin-top:20px}}
  .foot{{background:#f9f9f9;padding:18px 32px;text-align:center;
         font-size:11px;color:#aaa;border-top:1px solid #eee}}
</style></head>
<body>
<div class='wrap'>
  <div class='hdr'><h1>&#128274; OTP VERIFY</h1><p>Secure One-Time Password</p></div>
  <div class='body'>
    <p class='info'>Hello,<br/>Use the OTP below to verify <strong>{toEmail}</strong>:</p>
    <div class='otp-box'>
      <div class='otp'>{otp}</div>
      <div class='lbl'>One-Time Password &bull; Valid for 5 minutes</div>
    </div>
    <p class='info'>This code can only be used once.</p>
    <div class='warn'>&#9888; <strong>Security:</strong> Never share this OTP with anyone.</div>
  </div>
  <div class='foot'>Sent to {toEmail} &bull; {DateTime.Now.Year} OTP Demo</div>
</div>
</body></html>";
    }
}
