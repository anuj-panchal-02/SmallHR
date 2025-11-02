using SmallHR.Core.DTOs.Subscription;

namespace SmallHR.Core.Interfaces;

public interface ITenantProvisioningService
{
    /// <summary>
    /// Provisions a tenant with default roles, permissions, modules, departments, positions, subscription, and admin user.
    /// This method is idempotent - can be called multiple times safely.
    /// </summary>
    /// <param name="tenantId">The ID of the tenant to provision</param>
    /// <param name="adminEmail">Email address for the tenant admin user</param>
    /// <param name="adminFirstName">First name for the tenant admin user</param>
    /// <param name="adminLastName">Last name for the tenant admin user</param>
    /// <param name="subscriptionPlanId">Optional subscription plan ID (defaults to Free plan if not specified)</param>
    /// <param name="startTrial">Whether to start a trial (if plan supports it)</param>
    /// <returns>Tuple with success status and error message (if failed)</returns>
    Task<(bool Success, string? ErrorMessage, ProvisioningResult? Result)> ProvisionTenantAsync(
        int tenantId, 
        string adminEmail, 
        string adminFirstName, 
        string adminLastName,
        int? subscriptionPlanId = null,
        bool startTrial = false);

    /// <summary>
    /// Provisioning result details
    /// </summary>
    public class ProvisioningResult
    {
        public int TenantId { get; set; }
        public string TenantName { get; set; } = string.Empty;
        public int? SubscriptionId { get; set; }
        public string AdminEmail { get; set; } = string.Empty;
        public string AdminUserId { get; set; } = string.Empty;
        public bool EmailSent { get; set; }
        public List<string> StepsCompleted { get; set; } = new();
    }
}

