namespace backend.DTOs;

public class MaintenanceTypeDto
{
    public int MaintenanceTypeId { get; set; }
    public string MaintenanceTypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? CreatedDate { get; set; }
}

public class CreateMaintenanceTypeRequest
{
    public string MaintenanceTypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}

public class UpdateMaintenanceTypeRequest
{
    public string? MaintenanceTypeName { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}

public class MaintenanceStatusDto
{
    public int MaintenanceStatusId { get; set; }
    public string MaintenanceStatusName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? CreatedDate { get; set; }
}

public class CreateMaintenanceStatusRequest
{
    public string MaintenanceStatusName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
}

public class UpdateMaintenanceStatusRequest
{
    public string? MaintenanceStatusName { get; set; }
    public string? Description { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
}

public class MaintenanceResponseDto
{
    public int MaintenanceId { get; set; }
    public int PropertyGroupId { get; set; }
    public string? PropertyGroupName { get; set; }
    public int PropertyId { get; set; }
    public string? PropertyName { get; set; }
    public int MaintenanceTypeId { get; set; }
    public string MaintenanceTypeName { get; set; } = string.Empty;
    public int? MaintenanceStatusId { get; set; }
    public string? MaintenanceStatusName { get; set; }
    public string? Summary { get; set; }
    public string? DetailNotes { get; set; }
    public DateTime? WorkDate { get; set; }
    public DateTime? CreatedDate { get; set; }
}

public class CreateMaintenanceRequest
{
    public int PropertyGroupId { get; set; }
    public int PropertyId { get; set; }
    public int MaintenanceTypeId { get; set; }
    public int? MaintenanceStatusId { get; set; }
    public string? Summary { get; set; }
    public string? DetailNotes { get; set; }
    public DateTime? WorkDate { get; set; }
}

public class UpdateMaintenanceRequest
{
    public int? PropertyGroupId { get; set; }
    public int? PropertyId { get; set; }
    public int? MaintenanceTypeId { get; set; }
    public int? MaintenanceStatusId { get; set; }
    public string? Summary { get; set; }
    public string? DetailNotes { get; set; }
    public DateTime? WorkDate { get; set; }
}
