using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuthService _authService;

    public UsersController(IUserService userService, IAuthService authService)
    {
        _userService = userService;
        _authService = authService;
    }

    [HttpGet]
    [Authorize(Roles = "Global Admin")]
    public async Task<ActionResult<List<UserResponseDto>>> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserResponseDto>> GetUser(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        // Allow users to view their own profile, or require Global Admin for others
        if (currentUserId.Value != id)
        {
            var currentUser = await _authService.GetCurrentUserAsync(currentUserId.Value);
            if (currentUser == null || !_authService.IsGlobalAdmin(currentUser))
            {
                return Forbid();
            }
        }

        var user = await _userService.GetUserByIdAsync(id);
        if (user == null) return NotFound();

        return Ok(user);
    }

    [HttpPost]
    [Authorize(Roles = "Global Admin")]
    public async Task<ActionResult<UserResponseDto>> CreateUser([FromBody] CreateUserRequest request)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        try
        {
            var user = await _userService.CreateUserAsync(request, currentUserId.Value);
            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, user);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Global Admin")]
    public async Task<ActionResult<UserResponseDto>> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        try
        {
            var user = await _userService.UpdateUserAsync(id, request, currentUserId.Value);
            if (user == null) return NotFound();

            return Ok(user);
        }
        catch (Exception ex)
        {
            // Log the exception details for debugging
            // Include inner exception if available
            var errorMessage = ex.Message;
            if (ex.InnerException != null)
            {
                errorMessage += $" Inner: {ex.InnerException.Message}";
            }
            
            // Log stack trace for debugging (in production, use proper logging)
            Console.WriteLine($"Error updating user {id}: {errorMessage}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            
            return StatusCode(500, new { 
                error = "An error occurred while updating the user", 
                message = errorMessage,
                details = ex.GetType().Name
            });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Global Admin")]
    public async Task<ActionResult> DeleteUser(int id)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var result = await _userService.DeleteUserAsync(id, currentUserId.Value);
        if (!result) return NotFound();

        return NoContent();
    }

    [HttpPost("{id}/reset-password")]
    [Authorize(Roles = "Global Admin")]
    public async Task<ActionResult> ResetPassword(int id, [FromBody] ResetPasswordRequest request)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId == null) return Unauthorized();

        var result = await _userService.ResetPasswordAsync(id, request, currentUserId.Value);
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
