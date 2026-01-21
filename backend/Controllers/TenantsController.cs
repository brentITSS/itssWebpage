using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("api/tenants")]
[Authorize]
public class TenantsController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly IAuthService _authService;

    public TenantsController(ITenantService tenantService, IAuthService authService)
    {
        _tenantService = tenantService;
        _authService = authService;
    }

    /// <summary>
    /// Get all tenants. Only accessible to Property Hub workstream users or Global Admins.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<TenantResponseDto>>> GetAllTenants()
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var currentUser = await _authService.GetCurrentUserAsync(currentUserId.Value);
        if (currentUser == null) return Unauthorized();

        // Check Property Hub workstream access
        if (!HasPropertyHubAccess(currentUser))
        {
            return Forbid("Access denied: Property Hub workstream access required");
        }

        var tenants = await _tenantService.GetAllTenantsAsync();
        return Ok(tenants);
    }

    /// <summary>
    /// Get tenant by ID. Only accessible to Property Hub workstream users or Global Admins.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<TenantResponseDto>> GetTenant(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var currentUser = await _authService.GetCurrentUserAsync(currentUserId.Value);
        if (currentUser == null) return Unauthorized();

        // Check Property Hub workstream access
        if (!HasPropertyHubAccess(currentUser))
        {
            return Forbid("Access denied: Property Hub workstream access required");
        }

        var tenant = await _tenantService.GetTenantByIdAsync(id);
        if (tenant == null) return NotFound();

        return Ok(tenant);
    }

    /// <summary>
    /// Create tenant. Only accessible to Property Hub Admin or Global Admins.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TenantResponseDto>> CreateTenant([FromBody] CreateTenantRequest request)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            var currentUser = await _authService.GetCurrentUserAsync(currentUserId.Value);
            if (currentUser == null) return Unauthorized();

            // Check Property Hub Admin access
            if (!_authService.HasPropertyHubAdminAccess(currentUser))
            {
                return Forbid("Access denied: Property Hub Admin permission required");
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.FirstName))
            {
                return BadRequest("First Name is required");
            }

            if (string.IsNullOrWhiteSpace(request.LastName))
            {
                return BadRequest("Last Name is required");
            }

            var tenant = await _tenantService.CreateTenantAsync(request, currentUserId.Value);
            return CreatedAtAction(nameof(GetTenant), new { id = tenant.TenantId }, tenant);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating tenant: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return StatusCode(500, new { message = "An error occurred while creating the tenant", error = ex.Message });
        }
    }

    /// <summary>
    /// Update tenant. Only accessible to Property Hub Admin or Global Admins.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<TenantResponseDto>> UpdateTenant(int id, [FromBody] UpdateTenantRequest request)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var currentUser = await _authService.GetCurrentUserAsync(currentUserId.Value);
        if (currentUser == null) return Unauthorized();

        // Check Property Hub Admin access
        if (!_authService.HasPropertyHubAdminAccess(currentUser))
        {
            return Forbid("Access denied: Property Hub Admin permission required");
        }

        var tenant = await _tenantService.UpdateTenantAsync(id, request, currentUserId.Value);
        if (tenant == null) return NotFound();

        return Ok(tenant);
    }

    /// <summary>
    /// Delete tenant. Only accessible to Property Hub Admin or Global Admins.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTenant(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var currentUser = await _authService.GetCurrentUserAsync(currentUserId.Value);
        if (currentUser == null) return Unauthorized();

        // Check Property Hub Admin access
        if (!_authService.HasPropertyHubAdminAccess(currentUser))
        {
            return Forbid("Access denied: Property Hub Admin permission required");
        }

        var result = await _tenantService.DeleteTenantAsync(id, currentUserId.Value);
        if (!result) return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Check if user has Property Hub workstream access.
    /// </summary>
    private bool HasPropertyHubAccess(DTOs.UserDto user)
    {
        if (user.IsGlobalAdmin) return true;

        return user.WorkstreamAccess.Any(wa =>
            wa.WorkstreamName.Equals("Property Hub", StringComparison.OrdinalIgnoreCase) ||
            wa.WorkstreamName.Contains("Property", StringComparison.OrdinalIgnoreCase));
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
