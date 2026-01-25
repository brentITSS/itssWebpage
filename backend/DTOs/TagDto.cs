namespace backend.DTOs;

public class CreateTagTypeRequest
{
    public string TagTypeName { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}

public class UpdateTagTypeRequest
{
    public string? TagTypeName { get; set; }
    public string? Color { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}

public class TagTypeResponseDto
{
    public int TagTypeId { get; set; }
    public string TagTypeName { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CreateTagLogRequest
{
    public int TagTypeId { get; set; }
    public string EntityType { get; set; } = string.Empty; // "Property", "PropertyGroup", "Tenant", "ContactLog"
    public int EntityId { get; set; }
}

public class TagDto
{
    public int TagLogId { get; set; }
    public int TagTypeId { get; set; }
    public string TagTypeName { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public DateTime CreatedDate { get; set; }
}
