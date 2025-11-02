using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmallHR.Core.Interfaces;

namespace SmallHR.API.Controllers;

/// <summary>
/// Tenant Lifecycle Management Controller
/// Handles tenant signup, activation, suspension, cancellation, and data export
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class TenantLifecycleController : ControllerBase
{
    private readonly ITenantLifecycleService _lifecycleService;
    private readonly ILogger<TenantLifecycleController> _logger;

    public TenantLifecycleController(
        ITenantLifecycleService lifecycleService,
        ILogger<TenantLifecycleController> logger)
    {
        _lifecycleService = lifecycleService;
        _logger = logger;
    }

    /// <summary>
    /// Sign up a new tenant
    /// </summary>
    [HttpPost("signup")]
    [AllowAnonymous] // Allow public signup
    public async Task<ActionResult> Signup([FromBody] SignupRequest request)
    {
        try
        {
            var result = await _lifecycleService.SignupAsync(request);
            
            if (result.Success)
            {
                return Ok(new 
                { 
                    tenantId = result.TenantId,
                    message = "Tenant signup initiated. Provisioning will start automatically.",
                    status = "Provisioning"
                });
            }
            
            return BadRequest(new { error = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during tenant signup: {Message}", ex.Message);
            return StatusCode(500, new { error = "An error occurred during signup" });
        }
    }

    /// <summary>
    /// Activate a tenant (after billing confirmation)
    /// </summary>
    [HttpPost("{tenantId}/activate")]
    public async Task<ActionResult> ActivateTenant(int tenantId, [FromQuery] string? externalCustomerId = null)
    {
        try
        {
            var success = await _lifecycleService.ActivateTenantAsync(tenantId, externalCustomerId);
            
            if (success)
            {
                return Ok(new { message = "Tenant activated successfully" });
            }
            
            return BadRequest(new { error = "Failed to activate tenant" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating tenant {TenantId}: {Message}", tenantId, ex.Message);
            return StatusCode(500, new { error = "An error occurred during activation" });
        }
    }

    /// <summary>
    /// Upgrade tenant plan
    /// </summary>
    [HttpPost("{tenantId}/upgrade")]
    public async Task<ActionResult> UpgradePlan(int tenantId, [FromBody] UpgradePlanRequest request)
    {
        try
        {
            var success = await _lifecycleService.UpgradePlanAsync(tenantId, request.NewPlanId);
            
            if (success)
            {
                return Ok(new { message = "Plan upgraded successfully" });
            }
            
            return BadRequest(new { error = "Failed to upgrade plan" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upgrading plan for tenant {TenantId}: {Message}", tenantId, ex.Message);
            return StatusCode(500, new { error = "An error occurred during upgrade" });
        }
    }

    /// <summary>
    /// Downgrade tenant plan
    /// </summary>
    [HttpPost("{tenantId}/downgrade")]
    public async Task<ActionResult> DowngradePlan(int tenantId, [FromBody] DowngradePlanRequest request)
    {
        try
        {
            var success = await _lifecycleService.DowngradePlanAsync(tenantId, request.NewPlanId);
            
            if (success)
            {
                return Ok(new { message = "Plan downgraded successfully" });
            }
            
            return BadRequest(new { error = "Failed to downgrade plan" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downgrading plan for tenant {TenantId}: {Message}", tenantId, ex.Message);
            return StatusCode(500, new { error = "An error occurred during downgrade" });
        }
    }

    /// <summary>
    /// Suspend a tenant (e.g., payment failure)
    /// </summary>
    [HttpPost("{tenantId}/suspend")]
    [Authorize(Roles = "SuperAdmin")] // Only SuperAdmin can suspend
    public async Task<ActionResult> SuspendTenant(int tenantId, [FromBody] SuspendTenantRequest request)
    {
        try
        {
            var success = await _lifecycleService.SuspendTenantAsync(tenantId, request.Reason, request.GracePeriodDays);
            
            if (success)
            {
                return Ok(new { message = "Tenant suspended successfully" });
            }
            
            return BadRequest(new { error = "Failed to suspend tenant" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suspending tenant {TenantId}: {Message}", tenantId, ex.Message);
            return StatusCode(500, new { error = "An error occurred during suspension" });
        }
    }

    /// <summary>
    /// Resume a suspended tenant
    /// </summary>
    [HttpPost("{tenantId}/resume")]
    [Authorize(Roles = "SuperAdmin")] // Only SuperAdmin can resume
    public async Task<ActionResult> ResumeTenant(int tenantId)
    {
        try
        {
            var success = await _lifecycleService.ResumeTenantAsync(tenantId);
            
            if (success)
            {
                return Ok(new { message = "Tenant resumed successfully" });
            }
            
            return BadRequest(new { error = "Failed to resume tenant" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming tenant {TenantId}: {Message}", tenantId, ex.Message);
            return StatusCode(500, new { error = "An error occurred during resume" });
        }
    }

    /// <summary>
    /// Cancel a tenant
    /// </summary>
    [HttpPost("{tenantId}/cancel")]
    [Authorize(Roles = "SuperAdmin")] // Only SuperAdmin can cancel
    public async Task<ActionResult> CancelTenant(int tenantId, [FromBody] CancelTenantRequest request)
    {
        try
        {
            var success = await _lifecycleService.CancelTenantAsync(tenantId, request.Reason, request.ScheduleDeletion, request.RetentionDays);
            
            if (success)
            {
                return Ok(new { message = "Tenant cancelled successfully" });
            }
            
            return BadRequest(new { error = "Failed to cancel tenant" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling tenant {TenantId}: {Message}", tenantId, ex.Message);
            return StatusCode(500, new { error = "An error occurred during cancellation" });
        }
    }

    /// <summary>
    /// Export tenant data
    /// </summary>
    [HttpGet("{tenantId}/export")]
    public async Task<ActionResult> ExportTenantData(int tenantId)
    {
        try
        {
            var exportData = await _lifecycleService.ExportTenantDataAsync(tenantId);
            
            return File(exportData, "application/json", $"tenant-{tenantId}-export-{DateTime.UtcNow:yyyyMMddHHmmss}.json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting data for tenant {TenantId}: {Message}", tenantId, ex.Message);
            return StatusCode(500, new { error = "An error occurred during export" });
        }
    }

    /// <summary>
    /// Get lifecycle events for a tenant
    /// </summary>
    [HttpGet("{tenantId}/events")]
    public async Task<ActionResult> GetLifecycleEvents(int tenantId, [FromQuery] int limit = 100)
    {
        try
        {
            var events = await _lifecycleService.GetLifecycleEventsAsync(tenantId, limit);
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lifecycle events for tenant {TenantId}: {Message}", tenantId, ex.Message);
            return StatusCode(500, new { error = "An error occurred while retrieving events" });
        }
    }

    /// <summary>
    /// Get suspension info for a tenant
    /// </summary>
    [HttpGet("{tenantId}/suspension-info")]
    public async Task<ActionResult> GetSuspensionInfo(int tenantId)
    {
        try
        {
            var info = await _lifecycleService.GetSuspensionInfoAsync(tenantId);
            
            if (info == null)
            {
                return NotFound(new { error = "Suspension info not found" });
            }
            
            return Ok(info);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting suspension info for tenant {TenantId}: {Message}", tenantId, ex.Message);
            return StatusCode(500, new { error = "An error occurred while retrieving suspension info" });
        }
    }
}

// Request DTOs
public class UpgradePlanRequest
{
    public int NewPlanId { get; set; }
}

public class DowngradePlanRequest
{
    public int NewPlanId { get; set; }
}

public class SuspendTenantRequest
{
    public required string Reason { get; set; }
    public int GracePeriodDays { get; set; } = 30;
}

public class CancelTenantRequest
{
    public required string Reason { get; set; }
    public bool ScheduleDeletion { get; set; } = true;
    public int RetentionDays { get; set; } = 90;
}

