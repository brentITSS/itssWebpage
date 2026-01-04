using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("api/tags")]
[Authorize]
public class TagsController : ControllerBase
{
    private readonly ITagService _tagService;
    private readonly IAuthService _authService;

    public TagsController(ITagService tagService, IAuthService authService)
    {
        _tagService = tagService;
        _authService = authService;
    }

    [HttpGet("types")]
    public async Task<ActionResult<List<TagTypeResponseDto>>> GetAllTagTypes()
    {
        var tagTypes = await _tagService.GetAllTagTypesAsync();
        return Ok(tagTypes);
    }

    [HttpGet("types/{id}")]
    public async Task<ActionResult<TagTypeResponseDto>> GetTagType(int id)
    {
        var tagType = await _tagService.GetTagTypeByIdAsync(id);
        if (tagType == null) return NotFound();

        return Ok(tagType);
    }

    [HttpPost("types")]
    [Authorize] // Should check Property Hub Admin permission
    public async Task<ActionResult<TagTypeResponseDto>> CreateTagType([FromBody] CreateTagTypeRequest request)
    {
        var tagType = await _tagService.CreateTagTypeAsync(request);
        return CreatedAtAction(nameof(GetTagType), new { id = tagType.TagTypeId }, tagType);
    }

    [HttpPut("types/{id}")]
    [Authorize] // Should check Property Hub Admin permission
    public async Task<ActionResult<TagTypeResponseDto>> UpdateTagType(int id, [FromBody] UpdateTagTypeRequest request)
    {
        var tagType = await _tagService.UpdateTagTypeAsync(id, request);
        if (tagType == null) return NotFound();

        return Ok(tagType);
    }

    [HttpDelete("types/{id}")]
    [Authorize] // Should check Property Hub Admin permission
    public async Task<ActionResult> DeleteTagType(int id)
    {
        var result = await _tagService.DeleteTagTypeAsync(id);
        if (!result) return NotFound();

        return NoContent();
    }

    [HttpPost("tag-log")]
    [Authorize] // Should check Property Hub access
    public async Task<ActionResult<TagDto>> CreateTagLog([FromBody] CreateTagLogRequest request)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        try
        {
            var tagLog = await _tagService.CreateTagLogAsync(request, currentUserId.Value);
            return Ok(tagLog);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("entity/{entityType}/{entityId}")]
    public async Task<ActionResult<List<TagDto>>> GetTagLogsByEntity(string entityType, int entityId)
    {
        var tagLogs = await _tagService.GetTagLogsByEntityAsync(entityType, entityId);
        return Ok(tagLogs);
    }

    [HttpDelete("tag-log/{id}")]
    [Authorize] // Should check Property Hub access
    public async Task<ActionResult> DeleteTagLog(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var result = await _tagService.DeleteTagLogAsync(id, currentUserId.Value);
        if (!result) return NotFound();

        return NoContent();
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return null;
        }
        return userId;
    }
}
