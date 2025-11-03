using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmallHR.Core.Interfaces;
using System.Security.Claims;

namespace SmallHR.API.Base;

/// <summary>
/// Base API controller with common error handling and helper methods
/// </summary>
[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected readonly ILogger Logger;
    private IPermissionService? _permissionService;

    protected BaseApiController(ILogger logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the permission service (injected via HttpContext)
    /// </summary>
    protected IPermissionService PermissionService
    {
        get
        {
            if (_permissionService == null)
            {
                _permissionService = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
            }
            return _permissionService;
        }
    }

    /// <summary>
    /// Gets the current user's role from claims
    /// </summary>
    protected string? CurrentUserRole => User.FindFirst(ClaimTypes.Role)?.Value;

    /// <summary>
    /// Checks if the current user is SuperAdmin
    /// </summary>
    protected bool IsSuperAdmin => PermissionService.IsSuperAdmin(CurrentUserRole);

    /// <summary>
    /// Checks if the current user has one of the specified roles
    /// </summary>
    /// <param name="allowedRoles">Comma-separated list of allowed roles</param>
    /// <returns>True if user has one of the allowed roles</returns>
    protected bool HasRole(string allowedRoles) => PermissionService.HasRole(CurrentUserRole, allowedRoles);

    /// <summary>
    /// Validates the model state and returns BadRequest if invalid
    /// </summary>
    /// <returns>BadRequest result if model state is invalid, null otherwise</returns>
    protected IActionResult? ValidateModelState()
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        return null;
    }

    /// <summary>
    /// Handles service operations with common error handling
    /// </summary>
    /// <typeparam name="T">The return type</typeparam>
    /// <param name="operation">The async operation to execute</param>
    /// <param name="operationName">Name of the operation for logging</param>
    /// <returns>ActionResult with the result</returns>
    protected async Task<ActionResult<T>> HandleServiceResultAsync<T>(
        Func<Task<T>> operation,
        string operationName)
    {
        try
        {
            var result = await operation();
            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while {OperationName}", operationName);
            return CreateErrorResponse($"An error occurred while {operationName}", ex);
        }
    }

    /// <summary>
    /// Handles service operations that may return null (not found scenario)
    /// </summary>
    /// <typeparam name="T">The return type</typeparam>
    /// <param name="operation">The async operation to execute</param>
    /// <param name="operationName">Name of the operation for logging</param>
    /// <param name="resourceName">Name of the resource for not found messages</param>
    /// <returns>ActionResult with the result or NotFound</returns>
    protected async Task<ActionResult<T>> HandleServiceResultOrNotFoundAsync<T>(
        Func<Task<T?>> operation,
        string operationName,
        string resourceName) where T : class
    {
        try
        {
            var result = await operation();
            if (result == null)
            {
                Logger.LogWarning("{ResourceName} not found during {OperationName}", resourceName, operationName);
                return NotFound();
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while {OperationName}", operationName);
            return CreateErrorResponse($"An error occurred while {operationName}", ex);
        }
    }

    /// <summary>
    /// Handles create operations with CreatedAtAction response
    /// </summary>
    /// <typeparam name="T">The return type</typeparam>
    /// <typeparam name="TId">The ID type</typeparam>
    /// <param name="operation">The async operation to execute</param>
    /// <param name="getActionName">Name of the GET action for CreatedAtAction</param>
    /// <param name="getId">Function to extract ID from result</param>
    /// <param name="operationName">Name of the operation for logging</param>
    /// <returns>CreatedAtAction result</returns>
    protected async Task<ActionResult<T>> HandleCreateResultAsync<T, TId>(
        Func<Task<T>> operation,
        string getActionName,
        Func<T, TId> getId,
        string operationName) where T : class
    {
        try
        {
            var result = await operation();
            return CreatedAtAction(getActionName, new { id = getId(result) }, result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while {OperationName}", operationName);
            return CreateErrorResponse($"An error occurred while {operationName}", ex);
        }
    }

    /// <summary>
    /// Handles update operations
    /// </summary>
    /// <typeparam name="T">The return type</typeparam>
    /// <param name="existsCheck">Function to check if resource exists</param>
    /// <param name="updateOperation">The async update operation</param>
    /// <param name="resourceId">The resource ID</param>
    /// <param name="operationName">Name of the operation for logging</param>
    /// <param name="resourceName">Name of the resource</param>
    /// <returns>ActionResult with updated result or NotFound</returns>
    protected async Task<ActionResult<T>> HandleUpdateResultAsync<T>(
        Func<Task<bool>> existsCheck,
        Func<Task<T?>> updateOperation,
        int resourceId,
        string operationName,
        string resourceName) where T : class
    {
        try
        {
            if (!await existsCheck())
            {
                Logger.LogWarning("{ResourceName} with ID {ResourceId} not found during {OperationName}", 
                    resourceName, resourceId, operationName);
                return NotFound();
            }

            var result = await updateOperation();
            if (result == null)
            {
                Logger.LogWarning("{ResourceName} update returned null for ID {ResourceId} during {OperationName}", 
                    resourceName, resourceId, operationName);
                return NotFound();
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while {OperationName} for {ResourceName} with ID {ResourceId}", 
                operationName, resourceName, resourceId);
            return CreateErrorResponse($"An error occurred while {operationName}", ex);
        }
    }

    /// <summary>
    /// Handles delete operations
    /// </summary>
    /// <param name="existsCheck">Function to check if resource exists</param>
    /// <param name="deleteOperation">The async delete operation</param>
    /// <param name="resourceId">The resource ID</param>
    /// <param name="operationName">Name of the operation for logging</param>
    /// <param name="resourceName">Name of the resource</param>
    /// <returns>NoContent or NotFound/BadRequest</returns>
    protected async Task<ActionResult> HandleDeleteResultAsync(
        Func<Task<bool>> existsCheck,
        Func<Task<bool>> deleteOperation,
        int resourceId,
        string operationName,
        string resourceName)
    {
        try
        {
            if (!await existsCheck())
            {
                Logger.LogWarning("{ResourceName} with ID {ResourceId} not found during {OperationName}", 
                    resourceName, resourceId, operationName);
                return NotFound();
            }

            var result = await deleteOperation();
            if (!result)
            {
                Logger.LogWarning("{ResourceName} delete operation failed for ID {ResourceId} during {OperationName}", 
                    resourceName, resourceId, operationName);
                return BadRequest(new { message = $"Failed to delete {resourceName}" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while {OperationName} for {ResourceName} with ID {ResourceId}", 
                operationName, resourceName, resourceId);
            return CreateErrorResponse($"An error occurred while {operationName}", ex);
        }
    }

    /// <summary>
    /// Handles collection operations (GetAll, etc.)
    /// </summary>
    /// <typeparam name="T">The return type</typeparam>
    /// <param name="operation">The async operation to execute</param>
    /// <param name="operationName">Name of the operation for logging</param>
    /// <returns>ActionResult with collection</returns>
    protected async Task<ActionResult<IEnumerable<T>>> HandleCollectionResultAsync<T>(
        Func<Task<IEnumerable<T>>> operation,
        string operationName)
    {
        try
        {
            var result = await operation();
            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An error occurred while {OperationName}", operationName);
            return CreateErrorResponse($"An error occurred while {operationName}", ex);
        }
    }

    /// <summary>
    /// Creates a standardized error response
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="exception">The exception (optional)</param>
    /// <returns>ObjectResult with 500 status code</returns>
    protected ObjectResult CreateErrorResponse(string message, Exception? exception = null)
    {
        var errorResponse = new
        {
            message = message,
            detail = exception?.Message,
            errorType = exception?.GetType().Name
        };

        return StatusCode(500, errorResponse);
    }

    /// <summary>
    /// Creates a standardized not found response
    /// </summary>
    /// <param name="resourceName">Name of the resource</param>
    /// <returns>NotFound result</returns>
    protected NotFoundResult CreateNotFoundResponse(string resourceName)
    {
        return NotFound();
    }

    /// <summary>
    /// Creates a standardized bad request response
    /// </summary>
    /// <param name="message">Error message</param>
    /// <returns>BadRequest result</returns>
    protected BadRequestObjectResult CreateBadRequestResponse(string message)
    {
        return BadRequest(new { message = message });
    }
}

