namespace backend.DTOs;

public class CreateContactLogRequest
{
    public int PropertyId { get; set; }
    public int? TenantId { get; set; }
    public int ContactLogTypeId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime ContactDate { get; set; }
}

public class UpdateContactLogRequest
{
    public int? PropertyId { get; set; }
    public int? TenantId { get; set; }
    public int? ContactLogTypeId { get; set; }
    public string? Subject { get; set; }
    public string? Notes { get; set; }
    public DateTime? ContactDate { get; set; }
}

public class ContactLogResponseDto
{
    public int ContactLogId { get; set; }
    public int PropertyId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public int? TenantId { get; set; }
    public string? TenantName { get; set; }
    public int ContactLogTypeId { get; set; }
    public string ContactLogTypeName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime ContactDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public List<AttachmentDto> Attachments { get; set; } = new();
    public List<TagDto> Tags { get; set; } = new();
}

public class ContactLogTypeDto
{
    public int ContactLogTypeId { get; set; }
    public string ContactLogTypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
