using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmallHR.API.Base;
using SmallHR.API.Authorization;
using SmallHR.Core.Interfaces;

namespace SmallHR.API.Controllers;

/// <summary>
/// Tenant Lifecycle Management Controller
/// Handles tenant signup, activation, suspension, cancellation, and data export
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AuthorizeAdmin]
public class TenantLifecycleController : BaseApiController
{
    private readonly ITenantLifecycleService _lifecycleService;

    public TenantLifecycleController(
        ITenantLifecycleService lifecycleService,
        ILogger<TenantLifecycleController> logger) : base(logger)
    {
        _lifecycleService = lifecycleService;
    }

    /// <summary>
    /// Sign up a new tenant
    /// </summary>
    [HttpPost("signup")]
    [AllowAnonymous] // Allow public signup
    public async Task<ActionResult<object>> Signup([FromBody] SignupRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return await HandleServiceResultAsync<object>(
            async () =>
            {
                var result = await _lifecycleService.SignupAsync(request);
                
                if (result.Success)
                {
                    return new 
                    { 
                        tenantId = result.TenantId,
                        message = "Tenant signup initiated. Provisioning will start automatically.",
                        status = "Provisioning"
                    } as object;
                }
                
                throw new InvalidOperationException(result.ErrorMessage ?? "Failed to signup tenant");
            },
            "signing up tenant"
        );
    }

    /// <summary>
    /// Activate a tenant (after billing confirmation)
    /// </summary>
    [HttpPost("{tenantId}/activate")]
    public async Task<ActionResult<object>> ActivateTenant(int tenantId, [FromQuery] string? externalCustomerId = null)
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                var success = await _lifecycleService.ActivateTenantAsync(tenantId, externalCustomerId);
                
                if (!success)
                {
                    throw new InvalidOperationException("Failed to activate tenant");
                }
                
                return new { message = "Tenant activated successfully" };
            },
            "activating tenant"
        );
    }

    /// <summary>
    /// Upgrade tenant plan
    /// </summary>
    [HttpPost("{tenantId}/upgrade")]
    public async Task<ActionResult<object>> UpgradePlan(int tenantId, [FromBody] UpgradePlanRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return await HandleServiceResultAsync(
            async () =>
            {
                var success = await _lifecycleService.UpgradePlanAsync(tenantId, request.NewPlanId);
                
                if (!success)
                {
                    throw new InvalidOperationException("Failed to upgrade plan");
                }
                
                return new { message = "Plan upgraded successfully" };
            },
            "upgrading plan"
        );
    }

    /// <summary>
    /// Downgrade tenant plan
    /// </summary>
    [HttpPost("{tenantId}/downgrade")]
    public async Task<ActionResult<object>> DowngradePlan(int tenantId, [FromBody] DowngradePlanRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return await HandleServiceResultAsync(
            async () =>
            {
                var success = await _lifecycleService.DowngradePlanAsync(tenantId, request.NewPlanId);
                
                if (!success)
                {
                    throw new InvalidOperationException("Failed to downgrade plan");
                }
                
                return new { message = "Plan downgraded successfully" };
            },
            "downgrading plan"
        );
    }

    /// <summary>
    /// Suspend a tenant (e.g., payment failure)
    /// </summary>
    [HttpPost("{tenantId}/suspend")]
    [AuthorizeSuperAdmin] // Only SuperAdmin can suspend
    public async Task<ActionResult<object>> SuspendTenant(int tenantId, [FromBody] SuspendTenantRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return await HandleServiceResultAsync(
            async () =>
            {
                var success = await _lifecycleService.SuspendTenantAsync(tenantId, request.Reason, request.GracePeriodDays);
                
                if (!success)
                {
                    throw new InvalidOperationException("Failed to suspend tenant");
                }
                
                return new { message = "Tenant suspended successfully" };
            },
            "suspending tenant"
        );
    }

    /// <summary>
    /// Resume a suspended tenant
    /// </summary>
    [HttpPost("{tenantId}/resume")]
    [AuthorizeSuperAdmin] // Only SuperAdmin can resume
    public async Task<ActionResult<object>> ResumeTenant(int tenantId)
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                var success = await _lifecycleService.ResumeTenantAsync(tenantId);
                
                if (!success)
                {
                    throw new InvalidOperationException("Failed to resume tenant");
                }
                
                return new { message = "Tenant resumed successfully" };
            },
            "resuming tenant"
        );
    }

    /// <summary>
    /// Cancel a tenant
    /// </summary>
    [HttpPost("{tenantId}/cancel")]
    [AuthorizeSuperAdmin] // Only SuperAdmin can cancel
    public async Task<ActionResult<object>> CancelTenant(int tenantId, [FromBody] CancelTenantRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return await HandleServiceResultAsync(
            async () =>
            {
                var success = await _lifecycleService.CancelTenantAsync(tenantId, request.Reason, request.ScheduleDeletion, request.RetentionDays);
                
                if (!success)
                {
                    throw new InvalidOperationException("Failed to cancel tenant");
                }
                
                return new { message = "Tenant cancelled successfully" };
            },
            "cancelling tenant"
        );
    }

    /// <summary>
    /// Export tenant data
    /// </summary>
    [HttpGet("{tenantId}/export")]
    public async Task<ActionResult<FileContentResult>> ExportTenantData(int tenantId)
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                var exportData = await _lifecycleService.ExportTenantDataAsync(tenantId);
                return File(exportData, "application/json", $"tenant-{tenantId}-export-{DateTime.UtcNow:yyyyMMddHHmmss}.json");
            },
            "exporting tenant data"
        );
    }

    /// <summary>
    /// Get lifecycle events for a tenant
    /// </summary>
    [HttpGet("{tenantId}/events")]
    public async Task<ActionResult<object>> GetLifecycleEvents(int tenantId, [FromQuery] int limit = 100)
    {
        return await HandleServiceResultAsync(
            () => _lifecycleService.GetLifecycleEventsAsync(tenantId, limit),
            "getting lifecycle events"
        );
    }

    /// <summary>
    /// Get suspension info for a tenant
    /// </summary>
    [HttpGet("{tenantId}/suspension-info")]
    public async Task<ActionResult<object>> GetSuspensionInfo(int tenantId)
    {
        return await HandleServiceResultOrNotFoundAsync(
            () => _lifecycleService.GetSuspensionInfoAsync(tenantId),
            $"getting suspension info for tenant {tenantId}",
            "SuspensionInfo"
        );
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

