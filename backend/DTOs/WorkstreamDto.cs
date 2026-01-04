namespace backend.DTOs;

public class CreateWorkstreamRequest
{
    public string WorkstreamName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateWorkstreamRequest
{
    public string? WorkstreamName { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}

public class WorkstreamResponseDto
{
    public int WorkstreamId { get; set; }
    public string WorkstreamName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class AssignWorkstreamUserRequest
{
    public int UserId { get; set; }
    public int PermissionTypeId { get; set; }
}

public class PermissionTypeDto
{
    public int PermissionTypeId { get; set; }
    public string PermissionTypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
