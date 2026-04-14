using backend.DTOs;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers;

/// <summary>
/// Authentication controller for Task 2.
/// Handles user login and current user retrieval.
/// All authentication queries map exactly to the existing tblUser table.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Email and password are required");
        }

        var response = await _authService.LoginAsync(request);
        if (response == null)
        {
            return Unauthorized("Invalid email or password");
        }

        return Ok(response);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized();
        }

        var user = await _authService.GetCurrentUserAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<ForgotPasswordResponse>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { message = "Email is required." });
        }

        try
        {
            var response = await _authService.RequestPasswordResetAsync(request.Email.Trim());
            return Ok(response);
        }
        catch
        {
            return StatusCode(500, new
            {
                message = "Password reset is temporarily unavailable. Please contact support if this persists."
            });
        }
    }

    [HttpPost("complete-password-reset")]
    public async Task<IActionResult> CompletePasswordReset([FromBody] CompletePasswordResetRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new { message = "Token and new password are required." });
        }

        if (request.NewPassword.Length < 8)
        {
            return BadRequest(new { message = "Password must be at least 8 characters." });
        }

        try
        {
            var ok = await _authService.CompletePasswordResetAsync(request.Token.Trim(), request.NewPassword);
            if (!ok)
            {
                return BadRequest(new { message = "This reset link is invalid or has expired. Please request a new one." });
            }
        }
        catch
        {
            return StatusCode(500, new
            {
                message = "Password reset is temporarily unavailable. Please contact support if this persists."
            });
        }

        return NoContent();
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new { message = "New password is required." });
        }

        if (request.NewPassword.Length < 8)
        {
            return BadRequest(new { message = "Password must be at least 8 characters." });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var ok = await _authService.ChangePasswordAsync(userId, request.NewPassword);
        if (!ok)
        {
            return NotFound(new { message = "User account not found." });
        }

        return NoContent();
    }
}
