using SmallHR.Core.Interfaces;

namespace SmallHR.API.Services;

public class ConsoleEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConsoleEmailService> _logger;

    public ConsoleEmailService(IConfiguration configuration, ILogger<ConsoleEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendVerificationEmailAsync(string email, string token, string userId)
    {
        var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "http://localhost:5173";
        var verificationLink = $"{baseUrl}/verify-email?token={Uri.EscapeDataString(token)}&userId={userId}";
        
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #4f46e5; color: white; text-decoration: none; border-radius: 8px; margin: 20px 0; }}
        .button:hover {{ background-color: #4338ca; }}
        .footer {{ margin-top: 40px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class=""container"">
        <h2>Verify Your Email Address</h2>
        <p>Thank you for registering with SmallHR!</p>
        <p>Please verify your email address by clicking the button below:</p>
        <a href=""{verificationLink}"" class=""button"">Verify Email</a>
        <p>Or copy and paste this link into your browser:</p>
        <p style=""word-break: break-all;"">{verificationLink}</p>
        <p><strong>This link will expire in 24 hours.</strong></p>
        <div class=""footer"">
            <p>If you didn't create this account, you can safely ignore this email.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(email, "Verify Your Email - SmallHR", htmlBody);
        _logger.LogInformation("📧 Verification email sent to {Email}", email);
    }

    public async Task SendPasswordResetEmailAsync(string email, string token)
    {
        var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "http://localhost:5173";
        var resetLink = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(email)}";
        
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .button {{ display: inline-block; padding: 12px 24px; background-color: #ef4444; color: white; text-decoration: none; border-radius: 8px; margin: 20px 0; }}
        .button:hover {{ background-color: #dc2626; }}
        .footer {{ margin-top: 40px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class=""container"">
        <h2>Reset Your Password</h2>
        <p>You requested a password reset for your SmallHR account.</p>
        <p>Click the button below to set a new password:</p>
        <a href=""{resetLink}"" class=""button"">Reset Password</a>
        <p>Or copy and paste this link into your browser:</p>
        <p style=""word-break: break-all;"">{resetLink}</p>
        <p><strong>This link will expire in 1 hour.</strong></p>
        <p><strong>If you didn't request this, please ignore this email.</strong></p>
        <div class=""footer"">
            <p>Your password will remain unchanged if you don't click the link.</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(email, "Reset Your Password - SmallHR", htmlBody);
        _logger.LogInformation("📧 Password reset email sent to {Email}", email);
    }

    public async Task SendWelcomeEmailAsync(string email, string firstName)
    {
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .footer {{ margin-top: 40px; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class=""container"">
        <h2>Welcome to SmallHR, {firstName}!</h2>
        <p>Your account has been successfully created and verified.</p>
        <p>You can now log in and start using SmallHR to manage your HR operations.</p>
        <p>If you have any questions, please don't hesitate to contact our support team.</p>
        <div class=""footer"">
            <p>Thank you for choosing SmallHR!</p>
        </div>
    </div>
</body>
</html>";

        await SendEmailAsync(email, "Welcome to SmallHR!", htmlBody);
        _logger.LogInformation("📧 Welcome email sent to {Email}", email);
    }

    public async Task SendEmailAsync(string email, string subject, string htmlBody)
    {
        // For development: Log to console instead of sending actual emails
        // In production, replace this with SMTP/SendGrid/etc.
        await Task.CompletedTask;
        
        _logger.LogInformation(@"
╔═══════════════════════════════════════════════════════════════╗
║                     📧 EMAIL SENT (DEV MODE)                  ║
╠═══════════════════════════════════════════════════════════════╣
║ To:      {Email}                                              ║
║ Subject: {Subject}                                           ║
╠═══════════════════════════════════════════════════════════════╣
║                                                               ║
{Body}
║                                                               ║
╠═══════════════════════════════════════════════════════════════╣
║ Note: Replace ConsoleEmailService with real email service    ║
║       in production (SendGrid, SMTP, etc.)                    ║
╚═══════════════════════════════════════════════════════════════╝
", email, subject, StripHtml(htmlBody));
    }

    private static string StripHtml(string html)
    {
        // Simple HTML stripping for console display
        var plainText = System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", " ");
        return System.Text.RegularExpressions.Regex.Replace(plainText, @"\s+", " ").Trim();
    }
}

