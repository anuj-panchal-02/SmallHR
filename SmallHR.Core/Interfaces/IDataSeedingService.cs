namespace SmallHR.Core.Interfaces;

/// <summary>
/// Service for seeding initial data into the database
/// </summary>
public interface IDataSeedingService
{
    /// <summary>
    /// Seeds all required roles (SuperAdmin, Admin, HR, Employee)
    /// </summary>
    Task SeedRolesAsync();

    /// <summary>
    /// Cleans up users, keeping only the specified SuperAdmin user
    /// </summary>
    /// <param name="superAdminEmail">Email of the SuperAdmin user to keep</param>
    Task CleanupUsersAsync(string superAdminEmail);

    /// <summary>
    /// Ensures a SuperAdmin user exists with the specified email
    /// Creates the user if it doesn't exist, updates it if it does
    /// </summary>
    /// <param name="superAdminEmail">Email of the SuperAdmin user</param>
    Task EnsureSuperAdminExistsAsync(string superAdminEmail);
}

