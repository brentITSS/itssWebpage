using backend.DTOs;
using backend.Models;
using backend.Repositories;
using Microsoft.AspNetCore.StaticFiles;

namespace backend.Services;

public class JournalLogService : IJournalLogService
{
    private readonly IJournalLogRepository _journalLogRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IPropertyRepository _propertyRepository;
    private readonly ICalendarAppointmentRepository _calendarAppointmentRepository;
    private readonly IWebHostEnvironment _environment;
    private const string SourceType = "journallog";

    public JournalLogService(
        IJournalLogRepository journalLogRepository,
        IAuditLogRepository auditLogRepository,
        IPropertyRepository propertyRepository,
        ICalendarAppointmentRepository calendarAppointmentRepository,
        IWebHostEnvironment environment)
    {
        _journalLogRepository = journalLogRepository;
        _auditLogRepository = auditLogRepository;
        _propertyRepository = propertyRepository;
        _calendarAppointmentRepository = calendarAppointmentRepository;
        _environment = environment;
    }

    public async Task<List<JournalLogResponseDto>> GetAllJournalLogsAsync()
    {
        var journalLogs = await _journalLogRepository.GetAllAsync();
        var rows = journalLogs.Select(MapToJournalLogResponseDto).ToList();
        await AttachCalendarLinksAsync(rows);
        return rows;
    }

    public async Task<List<JournalLogResponseDto>> GetAllJournalLogsForUserAsync(int userId, bool isGlobalAdmin, bool isPropertyHubAdmin)
    {
        var allJournalLogs = await _journalLogRepository.GetAllAsync();
        
        // Global Admins and Property Hub Admins see all journal logs
        if (isGlobalAdmin || isPropertyHubAdmin)
        {
            var allRows = allJournalLogs.Select(MapToJournalLogResponseDto).ToList();
            await AttachCalendarLinksAsync(allRows);
            return allRows;
        }

        // Regular users: get their assigned property group IDs
        var userPropertyGroupIds = await _propertyRepository.GetUserPropertyGroupIdsAsync(userId);
        
        // If user has no specific assignments, show all (backward compatible)
        if (userPropertyGroupIds.Count == 0)
        {
            var allRows = allJournalLogs.Select(MapToJournalLogResponseDto).ToList();
            await AttachCalendarLinksAsync(allRows);
            return allRows;
        }

        // Get all properties in user's accessible property groups
        var allProperties = await _propertyRepository.GetAllPropertiesAsync();
        var accessiblePropertyIds = allProperties
            .Where(p => p.PropertyGroupId.HasValue && userPropertyGroupIds.Contains(p.PropertyGroupId.Value))
            .Select(p => p.PropertyId)
            .ToList();

        // Filter journal logs to only those for accessible properties
        var rows = allJournalLogs
            .Where(jl =>
                (jl.PropertyId.HasValue && accessiblePropertyIds.Contains(jl.PropertyId.Value)) ||
                (jl.PropertyGroupId.HasValue && userPropertyGroupIds.Contains(jl.PropertyGroupId.Value)))
            .Select(MapToJournalLogResponseDto)
            .ToList();
        await AttachCalendarLinksAsync(rows);
        return rows;
    }

    public async Task<JournalLogResponseDto?> GetJournalLogByIdAsync(int journalLogId)
    {
        var journalLog = await _journalLogRepository.GetByIdAsync(journalLogId);
        if (journalLog == null) return null;

        var dto = MapToJournalLogResponseDto(journalLog);
        await AttachCalendarLinksAsync(new List<JournalLogResponseDto> { dto });
        return dto;
    }

    public async Task<List<JournalLogResponseDto>> GetJournalLogsByPropertyIdAsync(int propertyId)
    {
        var journalLogs = await _journalLogRepository.GetByPropertyIdAsync(propertyId);
        var rows = journalLogs.Select(MapToJournalLogResponseDto).ToList();
        await AttachCalendarLinksAsync(rows);
        return rows;
    }

    public async Task<JournalLogResponseDto> CreateJournalLogAsync(CreateJournalLogRequest request, int createdByUserId)
    {
        var journalLog = new JournalLog
        {
            PropertyId = request.PropertyId,
            TenancyId = request.TenancyId,
            TenantId = request.TenantId,
            JournalTypeId = request.JournalTypeId,
            JournalSubTypeId = request.JournalSubTypeId,
            TransactionDate = request.TransactionDate,
            // Store Amount/Description in computed properties (not persisted)
            Amount = request.Amount,
            Description = request.Description
        };

        journalLog = await _journalLogRepository.CreateAsync(journalLog);
        await SyncCalendarAppointmentAsync(
            journalLog.JournalLogId,
            request.AddToCalendar,
            request.CalendarDate ?? request.TransactionDate);
        journalLog = await _journalLogRepository.GetByIdAsync(journalLog.JournalLogId);

        // Audit log
        await _auditLogRepository.CreateAsync(new AuditLog
        {
            UserId = createdByUserId,
            Action = "Create",
            EntityType = "JournalLog",
            EntityId = journalLog.JournalLogId,
            NewValues = $"PropertyId: {journalLog.PropertyId}, Amount: {journalLog.Amount?.ToString() ?? "0"}, Date: {journalLog.TransactionDate?.ToString() ?? "N/A"}",
            CreatedDate = DateTime.UtcNow
        });

        var dto = MapToJournalLogResponseDto(journalLog!);
        await AttachCalendarLinksAsync(new List<JournalLogResponseDto> { dto });
        return dto;
    }

    public async Task<JournalLogResponseDto?> UpdateJournalLogAsync(int journalLogId, UpdateJournalLogRequest request, int modifiedByUserId)
    {
        var journalLog = await _journalLogRepository.GetByIdAsync(journalLogId);
        if (journalLog == null) return null;

        var oldValues = $"PropertyId: {journalLog.PropertyId}, Amount: {journalLog.Amount?.ToString() ?? "0"}";

        if (request.PropertyId.HasValue) journalLog.PropertyId = request.PropertyId.Value;
        if (request.TenancyId.HasValue) journalLog.TenancyId = request.TenancyId;
        if (request.TenantId.HasValue) journalLog.TenantId = request.TenantId;
        if (request.JournalTypeId.HasValue) journalLog.JournalTypeId = request.JournalTypeId.Value;
        if (request.JournalSubTypeId.HasValue) journalLog.JournalSubTypeId = request.JournalSubTypeId;
        if (request.TransactionDate.HasValue) journalLog.TransactionDate = request.TransactionDate.Value;
        // Update computed properties (not persisted to DB)
        if (request.Amount.HasValue) journalLog.Amount = request.Amount.Value;
        if (request.Description != null) journalLog.Description = request.Description;

        journalLog = await _journalLogRepository.UpdateAsync(journalLog);
        if (request.AddToCalendar.HasValue)
        {
            await SyncCalendarAppointmentAsync(
                journalLogId,
                request.AddToCalendar.Value,
                request.CalendarDate ?? request.TransactionDate ?? journalLog.TransactionDate);
        }
        journalLog = await _journalLogRepository.GetByIdAsync(journalLogId);

        // Audit log
        var newValues = $"PropertyId: {journalLog!.PropertyId}, Amount: {journalLog.Amount?.ToString() ?? "0"}";
        await _auditLogRepository.CreateAsync(new AuditLog
        {
            UserId = modifiedByUserId,
            Action = "Update",
            EntityType = "JournalLog",
            EntityId = journalLogId,
            OldValues = oldValues,
            NewValues = newValues,
            CreatedDate = DateTime.UtcNow
        });

        var dto = MapToJournalLogResponseDto(journalLog);
        await AttachCalendarLinksAsync(new List<JournalLogResponseDto> { dto });
        return dto;
    }

    public async Task<bool> DeleteJournalLogAsync(int journalLogId, int deletedByUserId)
    {
        var journalLog = await _journalLogRepository.GetByIdAsync(journalLogId);
        if (journalLog == null) return false;

        // Deterministic child cleanup: do not rely solely on DB FK cascade.
        await _journalLogRepository.DeleteAttachmentsByJournalLogIdAsync(journalLogId);
        await _journalLogRepository.DeleteTagsByJournalLogIdAsync(journalLogId);
        await _calendarAppointmentRepository.DeleteBySourceAsync(SourceType, journalLogId);

        var result = await _journalLogRepository.DeleteAsync(journalLogId);

        if (result)
        {
            await _auditLogRepository.CreateAsync(new AuditLog
            {
                UserId = deletedByUserId,
                Action = "Delete",
                EntityType = "JournalLog",
                EntityId = journalLogId,
                OldValues = $"PropertyId: {journalLog.PropertyId}, Amount: {journalLog.Amount?.ToString() ?? "0"}",
                CreatedDate = DateTime.UtcNow
            });
        }

        return result;
    }

    public async Task<List<JournalTypeDto>> GetAllJournalTypesAsync()
    {
        var journalTypes = await _journalLogRepository.GetAllJournalTypesAsync();
        return journalTypes.Select(jt => new JournalTypeDto
        {
            JournalTypeId = jt.JournalTypeId,
            JournalTypeName = jt.JournalTypeName ?? string.Empty,
            Description = jt.Description,
            IsActive = jt.IsActive,
            SubTypes = jt.JournalSubTypes != null && jt.JournalSubTypes.Any()
                ? jt.JournalSubTypes.Select(jst => new JournalSubTypeDto
                {
                    JournalSubTypeId = jst.JournalSubTypeId,
                    JournalSubTypeName = jst.JournalSubTypeName ?? string.Empty,
                    Description = jst.Description,
                    IsActive = jst.IsActive
                }).ToList()
                : new List<JournalSubTypeDto>()
        }).ToList();
    }

    public async Task<JournalTypeDto> CreateJournalTypeAsync(CreateJournalTypeRequest request)
    {
        var journalType = new JournalType
        {
            JournalTypeName = request.JournalTypeName,
            Description = request.Description,
            IsActive = request.IsActive ?? true
        };

        journalType = await _journalLogRepository.CreateJournalTypeAsync(journalType);
        journalType = await _journalLogRepository.GetJournalTypeByIdAsync(journalType.JournalTypeId);

        return new JournalTypeDto
        {
            JournalTypeId = journalType.JournalTypeId,
            JournalTypeName = journalType.JournalTypeName ?? string.Empty,
            Description = journalType.Description,
            IsActive = journalType.IsActive,
            SubTypes = new List<JournalSubTypeDto>()
        };
    }

    public async Task<JournalTypeDto?> UpdateJournalTypeAsync(int journalTypeId, UpdateJournalTypeRequest request)
    {
        var journalType = await _journalLogRepository.GetJournalTypeByIdAsync(journalTypeId);
        if (journalType == null) return null;

        if (request.JournalTypeName != null) journalType.JournalTypeName = request.JournalTypeName;
        if (request.Description != null) journalType.Description = request.Description;
        if (request.IsActive.HasValue) journalType.IsActive = request.IsActive.Value;

        journalType = await _journalLogRepository.UpdateJournalTypeAsync(journalType);
        journalType = await _journalLogRepository.GetJournalTypeByIdAsync(journalType.JournalTypeId);

        return new JournalTypeDto
        {
            JournalTypeId = journalType.JournalTypeId,
            JournalTypeName = journalType.JournalTypeName ?? string.Empty,
            Description = journalType.Description,
            IsActive = journalType.IsActive,
            SubTypes = journalType.JournalSubTypes != null && journalType.JournalSubTypes.Any()
                ? journalType.JournalSubTypes.Select(jst => new JournalSubTypeDto
                {
                    JournalSubTypeId = jst.JournalSubTypeId,
                    JournalSubTypeName = jst.JournalSubTypeName ?? string.Empty,
                    Description = jst.Description,
                    IsActive = jst.IsActive
                }).ToList()
                : new List<JournalSubTypeDto>()
        };
    }

    public async Task<bool> DeleteJournalTypeAsync(int journalTypeId)
    {
        return await _journalLogRepository.DeleteJournalTypeAsync(journalTypeId);
    }

    public async Task<JournalSubTypeDto> CreateJournalSubTypeAsync(CreateJournalSubTypeRequest request)
    {
        var journalSubType = new JournalSubType
        {
            JournalTypeId = request.JournalTypeId,
            JournalSubTypeName = request.JournalSubTypeName,
            Description = request.Description,
            IsActive = request.IsActive ?? true
        };

        journalSubType = await _journalLogRepository.CreateJournalSubTypeAsync(journalSubType);
        journalSubType = await _journalLogRepository.GetJournalSubTypeByIdAsync(journalSubType.JournalSubTypeId);

        return new JournalSubTypeDto
        {
            JournalSubTypeId = journalSubType.JournalSubTypeId,
            JournalSubTypeName = journalSubType.JournalSubTypeName ?? string.Empty,
            Description = journalSubType.Description,
            IsActive = journalSubType.IsActive
        };
    }

    public async Task<JournalSubTypeDto?> UpdateJournalSubTypeAsync(int journalSubTypeId, UpdateJournalSubTypeRequest request)
    {
        var journalSubType = await _journalLogRepository.GetJournalSubTypeByIdAsync(journalSubTypeId);
        if (journalSubType == null) return null;

        if (request.JournalSubTypeName != null) journalSubType.JournalSubTypeName = request.JournalSubTypeName;
        if (request.Description != null) journalSubType.Description = request.Description;
        if (request.IsActive.HasValue) journalSubType.IsActive = request.IsActive.Value;

        journalSubType = await _journalLogRepository.UpdateJournalSubTypeAsync(journalSubType);
        journalSubType = await _journalLogRepository.GetJournalSubTypeByIdAsync(journalSubType.JournalSubTypeId);

        return new JournalSubTypeDto
        {
            JournalSubTypeId = journalSubType.JournalSubTypeId,
            JournalSubTypeName = journalSubType.JournalSubTypeName ?? string.Empty,
            Description = journalSubType.Description,
            IsActive = journalSubType.IsActive
        };
    }

    public async Task<bool> DeleteJournalSubTypeAsync(int journalSubTypeId)
    {
        return await _journalLogRepository.DeleteJournalSubTypeAsync(journalSubTypeId);
    }

    public async Task<AttachmentDto> AddAttachmentAsync(int journalLogId, IFormFile file, int createdByUserId)
    {
        var journalLog = await _journalLogRepository.GetByIdAsync(journalLogId);
        if (journalLog == null)
            throw new InvalidOperationException("Journal log not found");

        var uploadsFolder = Path.Combine(_environment.ContentRootPath, "uploads", "journals");
        Directory.CreateDirectory(uploadsFolder);

        var attachment = new JournalLogAttachment
        {
            JournalLogId = journalLogId,
            // Persist the original file name in attachedBy so we can display and resolve it later.
            AttachedBy = file.FileName
        };

        attachment = await _journalLogRepository.AddAttachmentAsync(attachment);

        var safeName = SanitizeFileName(file.FileName);
        var fileName = $"{attachment.JournalLogAttachmentId}_{safeName}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return new AttachmentDto
        {
            AttachmentId = attachment.JournalLogAttachmentId,
            FileName = file.FileName,
            FileType = file.ContentType,
            FileSize = file.Length,
            CreatedDate = DateTime.UtcNow
        };
    }

    public async Task<bool> DeleteAttachmentAsync(int attachmentId, int deletedByUserId)
    {
        var deleted = await _journalLogRepository.DeleteAttachmentAsync(attachmentId);
        if (!deleted) return false;

        var uploadsFolder = Path.Combine(_environment.ContentRootPath, "uploads", "journals");
        DeleteAttachmentFilesByPrefix(uploadsFolder, attachmentId);
        return true;
    }

    public async Task<AttachmentDownloadDto?> GetAttachmentDownloadAsync(int attachmentId)
    {
        var attachment = await _journalLogRepository.GetAttachmentByIdAsync(attachmentId);
        if (attachment == null) return null;

        var uploadsFolder = Path.Combine(_environment.ContentRootPath, "uploads", "journals");
        if (!Directory.Exists(uploadsFolder))
        {
            return null;
        }

        var prefix = $"{attachmentId}_";
        var filePath = Directory
            .EnumerateFiles(uploadsFolder, $"{prefix}*")
            .OrderByDescending(File.GetCreationTimeUtc)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return null;
        }

        var fileName = Path.GetFileName(filePath);
        if (fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            fileName = fileName[prefix.Length..];
        }

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(fileName, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        return new AttachmentDownloadDto
        {
            FilePath = filePath,
            FileName = fileName,
            ContentType = contentType
        };
    }

    public async Task<DeleteImpactResponseDto?> GetDeleteImpactAsync(int journalLogId)
    {
        var journalLog = await _journalLogRepository.GetByIdAsync(journalLogId);
        if (journalLog == null) return null;

        var attachmentCount = await _journalLogRepository.CountAttachmentsAsync(journalLogId);
        var tagCount = await _journalLogRepository.CountTagsAsync(journalLogId);
        var calendar = await _calendarAppointmentRepository.GetBySourceAsync(SourceType, journalLogId);

        return new DeleteImpactResponseDto
        {
            EntityId = journalLogId,
            AttachmentCount = attachmentCount,
            TagCount = tagCount,
            CalendarAppointmentCount = calendar == null ? 0 : 1
        };
    }

    private JournalLogResponseDto MapToJournalLogResponseDto(JournalLog journalLog)
    {
        return new JournalLogResponseDto
        {
            JournalLogId = journalLog.JournalLogId,
            PropertyId = journalLog.PropertyId ?? 0,
            PropertyName = journalLog.Property?.PropertyName ?? string.Empty,
            PropertyGroupId = journalLog.PropertyGroupId ?? journalLog.Property?.PropertyGroupId,
            PropertyGroupName = journalLog.PropertyGroup?.PropertyGroupName ?? journalLog.Property?.PropertyGroup?.PropertyGroupName,
            TenancyId = journalLog.TenancyId,
            TenantId = journalLog.TenantId,
            TenantName = journalLog.Tenant != null ? $"{journalLog.Tenant.FirstName} {journalLog.Tenant.LastName}".Trim() : null,
            JournalTypeId = journalLog.JournalTypeId ?? 0,
            JournalTypeName = journalLog.JournalType?.JournalTypeName ?? string.Empty,
            JournalSubTypeId = journalLog.JournalSubTypeId,
            JournalSubTypeName = journalLog.JournalSubType?.JournalSubTypeName,
            Amount = journalLog.Amount ?? 0,
            Description = journalLog.Description,
            TransactionDate = journalLog.TransactionDate ?? DateTime.UtcNow,
            CreatedDate = DateTime.UtcNow,
            Attachments = journalLog.Attachments.Select(a => new AttachmentDto
            {
                AttachmentId = a.JournalLogAttachmentId,
                FileName = ResolveAttachmentDisplayName(a),
                FileType = a.FileType,
                FileSize = a.FileSize ?? 0,
                CreatedDate = a.DateAttached ?? DateTime.UtcNow
            }).ToList()
        };
    }

    private static string ResolveAttachmentDisplayName(JournalLogAttachment attachment)
    {
        if (!string.IsNullOrWhiteSpace(attachment.FileName))
        {
            return attachment.FileName;
        }

        var attachedBy = attachment.AttachedBy?.Trim();
        if (string.IsNullOrWhiteSpace(attachedBy))
        {
            return "Unknown";
        }

        var openParen = attachedBy.LastIndexOf('(');
        var closeParen = attachedBy.LastIndexOf(')');
        if (openParen >= 0 && closeParen > openParen)
        {
            var inner = attachedBy[(openParen + 1)..closeParen].Trim();
            if (!string.IsNullOrWhiteSpace(inner))
            {
                return inner;
            }
        }

        return attachedBy;
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(fileName.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "attachment.bin" : cleaned;
    }

    private static void DeleteAttachmentFilesByPrefix(string folder, int attachmentId)
    {
        if (!Directory.Exists(folder))
        {
            return;
        }

        var prefix = $"{attachmentId}_";
        foreach (var file in Directory.EnumerateFiles(folder, $"{prefix}*"))
        {
            try
            {
                File.Delete(file);
            }
            catch
            {
                // Best-effort file cleanup; DB row already removed.
            }
        }
    }

    private async Task AttachCalendarLinksAsync(List<JournalLogResponseDto> rows)
    {
        if (rows.Count == 0) return;

        var appointmentMap = await _calendarAppointmentRepository.GetBySourceIdsAsync(
            SourceType,
            rows.Select(x => x.JournalLogId));

        foreach (var row in rows)
        {
            if (appointmentMap.TryGetValue(row.JournalLogId, out var appointment) && appointment.IsActive)
            {
                row.HasCalendarAppointment = true;
                row.CalendarDate = appointment.AppointmentDate;
            }
            else
            {
                row.HasCalendarAppointment = false;
                row.CalendarDate = null;
            }
        }
    }

    private async Task SyncCalendarAppointmentAsync(int journalLogId, bool addToCalendar, DateTime? calendarDate)
    {
        if (!addToCalendar)
        {
            await _calendarAppointmentRepository.DeleteBySourceAsync(SourceType, journalLogId);
            return;
        }

        if (!calendarDate.HasValue)
            throw new InvalidOperationException("Calendar date is required when adding this journal log to calendar.");

        await _calendarAppointmentRepository.UpsertAsync(
            SourceType,
            journalLogId,
            calendarDate.Value.Date,
            isAllDay: true);
    }
}
