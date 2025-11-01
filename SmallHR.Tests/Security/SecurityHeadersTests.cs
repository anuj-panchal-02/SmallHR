using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Moq;
using SmallHR.API.Middleware;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SmallHR.Tests.Security;

public class SecurityHeadersTests
{
    private class MockHostEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "SmallHR.API";
        public string ContentRootPath { get; set; } = "/";
        public string WebRootPath { get; set; } = "/wwwroot";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!;
    }

    [Fact]
    public async Task Should_Add_XContentTypeOptions_Header()
    {
        // Arrange
        var mockEnv = new MockHostEnvironment();
        var middleware = new SecurityHeadersMiddleware(
            context => Task.CompletedTask,
            mockEnv);
        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("X-Content-Type-Options"));
        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"].ToString());
    }

    [Fact]
    public async Task Should_Add_XFrameOptions_Header()
    {
        // Arrange
        var mockEnv = new MockHostEnvironment();
        var middleware = new SecurityHeadersMiddleware(
            context => Task.CompletedTask,
            mockEnv);
        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("X-Frame-Options"));
        Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"].ToString());
    }

    [Fact]
    public async Task Should_Add_XXssProtection_Header()
    {
        // Arrange
        var mockEnv = new MockHostEnvironment();
        var middleware = new SecurityHeadersMiddleware(
            context => Task.CompletedTask,
            mockEnv);
        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("X-XSS-Protection"));
        Assert.Equal("1; mode=block", context.Response.Headers["X-XSS-Protection"].ToString());
    }

    [Fact]
    public async Task Should_Add_ReferrerPolicy_Header()
    {
        // Arrange
        var mockEnv = new MockHostEnvironment();
        var middleware = new SecurityHeadersMiddleware(
            context => Task.CompletedTask,
            mockEnv);
        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("Referrer-Policy"));
        Assert.Equal("strict-origin-when-cross-origin", context.Response.Headers["Referrer-Policy"].ToString());
    }

    [Fact]
    public async Task Should_Add_XDownloadOptions_Header()
    {
        // Arrange
        var mockEnv = new MockHostEnvironment();
        var middleware = new SecurityHeadersMiddleware(
            context => Task.CompletedTask,
            mockEnv);
        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("X-Download-Options"));
        Assert.Equal("noopen", context.Response.Headers["X-Download-Options"].ToString());
    }

    [Fact]
    public async Task Should_Add_XDnsPrefetchControl_Header()
    {
        // Arrange
        var mockEnv = new MockHostEnvironment();
        var middleware = new SecurityHeadersMiddleware(
            context => Task.CompletedTask,
            mockEnv);
        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("X-DNS-Prefetch-Control"));
        Assert.Equal("off", context.Response.Headers["X-DNS-Prefetch-Control"].ToString());
    }

    [Fact]
    public async Task Should_Add_XPermittedCrossDomainPolicies_Header()
    {
        // Arrange
        var mockEnv = new MockHostEnvironment();
        var middleware = new SecurityHeadersMiddleware(
            context => Task.CompletedTask,
            mockEnv);
        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("X-Permitted-Cross-Domain-Policies"));
        Assert.Equal("none", context.Response.Headers["X-Permitted-Cross-Domain-Policies"].ToString());
    }

    [Fact]
    public async Task Should_Add_PermissionsPolicy_Header()
    {
        // Arrange
        var mockEnv = new MockHostEnvironment();
        var middleware = new SecurityHeadersMiddleware(
            context => Task.CompletedTask,
            mockEnv);
        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("Permissions-Policy"));
        var permissionsPolicy = context.Response.Headers["Permissions-Policy"].ToString();
        Assert.Contains("accelerometer=()", permissionsPolicy);
        Assert.Contains("camera=()", permissionsPolicy);
        Assert.Contains("microphone=()", permissionsPolicy);
    }

    [Fact]
    public async Task Should_Add_ContentSecurityPolicy_Header()
    {
        // Arrange
        var mockEnv = new MockHostEnvironment();
        var middleware = new SecurityHeadersMiddleware(
            context => Task.CompletedTask,
            mockEnv);
        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("Content-Security-Policy"));
        var csp = context.Response.Headers["Content-Security-Policy"].ToString();
        Assert.Contains("default-src 'self'", csp);
        Assert.Contains("frame-ancestors 'none'", csp);
    }

    [Fact]
    public async Task Should_Add_StrictTransportSecurity_On_Https()
    {
        // Arrange
        var mockEnv = new MockHostEnvironment();
        var middleware = new SecurityHeadersMiddleware(
            context => Task.CompletedTask,
            mockEnv);
        var context = new DefaultHttpContext();
        context.Request.IsHttps = true;
        context.Request.Host = new HostString("example.com");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("Strict-Transport-Security"));
        var hsts = context.Response.Headers["Strict-Transport-Security"].ToString();
        Assert.Contains("max-age=31536000", hsts);
        Assert.Contains("includeSubDomains", hsts);
        Assert.Contains("preload", hsts);
    }

    [Fact]
    public async Task Should_Not_Add_HSTS_On_Http()
    {
        // Arrange
        var mockEnv = new MockHostEnvironment();
        var middleware = new SecurityHeadersMiddleware(
            context => Task.CompletedTask,
            mockEnv);
        var context = new DefaultHttpContext();
        context.Request.IsHttps = false;

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.False(context.Response.Headers.ContainsKey("Strict-Transport-Security"));
    }

    [Fact]
    public async Task Should_Not_Add_HSTS_On_Localhost()
    {
        // Arrange
        var mockEnv = new MockHostEnvironment();
        var middleware = new SecurityHeadersMiddleware(
            context => Task.CompletedTask,
            mockEnv);
        var context = new DefaultHttpContext();
        context.Request.IsHttps = true;
        context.Request.Host = new HostString("localhost:5000");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.False(context.Response.Headers.ContainsKey("Strict-Transport-Security"));
    }

    [Fact]
    public async Task Should_Use_Permissive_CSP_In_Development()
    {
        // Arrange
        var mockEnv = new MockHostEnvironment { EnvironmentName = Environments.Development };
        var middleware = new SecurityHeadersMiddleware(
            context => Task.CompletedTask,
            mockEnv);
        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var csp = context.Response.Headers["Content-Security-Policy"].ToString();
        // Development allows unsafe-inline and unsafe-eval
        Assert.Contains("'unsafe-inline'", csp);
        Assert.Contains("'unsafe-eval'", csp);
    }

    [Fact]
    public async Task Should_Use_Strict_CSP_In_Production()
    {
        // Arrange
        var mockEnv = new MockHostEnvironment { EnvironmentName = Environments.Production };
        var middleware = new SecurityHeadersMiddleware(
            context => Task.CompletedTask,
            mockEnv);
        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var csp = context.Response.Headers["Content-Security-Policy"].ToString();
        // Production should not allow unsafe-inline or unsafe-eval
        Assert.DoesNotContain("'unsafe-inline'", csp);
        Assert.DoesNotContain("'unsafe-eval'", csp);
        // Should include upgrade-insecure-requests
        Assert.Contains("upgrade-insecure-requests", csp);
    }

    [Fact]
    public async Task Should_Call_Next_Middleware()
    {
        // Arrange
        var mockEnv = new MockHostEnvironment();
        var nextCalled = false;
        var middleware = new SecurityHeadersMiddleware(
            context => 
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            mockEnv);
        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Should_Include_All_Security_Headers()
    {
        // Arrange
        var mockEnv = new MockHostEnvironment();
        var middleware = new SecurityHeadersMiddleware(
            context => Task.CompletedTask,
            mockEnv);
        var context = new DefaultHttpContext();
        context.Request.IsHttps = true;
        context.Request.Host = new HostString("example.com");

        // Act
        await middleware.InvokeAsync(context);

        // Assert - Verify all required headers are present
        var requiredHeaders = new[]
        {
            "X-Content-Type-Options",
            "X-Frame-Options",
            "X-XSS-Protection",
            "Referrer-Policy",
            "X-Download-Options",
            "X-DNS-Prefetch-Control",
            "X-Permitted-Cross-Domain-Policies",
            "Permissions-Policy",
            "Content-Security-Policy",
            "Strict-Transport-Security"
        };

        foreach (var header in requiredHeaders)
        {
            Assert.True(
                context.Response.Headers.ContainsKey(header),
                $"Missing required security header: {header}");
        }
    }
}

