using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace GrubBytes.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            var senderEmail = _config["EmailSettings:SenderEmail"] ?? string.Empty;
            var senderName = _config["EmailSettings:SenderName"] ?? "GrubBytes";
            var appPassword = _config["EmailSettings:AppPassword"] ?? string.Empty;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(senderEmail, appPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        public async Task SendOrderConfirmationAsync(string toEmail, string toName,
            int orderId, decimal total, List<string> itemNames)
        {
            var itemList = string.Join("", itemNames.Select(i => $"<li>{i}</li>"));
            var html = $"""
                <div style="font-family:sans-serif; max-width:500px; margin:auto;">
                    <h2 style="color:#FF6B35;">GrubBytes — Order Confirmed!</h2>
                    <p>Hi {toName},</p>
                    <p>Your order <strong>#{orderId}</strong> has been placed successfully.</p>
                    <h3>Items:</h3>
                    <ul>{itemList}</ul>
                    <p style="font-size:1.2rem;">
                        <strong>Total: ₺{total:0.00}</strong>
                    </p>
                    <p style="color:#888;">Thank you for ordering with GrubBytes!</p>
                </div>
                """;

            await SendAsync(toEmail, toName, $"GrubBytes — Order #{orderId} Confirmed", html);
        }

        public async Task SendPasswordResetAsync(string toEmail, string name, string resetLink)
        {
            var html = $@"
        <div style='font-family:sans-serif; max-width:480px; margin:auto;'>
            <h2 style='color:#FF6B35;'>Password Reset</h2>
            <p>Hey {name},</p>
            <p>Click the button below to reset your GrubBytes password.
               This link expires in <strong>24 hours</strong>.</p>
            <a href='{resetLink}'
               style='display:inline-block; padding:12px 24px; background:#FF6B35;
                      color:#fff; border-radius:6px; text-decoration:none;
                      font-weight:bold; margin:1rem 0;'>
                Reset My Password
            </a>
            <p style='color:#888; font-size:0.85rem;'>
                If you didn't request this, ignore this email.
            </p>
        </div>";

            await SendAsync(toEmail, name, "GrubBytes — Password Reset", html);
        }
    }
}