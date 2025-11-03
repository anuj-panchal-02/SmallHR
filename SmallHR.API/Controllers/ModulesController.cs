using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmallHR.API.Base;
using SmallHR.Infrastructure.Data;
using SmallHR.Core.Interfaces;

namespace SmallHR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ModulesController : BaseApiController
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantProvider _tenantProvider;
    
    public ModulesController(
        ApplicationDbContext db, 
        ITenantProvider tenantProvider,
        ILogger<ModulesController> logger) : base(logger)
    {
        _db = db;
        _tenantProvider = tenantProvider;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<object>> GetModules()
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                // Check if user is SuperAdmin
                var isSuperAdmin = IsSuperAdmin;
                
                // SuperAdmin gets all modules (no TenantId filter)
                // Regular users get modules filtered by their TenantId
                var query = _db.Modules.AsNoTracking().Where(m => m.IsActive && !m.IsDeleted);
                
                if (!isSuperAdmin)
                {
                    // For non-SuperAdmin users, filter by TenantId
                    var tenantId = _tenantProvider.TenantId;
                    query = query.Where(m => m.TenantId == tenantId);
                }
                // SuperAdmin: no TenantId filter, gets all modules
                
                var modules = await query
                    .OrderBy(m => m.ParentPath)
                    .ThenBy(m => m.DisplayOrder)
                    .ToListAsync();

                var byPath = modules.ToDictionary(m => m.Path, m => new
                {
                    name = m.Name,
                    path = m.Path,
                    description = m.Description,
                    icon = m.Icon,
                    children = new List<object>()
                });

                var roots = new List<object>();
                foreach (var m in modules)
                {
                    if (!string.IsNullOrWhiteSpace(m.ParentPath) && byPath.ContainsKey(m.ParentPath))
                    {
                        ((List<object>)byPath[m.ParentPath].children!).Add(byPath[m.Path]);
                    }
                    else
                    {
                        roots.Add(byPath[m.Path]);
                    }
                }

                return (IEnumerable<object>)roots;
            },
            "getting modules"
        );
    }

    [HttpPost("seed")]
    [Authorize]
    public async Task<ActionResult<object>> Seed()
    {
        if (await _db.Modules.AnyAsync())
        {
            return Ok(new { message = "Modules already exist" });
        }

        var now = DateTime.UtcNow;
        var tenantId = _tenantProvider.TenantId;
        var mods = new[]
        {
            new Core.Entities.Module { TenantId = tenantId, Name = "Dashboard", Path = "/dashboard", ParentPath = null, Icon = "dashboard", DisplayOrder = 1, IsActive = true, Description = "Overview", CreatedAt = now, IsDeleted = false },
            new Core.Entities.Module { TenantId = tenantId, Name = "Employees", Path = "/employees", ParentPath = null, Icon = "user", DisplayOrder = 2, IsActive = true, Description = "Manage employees", CreatedAt = now, IsDeleted = false },
            new Core.Entities.Module { TenantId = tenantId, Name = "Departments", Path = "/department", ParentPath = null, Icon = "team", DisplayOrder = 3, IsActive = true, Description = "Departments", CreatedAt = now, IsDeleted = false },
            new Core.Entities.Module { TenantId = tenantId, Name = "Calendar", Path = "/calendar", ParentPath = null, Icon = "calendar", DisplayOrder = 4, IsActive = true, Description = "Events", CreatedAt = now, IsDeleted = false },
            new Core.Entities.Module { TenantId = tenantId, Name = "Notice Board", Path = "/notice-board", ParentPath = null, Icon = "notification", DisplayOrder = 5, IsActive = true, Description = "Announcements", CreatedAt = now, IsDeleted = false },
            new Core.Entities.Module { TenantId = tenantId, Name = "Expenses", Path = "/expenses", ParentPath = null, Icon = "dollar", DisplayOrder = 6, IsActive = true, Description = "Expenses", CreatedAt = now, IsDeleted = false },
            new Core.Entities.Module { TenantId = tenantId, Name = "Payroll", Path = "/payroll", ParentPath = null, Icon = "money", DisplayOrder = 7, IsActive = true, Description = "Payroll root", CreatedAt = now, IsDeleted = false },
            new Core.Entities.Module { TenantId = tenantId, Name = "Payroll Reports", Path = "/payroll/reports", ParentPath = "/payroll", Icon = "bar-chart", DisplayOrder = 1, IsActive = true, Description = "Reports", CreatedAt = now, IsDeleted = false },
            new Core.Entities.Module { TenantId = tenantId, Name = "Payroll Settings", Path = "/payroll/settings", ParentPath = "/payroll", Icon = "setting", DisplayOrder = 2, IsActive = true, Description = "Settings", CreatedAt = now, IsDeleted = false },
            new Core.Entities.Module { TenantId = tenantId, Name = "Tenant Settings", Path = "/tenant-settings", ParentPath = null, Icon = "apartment", DisplayOrder = 98, IsActive = true, Description = "Manage tenants", CreatedAt = now, IsDeleted = false },
            new Core.Entities.Module { TenantId = tenantId, Name = "Role Permissions", Path = "/role-permissions", ParentPath = null, Icon = "safety", DisplayOrder = 99, IsActive = true, Description = "Access control", CreatedAt = now, IsDeleted = false },
        };

        await _db.Modules.AddRangeAsync(mods);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Modules seeded" });
    }

    [HttpPost("add-missing")]
    [Authorize]
    public async Task<ActionResult<object>> AddMissingModules()
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                var tenantId = _tenantProvider.TenantId;
                var now = DateTime.UtcNow;
                int added = 0;

                // Check and add Tenant Settings if missing
                var tenantSettingsExists = await _db.Modules.AnyAsync(m => m.Path == "/tenant-settings" && m.TenantId == tenantId);
                if (!tenantSettingsExists)
                {
                    var tenantSettingsModule = new Core.Entities.Module
                    {
                        TenantId = tenantId,
                        Name = "Tenant Settings",
                        Path = "/tenant-settings",
                        ParentPath = null,
                        Icon = "apartment",
                        DisplayOrder = 98,
                        IsActive = true,
                        Description = "Manage tenants",
                        CreatedAt = now,
                        IsDeleted = false
                    };
                    await _db.Modules.AddAsync(tenantSettingsModule);
                    added++;
                }

                await _db.SaveChangesAsync();
                return new { message = $"Added {added} missing module(s)" };
            },
            "adding missing modules"
        );
    }
}


