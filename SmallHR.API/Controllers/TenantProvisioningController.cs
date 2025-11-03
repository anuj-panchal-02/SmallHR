using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmallHR.API.Base;
using SmallHR.API.Authorization;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using SmallHR.Infrastructure.Data;

namespace SmallHR.API.Controllers;

/// <summary>
/// Tenant Provisioning Controller
/// Handles automated tenant provisioning with subscriptions, data seeding, and admin user creation
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AuthorizeSuperAdmin]
public class TenantProvisioningController : BaseApiController
{
    private readonly ITenantProvisioningService _provisioningService;
    private readonly ApplicationDbContext _context;

    public TenantProvisioningController(
        ITenantProvisioningService provisioningService,
        ApplicationDbContext context,
        ILogger<TenantProvisioningController> logger) : base(logger)
    {
        _provisioningService = provisioningService;
        _context = context;
    }

    /// <summary>
    /// Provision a tenant synchronously
    /// Creates subscription, seeds default data, creates admin user, and sends welcome email
    /// 
    /// Note: Tenant must be created first via POST /api/tenants
    /// </summary>
    [HttpPost("{tenantId}")]
    public async Task<ActionResult<object>> ProvisionTenant(
        int tenantId,
        [FromBody] ProvisionTenantRequest? request = null)
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                // Get tenant record to retrieve admin info
                var tenant = await _context.Tenants.FindAsync(tenantId);
                if (tenant == null)
                {
                    throw new KeyNotFoundException($"Tenant with ID {tenantId} not found");
                }

                // Use admin info from tenant record, or from request if provided
                var adminEmail = request?.AdminEmail ?? tenant.AdminEmail ?? throw new InvalidOperationException("AdminEmail is required");
                var adminFirstName = request?.AdminFirstName ?? tenant.AdminFirstName ?? "Admin";
                var adminLastName = request?.AdminLastName ?? tenant.AdminLastName ?? tenant.Name ?? "User";
                var subscriptionPlanId = request?.SubscriptionPlanId;
                var startTrial = request?.StartTrial ?? false;
                
                var (success, errorMessage, result) = await _provisioningService.ProvisionTenantAsync(
                    tenantId,
                    adminEmail,
                    adminFirstName,
                    adminLastName,
                    subscriptionPlanId,
                    startTrial);

                if (success && result != null)
                {
                    return new
                    {
                        message = "Tenant provisioned successfully",
                        tenantId = result.TenantId,
                        tenantName = result.TenantName,
                        subscriptionId = result.SubscriptionId,
                        adminEmail = result.AdminEmail,
                        adminUserId = result.AdminUserId,
                        emailSent = result.EmailSent,
                        stepsCompleted = result.StepsCompleted
                    };
                }

                throw new InvalidOperationException(errorMessage ?? "Failed to provision tenant");
            },
            "provisioning tenant"
        );
    }

    /// <summary>
    /// Provision a tenant synchronously (for testing/debugging)
    /// Use this for immediate provisioning during development
    /// </summary>
    [HttpPost("{tenantId}/sync")]
    public async Task<ActionResult<object>> ProvisionTenantSync(
        int tenantId,
        [FromBody] ProvisionTenantRequest? request = null)
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                request ??= new ProvisionTenantRequest
                {
                    AdminEmail = "admin@tenant.com",
                    AdminFirstName = "Admin",
                    AdminLastName = "User"
                };

                if (string.IsNullOrWhiteSpace(request.AdminEmail))
                {
                    throw new ArgumentException("AdminEmail is required");
                }

                var (success, errorMessage, result) = await _provisioningService.ProvisionTenantAsync(
                    tenantId,
                    request.AdminEmail,
                    request.AdminFirstName ?? "Admin",
                    request.AdminLastName ?? "User",
                    request.SubscriptionPlanId,
                    request.StartTrial ?? false);

                if (success && result != null)
                {
                    return new
                    {
                        message = "Tenant provisioned successfully",
                        tenantId = result.TenantId,
                        tenantName = result.TenantName,
                        subscriptionId = result.SubscriptionId,
                        adminEmail = result.AdminEmail,
                        adminUserId = result.AdminUserId,
                        emailSent = result.EmailSent,
                        stepsCompleted = result.StepsCompleted
                    };
                }

                throw new InvalidOperationException(errorMessage ?? "Failed to provision tenant");
            },
            "provisioning tenant synchronously"
        );
    }

    /// <summary>
    /// Provisioning request model
    /// </summary>
    public class ProvisionTenantRequest
    {
        public string AdminEmail { get; set; } = string.Empty;
        public string? AdminFirstName { get; set; }
        public string? AdminLastName { get; set; }
        public int? SubscriptionPlanId { get; set; }
        public bool? StartTrial { get; set; }
    }
}

