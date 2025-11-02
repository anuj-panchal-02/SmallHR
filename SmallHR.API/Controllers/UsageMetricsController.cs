using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmallHR.Core.Interfaces;

namespace SmallHR.API.Controllers;

/// <summary>
/// Usage Metrics Controller
/// Tracks and reports usage metrics for plan limit enforcement
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsageMetricsController : ControllerBase
{
    private readonly IUsageMetricsService _usageMetricsService;
    private readonly ILogger<UsageMetricsController> _logger;

    public UsageMetricsController(
        IUsageMetricsService usageMetricsService,
        ILogger<UsageMetricsController> logger)
    {
        _usageMetricsService = usageMetricsService;
        _logger = logger;
    }

    /// <summary>
    /// Get current usage summary for tenant
    /// </summary>
    [HttpGet("summary")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<UsageSummaryDto>> GetUsageSummary([FromQuery] int? tenantId = null)
    {
        try
        {
            // TODO: Get tenant ID from authenticated user's tenant claim or context
            // For now, this is a placeholder - would need tenant ID resolution
            if (!tenantId.HasValue)
            {
                return BadRequest(new { message = "Tenant ID is required" });
            }

            var summary = await _usageMetricsService.GetUsageSummaryAsync(tenantId.Value);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage summary: {Message}", ex.Message);
            return StatusCode(500, new { message = "Error getting usage summary" });
        }
    }

    /// <summary>
    /// Get detailed usage breakdown
    /// </summary>
    [HttpGet("breakdown")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<Dictionary<string, object>>> GetUsageBreakdown([FromQuery] int? tenantId = null)
    {
        try
        {
            if (!tenantId.HasValue)
            {
                return BadRequest(new { message = "Tenant ID is required" });
            }

            var breakdown = await _usageMetricsService.GetUsageBreakdownAsync(tenantId.Value);
            return Ok(breakdown);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage breakdown: {Message}", ex.Message);
            return StatusCode(500, new { message = "Error getting usage breakdown" });
        }
    }

    /// <summary>
    /// Get employee count
    /// </summary>
    [HttpGet("employees/count")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<int>> GetEmployeeCount([FromQuery] int? tenantId = null)
    {
        try
        {
            if (!tenantId.HasValue)
            {
                return BadRequest(new { message = "Tenant ID is required" });
            }

            var count = await _usageMetricsService.GetEmployeeCountAsync(tenantId.Value);
            return Ok(new { count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting employee count: {Message}", ex.Message);
            return StatusCode(500, new { message = "Error getting employee count" });
        }
    }

    /// <summary>
    /// Get storage usage
    /// </summary>
    [HttpGet("storage")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<long>> GetStorageUsage([FromQuery] int? tenantId = null)
    {
        try
        {
            if (!tenantId.HasValue)
            {
                return BadRequest(new { message = "Tenant ID is required" });
            }

            var usage = await _usageMetricsService.GetStorageUsageAsync(tenantId.Value);
            return Ok(new { storageBytes = usage, storageMB = usage / (1024.0 * 1024.0), storageGB = usage / (1024.0 * 1024.0 * 1024.0) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting storage usage: {Message}", ex.Message);
            return StatusCode(500, new { message = "Error getting storage usage" });
        }
    }

    /// <summary>
    /// Get API request count
    /// </summary>
    [HttpGet("api-requests")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<long>> GetApiRequestCount([FromQuery] int? tenantId = null, [FromQuery] DateTime? fromDate = null)
    {
        try
        {
            if (!tenantId.HasValue)
            {
                return BadRequest(new { message = "Tenant ID is required" });
            }

            var count = await _usageMetricsService.GetApiRequestCountAsync(tenantId.Value, fromDate);
            return Ok(new { count, fromDate = fromDate ?? DateTime.UtcNow.Date });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting API request count: {Message}", ex.Message);
            return StatusCode(500, new { message = "Error getting API request count" });
        }
    }
}

