using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmallHR.Core.DTOs.Position;
using SmallHR.Core.Interfaces;

namespace SmallHR.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin,Admin,HR")]
public class PositionsController : ControllerBase
{
    private readonly IPositionService _positionService;
    private readonly ILogger<PositionsController> _logger;

    public PositionsController(IPositionService positionService, ILogger<PositionsController> logger)
    {
        _positionService = positionService;
        _logger = logger;
    }

    /// <summary>
    /// Get all positions
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PositionDto>>> GetPositions()
    {
        try
        {
            var positions = await _positionService.GetAllPositionsAsync();
            return Ok(positions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting positions");
            return StatusCode(500, new { message = "An error occurred while getting positions" });
        }
    }

    /// <summary>
    /// Get position by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PositionDto>> GetPosition(int id)
    {
        try
        {
            var position = await _positionService.GetPositionByIdAsync(id);
            if (position == null)
            {
                return NotFound();
            }

            return Ok(position);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting position with ID: {Id}", id);
            return StatusCode(500, new { message = "An error occurred while getting position" });
        }
    }

    /// <summary>
    /// Get positions by department ID
    /// </summary>
    [HttpGet("department/{departmentId}")]
    public async Task<ActionResult<IEnumerable<PositionDto>>> GetPositionsByDepartment(int departmentId)
    {
        try
        {
            var positions = await _positionService.GetPositionsByDepartmentIdAsync(departmentId);
            return Ok(positions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting positions for department: {DepartmentId}", departmentId);
            return StatusCode(500, new { message = "An error occurred while getting positions" });
        }
    }

    /// <summary>
    /// Get position titles (for dropdowns)
    /// </summary>
    [HttpGet("titles")]
    public async Task<ActionResult<IEnumerable<string>>> GetPositionTitles()
    {
        try
        {
            var titles = await _positionService.GetPositionTitlesAsync();
            return Ok(titles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting position titles");
            return StatusCode(500, new { message = "An error occurred while getting position titles" });
        }
    }

    /// <summary>
    /// Create new position
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<PositionDto>> CreatePosition([FromBody] CreatePositionDto createPositionDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var position = await _positionService.CreatePositionAsync(createPositionDto);
            return CreatedAtAction(nameof(GetPosition), new { id = position.Id }, position);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating position");
            return StatusCode(500, new { message = "An error occurred while creating position" });
        }
    }

    /// <summary>
    /// Update position
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult<PositionDto>> UpdatePosition(int id, [FromBody] UpdatePositionDto updatePositionDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!await _positionService.PositionExistsAsync(id))
            {
                return NotFound();
            }

            var position = await _positionService.UpdatePositionAsync(id, updatePositionDto);
            if (position == null)
            {
                return NotFound();
            }

            return Ok(position);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating position with ID: {Id}", id);
            return StatusCode(500, new { message = "An error occurred while updating position" });
        }
    }

    /// <summary>
    /// Delete position
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<ActionResult> DeletePosition(int id)
    {
        try
        {
            if (!await _positionService.PositionExistsAsync(id))
            {
                return NotFound();
            }

            var result = await _positionService.DeletePositionAsync(id);
            if (!result)
            {
                return BadRequest();
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting position with ID: {Id}", id);
            return StatusCode(500, new { message = "An error occurred while deleting position" });
        }
    }
}

