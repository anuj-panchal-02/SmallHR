using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using SmallHR.Core.DTOs.Auth;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;

namespace SmallHR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly UserManager<User> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthController> _logger;
    private readonly IHostEnvironment _environment;

    public AuthController(
        IAuthService authService, 
        UserManager<User> userManager,
        IEmailService emailService,
        ILogger<AuthController> logger, 
        IHostEnvironment environment)
    {
        _authService = authService;
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Login user
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto loginDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

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
            _logger.LogWarning("Login failed for email: {Email}, Error: {Error}", sanitizedEmail, ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            var sanitizedEmail = SanitizeEmail(loginDto.Email);
            _logger.LogError(ex, "An error occurred during login for email: {Email}", sanitizedEmail);
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    /// <summary>
    /// Register new user
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto registerDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

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
            _logger.LogWarning("Registration failed for email: {Email}, Error: {Error}", sanitizedEmail, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            var sanitizedEmail = SanitizeEmail(registerDto.Email);
            _logger.LogError(ex, "An error occurred during registration for email: {Email}", sanitizedEmail);
            return StatusCode(500, new { message = "An error occurred during registration" });
        }
    }

    /// <summary>
    /// Refresh JWT token
    /// </summary>
    [HttpPost("refresh-token")]
    public async Task<ActionResult<AuthResponseDto>> RefreshToken(RefreshTokenDto refreshTokenDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

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
            _logger.LogWarning("Token refresh failed: {Error}", ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during token refresh");
            return StatusCode(500, new { message = "An error occurred during token refresh" });
        }
    }

    /// <summary>
    /// Revoke refresh token
    /// </summary>
    [HttpPost("revoke-token")]
    [Authorize]
    public async Task<ActionResult> RevokeToken(RefreshTokenDto refreshTokenDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.RevokeTokenAsync(refreshTokenDto.RefreshToken);
            if (!result)
            {
                return BadRequest(new { message = "Invalid refresh token" });
            }

            return Ok(new { message = "Token revoked successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during token revocation");
            return StatusCode(500, new { message = "An error occurred during token revocation" });
        }
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting current user");
            return StatusCode(500, new { message = "An error occurred while getting current user" });
        }
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
    public async Task<ActionResult> VerifyEmail([FromQuery] string token, [FromQuery] string userId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(userId))
            {
                return BadRequest(new { message = "Invalid verification link" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return BadRequest(new { message = "Invalid verification link" });
            }

            if (user.EmailConfirmed)
            {
                return Ok(new { message = "Email already verified" });
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                _logger.LogInformation("Email verified successfully for user: {Email}", user.Email);
                return Ok(new { message = "Email verified successfully" });
            }

            _logger.LogWarning("Email verification failed for user: {Email}", user.Email);
            return BadRequest(new { message = "Invalid or expired verification token" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during email verification");
            return StatusCode(500, new { message = "An error occurred during email verification" });
        }
    }
    
    /// <summary>
    /// Resend verification email
    /// </summary>
    [HttpPost("resend-verification")]
    [AllowAnonymous]
    public async Task<ActionResult> ResendVerificationEmail([FromBody] ResendVerificationDto dto)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                // Don't reveal if email exists (security best practice)
                return Ok(new { message = "If email exists, verification link sent" });
            }

            if (user.EmailConfirmed)
            {
                return Ok(new { message = "Email already verified" });
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            await _emailService.SendVerificationEmailAsync(user.Email!, token, user.Id);

            _logger.LogInformation("Verification email resent to: {Email}", dto.Email);
            return Ok(new { message = "If email exists, verification link sent" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while resending verification email");
            return StatusCode(500, new { message = "An error occurred while sending verification email" });
        }
    }
    
    /// <summary>
    /// Request password reset
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                // Don't reveal if email exists (security best practice)
                return Ok(new { message = "If email exists, reset instructions sent" });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            await _emailService.SendPasswordResetEmailAsync(user.Email!, token);

            _logger.LogInformation("Password reset email sent to: {Email}", dto.Email);
            return Ok(new { message = "If email exists, reset instructions sent" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during password reset request");
            return StatusCode(500, new { message = "An error occurred during password reset request" });
        }
    }
    
    /// <summary>
    /// Reset password with token
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                return BadRequest(new { message = "Invalid request" });
            }

            var result = await _userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                _logger.LogWarning("Password reset failed for {Email}: {Errors}", dto.Email, string.Join(", ", errors));
                return BadRequest(new { 
                    message = "Password reset failed", 
                    errors = errors 
                });
            }

            _logger.LogInformation("Password reset successful for: {Email}", dto.Email);
            return Ok(new { message = "Password reset successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during password reset");
            return StatusCode(500, new { message = "An error occurred during password reset" });
        }
    }
    
    /// <summary>
    /// Setup password for admin user (using userId and token)
    /// Used for initial password setup when tenant admin is created
    /// </summary>
    [HttpPost("setup-password")]
    [AllowAnonymous]
    public async Task<ActionResult> SetupPassword([FromBody] SetupPasswordDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(dto.UserId) || string.IsNullOrWhiteSpace(dto.Token) || string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                return BadRequest(new { message = "UserId, token, and new password are required" });
            }

            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null)
            {
                return BadRequest(new { message = "Invalid setup link" });
            }

            var result = await _userManager.ResetPasswordAsync(user, Uri.UnescapeDataString(dto.Token), dto.NewPassword);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                _logger.LogWarning("Password setup failed for user {UserId}: {Errors}", dto.UserId, string.Join(", ", errors));
                return BadRequest(new { 
                    message = "Password setup failed", 
                    errors = errors 
                });
            }

            // Optionally confirm email if not already confirmed
            if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                await _userManager.UpdateAsync(user);
            }

            _logger.LogInformation("Password setup successful for user: {UserId} ({Email})", dto.UserId, user.Email);
            return Ok(new { message = "Password set successfully. You can now login." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during password setup");
            return StatusCode(500, new { message = "An error occurred during password setup" });
        }
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
