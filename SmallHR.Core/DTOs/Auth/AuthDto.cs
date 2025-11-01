using System.ComponentModel.DataAnnotations;

namespace SmallHR.Core.DTOs.Auth;

public class LoginDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Password { get; set; } = string.Empty;
}

public class RegisterDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MinLength(12, ErrorMessage = "Password must be at least 12 characters")]
    public string Password { get; set; } = string.Empty;
    
    [Required]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    public DateTime DateOfBirth { get; set; }
    
    public string Address { get; set; } = string.Empty;
    
    public string City { get; set; } = string.Empty;
    
    public string State { get; set; } = string.Empty;
    
    public string ZipCode { get; set; } = string.Empty;
    
    public string Country { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    
    public string RefreshToken { get; set; } = string.Empty;
    
    public DateTime Expiration { get; set; }
    
    public UserDto User { get; set; } = null!;
}

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public string FirstName { get; set; } = string.Empty;
    
    public string LastName { get; set; } = string.Empty;
    
    public string FullName => $"{FirstName} {LastName}";
    
    public DateTime DateOfBirth { get; set; }
    
    public string Address { get; set; } = string.Empty;
    
    public string City { get; set; } = string.Empty;
    
    public string State { get; set; } = string.Empty;
    
    public string ZipCode { get; set; } = string.Empty;
    
    public string Country { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    public bool IsActive { get; set; }
    
    public IList<string> Roles { get; set; } = new List<string>();
}

public class RefreshTokenDto
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class ResendVerificationDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ForgotPasswordDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Token { get; set; } = string.Empty;
    
    [Required]
    [MinLength(12, ErrorMessage = "Password must be at least 12 characters")]
    public string NewPassword { get; set; } = string.Empty;
}
