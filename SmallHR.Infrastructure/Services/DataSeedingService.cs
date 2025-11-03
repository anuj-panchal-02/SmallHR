using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;

namespace SmallHR.Infrastructure.Services;

public class DataSeedingService : IDataSeedingService
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<DataSeedingService> _logger;

    public DataSeedingService(
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<DataSeedingService> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SeedRolesAsync()
    {
        // Create roles only - essential for the system
        var roles = new[] { "SuperAdmin", "Admin", "HR", "Employee" };
        
        foreach (var roleName in roles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName));
                _logger.LogInformation("Created role: {Role}", roleName);
            }
        }
    }

    public async Task CleanupUsersAsync(string superAdminEmail)
    {
        // Delete all existing users except SuperAdmin (to ensure only 1 SuperAdmin exists)
        var allUsers = _userManager.Users.ToList();
        foreach (var user in allUsers)
        {
            if (user.Email == null || !user.Email.Equals(superAdminEmail, StringComparison.OrdinalIgnoreCase))
            {
                await _userManager.DeleteAsync(user);
                _logger.LogInformation("Deleted user: {Email}", user.Email);
            }
        }

        // Delete duplicate SuperAdmin users (keep only one)
        var allSuperAdmins = await _userManager.GetUsersInRoleAsync("SuperAdmin");
        if (allSuperAdmins.Count > 1)
        {
            // Keep the first one, delete the rest
            var superAdminToKeep = allSuperAdmins.FirstOrDefault(u => u.Email != null && u.Email.Equals(superAdminEmail, StringComparison.OrdinalIgnoreCase))
                ?? allSuperAdmins.First();
            
            foreach (var admin in allSuperAdmins)
            {
                if (admin.Id != superAdminToKeep.Id)
                {
                    await _userManager.DeleteAsync(admin);
                    _logger.LogInformation("Deleted duplicate SuperAdmin: {Email}", admin.Email);
                }
            }
        }
    }

    public async Task EnsureSuperAdminExistsAsync(string superAdminEmail)
    {
        // Create or update SuperAdmin user (ensure only 1 exists)
        var superAdminUser = await _userManager.FindByEmailAsync(superAdminEmail);
        if (superAdminUser == null)
        {
            superAdminUser = new User
            {
                UserName = superAdminEmail,
                Email = superAdminEmail,
                FirstName = "Super",
                LastName = "Admin",
                DateOfBirth = new DateTime(1985, 1, 1),
                IsActive = true,
                TenantId = null // SuperAdmin operates at platform layer, no tenant association
            };
            
            await _userManager.CreateAsync(superAdminUser, "SuperAdmin@123");
            await _userManager.AddToRoleAsync(superAdminUser, "SuperAdmin");
            _logger.LogInformation("Created SuperAdmin user: {Email}", superAdminEmail);
        }
        else
        {
            // Ensure existing SuperAdmin has TenantId = null and correct role
            if (superAdminUser.TenantId != null)
            {
                superAdminUser.TenantId = null;
                await _userManager.UpdateAsync(superAdminUser);
                _logger.LogInformation("Updated existing SuperAdmin user to have TenantId = null");
            }
            
            // Ensure SuperAdmin role is assigned
            if (!await _userManager.IsInRoleAsync(superAdminUser, "SuperAdmin"))
            {
                await _userManager.AddToRoleAsync(superAdminUser, "SuperAdmin");
                _logger.LogInformation("Assigned SuperAdmin role to existing user: {Email}", superAdminEmail);
            }
        }
    }
}

