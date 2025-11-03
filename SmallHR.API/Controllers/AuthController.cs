using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using SmallHR.API.Base;
using SmallHR.Core.DTOs.Auth;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;

namespace SmallHR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;
    private readonly UserManager<User> _userManager;
    private readonly IEmailService _emailService;
    private readonly IHostEnvironment _environment;

    public AuthController(
        IAuthService authService, 
        UserManager<User> userManager,
        IEmailService emailService,
        ILogger<AuthController> logger, 
        IHostEnvironment environment) : base(logger)
    {
        _authService = authService;
        _userManager = userManager;
        _emailService = emailService;
        _environment = environment;
    }

    /// <summary>
    /// Login user
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<object>> Login(LoginDto loginDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _authService.LoginAsync(loginDto);
            
            // Set httpOnly cookies for security (prevent XSS attacks on tokens)
            SetAuthCookies(result.Token, result.RefreshToken);
            
            // Return response without tokens (they're in cookies)
            var safeResponse = new
            {
                expiration = result.Expiration,
                user = result.User
            };
            
            return Ok(safeResponse);
        }
        catch (UnauthorizedAccessException ex)
        {
            // Sanitize email: only log first 3 characters and domain
            var sanitizedEmail = SanitizeEmail(loginDto.Email);
            Logger.LogWarning("Login failed for email: {Email}, Error: {Error}", sanitizedEmail, ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            var sanitizedEmail = SanitizeEmail(loginDto.Email);
            Logger.LogError(ex, "An error occurred during login for email: {Email}", sanitizedEmail);
            return CreateErrorResponse("An error occurred during login", ex);
        }
    }

    /// <summary>
    /// Register new user
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<object>> Register(RegisterDto registerDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _authService.RegisterAsync(registerDto);
            
            // Set httpOnly cookies for security
            SetAuthCookies(result.Token, result.RefreshToken);
            
            // Return response without tokens (they're in cookies)
            var safeResponse = new
            {
                expiration = result.Expiration,
                user = result.User
            };
            
            return CreatedAtAction(nameof(GetCurrentUser), new { }, safeResponse);
        }
        catch (InvalidOperationException ex)
        {
            var sanitizedEmail = SanitizeEmail(registerDto.Email);
            Logger.LogWarning("Registration failed for email: {Email}, Error: {Error}", sanitizedEmail, ex.Message);
            return CreateBadRequestResponse(ex.Message);
        }
        catch (Exception ex)
        {
            var sanitizedEmail = SanitizeEmail(registerDto.Email);
            Logger.LogError(ex, "An error occurred during registration for email: {Email}", sanitizedEmail);
            return CreateErrorResponse("An error occurred during registration", ex);
        }
    }

    /// <summary>
    /// Refresh JWT token
    /// </summary>
    [HttpPost("refresh-token")]
    public async Task<ActionResult<object>> RefreshToken(RefreshTokenDto refreshTokenDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _authService.RefreshTokenAsync(refreshTokenDto);
            
            // Set httpOnly cookies with new tokens
            SetAuthCookies(result.Token, result.RefreshToken);
            
            // Return response without tokens (they're in cookies)
            var safeResponse = new
            {
                expiration = result.Expiration,
                user = result.User
            };
            
            return Ok(safeResponse);
        }
        catch (UnauthorizedAccessException ex)
        {
            Logger.LogWarning("Token refresh failed: {Error}", ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred during token refresh");
            return CreateErrorResponse("An error occurred during token refresh", ex);
        }
    }

    /// <summary>
    /// Revoke refresh token
    /// </summary>
    [HttpPost("revoke-token")]
    [Authorize]
    public async Task<ActionResult<object>> RevokeToken(RefreshTokenDto refreshTokenDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return await HandleServiceResultAsync(
            async () =>
            {
                var result = await _authService.RevokeTokenAsync(refreshTokenDto.RefreshToken);
                if (!result)
                {
                    throw new InvalidOperationException("Invalid refresh token");
                }

                return new { message = "Token revoked successfully" };
            },
            "revoking token"
        );
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        return await HandleServiceResultOrNotFoundAsync(
            () => _authService.GetUserByIdAsync(userId),
            "getting current user",
            "User"
        );
    }
    
    /// <summary>
    /// Logout user (clear authentication cookies)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public ActionResult Logout()
    {
        ClearAuthCookies();
        return Ok(new { message = "Logged out successfully" });
    }
    
    /// <summary>
    /// Verify email address with token
    /// </summary>
    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> VerifyEmail([FromQuery] string token, [FromQuery] string userId)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(userId))
        {
            return CreateBadRequestResponse("Invalid verification link");
        }

        return await HandleServiceResultAsync(
            async () =>
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    throw new ArgumentException("Invalid verification link");
                }

                if (user.EmailConfirmed)
                {
                    return new { message = "Email already verified" };
                }

                var result = await _userManager.ConfirmEmailAsync(user, token);
                if (!result.Succeeded)
                {
                    Logger.LogWarning("Email verification failed for user: {Email}", user.Email);
                    throw new InvalidOperationException("Invalid or expired verification token");
                }

                Logger.LogInformation("Email verified successfully for user: {Email}", user.Email);
                return new { message = "Email verified successfully" };
            },
            "verifying email"
        );
    }
    
    /// <summary>
    /// Resend verification email
    /// </summary>
    [HttpPost("resend-verification")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> ResendVerificationEmail([FromBody] ResendVerificationDto dto)
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                var user = await _userManager.FindByEmailAsync(dto.Email);
                if (user == null)
                {
                    // Don't reveal if email exists (security best practice)
                    return new { message = "If email exists, verification link sent" };
                }

                if (user.EmailConfirmed)
                {
                    return new { message = "Email already verified" };
                }

                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                await _emailService.SendVerificationEmailAsync(user.Email!, token, user.Id);

                Logger.LogInformation("Verification email resent to: {Email}", dto.Email);
                return new { message = "If email exists, verification link sent" };
            },
            "resending verification email"
        );
    }
    
    /// <summary>
    /// Request password reset
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                var user = await _userManager.FindByEmailAsync(dto.Email);
                if (user == null)
                {
                    // Don't reveal if email exists (security best practice)
                    return new { message = "If email exists, reset instructions sent" };
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                await _emailService.SendPasswordResetEmailAsync(user.Email!, token);

                Logger.LogInformation("Password reset email sent to: {Email}", dto.Email);
                return new { message = "If email exists, reset instructions sent" };
            },
            "requesting password reset"
        );
    }
    
    /// <summary>
    /// Reset password with token
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return await HandleServiceResultAsync(
            async () =>
            {
                var user = await _userManager.FindByEmailAsync(dto.Email);
                if (user == null)
                {
                    throw new ArgumentException("Invalid request");
                }

                var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    Logger.LogWarning("Password reset failed for {Email}: {Errors}", dto.Email, string.Join(", ", errors));
                    throw new InvalidOperationException($"Password reset failed: {string.Join(", ", errors)}");
                }

                Logger.LogInformation("Password reset successful for: {Email}", dto.Email);
                return new { message = "Password reset successfully" };
            },
            "resetting password"
        );
    }
    
    /// <summary>
    /// Setup password for admin user (using userId and token)
    /// Used for initial password setup when tenant admin is created
    /// </summary>
    [HttpPost("setup-password")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> SetupPassword([FromBody] SetupPasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (string.IsNullOrWhiteSpace(dto.UserId) || string.IsNullOrWhiteSpace(dto.Token) || string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            return CreateBadRequestResponse("UserId, token, and new password are required");
        }

        return await HandleServiceResultAsync(
            async () =>
            {
                var user = await _userManager.FindByIdAsync(dto.UserId);
                if (user == null)
                {
                    throw new ArgumentException("Invalid setup link");
                }

                var result = await _userManager.ResetPasswordAsync(user, Uri.UnescapeDataString(dto.Token), dto.NewPassword);
                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    Logger.LogWarning("Password setup failed for user {UserId}: {Errors}", dto.UserId, string.Join(", ", errors));
                    throw new InvalidOperationException($"Password setup failed: {string.Join(", ", errors)}");
                }

                // Optionally confirm email if not already confirmed
                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(user);
                }

                Logger.LogInformation("Password setup successful for user: {UserId} ({Email})", dto.UserId, user.Email);
                return new { message = "Password set successfully. You can now login." };
            },
            "setting up password"
        );
    }
    
    /// <summary>
    /// Set httpOnly cookies for access and refresh tokens
    /// </summary>
    private void SetAuthCookies(string accessToken, string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true, // Prevent JavaScript access (XSS protection)
            Secure = !_environment.IsDevelopment(), // HTTPS only in production
            SameSite = SameSiteMode.Strict, // CSRF protection
            Expires = DateTimeOffset.UtcNow.AddMinutes(60), // Access token lifetime
            Path = "/"
        };
        
        Response.Cookies.Append("accessToken", accessToken, cookieOptions);
        
        var refreshCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = !_environment.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7), // Refresh token lifetime
            Path = "/"
        };
        
        Response.Cookies.Append("refreshToken", refreshToken, refreshCookieOptions);
    }
    
    /// <summary>
    /// Clear authentication cookies
    /// </summary>
    private void ClearAuthCookies()
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = !_environment.IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(-1), // Expire immediately
            Path = "/"
        };
        
        Response.Cookies.Append("accessToken", "", cookieOptions);
        Response.Cookies.Append("refreshToken", "", cookieOptions);
    }
    
    private static string SanitizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "***";
        var parts = email.Split('@');
        if (parts.Length != 2) return "***";
        var local = parts[0];
        var domain = parts[1];
        return local.Length > 3 
            ? $"{local.Substring(0, 3)}***@{domain}" 
            : $"***@{domain}";
    }
}
