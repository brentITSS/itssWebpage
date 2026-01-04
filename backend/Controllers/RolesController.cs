using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize(Roles = "Global Admin")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    public async Task<ActionResult<List<RoleResponseDto>>> GetAllRoles()
    {
        var roles = await _roleService.GetAllRolesAsync();
        return Ok(roles);
    }

    [HttpGet("types")]
    public async Task<ActionResult<List<RoleTypeDto>>> GetAllRoleTypes()
    {
        var roleTypes = await _roleService.GetAllRoleTypesAsync();
        return Ok(roleTypes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RoleResponseDto>> GetRole(int id)
    {
        var role = await _roleService.GetRoleByIdAsync(id);
        if (role == null) return NotFound();

        return Ok(role);
    }

    [HttpPost]
    public async Task<ActionResult<RoleResponseDto>> CreateRole([FromBody] CreateRoleRequest request)
    {
        try
        {
            var role = await _roleService.CreateRoleAsync(request);
            return CreatedAtAction(nameof(GetRole), new { id = role.RoleId }, role);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<RoleResponseDto>> UpdateRole(int id, [FromBody] UpdateRoleRequest request)
    {
        try
        {
            var role = await _roleService.UpdateRoleAsync(id, request);
            if (role == null) return NotFound();

            return Ok(role);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteRole(int id)
    {
        var result = await _roleService.DeleteRoleAsync(id);
        if (!result) return NotFound();

        return NoContent();
    }
}
