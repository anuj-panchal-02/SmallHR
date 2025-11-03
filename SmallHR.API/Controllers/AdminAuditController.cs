using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmallHR.API.Base;
using SmallHR.API.Authorization;
using SmallHR.Core.Entities;
using SmallHR.Infrastructure.Data;

namespace SmallHR.API.Controllers;

/// <summary>
/// Controller for querying AdminAudit logs
/// Only SuperAdmin can access audit logs
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AuthorizeSuperAdmin]
public class AdminAuditController : BaseApiController
{
    private readonly ApplicationDbContext _context;

    public AdminAuditController(
        ApplicationDbContext context,
        ILogger<AdminAuditController> logger) : base(logger)
    {
        _context = context;
    }

    /// <summary>
    /// Get all audit logs with optional filtering
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<object>> GetAuditLogs(
        [FromQuery] string? adminEmail = null,
        [FromQuery] string? actionType = null,
        [FromQuery] string? targetTenantId = null,
        [FromQuery] bool? isSuccess = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                var query = _context.AdminAudits.AsQueryable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(adminEmail))
                {
                    query = query.Where(a => a.AdminEmail.Contains(adminEmail));
                }

                if (!string.IsNullOrWhiteSpace(actionType))
                {
                    query = query.Where(a => a.ActionType.Contains(actionType));
                }

                if (!string.IsNullOrWhiteSpace(targetTenantId))
                {
                    query = query.Where(a => a.TargetTenantId == targetTenantId);
                }

                if (isSuccess.HasValue)
                {
                    query = query.Where(a => a.IsSuccess == isSuccess.Value);
                }

                if (startDate.HasValue)
                {
                    query = query.Where(a => a.CreatedAt >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(a => a.CreatedAt <= endDate.Value);
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination
                var auditLogs = await query
                    .OrderByDescending(a => a.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return new
                {
                    totalCount,
                    pageNumber,
                    pageSize,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    auditLogs = auditLogs.Select(a => new
                    {
                        a.Id,
                        a.AdminUserId,
                        a.AdminEmail,
                        a.ActionType,
                        a.HttpMethod,
                        a.Endpoint,
                        a.TargetTenantId,
                        a.TargetEntityType,
                        a.TargetEntityId,
                        a.StatusCode,
                        a.IsSuccess,
                        a.IpAddress,
                        a.UserAgent,
                        a.ErrorMessage,
                        a.DurationMs,
                        a.CreatedAt
                    })
                };
            },
            "getting audit logs"
        );
    }

    /// <summary>
    /// Get audit log by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetAuditLog(int id)
    {
        return await HandleServiceResultOrNotFoundAsync(
            async () =>
            {
                var auditLog = await _context.AdminAudits.FindAsync(id);
                if (auditLog == null)
                {
                    return null;
                }

                return new
                {
                    auditLog.Id,
                    auditLog.AdminUserId,
                    auditLog.AdminEmail,
                    auditLog.ActionType,
                    auditLog.HttpMethod,
                    auditLog.Endpoint,
                    auditLog.TargetTenantId,
                    auditLog.TargetEntityType,
                    auditLog.TargetEntityId,
                    auditLog.RequestPayload,
                    auditLog.StatusCode,
                    auditLog.IsSuccess,
                    auditLog.IpAddress,
                    auditLog.UserAgent,
                    auditLog.Metadata,
                    auditLog.ErrorMessage,
                    auditLog.DurationMs,
                    auditLog.CreatedAt
                } as object;
            },
            $"getting audit log with ID {id}",
            "AuditLog"
        );
    }

    /// <summary>
    /// Get statistics about admin actions
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<object>> GetStatistics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        return await HandleServiceResultAsync(
            async () =>
            {
                var query = _context.AdminAudits.AsQueryable();

                if (startDate.HasValue)
                {
                    query = query.Where(a => a.CreatedAt >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(a => a.CreatedAt <= endDate.Value);
                }

                var totalActions = await query.CountAsync();
                var successfulActions = await query.CountAsync(a => a.IsSuccess);
                var failedActions = totalActions - successfulActions;

                var actionsByType = await query
                    .GroupBy(a => a.ActionType)
                    .Select(g => new
                    {
                        ActionType = g.Key,
                        Count = g.Count(),
                        SuccessCount = g.Count(a => a.IsSuccess),
                        FailureCount = g.Count(a => !a.IsSuccess)
                    })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToListAsync();

                var actionsByAdmin = await query
                    .GroupBy(a => a.AdminEmail)
                    .Select(g => new
                    {
                        AdminEmail = g.Key,
                        Count = g.Count(),
                        SuccessCount = g.Count(a => a.IsSuccess),
                        FailureCount = g.Count(a => !a.IsSuccess),
                        LastAction = g.Max(a => a.CreatedAt)
                    })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToListAsync();

                var averageDuration = await query
                    .Where(a => a.DurationMs.HasValue)
                    .Select(a => (double?)a.DurationMs!.Value)
                    .AverageAsync();

                return new
                {
                    totalActions,
                    successfulActions,
                    failedActions,
                    successRate = totalActions > 0 ? (double)successfulActions / totalActions * 100 : 0,
                    averageDurationMs = averageDuration.HasValue ? (long?)averageDuration.Value : null,
                    topActionTypes = actionsByType,
                    topAdmins = actionsByAdmin
                };
            },
            "getting audit statistics"
        );
    }
}

