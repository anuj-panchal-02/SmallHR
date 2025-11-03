using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmallHR.API.Base;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using SmallHR.Core.DTOs.Tenant;
using SmallHR.Infrastructure.Data;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.DependencyInjection;
using SmallHR.API.Services;
using SmallHR.API.Authorization;

namespace SmallHR.API.Controllers;

/// <summary>
/// Tenant Administration Console for SuperAdmin
/// Provides comprehensive tenant management, impersonation, metrics, and plan adjustments
/// </summary>
[ApiController]
[Route("api/admin/tenants")]
[AuthorizeSuperAdmin]
public class TenantAdminController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IAdminAuditService _adminAuditService;
    private readonly IConfiguration _configuration;
    private readonly ITenantProvisioningService? _provisioningService;
    private readonly ISortStrategyFactory<Tenant> _sortStrategyFactory;

    public TenantAdminController(
        ApplicationDbContext context,
        UserManager<User> userManager,
        IAdminAuditService adminAuditService,
        IConfiguration configuration,
        ILogger<TenantAdminController> logger,
        IServiceProvider serviceProvider,
        ISortStrategyFactory<Tenant> sortStrategyFactory) : base(logger)
    {
        _context = context;
        _userManager = userManager;
        _adminAuditService = adminAuditService;
        _configuration = configuration;
        // Get provisioning service if available (optional dependency)
        _provisioningService = serviceProvider.GetService<ITenantProvisioningService>();
        _sortStrategyFactory = sortStrategyFactory;
    }

    /// <summary>
    /// Get all tenants with comprehensive details (status, plan, usage, users)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<TenantListResponseDto>> GetAllTenants(
        [FromQuery] string? search = null,
        [FromQuery] TenantStatus? status = null,
        [FromQuery] SubscriptionStatus? subscriptionStatus = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] string? sortOrder = "desc")
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                Logger.LogInformation("Fetching tenants - Starting query. User role: {Role}", 
                    User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "Unknown");
                
                // Use IgnoreQueryFilters to ensure SuperAdmin sees all tenants
                // Note: Tenant entity doesn't have a query filter, but using IgnoreQueryFilters for safety
                var query = _context.Tenants.IgnoreQueryFilters().AsQueryable();
                
                // Log query for debugging - try multiple query approaches to verify data exists
                var totalTenantCountDirect = await _context.Tenants.CountAsync();
                var totalTenantCountIgnored = await _context.Tenants.IgnoreQueryFilters().CountAsync();
                
                Logger.LogInformation("Fetching tenants - Direct count: {DirectCount}, IgnoreQueryFilters count: {IgnoredCount}", 
                    totalTenantCountDirect, totalTenantCountIgnored);
                
                // Try to get raw tenant list to verify data exists
                var rawTenantsDirect = await _context.Tenants.Take(5).ToListAsync();
                var rawTenantsIgnored = await _context.Tenants.IgnoreQueryFilters().Take(5).ToListAsync();
                
                Logger.LogInformation("Fetching tenants - Direct sample tenant IDs: {Ids}", 
                    string.Join(", ", rawTenantsDirect.Select(t => t.Id)));
                Logger.LogInformation("Fetching tenants - IgnoreQueryFilters sample tenant IDs: {Ids}", 
                    string.Join(", ", rawTenantsIgnored.Select(t => t.Id)));
                
                if (totalTenantCountIgnored == 0)
                {
                    Logger.LogWarning("No tenants found in database. SuperAdmin may need to create tenants.");
                }
                
                // Use the count that found data
                var totalTenantCount = totalTenantCountIgnored > 0 ? totalTenantCountIgnored : totalTenantCountDirect;

            // Search filter
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(t => 
                    t.Name.Contains(search) || 
                    (t.Domain != null && t.Domain.Contains(search)) ||
                    (t.AdminEmail != null && t.AdminEmail.Contains(search)));
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
                    .IgnoreQueryFilters()
                    .Where(s => s.Status == subscriptionStatus.Value)
                    .Select(s => s.TenantId)
                    .ToListAsync();
                query = query.Where(t => tenantIds.Contains(t.Id));
            }

            // Sorting - use strategy pattern (follows Open/Closed Principle)
            var sortDirection = sortOrder?.ToLower() == "asc" ? "asc" : "desc";
            var strategy = _sortStrategyFactory.GetStrategy(sortBy ?? "createdat");
            if (strategy == null)
            {
                strategy = _sortStrategyFactory.GetDefaultStrategy();
            }
            query = strategy.ApplySort(query, sortDirection);

            var totalCount = await query.CountAsync();
            Logger.LogInformation("Fetching tenants - After filters: {Count} tenants match criteria", totalCount);
            
            // If totalCount is 0 but we know data exists, log a warning
            if (totalCount == 0 && totalTenantCount > 0)
            {
                Logger.LogWarning("Tenants exist ({TotalTenantCount}) but filters returned 0. Search: {Search}, Status: {Status}, SubscriptionStatus: {SubscriptionStatus}", 
                    totalTenantCount, search ?? "none", status?.ToString() ?? "none", subscriptionStatus?.ToString() ?? "none");
            }
            
            var tenants = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            Logger.LogInformation("Fetching tenants - Retrieved {Count} tenants for page {Page} (totalCount: {TotalCount})", 
                tenants.Count, pageNumber, totalCount);
            
            if (tenants.Count == 0 && totalCount > 0)
            {
                Logger.LogWarning("Tenants exist in DB ({TotalCount}) but query returned 0 for page {Page}, pageSize {PageSize}", 
                    totalCount, pageNumber, pageSize);
            }
            
            if (tenants.Count == 0 && totalTenantCount > 0 && totalCount == 0)
            {
                Logger.LogError("CRITICAL: Tenants exist ({TotalTenantCount}) but query with filters returned 0. This indicates a filtering issue.", 
                    totalTenantCount);
            }

            // Get user counts and usage metrics for each tenant
            // Use IgnoreQueryFilters for related entities to ensure SuperAdmin sees all data
            // OPTIMIZED: Batch load all related data to avoid N+1 queries and timeouts
            var tenantDetails = new List<TenantListDto>();
            Logger.LogInformation("Fetching tenants - Processing {Count} tenants for details", tenants.Count);
            
            if (tenants.Count > 0)
            {
                var tenantIds = tenants.Select(t => t.Id).ToList();
                var tenantIdStrings = tenantIds.Select(id => id.ToString()).ToList();
                
                // Batch load user counts per tenant (single query)
                // TenantId is stored as string, so compare as strings
                var userCountsByTenant = await _context.Users
                    .IgnoreQueryFilters()
                    .Where(u => u.TenantId != null && tenantIdStrings.Contains(u.TenantId))
                    .GroupBy(u => u.TenantId)
                    .Select(g => new { TenantIdStr = g.Key!, Count = g.Count() })
                    .ToListAsync();
                
                var userCountsDict = userCountsByTenant
                    .Where(x => int.TryParse(x.TenantIdStr, out _))
                    .ToDictionary(x => int.Parse(x.TenantIdStr), x => x.Count);
                
                // Batch load employee counts per tenant (single query)
                var employeeCountsByTenant = await _context.Employees
                    .IgnoreQueryFilters()
                    .Where(e => tenantIdStrings.Contains(e.TenantId))
                    .GroupBy(e => e.TenantId)
                    .Select(g => new { TenantIdStr = g.Key, Count = g.Count() })
                    .ToListAsync();
                
                var employeeCountsDict = employeeCountsByTenant
                    .Where(x => int.TryParse(x.TenantIdStr, out _))
                    .ToDictionary(x => int.Parse(x.TenantIdStr), x => x.Count);
                
                // Batch load usage metrics for all tenants (single query)
                var usageMetricsByTenant = await _context.TenantUsageMetrics
                    .IgnoreQueryFilters()
                    .Where(m => tenantIds.Contains(m.TenantId))
                    .ToDictionaryAsync(m => m.TenantId);
                
                // Batch load active subscriptions for all tenants (single query with Include)
                var activeSubscriptionsByTenant = await _context.Subscriptions
                    .IgnoreQueryFilters()
                    .Include(s => s.Plan)
                    .Where(s => tenantIds.Contains(s.TenantId) && s.Status == SubscriptionStatus.Active)
                    .ToDictionaryAsync(s => s.TenantId);
                
                Logger.LogInformation("Fetching tenants - Batch loaded data: Users for {UserTenantCount} tenants, Employees for {EmployeeTenantCount} tenants, UsageMetrics for {MetricsTenantCount} tenants, Subscriptions for {SubscriptionTenantCount} tenants",
                    userCountsDict.Count, employeeCountsDict.Count, usageMetricsByTenant.Count, activeSubscriptionsByTenant.Count);
                
                // Build tenant details using pre-loaded data (no additional queries)
                foreach (var tenant in tenants)
                {
                    // Get counts with default value of 0 if not found
                    var userCount = userCountsDict.TryGetValue(tenant.Id, out var uc) ? uc : 0;
                    var employeeCount = employeeCountsDict.TryGetValue(tenant.Id, out var ec) ? ec : 0;
                    usageMetricsByTenant.TryGetValue(tenant.Id, out var usageMetrics);
                    activeSubscriptionsByTenant.TryGetValue(tenant.Id, out var activeSubscription);
                    
                    var subscription = activeSubscription != null
                        ? new TenantSubscriptionDto
                        {
                            PlanName = activeSubscription.Plan?.Name ?? "Unknown",
                            Status = activeSubscription.Status.ToString(),
                            CurrentPeriodEnd = activeSubscription.EndDate,
                            Price = activeSubscription.Price,
                            BillingPeriod = activeSubscription.BillingPeriod.ToString()
                        }
                        : null;

                    tenantDetails.Add(new TenantListDto
                    {
                        Id = tenant.Id,
                        Name = tenant.Name,
                        Domain = tenant.Domain,
                        Status = tenant.Status.ToString(),
                        IsActive = tenant.IsActive,
                        IsSubscriptionActive = tenant.IsSubscriptionActive,
                        AdminEmail = tenant.AdminEmail,
                        AdminFirstName = tenant.AdminFirstName,
                        AdminLastName = tenant.AdminLastName,
                        CreatedAt = tenant.CreatedAt,
                        UpdatedAt = tenant.UpdatedAt ?? tenant.CreatedAt,
                        Subscription = subscription,
                        UserCount = userCount,
                        EmployeeCount = employeeCount,
                        UsageMetrics = usageMetrics != null ? new TenantUsageMetricsDto
                        {
                            ApiRequestCount = usageMetrics.ApiRequestCount,
                            LastUpdated = usageMetrics.UpdatedAt ?? DateTime.UtcNow
                        } : null
                    });
                }
            }

                var response = new TenantListResponseDto
                {
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    Tenants = tenantDetails
                };
                
                Logger.LogInformation("Fetching tenants - Returning response with {TenantCount} tenants, totalCount: {TotalCount}", 
                    tenantDetails.Count, totalCount);
                
                // Log first tenant details for debugging
                if (tenantDetails.Count > 0)
                {
                    var firstTenant = tenantDetails[0];
                    Logger.LogInformation("Fetching tenants - First tenant ID: {TenantId}, Name: {TenantName}", 
                        firstTenant.Id, firstTenant.Name);
                    
                    // Serialize to JSON to verify structure
                    var json = System.Text.Json.JsonSerializer.Serialize(response, new System.Text.Json.JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    });
                    Logger.LogInformation("Fetching tenants - Full serialized response: {Json}", json);
                    Logger.LogInformation("Fetching tenants - Response type: {Type}", response.GetType().FullName);
                    Logger.LogInformation("Fetching tenants - Response has 'Tenants' property: {HasTenants}, Count: {Count}", 
                        response.Tenants != null, response.Tenants?.Count ?? 0);
                }
                else
                {
                    Logger.LogWarning("Fetching tenants - Response has 0 tenants but totalCount is {TotalCount}", totalCount);
                }
                
                // Return the response directly - HandleServiceResultAsync will wrap it in Ok()
                return response;
            },
            "fetching tenants"
        );
    }

    /// <summary>
    /// Get tenant details by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<TenantDetailResponseDto>> GetTenantById(int id)
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                // Use IgnoreQueryFilters to ensure SuperAdmin can access tenant data
                var tenant = await _context.Tenants
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (tenant == null)
                {
                    throw new KeyNotFoundException("Tenant not found");
                }

                var tenantIdString = tenant.Id.ToString();
                
                // Use IgnoreQueryFilters for related data to ensure SuperAdmin sees all data
                var userCount = await _context.Users
                    .IgnoreQueryFilters()
                    .CountAsync(u => u.TenantId == tenantIdString);
                    
                var employeeCount = await _context.Employees
                    .IgnoreQueryFilters()
                    .CountAsync(e => e.TenantId == tenantIdString);
                    
                var usageMetrics = await _context.TenantUsageMetrics
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(m => m.TenantId == tenant.Id);
                
                var subscriptionHistory = await _context.Subscriptions
                    .IgnoreQueryFilters()
                    .Include(s => s.Plan)
                    .Where(s => s.TenantId == tenant.Id)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();

                var lifecycleEvents = await _context.TenantLifecycleEvents
                    .IgnoreQueryFilters()
                    .Where(e => e.TenantId == tenant.Id)
                    .OrderByDescending(e => e.EventDate)
                    .Take(10)
                    .ToListAsync();

                Logger.LogInformation("Fetching tenant {TenantId} ({TenantName}) - UserCount: {UserCount}, EmployeeCount: {EmployeeCount}, SubscriptionCount: {SubscriptionCount}, LifecycleEventCount: {LifecycleEventCount}",
                    tenant.Id, tenant.Name, userCount, employeeCount, subscriptionHistory.Count, lifecycleEvents.Count);

                var response = new TenantDetailResponseDto
                {
                    Id = tenant.Id,
                    Name = tenant.Name,
                    Domain = tenant.Domain,
                    Status = tenant.Status.ToString(),
                    IsActive = tenant.IsActive,
                    IsSubscriptionActive = tenant.IsSubscriptionActive,
                    AdminEmail = tenant.AdminEmail,
                    AdminFirstName = tenant.AdminFirstName,
                    AdminLastName = tenant.AdminLastName,
                    CreatedAt = tenant.CreatedAt,
                    UpdatedAt = tenant.UpdatedAt ?? tenant.CreatedAt,
                    UserCount = userCount,
                    EmployeeCount = employeeCount,
                    UsageMetrics = usageMetrics != null ? new TenantUsageMetricsDetailDto
                    {
                        ApiRequestCount = usageMetrics.ApiRequestCount,
                        EmployeeCount = employeeCount,
                        UserCount = userCount,
                        LastUpdated = usageMetrics.UpdatedAt ?? DateTime.UtcNow
                    } : null,
                    Subscriptions = subscriptionHistory.Select(s => new TenantSubscriptionHistoryDto
                    {
                        Id = s.Id,
                        PlanName = s.Plan?.Name ?? "Unknown",
                        Status = s.Status.ToString(),
                        Price = s.Price,
                        BillingPeriod = s.BillingPeriod.ToString(),
                        StartDate = s.StartDate,
                        EndDate = s.EndDate,
                        CreatedAt = s.CreatedAt
                    }).ToList(),
                    RecentLifecycleEvents = lifecycleEvents.Select(e => new TenantLifecycleEventDto
                    {
                        EventType = e.EventType.ToString(),
                        EventDate = e.EventDate,
                        Description = e.Reason,
                        Metadata = e.Metadata ?? (object?)null
                    }).ToList()
                };
                
                Logger.LogInformation("Fetching tenant - Returning response with {SubscriptionCount} subscriptions, {LifecycleEventCount} lifecycle events",
                    response.Subscriptions.Count, response.RecentLifecycleEvents.Count);
                
                return response;
            },
            $"fetching tenant with ID {id}"
        );
    }

    /// <summary>
    /// Impersonate a tenant - Generate short-lived JWT token for SuperAdmin to view as tenant
    /// </summary>
    [HttpPost("{id}/impersonate")]
    public async Task<ActionResult<ImpersonateResponseDto>> ImpersonateTenant(int id, [FromQuery] int? durationMinutes = 30)
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                var tenant = await _context.Tenants.FindAsync(id);
                if (tenant == null)
                {
                    throw new KeyNotFoundException("Tenant not found");
                }

                if (!tenant.IsActive || tenant.Status != TenantStatus.Active)
                {
                    throw new InvalidOperationException("Cannot impersonate inactive or suspended tenant");
                }

                var adminUser = await _userManager.GetUserAsync(User);
                if (adminUser == null)
                {
                    throw new UnauthorizedAccessException("User not authenticated");
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

                Logger.LogInformation("SuperAdmin {AdminEmail} impersonated tenant {TenantId} ({TenantName})", 
                    adminUser.Email, tenant.Id, tenant.Name);

                var response = new ImpersonateResponseDto
                {
                    Message = $"You are now impersonating tenant: {tenant.Name}",
                    ImpersonationToken = token,
                    Tenant = new ImpersonateTenantDto
                    {
                        Id = tenant.Id,
                        Name = tenant.Name,
                        Domain = tenant.Domain
                    },
                    ExpiresAt = DateTime.UtcNow.AddMinutes(durationMinutes ?? 30),
                    Banner = $"You're viewing as Tenant: {tenant.Name} (Impersonation expires in {durationMinutes ?? 30} minutes)"
                };
                
                Logger.LogInformation("Impersonate response - Token: {TokenLength} chars, TenantId: {TenantId}, ExpiresAt: {ExpiresAt}",
                    token.Length, response.Tenant.Id, response.ExpiresAt);
                
                return response;
            },
            "impersonating tenant"
        );
    }

    /// <summary>
    /// Stop impersonation - Clear impersonation context
    /// </summary>
    [HttpPost("stop-impersonation")]
    public async Task<ActionResult<object>> StopImpersonation()
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                var adminUser = await _userManager.GetUserAsync(User);
                if (adminUser == null)
                {
                    throw new UnauthorizedAccessException("User not authenticated");
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

                return new { message = "Impersonation stopped successfully" };
            },
            "stopping impersonation"
        );
    }

    /// <summary>
    /// Suspend a tenant
    /// </summary>
    [HttpPost("{id}/suspend")]
    public async Task<ActionResult<object>> SuspendTenant(int id, [FromBody] SuspendTenantRequest? request = null)
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                var tenant = await _context.Tenants.FindAsync(id);
                if (tenant == null)
                {
                    throw new KeyNotFoundException("Tenant not found");
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

                return new { message = $"Tenant {tenant.Name} has been suspended", tenant = new { id = tenant.Id, name = tenant.Name, status = tenant.Status.ToString() } };
            },
            "suspending tenant"
        );
    }

    /// <summary>
    /// Resume/Reactivate a suspended tenant
    /// </summary>
    [HttpPost("{id}/resume")]
    public async Task<ActionResult<object>> ResumeTenant(int id)
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                var tenant = await _context.Tenants.FindAsync(id);
                if (tenant == null)
                {
                    throw new KeyNotFoundException("Tenant not found");
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

                return new { message = $"Tenant {tenant.Name} has been resumed", tenant = new { id = tenant.Id, name = tenant.Name, status = tenant.Status.ToString() } };
            },
            "resuming tenant"
        );
    }

    /// <summary>
    /// Delete a tenant (soft delete - marks as deleted, data retained)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult<object>> DeleteTenant(int id, [FromQuery] bool hardDelete = false)
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                var tenant = await _context.Tenants.FindAsync(id);
                if (tenant == null)
                {
                    throw new KeyNotFoundException("Tenant not found");
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
                        Logger.LogInformation("Deleting {Count} RolePermissions for tenant {TenantId}", rolePermissions.Count, tenant.Id);
                    }
                    
                    // Delete Modules for this tenant
                    var modules = await _context.Modules
                        .Where(m => m.TenantId == tenantIdString)
                        .ToListAsync();
                    if (modules.Any())
                    {
                        _context.Modules.RemoveRange(modules);
                        Logger.LogInformation("Deleting {Count} Modules for tenant {TenantId}", modules.Count, tenant.Id);
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

                    return new { message = $"Tenant {tenant.Name} has been permanently deleted", hardDelete = true };
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
                        Logger.LogInformation("Deleting {Count} RolePermissions for tenant {TenantId}", rolePermissions.Count, tenant.Id);
                    }
                    
                    // Delete Modules for this tenant
                    var modules = await _context.Modules
                        .Where(m => m.TenantId == tenantIdString)
                        .ToListAsync();
                    if (modules.Any())
                    {
                        _context.Modules.RemoveRange(modules);
                        Logger.LogInformation("Deleting {Count} Modules for tenant {TenantId}", modules.Count, tenant.Id);
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

                    return new { message = $"Tenant {tenant.Name} has been deleted (soft delete)", hardDelete = false };
                }
            },
            "deleting tenant"
        );
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
    public async Task<ActionResult<object>> GetAdminSetupLink(int id)
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                var tenant = await _context.Tenants.FindAsync(id);
                if (tenant == null)
                {
                    throw new KeyNotFoundException("Tenant not found");
                }

                if (string.IsNullOrWhiteSpace(tenant.AdminEmail))
                {
                    throw new ArgumentException("Tenant does not have an admin email");
                }

                // If tenant is in Provisioning status, try to complete provisioning first
                if (tenant.Status == TenantStatus.Provisioning && _provisioningService != null)
                {
                    Logger.LogInformation("Tenant {TenantId} is in Provisioning status. Attempting to complete provisioning...", id);
                    
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
                            Logger.LogInformation("Provisioning completed for tenant {TenantId}. Admin user should now exist.", id);
                            // Refresh tenant to get updated status
                            await _context.Entry(tenant).ReloadAsync();
                        }
                        else
                        {
                            Logger.LogWarning("Provisioning failed for tenant {TenantId}: {ErrorMessage}", id, errorMessage);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(ex, "Error during provisioning for tenant {TenantId}. Will continue to check for admin user.", id);
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
                        Logger.LogInformation("Admin user not found for tenant {TenantId}, retrying in {DelayMs}ms (attempt {Attempt}/{MaxRetries})...", 
                            id, retryDelayMs, i + 1, maxRetries);
                        await Task.Delay(retryDelayMs);
                    }
                }

                if (adminUser == null)
                {
                    // If still not found, check if we can trigger provisioning
                    if (tenant.Status == TenantStatus.Provisioning)
                    {
                        throw new InvalidOperationException("Admin user not found. Provisioning is still in progress. Please wait a moment and try again.");
                    }
                    else
                    {
                        throw new KeyNotFoundException("Admin user not found. The tenant may not be fully provisioned yet.");
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

                Logger.LogInformation("Generated admin setup link for tenant {TenantId} ({TenantName}) - Admin: {AdminEmail}", 
                    tenant.Id, tenant.Name, tenant.AdminEmail);

                return new
                {
                    tenantId = tenant.Id,
                    tenantName = tenant.Name,
                    adminEmail = tenant.AdminEmail,
                    adminUserId = adminUser.Id,
                    setupLink = setupLink,
                    token = passwordToken, // Include token for development purposes
                    expiresIn = "7 days", // Password reset tokens typically expire in 7 days
                    message = "Use this link to set up the admin password. This link expires in 7 days."
                };
            },
            "generating admin setup link"
        );
    }

    public record SuspendTenantRequest(string? Reason = null);
}

