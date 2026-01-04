namespace backend.DTOs;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
}

public class UserDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<WorkstreamAccessDto> WorkstreamAccess { get; set; } = new();
    public bool IsGlobalAdmin { get; set; }
}

public class WorkstreamAccessDto
{
    public int WorkstreamId { get; set; }
    public string WorkstreamName { get; set; } = string.Empty;
    public int PermissionTypeId { get; set; }
    public string PermissionTypeName { get; set; } = string.Empty;
}
