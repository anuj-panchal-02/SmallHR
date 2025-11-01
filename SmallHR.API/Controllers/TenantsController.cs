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
    public TenantsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tenants = await _db.Tenants.AsNoTracking().OrderBy(t => t.Name).ToListAsync();
        return Ok(tenants);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var tenant = await _db.Tenants.FindAsync(id);
        if (tenant == null) return NotFound();
        return Ok(tenant);
    }

    public record CreateTenantRequest(string Name, string? Domain, bool IsActive = true);
    public record UpdateTenantRequest(string Name, string? Domain, bool IsActive);
    public record UpdateDomainRequest(string? Domain);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTenantRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { message = "Name is required" });
        if (!string.IsNullOrWhiteSpace(req.Domain))
        {
            var exists = await _db.Tenants.AnyAsync(t => t.Domain == req.Domain);
            if (exists) return Conflict(new { message = "Domain already mapped to another tenant" });
        }

        var now = DateTime.UtcNow;
        var tenant = new Tenant
        {
            Name = req.Name.Trim(),
            Domain = string.IsNullOrWhiteSpace(req.Domain) ? null : req.Domain!.Trim().ToLowerInvariant(),
            IsActive = req.IsActive,
            CreatedAt = now,
            IsDeleted = false
        };

        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = tenant.Id }, tenant);
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
}


