namespace backend.DTOs;

public class CreateRoleRequest
{
    public string RoleName { get; set; } = string.Empty;
    public int RoleTypeId { get; set; }
}

public class UpdateRoleRequest
{
    public string? RoleName { get; set; }
    public int? RoleTypeId { get; set; }
}

public class RoleResponseDto
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public int RoleTypeId { get; set; }
    public string RoleTypeName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}

public class RoleTypeDto
{
    public int RoleTypeId { get; set; }
    public string RoleTypeName { get; set; } = string.Empty;
}
