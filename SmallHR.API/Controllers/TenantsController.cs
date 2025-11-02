using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmallHR.Core.Entities;
using SmallHR.Infrastructure.Data;

namespace SmallHR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class TenantsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<TenantsController> _logger;
    public TenantsController(ApplicationDbContext db, ILogger<TenantsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var tenants = await _db.Tenants.AsNoTracking().OrderBy(t => t.Name).ToListAsync();
            return Ok(tenants);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tenants: {Message}", ex.Message);
            _logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
            return StatusCode(500, new { message = "An error occurred while fetching tenants", detail = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        if (tenant == null) return NotFound();
        return Ok(tenant);
    }

    public record CreateTenantRequest(
        string Name, 
        string? Domain, 
        string AdminEmail, 
        string? AdminFirstName = null, 
        string? AdminLastName = null,
        string? IdempotencyToken = null,
        bool IsActive = true, 
        string SubscriptionPlan = "Free", 
        int MaxEmployees = 10);
    public record UpdateTenantRequest(string Name, string? Domain, bool IsActive);
    public record UpdateSubscriptionRequest(string SubscriptionPlan, int MaxEmployees);
    public record UpdateDomainRequest(string? Domain);
    public record TenantStatusResponse(
        int Id,
        string Name,
        TenantStatus Status,
        DateTime? ProvisionedAt,
        string? FailureReason);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTenantRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) 
            return BadRequest(new { message = "Name is required" });
        
        if (string.IsNullOrWhiteSpace(req.AdminEmail))
            return BadRequest(new { message = "AdminEmail is required" });

        if (!string.IsNullOrWhiteSpace(req.Domain))
        {
            var exists = await _db.Tenants.AnyAsync(t => t.Domain == req.Domain);
            if (exists) return Conflict(new { message = "Domain already mapped to another tenant" });
        }

        // Idempotency check (optional)
        if (!string.IsNullOrWhiteSpace(req.IdempotencyToken))
        {
            var existing = await _db.Tenants
                .FirstOrDefaultAsync(t => t.IdempotencyToken == req.IdempotencyToken);
            if (existing != null)
            {
                // Return the existing tenant with status
                return Accepted(new Uri($"/api/tenants/{existing.Id}/status", UriKind.Relative), 
                    new { id = existing.Id, status = existing.Status.ToString() });
            }
        }

        var now = DateTime.UtcNow;
        var tenant = new Tenant
        {
            Name = req.Name.Trim(),
            Domain = string.IsNullOrWhiteSpace(req.Domain) ? null : req.Domain!.Trim().ToLowerInvariant(),
            IsActive = req.IsActive,
            SubscriptionPlan = req.SubscriptionPlan,
            MaxEmployees = req.MaxEmployees,
            SubscriptionStartDate = now,
            SubscriptionEndDate = now.AddYears(1), // Default 1 year subscription
            IsSubscriptionActive = true,
            CreatedAt = now,
            IsDeleted = false,
            Status = TenantStatus.Provisioning,
            AdminEmail = req.AdminEmail.Trim().ToLowerInvariant(),
            AdminFirstName = req.AdminFirstName?.Trim(),
            AdminLastName = req.AdminLastName?.Trim(),
            IdempotencyToken = req.IdempotencyToken
        };

        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync();
        
        // Return 202 Accepted with location header pointing to status endpoint
        var statusUrl = Url.Action(nameof(GetStatus), new { id = tenant.Id }) 
            ?? $"/api/tenants/{tenant.Id}/status";
        
        return Accepted(new Uri(statusUrl, UriKind.Relative), new 
        { 
            id = tenant.Id, 
            status = tenant.Status.ToString(),
            statusUrl = statusUrl
        });
    }

    /// <summary>
    /// Get tenant provisioning status. This endpoint is publicly accessible for monitoring provisioning progress.
    /// </summary>
    [HttpGet("{id}/status")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStatus(int id)
    {
        try
        {
            // Ensure we're using the default tenant context for master/registry queries
            // The TenantResolutionMiddleware should set this, but we'll use AsNoTracking for safety
            var tenant = await _db.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);
            
            if (tenant == null) 
                return NotFound(new { message = "Tenant not found", id });

            var response = new TenantStatusResponse(
                tenant.Id,
                tenant.Name,
                tenant.Status,
                tenant.ProvisionedAt,
                tenant.FailureReason);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching tenant status for tenant {TenantId}: {Message}", id, ex.Message);
            return StatusCode(500, new { message = "An error occurred while fetching tenant status", detail = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTenantRequest req)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        if (tenant == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(req.Domain))
        {
            var exists = await _db.Tenants.AnyAsync(t => t.Id != id && t.Domain == req.Domain);
            if (exists) return Conflict(new { message = "Domain already mapped to another tenant" });
        }

        tenant.Name = req.Name?.Trim() ?? tenant.Name;
        tenant.Domain = string.IsNullOrWhiteSpace(req.Domain) ? null : req.Domain!.Trim().ToLowerInvariant();
        tenant.IsActive = req.IsActive;
        tenant.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(tenant);
    }

    [HttpPut("{id}/domain")]
    public async Task<IActionResult> MapDomain(int id, [FromBody] UpdateDomainRequest req)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        if (tenant == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(req.Domain))
        {
            var domain = req.Domain!.Trim().ToLowerInvariant();
            var exists = await _db.Tenants.AnyAsync(t => t.Id != id && t.Domain == domain);
            if (exists) return Conflict(new { message = "Domain already mapped to another tenant" });
            tenant.Domain = domain;
        }
        else
        {
            tenant.Domain = null;
        }

        tenant.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Domain mapping updated", tenant.Domain });
    }

    [HttpPut("{id}/subscription")]
    public async Task<IActionResult> UpdateSubscription(int id, [FromBody] UpdateSubscriptionRequest req)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        if (tenant == null) return NotFound();

        tenant.SubscriptionPlan = req.SubscriptionPlan;
        tenant.MaxEmployees = req.MaxEmployees;
        tenant.UpdatedAt = DateTime.UtcNow;
        
        await _db.SaveChangesAsync();
        return Ok(new { message = "Subscription updated", tenant.SubscriptionPlan, tenant.MaxEmployees });
    }

    [HttpGet("subscription-plans")]
    public IActionResult GetSubscriptionPlans()
    {
        var plans = new[]
        {
            new { Name = "Free", MaxEmployees = 10, Price = 0, Features = new[] { "Basic HR features", "Up to 10 employees" } },
            new { Name = "Basic", MaxEmployees = 50, Price = 99, Features = new[] { "All Free features", "Up to 50 employees", "Email support" } },
            new { Name = "Pro", MaxEmployees = 200, Price = 299, Features = new[] { "All Basic features", "Up to 200 employees", "Priority support", "Advanced analytics" } },
            new { Name = "Enterprise", MaxEmployees = 1000, Price = 999, Features = new[] { "All Pro features", "Unlimited employees", "24/7 support", "Custom integrations", "Dedicated account manager" } }
        };
        
        return Ok(plans);
    }
}


