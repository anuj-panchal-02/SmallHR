using SmallHR.Core.DTOs.Auth;
using SmallHR.Core.Entities;

namespace SmallHR.Tests.Services;

public class SimpleAuthServiceTests
{
    [Fact]
    public void LoginDto_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var loginDto = new LoginDto
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        // Assert
        Assert.Equal("test@example.com", loginDto.Email);
        Assert.Equal("Password123!", loginDto.Password);
    }

    [Fact]
    public void RegisterDto_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var registerDto = new RegisterDto
        {
            Email = "newuser@example.com",
            Password = "Password123!",
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateTime(1990, 1, 1)
        };

        // Assert
        Assert.Equal("newuser@example.com", registerDto.Email);
        Assert.Equal("Password123!", registerDto.Password);
        Assert.Equal("John", registerDto.FirstName);
        Assert.Equal("Doe", registerDto.LastName);
        Assert.Equal(new DateTime(1990, 1, 1), registerDto.DateOfBirth);
    }

    [Fact]
    public void AuthResponseDto_ShouldHaveRequiredProperties()
    {
        // Arrange & Act
        var authResponse = new AuthResponseDto
        {
            Token = "jwt-token",
            RefreshToken = "refresh-token",
            Expiration = DateTime.UtcNow.AddMinutes(60),
            User = new UserDto
            {
                Id = "1",
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe"
            }
        };

        // Assert
        Assert.Equal("jwt-token", authResponse.Token);
        Assert.Equal("refresh-token", authResponse.RefreshToken);
        Assert.NotNull(authResponse.User);
        Assert.Equal("test@example.com", authResponse.User.Email);
    }

    [Fact]
    public void UserDto_ShouldHaveFullNameProperty()
    {
        // Arrange & Act
        var userDto = new UserDto
        {
            FirstName = "John",
            LastName = "Doe"
        };

        // Assert
        Assert.Equal("John Doe", userDto.FullName);
    }

    [Fact]
    public void User_ShouldHaveRefreshTokenProperties()
    {
        // Arrange & Act
        var user = new User
        {
            Id = "1",
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            RefreshToken = "refresh-token",
            RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7)
        };

        // Assert
        Assert.Equal("refresh-token", user.RefreshToken);
        Assert.NotNull(user.RefreshTokenExpiryTime);
    }
}
