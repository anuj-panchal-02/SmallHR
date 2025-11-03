using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmallHR.API.Base;
using SmallHR.API.Authorization;
using SmallHR.Core.DTOs.Position;
using SmallHR.Core.Interfaces;

namespace SmallHR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AuthorizeHR]
public class PositionsController : BaseApiController
{
    private readonly IPositionService _positionService;

    public PositionsController(IPositionService positionService, ILogger<PositionsController> logger)
        : base(logger)
    {
        _positionService = positionService;
    }

    /// <summary>
    /// Get all positions
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PositionDto>>> GetPositions([FromQuery] string? tenantId = null)
    {
        var tenantIdForRequest = HttpContext.RequestServices
            .GetRequiredService<ITenantFilterService>()
            .ResolveTenantIdForRequest(IsSuperAdmin, tenantId);

        return await HandleCollectionResultAsync(
            () => _positionService.GetAllPositionsAsync(tenantIdForRequest),
            "getting positions"
        );
    }

    /// <summary>
    /// Get position by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PositionDto>> GetPosition(int id)
    {
        return await HandleServiceResultOrNotFoundAsync(
            () => _positionService.GetPositionByIdAsync(id),
            $"getting position with ID {id}",
            "Position"
        );
    }

    /// <summary>
    /// Get positions by department ID
    /// </summary>
    [HttpGet("department/{departmentId}")]
    public async Task<ActionResult<IEnumerable<PositionDto>>> GetPositionsByDepartment(int departmentId)
    {
        return await HandleCollectionResultAsync(
            () => _positionService.GetPositionsByDepartmentIdAsync(departmentId),
            $"getting positions for department {departmentId}"
        );
    }

    /// <summary>
    /// Get position titles (for dropdowns)
    /// </summary>
    [HttpGet("titles")]
    public async Task<ActionResult<IEnumerable<string>>> GetPositionTitles()
    {
        return await HandleCollectionResultAsync(
            () => _positionService.GetPositionTitlesAsync(),
            "getting position titles"
        );
    }

    /// <summary>
    /// Create new position
    /// </summary>
    [HttpPost]
    [AuthorizeAdmin]
    public async Task<ActionResult<PositionDto>> CreatePosition([FromBody] CreatePositionDto createPositionDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return await HandleCreateResultAsync(
            () => _positionService.CreatePositionAsync(createPositionDto),
            nameof(GetPosition),
            pos => pos.Id,
            "creating position"
        );
    }

    /// <summary>
    /// Update position
    /// </summary>
    [HttpPut("{id}")]
    [AuthorizeAdmin]
    public async Task<ActionResult<PositionDto>> UpdatePosition(int id, [FromBody] UpdatePositionDto updatePositionDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return await HandleUpdateResultAsync(
            () => _positionService.PositionExistsAsync(id),
            () => _positionService.UpdatePositionAsync(id, updatePositionDto),
            id,
            "updating position",
            "Position"
        );
    }

    /// <summary>
    /// Delete position
    /// </summary>
    [HttpDelete("{id}")]
    [AuthorizeAdmin]
    public async Task<ActionResult> DeletePosition(int id)
    {
        return await HandleDeleteResultAsync(
            () => _positionService.PositionExistsAsync(id),
            () => _positionService.DeletePositionAsync(id),
            id,
            "deleting position",
            "Position"
        );
    }
}

