using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SmallHR.API.Controllers;
using SmallHR.Core.DTOs.UsageMetrics;
using SmallHR.Core.Interfaces;

namespace SmallHR.Tests.Controllers;

public class UsageMetricsControllerTests
{
    private readonly Mock<IUsageMetricsService> _mockService;
    private readonly Mock<ILogger<UsageMetricsController>> _mockLogger;
    private readonly UsageMetricsController _controller;

    public UsageMetricsControllerTests()
    {
        _mockService = new Mock<IUsageMetricsService>();
        _mockLogger = new Mock<ILogger<UsageMetricsController>>();
        _controller = new UsageMetricsController(_mockService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetUsageSummary_ShouldReturnBadRequest_WhenTenantIdMissing()
    {
        var result = await _controller.GetUsageSummary(null);
        var action = Assert.IsType<ActionResult<UsageSummaryDto>>(result);
        Assert.IsType<BadRequestObjectResult>(action.Result);
    }

    [Fact]
    public async Task GetUsageSummary_ShouldReturnSummary_WhenTenantIdProvided()
    {
        var dto = new UsageSummaryDto { TenantId = 1, TenantName = "Acme" };
        _mockService.Setup(s => s.GetUsageSummaryAsync(1)).ReturnsAsync(dto);

        var result = await _controller.GetUsageSummary(1);
        var action = Assert.IsType<ActionResult<UsageSummaryDto>>(result);
        var ok = Assert.IsType<OkObjectResult>(action.Result);
        var payload = Assert.IsType<UsageSummaryDto>(ok.Value);
        Assert.Equal(1, payload.TenantId);
    }
}


