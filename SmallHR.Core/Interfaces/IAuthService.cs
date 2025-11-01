using SmallHR.Core.DTOs.Auth;

namespace SmallHR.Core.Interfaces;

public interface IAuthService : IService
{
    Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
    Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
    Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);
    Task<bool> RevokeTokenAsync(string refreshToken);
    Task<bool> UserExistsAsync(string email);
    Task<UserDto?> GetUserByIdAsync(string userId);
    Task<UserDto?> GetUserByEmailAsync(string email);
}
