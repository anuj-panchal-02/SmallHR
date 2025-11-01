using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SmallHR.Core.DTOs.Auth;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SmallHR.API.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly IMapper _mapper;
    private readonly ITenantProvider _tenantProvider;

    public AuthService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IConfiguration configuration,
        IMapper mapper,
        ITenantProvider tenantProvider)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _mapper = mapper;
        _tenantProvider = tenantProvider;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
    {
        var user = await _userManager.FindByEmailAsync(loginDto.Email);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid credentials - User not found");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Account is inactive. Please contact administrator.");
        }

        // Check if user is locked out
        if (await _userManager.IsLockedOutAsync(user))
        {
            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
            var message = lockoutEnd.HasValue && lockoutEnd.Value > DateTimeOffset.UtcNow
                ? $"Account is locked out until {lockoutEnd.Value:yyyy-MM-dd HH:mm:ss} UTC. Please try again later."
                : "Account is locked out. Please contact administrator.";
            throw new UnauthorizedAccessException(message);
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
        if (!result.Succeeded)
        {
            // Check why login failed
            if (result.IsLockedOut)
            {
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                var message = lockoutEnd.HasValue && lockoutEnd.Value > DateTimeOffset.UtcNow
                    ? $"Account is locked out until {lockoutEnd.Value:yyyy-MM-dd HH:mm:ss} UTC due to too many failed login attempts. Please try again later."
                    : "Account is locked out due to too many failed login attempts. Please contact administrator.";
                throw new UnauthorizedAccessException(message);
            }
            else if (result.IsNotAllowed)
            {
                throw new UnauthorizedAccessException("Login not allowed. Please contact administrator.");
            }
            else
            {
                throw new UnauthorizedAccessException("Invalid credentials - Incorrect email or password");
            }
        }

        var token = await GenerateJwtTokenAsync(user);
        var refreshToken = GenerateRefreshToken();

        // Store refresh token
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _userManager.UpdateAsync(user);

        // Get user roles
        var roles = await _userManager.GetRolesAsync(user);
        var userDto = _mapper.Map<UserDto>(user);
        userDto.Roles = roles;

        return new AuthResponseDto
        {
            Token = token,
            RefreshToken = refreshToken,
            Expiration = DateTime.UtcNow.AddMinutes(60),
            User = userDto
        };
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        var user = new User
        {
            UserName = registerDto.Email,
            Email = registerDto.Email,
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
            DateOfBirth = registerDto.DateOfBirth,
            Address = registerDto.Address,
            City = registerDto.City,
            State = registerDto.State,
            ZipCode = registerDto.ZipCode,
            Country = registerDto.Country,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, registerDto.Password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"User creation failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        // Add default role
        await _userManager.AddToRoleAsync(user, "Employee");

        var token = await GenerateJwtTokenAsync(user);
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _userManager.UpdateAsync(user);

        // Get user roles
        var roles = await _userManager.GetRolesAsync(user);
        var userDto = _mapper.Map<UserDto>(user);
        userDto.Roles = roles;

        return new AuthResponseDto
        {
            Token = token,
            RefreshToken = refreshToken,
            Expiration = DateTime.UtcNow.AddMinutes(60),
            User = userDto
        };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshTokenDto.RefreshToken);
        if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        var token = await GenerateJwtTokenAsync(user);
        var newRefreshToken = GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _userManager.UpdateAsync(user);

        // Get user roles
        var roles = await _userManager.GetRolesAsync(user);
        var userDto = _mapper.Map<UserDto>(user);
        userDto.Roles = roles;

        return new AuthResponseDto
        {
            Token = token,
            RefreshToken = newRefreshToken,
            Expiration = DateTime.UtcNow.AddMinutes(60),
            User = userDto
        };
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
        if (user == null) return false;

        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        await _userManager.UpdateAsync(user);
        return true;
    }

    public async Task<bool> UserExistsAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email) != null;
    }

    public async Task<UserDto?> GetUserByIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return null;

        var userDto = _mapper.Map<UserDto>(user);
        userDto.Roles = await _userManager.GetRolesAsync(user);
        return userDto;
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return null;

        var userDto = _mapper.Map<UserDto>(user);
        userDto.Roles = await _userManager.GetRolesAsync(user);
        return userDto;
    }

    private async Task<string> GenerateJwtTokenAsync(User user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        // Tenant claim
        var tenantId = _tenantProvider.TenantId;
        claims.Add(new Claim("tenant", tenantId));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(60);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        return Guid.NewGuid().ToString();
    }
}
