using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmallHR.Core.Entities;
using SmallHR.Core.Interfaces;
using SmallHR.Infrastructure.Data;
using System.Text.Json;

namespace SmallHR.Infrastructure.Services;

/// <summary>
/// Service for creating and managing system alerts
/// </summary>
public class AlertService : IAlertService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AlertService> _logger;

    public AlertService(
        ApplicationDbContext context,
        ILogger<AlertService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Alert> CreatePaymentFailureAlertAsync(
        int tenantId, 
        int? subscriptionId, 
        string message, 
        Dictionary<string, object>? metadata = null)
    {
        // Check if active alert already exists
        if (await HasActiveAlertAsync(tenantId, "PaymentFailure"))
        {
            _logger.LogInformation("Active payment failure alert already exists for tenant {TenantId}", tenantId);
            var existingAlert = await _context.Alerts
                .Where(a => a.TenantId == tenantId && 
                           a.AlertType == "PaymentFailure" && 
                           a.Status == "Active")
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();
            
            if (existingAlert != null)
            {
                return existingAlert;
            }
        }

        var alert = new Alert
        {
            TenantId = tenantId,
            SubscriptionId = subscriptionId,
            AlertType = "PaymentFailure",
            Severity = "High",
            Message = message,
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (metadata != null && metadata.Any())
        {
            alert.MetadataJson = JsonSerializer.Serialize(metadata);
        }

        await _context.Alerts.AddAsync(alert);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created payment failure alert for tenant {TenantId}, subscription {SubscriptionId}", 
            tenantId, subscriptionId);

        return alert;
    }

    public async Task<Alert> CreateCancellationAlertAsync(
        int tenantId, 
        int? subscriptionId, 
        string reason, 
        Dictionary<string, object>? metadata = null)
    {
        var alert = new Alert
        {
            TenantId = tenantId,
            SubscriptionId = subscriptionId,
            AlertType = "Cancellation",
            Severity = "Medium",
            Message = $"Subscription cancelled: {reason}",
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (metadata != null && metadata.Any())
        {
            alert.MetadataJson = JsonSerializer.Serialize(metadata);
        }

        await _context.Alerts.AddAsync(alert);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created cancellation alert for tenant {TenantId}, subscription {SubscriptionId}", 
            tenantId, subscriptionId);

        return alert;
    }

    public async Task<Alert> CreateOverageAlertAsync(
        int tenantId, 
        string resource, 
        int limit, 
        int usage, 
        Dictionary<string, object>? metadata = null)
    {
        // Check if active overage alert for this resource already exists
        var existingAlert = await _context.Alerts
            .Where(a => a.TenantId == tenantId && 
                       a.AlertType == "Overage" && 
                       a.Status == "Active" &&
                       a.MetadataJson != null &&
                       a.MetadataJson.Contains($"\"resource\":\"{resource}\""))
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync();

        if (existingAlert != null)
        {
            _logger.LogInformation("Active overage alert for resource {Resource} already exists for tenant {TenantId}", 
                resource, tenantId);
            return existingAlert;
        }

        var overagePercentage = ((double)(usage - limit) / limit * 100);
        var alert = new Alert
        {
            TenantId = tenantId,
            AlertType = "Overage",
            Severity = usage > limit * 1.5 ? "High" : "Medium", // High if 50%+ over limit
            Message = $"Usage overage for {resource}: {usage:N0} used (limit: {limit:N0}, {overagePercentage:F1}% over)",
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var alertMetadata = new Dictionary<string, object>
        {
            { "resource", resource },
            { "limit", limit },
            { "usage", usage },
            { "overage", usage - limit },
            { "overagePercentage", overagePercentage }
        };

        if (metadata != null)
        {
            foreach (var kvp in metadata)
            {
                alertMetadata[kvp.Key] = kvp.Value;
            }
        }

        alert.MetadataJson = JsonSerializer.Serialize(alertMetadata);

        await _context.Alerts.AddAsync(alert);
        await _context.SaveChangesAsync();

        _logger.LogWarning("Created overage alert for tenant {TenantId}, resource {Resource}: {Usage}/{Limit}", 
            tenantId, resource, usage, limit);

        return alert;
    }

    public async Task<Alert> CreateSuspensionAlertAsync(
        int tenantId, 
        string reason, 
        Dictionary<string, object>? metadata = null)
    {
        // Check if active suspension alert already exists
        if (await HasActiveAlertAsync(tenantId, "Suspension"))
        {
            _logger.LogInformation("Active suspension alert already exists for tenant {TenantId}", tenantId);
            var existingAlert = await _context.Alerts
                .Where(a => a.TenantId == tenantId && 
                           a.AlertType == "Suspension" && 
                           a.Status == "Active")
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();
            
            if (existingAlert != null)
            {
                return existingAlert;
            }
        }

        var alert = new Alert
        {
            TenantId = tenantId,
            AlertType = "Suspension",
            Severity = "Critical",
            Message = $"Tenant suspended: {reason}",
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (metadata != null && metadata.Any())
        {
            alert.MetadataJson = JsonSerializer.Serialize(metadata);
        }

        await _context.Alerts.AddAsync(alert);
        await _context.SaveChangesAsync();

        _logger.LogWarning("Created suspension alert for tenant {TenantId}: {Reason}", tenantId, reason);

        return alert;
    }

    public async Task<Alert> CreateErrorAlertAsync(
        int tenantId, 
        string message, 
        string? errorCode = null, 
        Dictionary<string, object>? metadata = null)
    {
        var alert = new Alert
        {
            TenantId = tenantId,
            AlertType = "Error",
            Severity = "High",
            Message = message,
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var alertMetadata = new Dictionary<string, object>();
        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            alertMetadata["errorCode"] = errorCode;
        }
        if (metadata != null)
        {
            foreach (var kvp in metadata)
            {
                alertMetadata[kvp.Key] = kvp.Value;
            }
        }

        if (alertMetadata.Any())
        {
            alert.MetadataJson = JsonSerializer.Serialize(alertMetadata);
        }

        await _context.Alerts.AddAsync(alert);
        await _context.SaveChangesAsync();

        _logger.LogError("Created error alert for tenant {TenantId}: {Message}", tenantId, message);

        return alert;
    }

    public async Task<bool> HasActiveAlertAsync(int tenantId, string alertType)
    {
        return await _context.Alerts
            .AnyAsync(a => a.TenantId == tenantId && 
                          a.AlertType == alertType && 
                          a.Status == "Active");
    }
}

