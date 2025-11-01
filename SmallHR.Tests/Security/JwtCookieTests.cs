using Microsoft.AspNetCore.Http;
using Moq;
using SmallHR.API.Middleware;
using Xunit;

namespace SmallHR.Tests.Security;

public class JwtCookieTests
{
    private static HttpContext CreateHttpContextWithCookie(string cookieName, string cookieValue)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Cookie = $"{cookieName}={cookieValue}";
        return httpContext;
    }

    [Fact]
    public async Task Should_Extract_Token_From_Cookie_And_Add_To_Header()
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        var httpContext = CreateHttpContextWithCookie("accessToken", "test-token-123");
        
        var middleware = new JwtCookieMiddleware(mockNext.Object);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        Assert.True(httpContext.Request.Headers.ContainsKey("Authorization"));
        Assert.Equal("Bearer test-token-123", httpContext.Request.Headers["Authorization"].ToString());
        mockNext.Verify(next => next(httpContext), Times.Once);
    }

    [Fact]
    public async Task Should_Not_Add_Header_When_Cookie_Is_Missing()
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        var httpContext = new DefaultHttpContext();
        
        var middleware = new JwtCookieMiddleware(mockNext.Object);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        Assert.False(httpContext.Request.Headers.ContainsKey("Authorization"));
        mockNext.Verify(next => next(httpContext), Times.Once);
    }

    [Fact]
    public async Task Should_Not_Override_Existing_Authorization_Header()
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        var httpContext = CreateHttpContextWithCookie("accessToken", "token-from-cookie");
        httpContext.Request.Headers["Authorization"] = "Bearer existing-token";
        
        var middleware = new JwtCookieMiddleware(mockNext.Object);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        Assert.Equal("Bearer existing-token", httpContext.Request.Headers["Authorization"].ToString());
        mockNext.Verify(next => next(httpContext), Times.Once);
    }

    [Fact]
    public async Task Should_Handle_Empty_Token_Gracefully()
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        var httpContext = CreateHttpContextWithCookie("accessToken", "");
        
        var middleware = new JwtCookieMiddleware(mockNext.Object);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        Assert.False(httpContext.Request.Headers.ContainsKey("Authorization"));
        mockNext.Verify(next => next(httpContext), Times.Once);
    }

    [Fact]
    public async Task Should_Proceed_To_Next_Middleware_Even_On_Error()
    {
        // Arrange
        var mockNext = new Mock<RequestDelegate>();
        mockNext.Setup(next => next(It.IsAny<HttpContext>()))
            .ThrowsAsync(new Exception("Test error"));
        
        var httpContext = CreateHttpContextWithCookie("accessToken", "test-token");
        
        var middleware = new JwtCookieMiddleware(mockNext.Object);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => middleware.InvokeAsync(httpContext));
        mockNext.Verify(next => next(httpContext), Times.Once);
    }
}

