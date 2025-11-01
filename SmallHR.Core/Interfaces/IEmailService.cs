namespace SmallHR.Core.Interfaces;

public interface IEmailService
{
    /// <summary>
    /// Send email verification email to newly registered user
    /// </summary>
    Task SendVerificationEmailAsync(string email, string token, string userId);
    
    /// <summary>
    /// Send password reset email to user
    /// </summary>
    Task SendPasswordResetEmailAsync(string email, string token);
    
    /// <summary>
    /// Send welcome email to newly registered user
    /// </summary>
    Task SendWelcomeEmailAsync(string email, string firstName);
    
    /// <summary>
    /// Send generic email with custom subject and body
    /// </summary>
    Task SendEmailAsync(string email, string subject, string htmlBody);
}

