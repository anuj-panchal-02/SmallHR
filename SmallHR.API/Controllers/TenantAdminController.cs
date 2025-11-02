using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using SmallHR.Infrastructure.Data;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.DependencyInjection;
using SmallHR.API.Services;

namespace SmallHR.API.Controllers;

/// <summary>
/// Tenant Administration Console for SuperAdmin
/// Provides comprehensive tenant management, impersonation, metrics, and plan adjustments
/// </summary>
[ApiController]
[Route("api/admin/tenants")]
[Authorize(Roles = "SuperAdmin")]
public class TenantAdminController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IAdminAuditService _adminAuditService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TenantAdminController> _logger;
    private readonly ITenantProvisioningService? _provisioningService;

    public TenantAdminController(
        ApplicationDbContext context,
        UserManager<User> userManager,
        IAdminAuditService adminAuditService,
        IConfiguration configuration,
        ILogger<TenantAdminController> logger,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _userManager = userManager;
        _adminAuditService = adminAuditService;
        _configuration = configuration;
        _logger = logger;
        // Get provisioning service if available (optional dependency)
        _provisioningService = serviceProvider.GetService<ITenantProvisioningService>();
    }

    /// <summary>
    /// Get all tenants with comprehensive details (status, plan, usage, users)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllTenants(
        [FromQuery] string? search = null,
        [FromQuery] TenantStatus? status = null,
        [FromQuery] SubscriptionStatus? subscriptionStatus = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] string? sortOrder = "desc")
    {
        try
        {
            var query = _context.Tenants.AsQueryable();

            // Search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(t => 
                    t.Name.Contains(search) || 
                    t.Domain.Contains(search) ||
                    t.AdminEmail.Contains(search));
            }

            // Status filter
            if (status.HasValue)
            {
                query = query.Where(t => t.Status == status.Value);
            }

            // Subscription status filter
            if (subscriptionStatus.HasValue)
            {
                var tenantIds = await _context.Subscriptions
                    .Where(s => s.Status == subscriptionStatus.Value)
                    .Select(s => s.TenantId)
                    .ToListAsync();
                query = query.Where(t => tenantIds.Contains(t.Id));
            }

            // Sorting
            query = sortBy?.ToLower() switch
            {
                "name" => sortOrder == "asc" ? query.OrderBy(t => t.Name) : query.OrderByDescending(t => t.Name),
                "createdat" => sortOrder == "asc" ? query.OrderBy(t => t.CreatedAt) : query.OrderByDescending(t => t.CreatedAt),
                "status" => sortOrder == "asc" ? query.OrderBy(t => t.Status) : query.OrderByDescending(t => t.Status),
                _ => query.OrderByDescending(t => t.CreatedAt)
            };

            var totalCount = await query.CountAsync();
            var tenants = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Get user counts and usage metrics for each tenant
            var tenantDetails = new List<object>();
            foreach (var tenant in tenants)
            {
                var userCount = await _context.Users.CountAsync(u => u.TenantId == tenant.Id.ToString());
                var employeeCount = await _context.Employees.CountAsync(e => e.TenantId == tenant.Id.ToString());
                var usageMetrics = await _context.TenantUsageMetrics
                    .FirstOrDefaultAsync(m => m.TenantId == tenant.Id);

                var activeSubscription = await _context.Subscriptions
                    .Include(s => s.Plan)
                    .FirstOrDefaultAsync(s => s.TenantId == tenant.Id && s.Status == SubscriptionStatus.Active);
                var subscription = activeSubscription != null
                    ? new
                    {
                    planName = activeSubscription.Plan?.Name ?? "Unknown",
                    status = activeSubscription.Status.ToString(),
                    currentPeriodEnd = activeSubscription.EndDate,
                    price = activeSubscription.Price,
                    billingPeriod = activeSubscription.BillingPeriod.ToString()
                    }
                    : null;

                tenantDetails.Add(new
                {
                    id = tenant.Id,
                    name = tenant.Name,
                    domain = tenant.Domain,
                    status = tenant.Status.ToString(),
                    isActive = tenant.IsActive,
                    isSubscriptionActive = tenant.IsSubscriptionActive,
                    adminEmail = tenant.AdminEmail,
                    adminFirstName = tenant.AdminFirstName,
                    adminLastName = tenant.AdminLastName,
                    createdAt = tenant.CreatedAt,
                    updatedAt = tenant.UpdatedAt,
                    subscription = subscription,
                    userCount = userCount,
                    employeeCount = employeeCount,
                    usageMetrics = usageMetrics != null ? new
                    {
                        apiRequestCount = usageMetrics.ApiRequestCount,
                        lastUpdated = usageMetrics.UpdatedAt
                    } : null
                });
            }

            return Ok(new
            {
                totalCount = totalCount,
                pageNumber = pageNumber,
                pageSize = pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                tenants = tenantDetails
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tenants");
            return StatusCode(500, new { message = "Error fetching tenants", error = ex.Message });
        }
    }

    /// <summary>
    /// Get tenant details by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTenantById(int id)
    {
        try
        {
            var tenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tenant == null)
            {
                return NotFound(new { message = "Tenant not found" });
            }

            var userCount = await _context.Users.CountAsync(u => u.TenantId == tenant.Id.ToString());
            var employeeCount = await _context.Employees.CountAsync(e => e.TenantId == tenant.Id.ToString());
            var usageMetrics = await _context.TenantUsageMetrics
                .FirstOrDefaultAsync(m => m.TenantId == tenant.Id);
            
            var subscriptionHistory = await _context.Subscriptions
                .Where(s => s.TenantId == tenant.Id)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var lifecycleEvents = await _context.TenantLifecycleEvents
                .Where(e => e.TenantId == tenant.Id)
                .OrderByDescending(e => e.EventDate)
                .Take(10)
                .ToListAsync();

            return Ok(new
            {
                id = tenant.Id,
                name = tenant.Name,
                domain = tenant.Domain,
                status = tenant.Status.ToString(),
                isActive = tenant.IsActive,
                isSubscriptionActive = tenant.IsSubscriptionActive,
                adminEmail = tenant.AdminEmail,
                adminFirstName = tenant.AdminFirstName,
                adminLastName = tenant.AdminLastName,
                createdAt = tenant.CreatedAt,
                updatedAt = tenant.UpdatedAt,
                userCount = userCount,
                employeeCount = employeeCount,
                usageMetrics = usageMetrics,
                subscriptions = subscriptionHistory.Select(s => new
                {
                    id = s.Id,
                    planName = s.Plan?.Name ?? "Unknown",
                    status = s.Status.ToString(),
                    price = s.Price,
                    billingPeriod = s.BillingPeriod.ToString(),
                    startDate = s.StartDate,
                    endDate = s.EndDate,
                    createdAt = s.CreatedAt
                }),
                recentLifecycleEvents = lifecycleEvents.Select(e => new
                {
                    eventType = e.EventType.ToString(),
                    eventDate = e.EventDate,
                    description = e.Reason,
                    metadata = e.Metadata
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tenant {TenantId}", id);
            return StatusCode(500, new { message = "Error fetching tenant", error = ex.Message });
        }
    }

    /// <summary>
    /// Impersonate a tenant - Generate short-lived JWT token for SuperAdmin to view as tenant
    /// </summary>
    [HttpPost("{id}/impersonate")]
    public async Task<IActionResult> ImpersonateTenant(int id, [FromQuery] int? durationMinutes = 30)
    {
        try
        {
            var tenant = await _context.Tenants.FindAsync(id);
            if (tenant == null)
            {
                return NotFound(new { message = "Tenant not found" });
            }

            if (!tenant.IsActive || tenant.Status != TenantStatus.Active)
            {
                return BadRequest(new { message = "Cannot impersonate inactive or suspended tenant" });
            }

            var adminUser = await _userManager.GetUserAsync(User);
            if (adminUser == null)
            {
                return Unauthorized();
            }

            // Generate impersonation token
            var token = GenerateImpersonationToken(adminUser, tenant.Id.ToString(), durationMinutes ?? 30);

            // Log impersonation action
            await _adminAuditService.LogActionAsync(
                adminUserId: adminUser.Id,
                adminEmail: adminUser.Email ?? "unknown",
                actionType: "Tenant.Impersonate",
                httpMethod: "POST",
                endpoint: $"/api/admin/tenants/{id}/impersonate",
                statusCode: 200,
                isSuccess: true,
                targetTenantId: tenant.Id.ToString(),
                targetEntityType: "Tenant",
                targetEntityId: tenant.Id.ToString(),
                metadata: $"{{\"durationMinutes\": {durationMinutes ?? 30}, \"tenantName\": \"{tenant.Name}\"}}",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: HttpContext.Request.Headers["User-Agent"].ToString());

            _logger.LogInformation("SuperAdmin {AdminEmail} impersonated tenant {TenantId} ({TenantName})", 
                adminUser.Email, tenant.Id, tenant.Name);

            return Ok(new
            {
                message = $"You are now impersonating tenant: {tenant.Name}",
                impersonationToken = token,
                tenant = new
                {
                    id = tenant.Id,
                    name = tenant.Name,
                    domain = tenant.Domain
                },
                expiresAt = DateTime.UtcNow.AddMinutes(durationMinutes ?? 30),
                banner = $"You're viewing as Tenant: {tenant.Name} (Impersonation expires in {durationMinutes ?? 30} minutes)"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error impersonating tenant {TenantId}", id);
            return StatusCode(500, new { message = "Error impersonating tenant", error = ex.Message });
        }
    }

    /// <summary>
    /// Stop impersonation - Clear impersonation context
    /// </summary>
    [HttpPost("stop-impersonation")]
    public async Task<IActionResult> StopImpersonation()
    {
        try
        {
            var adminUser = await _userManager.GetUserAsync(User);
            if (adminUser == null)
            {
                return Unauthorized();
            }

            await _adminAuditService.LogActionAsync(
                adminUserId: adminUser.Id,
                adminEmail: adminUser.Email ?? "unknown",
                actionType: "Tenant.StopImpersonation",
                httpMethod: "POST",
                endpoint: "/api/admin/tenants/stop-impersonation",
                statusCode: 200,
                isSuccess: true,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: HttpContext.Request.Headers["User-Agent"].ToString());

            return Ok(new { message = "Impersonation stopped successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping impersonation");
            return StatusCode(500, new { message = "Error stopping impersonation", error = ex.Message });
        }
    }

    /// <summary>
    /// Suspend a tenant
    /// </summary>
    [HttpPost("{id}/suspend")]
    public async Task<IActionResult> SuspendTenant(int id, [FromBody] SuspendTenantRequest? request = null)
    {
        try
        {
            var tenant = await _context.Tenants.FindAsync(id);
            if (tenant == null)
            {
                return NotFound(new { message = "Tenant not found" });
            }

            tenant.Status = TenantStatus.Suspended;
            tenant.IsActive = false;
            tenant.IsSubscriptionActive = false;
            await _context.SaveChangesAsync();

            var adminUser = await _userManager.GetUserAsync(User);
            await _adminAuditService.LogActionAsync(
                adminUserId: adminUser?.Id ?? "unknown",
                adminEmail: adminUser?.Email ?? "unknown",
                actionType: "Tenant.Suspend",
                httpMethod: "POST",
                endpoint: $"/api/admin/tenants/{id}/suspend",
                statusCode: 200,
                isSuccess: true,
                targetTenantId: tenant.Id.ToString(),
                targetEntityType: "Tenant",
                targetEntityId: tenant.Id.ToString(),
                requestPayload: request != null ? System.Text.Json.JsonSerializer.Serialize(request) : null,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: HttpContext.Request.Headers["User-Agent"].ToString());

            return Ok(new { message = $"Tenant {tenant.Name} has been suspended", tenant = new { id = tenant.Id, name = tenant.Name, status = tenant.Status.ToString() } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suspending tenant {TenantId}", id);
            return StatusCode(500, new { message = "Error suspending tenant", error = ex.Message });
        }
    }

    /// <summary>
    /// Resume/Reactivate a suspended tenant
    /// </summary>
    [HttpPost("{id}/resume")]
    public async Task<IActionResult> ResumeTenant(int id)
    {
        try
        {
            var tenant = await _context.Tenants.FindAsync(id);
            if (tenant == null)
            {
                return NotFound(new { message = "Tenant not found" });
            }

            tenant.Status = TenantStatus.Active;
            tenant.IsActive = true;
            // Check if subscription is active
            var activeSubscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.TenantId == tenant.Id && s.Status == SubscriptionStatus.Active);
            tenant.IsSubscriptionActive = activeSubscription != null;
            
            await _context.SaveChangesAsync();

            var adminUser = await _userManager.GetUserAsync(User);
            await _adminAuditService.LogActionAsync(
                adminUserId: adminUser?.Id ?? "unknown",
                adminEmail: adminUser?.Email ?? "unknown",
                actionType: "Tenant.Resume",
                httpMethod: "POST",
                endpoint: $"/api/admin/tenants/{id}/resume",
                statusCode: 200,
                isSuccess: true,
                targetTenantId: tenant.Id.ToString(),
                targetEntityType: "Tenant",
                targetEntityId: tenant.Id.ToString(),
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: HttpContext.Request.Headers["User-Agent"].ToString());

            return Ok(new { message = $"Tenant {tenant.Name} has been resumed", tenant = new { id = tenant.Id, name = tenant.Name, status = tenant.Status.ToString() } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming tenant {TenantId}", id);
            return StatusCode(500, new { message = "Error resuming tenant", error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a tenant (soft delete - marks as deleted, data retained)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTenant(int id, [FromQuery] bool hardDelete = false)
    {
        try
        {
            var tenant = await _context.Tenants.FindAsync(id);
            if (tenant == null)
            {
                return NotFound(new { message = "Tenant not found" });
            }

            var adminUser = await _userManager.GetUserAsync(User);
            
            // Convert tenant ID to string for matching with TenantId in RolePermissions and Modules
            var tenantIdString = tenant.Id.ToString();

            if (hardDelete)
            {
                // Hard delete - delete related data first, then remove tenant
                
                // Delete RolePermissions for this tenant
                var rolePermissions = await _context.RolePermissions
                    .Where(rp => rp.TenantId == tenantIdString)
                    .ToListAsync();
                if (rolePermissions.Any())
                {
                    _context.RolePermissions.RemoveRange(rolePermissions);
                    _logger.LogInformation("Deleting {Count} RolePermissions for tenant {TenantId}", rolePermissions.Count, tenant.Id);
                }
                
                // Delete Modules for this tenant
                var modules = await _context.Modules
                    .Where(m => m.TenantId == tenantIdString)
                    .ToListAsync();
                if (modules.Any())
                {
                    _context.Modules.RemoveRange(modules);
                    _logger.LogInformation("Deleting {Count} Modules for tenant {TenantId}", modules.Count, tenant.Id);
                }
                
                // Now delete the tenant itself
                _context.Tenants.Remove(tenant);
                await _context.SaveChangesAsync();

                await _adminAuditService.LogActionAsync(
                    adminUserId: adminUser?.Id ?? "unknown",
                    adminEmail: adminUser?.Email ?? "unknown",
                    actionType: "Tenant.HardDelete",
                    httpMethod: "DELETE",
                    endpoint: $"/api/admin/tenants/{id}?hardDelete=true",
                    statusCode: 200,
                    isSuccess: true,
                    targetTenantId: tenant.Id.ToString(),
                    targetEntityType: "Tenant",
                    targetEntityId: tenant.Id.ToString(),
                    metadata: "{\"hardDelete\": true}",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: HttpContext.Request.Headers["User-Agent"].ToString());

                return Ok(new { message = $"Tenant {tenant.Name} has been permanently deleted", hardDelete = true });
            }
            else
            {
                // Soft delete - mark as deleted, but also clean up RolePermissions and Modules
                
                // Delete RolePermissions for this tenant
                var rolePermissions = await _context.RolePermissions
                    .Where(rp => rp.TenantId == tenantIdString)
                    .ToListAsync();
                if (rolePermissions.Any())
                {
                    _context.RolePermissions.RemoveRange(rolePermissions);
                    _logger.LogInformation("Deleting {Count} RolePermissions for tenant {TenantId}", rolePermissions.Count, tenant.Id);
                }
                
                // Delete Modules for this tenant
                var modules = await _context.Modules
                    .Where(m => m.TenantId == tenantIdString)
                    .ToListAsync();
                if (modules.Any())
                {
                    _context.Modules.RemoveRange(modules);
                    _logger.LogInformation("Deleting {Count} Modules for tenant {TenantId}", modules.Count, tenant.Id);
                }
                
                // Mark tenant as deleted
                tenant.Status = TenantStatus.Deleted;
                tenant.IsActive = false;
                tenant.IsSubscriptionActive = false;
                await _context.SaveChangesAsync();

                await _adminAuditService.LogActionAsync(
                    adminUserId: adminUser?.Id ?? "unknown",
                    adminEmail: adminUser?.Email ?? "unknown",
                    actionType: "Tenant.Delete",
                    httpMethod: "DELETE",
                    endpoint: $"/api/admin/tenants/{id}",
                    statusCode: 200,
                    isSuccess: true,
                    targetTenantId: tenant.Id.ToString(),
                    targetEntityType: "Tenant",
                    targetEntityId: tenant.Id.ToString(),
                    metadata: "{\"hardDelete\": false}",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: HttpContext.Request.Headers["User-Agent"].ToString());

                return Ok(new { message = $"Tenant {tenant.Name} has been deleted (soft delete)", hardDelete = false });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tenant {TenantId}", id);
            return StatusCode(500, new { message = "Error deleting tenant", error = ex.Message });
        }
    }

    /// <summary>
    /// Generate impersonation JWT token
    /// </summary>
    private string GenerateImpersonationToken(User adminUser, string tenantId, int durationMinutes)
    {
        var roles = _userManager.GetRolesAsync(adminUser).Result;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, adminUser.Id),
            new(ClaimTypes.Email, adminUser.Email ?? string.Empty),
            new(ClaimTypes.Name, $"{adminUser.FirstName} {adminUser.LastName}"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            // Impersonation claims
            new("TenantId", tenantId),
            new("tenant", tenantId),
            new("IsImpersonating", "true"),
            new("OriginalUserId", adminUser.Id),
            new("OriginalEmail", adminUser.Email ?? string.Empty)
        };

        // Keep SuperAdmin role but also add impersonation context
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(durationMinutes);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Get password setup link for tenant admin user
    /// If tenant is in Provisioning status, will wait/retry for provisioning to complete
    /// </summary>
    [HttpGet("{id}/admin-setup-link")]
    public async Task<IActionResult> GetAdminSetupLink(int id)
    {
        try
        {
            var tenant = await _context.Tenants.FindAsync(id);
            if (tenant == null)
            {
                return NotFound(new { message = "Tenant not found" });
            }

            if (string.IsNullOrWhiteSpace(tenant.AdminEmail))
            {
                return BadRequest(new { message = "Tenant does not have an admin email" });
            }

            // If tenant is in Provisioning status, try to complete provisioning first
            if (tenant.Status == TenantStatus.Provisioning && _provisioningService != null)
            {
                _logger.LogInformation("Tenant {TenantId} is in Provisioning status. Attempting to complete provisioning...", id);
                
                try
                {
                    var (success, errorMessage, result) = await _provisioningService.ProvisionTenantAsync(
                        tenant.Id,
                        tenant.AdminEmail ?? string.Empty,
                        tenant.AdminFirstName ?? "Admin",
                        tenant.AdminLastName ?? tenant.Name ?? "User",
                        subscriptionPlanId: null,
                        startTrial: false);

                    if (success)
                    {
                        _logger.LogInformation("Provisioning completed for tenant {TenantId}. Admin user should now exist.", id);
                        // Refresh tenant to get updated status
                        await _context.Entry(tenant).ReloadAsync();
                    }
                    else
                    {
                        _logger.LogWarning("Provisioning failed for tenant {TenantId}: {ErrorMessage}", id, errorMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error during provisioning for tenant {TenantId}. Will continue to check for admin user.", id);
                }
            }

            // Retry logic: Wait for admin user to be created (max 10 seconds)
            User? adminUser = null;
            int maxRetries = 10;
            int retryDelayMs = 1000; // 1 second

            for (int i = 0; i < maxRetries; i++)
            {
                adminUser = await _userManager.FindByEmailAsync(tenant.AdminEmail);
                if (adminUser != null)
                {
                    break;
                }

                if (i < maxRetries - 1)
                {
                    _logger.LogInformation("Admin user not found for tenant {TenantId}, retrying in {DelayMs}ms (attempt {Attempt}/{MaxRetries})...", 
                        id, retryDelayMs, i + 1, maxRetries);
                    await Task.Delay(retryDelayMs);
                }
            }

            if (adminUser == null)
            {
                // If still not found, check if we can trigger provisioning
                if (tenant.Status == TenantStatus.Provisioning)
                {
                    return StatusCode(503, new 
                    { 
                        message = "Admin user not found. Provisioning is still in progress. Please wait a moment and try again.",
                        tenantStatus = tenant.Status.ToString(),
                        suggestion = "Wait a few seconds and try again, or trigger provisioning manually via POST /api/tenantprovisioning/{tenantId}"
                    });
                }
                else
                {
                    return NotFound(new 
                    { 
                        message = "Admin user not found. The tenant may not be fully provisioned yet.",
                        tenantStatus = tenant.Status.ToString(),
                        suggestion = "Try triggering provisioning manually via POST /api/tenantprovisioning/{tenantId}"
                    });
                }
            }

            // Generate password reset token (this is the token used for password setup)
            var passwordToken = await _userManager.GeneratePasswordResetTokenAsync(adminUser);
            
            // Build the setup link
            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "http://localhost:5173";
            var setupLink = $"{baseUrl}/setup-password?token={Uri.EscapeDataString(passwordToken)}&userId={adminUser.Id}";

            // Log action
            var adminUserLogged = await _userManager.GetUserAsync(User);
            await _adminAuditService.LogActionAsync(
                adminUserId: adminUserLogged?.Id ?? "unknown",
                adminEmail: adminUserLogged?.Email ?? "unknown",
                actionType: "Tenant.GetAdminSetupLink",
                httpMethod: "GET",
                endpoint: $"/api/admin/tenants/{id}/admin-setup-link",
                statusCode: 200,
                isSuccess: true,
                targetTenantId: tenant.Id.ToString(),
                targetEntityType: "Tenant",
                targetEntityId: tenant.Id.ToString(),
                metadata: $"{{\"adminEmail\": \"{tenant.AdminEmail}\", \"adminUserId\": \"{adminUser.Id}\"}}",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: HttpContext.Request.Headers["User-Agent"].ToString());

            _logger.LogInformation("Generated admin setup link for tenant {TenantId} ({TenantName}) - Admin: {AdminEmail}", 
                tenant.Id, tenant.Name, tenant.AdminEmail);

            return Ok(new
            {
                tenantId = tenant.Id,
                tenantName = tenant.Name,
                adminEmail = tenant.AdminEmail,
                adminUserId = adminUser.Id,
                setupLink = setupLink,
                token = passwordToken, // Include token for development purposes
                expiresIn = "7 days", // Password reset tokens typically expire in 7 days
                message = "Use this link to set up the admin password. This link expires in 7 days."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating admin setup link for tenant {TenantId}", id);
            return StatusCode(500, new { message = "Error generating setup link", error = ex.Message });
        }
    }

    public record SuspendTenantRequest(string? Reason = null);
}

