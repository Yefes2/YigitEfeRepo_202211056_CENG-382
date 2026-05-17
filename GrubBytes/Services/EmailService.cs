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

        public async Task SendOrderDeniedAsync(string toEmail, string toName,
        int orderId, decimal total, string reason)
        {
            var html = $@"
        <div style='font-family:sans-serif; max-width:500px; margin:auto;'>
            <h2 style='color:#e74c3c;'>GrubBytes — Order Denied</h2>
            <p>Hi {toName},</p>
            <p>Unfortunately your order <strong>#{orderId}</strong> has been denied by the caterer.</p>
            <div style='background:#e74c3c22; border:1px solid #e74c3c44; border-radius:6px;
                        padding:12px 16px; margin:1rem 0;'>
                <strong style='color:#e74c3c;'>Reason:</strong>
                <p style='margin:4px 0 0; color:#444;'>{reason}</p>
            </div>
            <p>Total of <strong>₺{total:0.00}</strong> will not be charged.</p>
            <p style='color:#888;'>We apologize for the inconvenience.</p>
        </div>";

            await SendAsync(toEmail, toName, $"GrubBytes — Order #{orderId} Denied", html);
        }

        public async Task SendOrderCompletedAsync(string toEmail, string toName,
            int orderId, decimal total)
        {
            var html = $@"
        <div style='font-family:sans-serif; max-width:500px; margin:auto;'>
            <h2 style='color:#2ECC71;'>GrubBytes — Order Completed!</h2>
            <p>Hi {toName},</p>
            <p>Your order <strong>#{orderId}</strong> has been marked as completed.</p>
            <p style='font-size:1.1rem;'>
                <strong>Total: ₺{total:0.00}</strong>
            </p>
            <p>You can now rate your experience from your Order History page.</p>
            <p style='color:#888;'>Thank you for ordering with GrubBytes!</p>
        </div>";

            await SendAsync(toEmail, toName, $"GrubBytes — Order #{orderId} Completed", html);
        }
    }
}