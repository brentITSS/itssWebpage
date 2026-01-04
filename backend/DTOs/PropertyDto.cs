namespace backend.DTOs;

public class CreatePropertyGroupRequest
{
    public string PropertyGroupName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdatePropertyGroupRequest
{
    public string? PropertyGroupName { get; set; }
    public string? Description { get; set; }
}

public class PropertyGroupResponseDto
{
    public int PropertyGroupId { get; set; }
    public string PropertyGroupName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedDate { get; set; }
    public int PropertyCount { get; set; }
}

public class CreatePropertyRequest
{
    public int PropertyGroupId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? PostCode { get; set; }
}

public class UpdatePropertyRequest
{
    public int? PropertyGroupId { get; set; }
    public string? PropertyName { get; set; }
    public string? Address { get; set; }
    public string? PostCode { get; set; }
}

public class PropertyResponseDto
{
    public int PropertyId { get; set; }
    public int PropertyGroupId { get; set; }
    public string PropertyGroupName { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? PostCode { get; set; }
    public DateTime CreatedDate { get; set; }
}
