namespace backend.DTOs;

public class CreateJournalLogRequest
{
    public int PropertyId { get; set; }
    public int? TenancyId { get; set; }
    public int? TenantId { get; set; }
    public int JournalTypeId { get; set; }
    public int? JournalSubTypeId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime TransactionDate { get; set; }
}

public class UpdateJournalLogRequest
{
    public int? PropertyId { get; set; }
    public int? TenancyId { get; set; }
    public int? TenantId { get; set; }
    public int? JournalTypeId { get; set; }
    public int? JournalSubTypeId { get; set; }
    public decimal? Amount { get; set; }
    public string? Description { get; set; }
    public DateTime? TransactionDate { get; set; }
}

public class JournalLogResponseDto
{
    public int JournalLogId { get; set; }
    public int PropertyId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public int? TenancyId { get; set; }
    public int? TenantId { get; set; }
    public string? TenantName { get; set; }
    public int JournalTypeId { get; set; }
    public string JournalTypeName { get; set; } = string.Empty;
    public int? JournalSubTypeId { get; set; }
    public string? JournalSubTypeName { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateTime TransactionDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public List<AttachmentDto> Attachments { get; set; } = new();
}

public class JournalTypeDto
{
    public int JournalTypeId { get; set; }
    public string JournalTypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<JournalSubTypeDto> SubTypes { get; set; } = new();
}

public class JournalSubTypeDto
{
    public int JournalSubTypeId { get; set; }
    public string JournalSubTypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class AttachmentDto
{
    public int AttachmentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? FileType { get; set; }
    public long FileSize { get; set; }
    public DateTime CreatedDate { get; set; }
}
