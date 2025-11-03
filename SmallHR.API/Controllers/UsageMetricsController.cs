using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmallHR.API.Base;
using SmallHR.API.Authorization;
using SmallHR.Core.Interfaces;
using SmallHR.Core.DTOs.UsageMetrics;

namespace SmallHR.API.Controllers;

/// <summary>
/// Usage Metrics Controller
/// Tracks and reports usage metrics for plan limit enforcement
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsageMetricsController : BaseApiController
{
    private readonly IUsageMetricsService _usageMetricsService;

    public UsageMetricsController(
        IUsageMetricsService usageMetricsService,
        ILogger<UsageMetricsController> logger) : base(logger)
    {
        _usageMetricsService = usageMetricsService;
    }

    /// <summary>
    /// Get current usage summary for tenant
    /// </summary>
    [HttpGet("summary")]
    [AuthorizeAdmin]
    public async Task<ActionResult<UsageSummaryDto>> GetUsageSummary([FromQuery] int? tenantId = null)
    {
        // TODO: Get tenant ID from authenticated user's tenant claim or context
        // For now, this is a placeholder - would need tenant ID resolution
        if (!tenantId.HasValue)
        {
            return CreateBadRequestResponse("Tenant ID is required");
        }

        return await HandleServiceResultAsync(
            () => _usageMetricsService.GetUsageSummaryAsync(tenantId.Value),
            "getting usage summary"
        );
    }

    /// <summary>
    /// Get detailed usage breakdown
    /// </summary>
    [HttpGet("breakdown")]
    [AuthorizeAdmin]
    public async Task<ActionResult<Dictionary<string, object>>> GetUsageBreakdown([FromQuery] int? tenantId = null)
    {
        if (!tenantId.HasValue)
        {
            return CreateBadRequestResponse("Tenant ID is required");
        }

        return await HandleServiceResultAsync(
            () => _usageMetricsService.GetUsageBreakdownAsync(tenantId.Value),
            "getting usage breakdown"
        );
    }

    /// <summary>
    /// Get employee count
    /// </summary>
    [HttpGet("employees/count")]
    [AuthorizeAdmin]
    public async Task<ActionResult<object>> GetEmployeeCount([FromQuery] int? tenantId = null)
    {
        if (!tenantId.HasValue)
        {
            return CreateBadRequestResponse("Tenant ID is required");
        }

        return await HandleServiceResultAsync(
            async () => new { count = await _usageMetricsService.GetEmployeeCountAsync(tenantId.Value) },
            "getting employee count"
        );
    }

    /// <summary>
    /// Get storage usage
    /// </summary>
    [HttpGet("storage")]
    [AuthorizeAdmin]
    public async Task<ActionResult<object>> GetStorageUsage([FromQuery] int? tenantId = null)
    {
        if (!tenantId.HasValue)
        {
            return CreateBadRequestResponse("Tenant ID is required");
        }

        return await HandleServiceResultAsync(
            async () =>
            {
                var usage = await _usageMetricsService.GetStorageUsageAsync(tenantId.Value);
                return new { storageBytes = usage, storageMB = usage / (1024.0 * 1024.0), storageGB = usage / (1024.0 * 1024.0 * 1024.0) };
            },
            "getting storage usage"
        );
    }

    /// <summary>
    /// Get API request count
    /// </summary>
    [HttpGet("api-requests")]
    [AuthorizeAdmin]
    public async Task<ActionResult<object>> GetApiRequestCount([FromQuery] int? tenantId = null, [FromQuery] DateTime? fromDate = null)
    {
        if (!tenantId.HasValue)
        {
            return CreateBadRequestResponse("Tenant ID is required");
        }

        return await HandleServiceResultAsync(
            async () =>
            {
                var count = await _usageMetricsService.GetApiRequestCountAsync(tenantId.Value, fromDate);
                return new { count, fromDate = fromDate ?? DateTime.UtcNow.Date };
            },
            "getting API request count"
        );
    }

    /// <summary>
    /// Get dashboard overview with aggregated metrics across all tenants
    /// SuperAdmin only - returns aggregated metrics and per-tenant breakdown
    /// </summary>
    [HttpGet("dashboard")]
    [AuthorizeSuperAdmin]
    public async Task<ActionResult<DashboardOverviewDto>> GetDashboard([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        return await HandleServiceResultAsync(
            () => _usageMetricsService.GetDashboardOverviewAsync(startDate, endDate),
            "getting dashboard overview"
        );
    }
}

