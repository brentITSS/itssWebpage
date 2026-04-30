using backend.DTOs;
using backend.Models;
using backend.Repositories;
using Microsoft.AspNetCore.StaticFiles;

namespace backend.Services;

public class ContactLogService : IContactLogService
{
    private readonly IContactLogRepository _contactLogRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IPropertyRepository _propertyRepository;
    private readonly ICalendarAppointmentRepository _calendarAppointmentRepository;
    private readonly IWebHostEnvironment _environment;
    private const string SourceType = "contactlog";

    public ContactLogService(
        IContactLogRepository contactLogRepository,
        IAuditLogRepository auditLogRepository,
        IPropertyRepository propertyRepository,
        ICalendarAppointmentRepository calendarAppointmentRepository,
        IWebHostEnvironment environment)
    {
        _contactLogRepository = contactLogRepository;
        _auditLogRepository = auditLogRepository;
        _propertyRepository = propertyRepository;
        _calendarAppointmentRepository = calendarAppointmentRepository;
        _environment = environment;
    }

    public async Task<List<ContactLogResponseDto>> GetAllContactLogsAsync()
    {
        var contactLogs = await _contactLogRepository.GetAllAsync();
        var rows = contactLogs.Select(MapToContactLogResponseDto).ToList();
        await AttachCalendarLinksAsync(rows);
        return rows;
    }

    public async Task<List<ContactLogResponseDto>> GetAllContactLogsForUserAsync(int userId, bool isGlobalAdmin, bool isPropertyHubAdmin)
    {
        var allContactLogs = await _contactLogRepository.GetAllAsync();
        
        // Global Admins and Property Hub Admins see all contact logs
        if (isGlobalAdmin || isPropertyHubAdmin)
        {
            var allRows = allContactLogs.Select(MapToContactLogResponseDto).ToList();
            await AttachCalendarLinksAsync(allRows);
            return allRows;
        }

        // Regular users: get their assigned property group IDs
        var userPropertyGroupIds = await _propertyRepository.GetUserPropertyGroupIdsAsync(userId);
        
        // If user has no specific assignments, show all (backward compatible)
        if (userPropertyGroupIds.Count == 0)
        {
            var allRows = allContactLogs.Select(MapToContactLogResponseDto).ToList();
            await AttachCalendarLinksAsync(allRows);
            return allRows;
        }

        // Get all properties in user's accessible property groups
        var allProperties = await _propertyRepository.GetAllPropertiesAsync();
        var accessiblePropertyIds = allProperties
            .Where(p => p.PropertyGroupId.HasValue && userPropertyGroupIds.Contains(p.PropertyGroupId.Value))
            .Select(p => p.PropertyId)
            .ToList();

        // Filter contact logs to only those for accessible properties
        var rows = allContactLogs
            .Where(cl => cl.PropertyId.HasValue && accessiblePropertyIds.Contains(cl.PropertyId.Value))
            .Select(MapToContactLogResponseDto)
            .ToList();
        await AttachCalendarLinksAsync(rows);
        return rows;
    }

    public async Task<ContactLogResponseDto?> GetContactLogByIdAsync(int contactLogId)
    {
        var contactLog = await _contactLogRepository.GetByIdAsync(contactLogId);
        if (contactLog == null) return null;

        var dto = MapToContactLogResponseDto(contactLog);
        await AttachCalendarLinksAsync(new List<ContactLogResponseDto> { dto });
        return dto;
    }

    public async Task<List<ContactLogResponseDto>> GetContactLogsByPropertyIdAsync(int propertyId)
    {
        var contactLogs = await _contactLogRepository.GetByPropertyIdAsync(propertyId);
        var rows = contactLogs.Select(MapToContactLogResponseDto).ToList();
        await AttachCalendarLinksAsync(rows);
        return rows;
    }

    public async Task<List<ContactLogResponseDto>> GetContactLogsByTenantIdAsync(int tenantId)
    {
        var contactLogs = await _contactLogRepository.GetByTenantIdAsync(tenantId);
        var rows = contactLogs.Select(MapToContactLogResponseDto).ToList();
        await AttachCalendarLinksAsync(rows);
        return rows;
    }

    public async Task<ContactLogResponseDto> CreateContactLogAsync(CreateContactLogRequest request, int createdByUserId)
    {
        var contactLog = new ContactLog
        {
            PropertyId = request.PropertyId,
            TenantId = request.TenantId,
            ContactLogTypeId = request.ContactLogTypeId,
            Notes = request.Subject ?? request.Notes ?? string.Empty, // Store Subject in Notes
            ContactDate = request.ContactDate,
            ContactBy = "System" // TODO: Get from authenticated user
        };

        contactLog = await _contactLogRepository.CreateAsync(contactLog);
        await SyncCalendarAppointmentAsync(
            contactLog.ContactLogId,
            request.AddToCalendar,
            request.CalendarDate ?? request.ContactDate);
        contactLog = await _contactLogRepository.GetByIdAsync(contactLog.ContactLogId);

        // Audit log
        await _auditLogRepository.CreateAsync(new AuditLog
        {
            UserId = createdByUserId,
            Action = "Create",
            EntityType = "ContactLog",
            EntityId = contactLog.ContactLogId,
            NewValues = $"PropertyId: {contactLog.PropertyId}, Subject: {contactLog.Subject ?? "N/A"}",
            CreatedDate = DateTime.UtcNow
        });

        var dto = MapToContactLogResponseDto(contactLog!);
        await AttachCalendarLinksAsync(new List<ContactLogResponseDto> { dto });
        return dto;
    }

    public async Task<ContactLogResponseDto?> UpdateContactLogAsync(int contactLogId, UpdateContactLogRequest request, int modifiedByUserId)
    {
        var contactLog = await _contactLogRepository.GetByIdAsync(contactLogId);
        if (contactLog == null) return null;

        var oldValues = $"PropertyId: {contactLog.PropertyId}, Subject: {contactLog.Subject ?? "N/A"}";

        if (request.PropertyId.HasValue) contactLog.PropertyId = request.PropertyId.Value;
        if (request.TenantId.HasValue) contactLog.TenantId = request.TenantId;
        if (request.ContactLogTypeId.HasValue) contactLog.ContactLogTypeId = request.ContactLogTypeId.Value;
        if (request.Subject != null || request.Notes != null) 
        {
            contactLog.Notes = request.Subject ?? request.Notes ?? string.Empty;
        }
        if (request.ContactDate.HasValue) contactLog.ContactDate = request.ContactDate.Value;

        contactLog = await _contactLogRepository.UpdateAsync(contactLog);
        if (request.AddToCalendar.HasValue)
        {
            await SyncCalendarAppointmentAsync(
                contactLogId,
                request.AddToCalendar.Value,
                request.CalendarDate ?? request.ContactDate ?? contactLog.ContactDate);
        }
        contactLog = await _contactLogRepository.GetByIdAsync(contactLogId);

        // Audit log
        var newValues = $"PropertyId: {contactLog!.PropertyId}, Subject: {contactLog.Subject ?? "N/A"}";
        await _auditLogRepository.CreateAsync(new AuditLog
        {
            UserId = modifiedByUserId,
            Action = "Update",
            EntityType = "ContactLog",
            EntityId = contactLogId,
            OldValues = oldValues,
            NewValues = newValues,
            CreatedDate = DateTime.UtcNow
        });

        var dto = MapToContactLogResponseDto(contactLog);
        await AttachCalendarLinksAsync(new List<ContactLogResponseDto> { dto });
        return dto;
    }

    public async Task<bool> DeleteContactLogAsync(int contactLogId, int deletedByUserId)
    {
        var contactLog = await _contactLogRepository.GetByIdAsync(contactLogId);
        if (contactLog == null) return false;

        var result = await _contactLogRepository.DeleteAsync(contactLogId);

        if (result)
        {
            await _calendarAppointmentRepository.DeleteBySourceAsync(SourceType, contactLogId);
            await _auditLogRepository.CreateAsync(new AuditLog
            {
                UserId = deletedByUserId,
                Action = "Delete",
                EntityType = "ContactLog",
                EntityId = contactLogId,
                OldValues = $"Subject: {contactLog.Subject ?? "N/A"}",
                CreatedDate = DateTime.UtcNow
            });
        }

        return result;
    }

    public async Task<List<ContactLogTypeDto>> GetAllContactLogTypesAsync()
    {
        var contactLogTypes = await _contactLogRepository.GetAllContactLogTypesAsync();
        return contactLogTypes.Select(clt => new ContactLogTypeDto
        {
            ContactLogTypeId = clt.ContactLogTypeId,
            ContactLogTypeName = clt.ContactLogTypeName ?? string.Empty,
            Description = clt.Description,
            IsActive = clt.IsActive
        }).ToList();
    }

    public async Task<ContactLogTypeDto> CreateContactLogTypeAsync(CreateContactLogTypeRequest request)
    {
        var contactLogType = new ContactLogType
        {
            ContactLogTypeName = request.ContactLogTypeName,
            Description = request.Description,
            IsActive = request.IsActive ?? true
        };

        contactLogType = await _contactLogRepository.CreateContactLogTypeAsync(contactLogType);
        contactLogType = await _contactLogRepository.GetContactLogTypeByIdAsync(contactLogType.ContactLogTypeId);

        return new ContactLogTypeDto
        {
            ContactLogTypeId = contactLogType.ContactLogTypeId,
            ContactLogTypeName = contactLogType.ContactLogTypeName ?? string.Empty,
            Description = contactLogType.Description,
            IsActive = contactLogType.IsActive
        };
    }

    public async Task<ContactLogTypeDto?> UpdateContactLogTypeAsync(int contactLogTypeId, UpdateContactLogTypeRequest request)
    {
        var contactLogType = await _contactLogRepository.GetContactLogTypeByIdAsync(contactLogTypeId);
        if (contactLogType == null) return null;

        if (request.ContactLogTypeName != null) contactLogType.ContactLogTypeName = request.ContactLogTypeName;
        if (request.Description != null) contactLogType.Description = request.Description;
        if (request.IsActive.HasValue) contactLogType.IsActive = request.IsActive.Value;

        contactLogType = await _contactLogRepository.UpdateContactLogTypeAsync(contactLogType);
        contactLogType = await _contactLogRepository.GetContactLogTypeByIdAsync(contactLogType.ContactLogTypeId);

        return new ContactLogTypeDto
        {
            ContactLogTypeId = contactLogType.ContactLogTypeId,
            ContactLogTypeName = contactLogType.ContactLogTypeName ?? string.Empty,
            Description = contactLogType.Description,
            IsActive = contactLogType.IsActive
        };
    }

    public async Task<bool> DeleteContactLogTypeAsync(int contactLogTypeId)
    {
        return await _contactLogRepository.DeleteContactLogTypeAsync(contactLogTypeId);
    }

    public async Task<AttachmentDto> AddAttachmentAsync(int contactLogId, IFormFile file, int createdByUserId)
    {
        var contactLog = await _contactLogRepository.GetByIdAsync(contactLogId);
        if (contactLog == null)
            throw new InvalidOperationException("Contact log not found");

        var uploadsFolder = Path.Combine(_environment.ContentRootPath, "uploads", "contacts");
        Directory.CreateDirectory(uploadsFolder);

        var attachment = new ContactLogAttachment
        {
            ContactLogId = contactLogId,
            Description = file.FileName
        };

        attachment = await _contactLogRepository.AddAttachmentAsync(attachment);

        var safeName = SanitizeFileName(file.FileName);
        var fileName = $"{attachment.ContactLogAttachmentId}_{safeName}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return new AttachmentDto
        {
            AttachmentId = attachment.ContactLogAttachmentId,
            FileName = attachment.Description ?? "Unknown",
            FileType = file.ContentType,
            FileSize = file.Length,
            CreatedDate = DateTime.UtcNow
        };
    }

    public async Task<bool> DeleteAttachmentAsync(int attachmentId, int deletedByUserId)
    {
        var deleted = await _contactLogRepository.DeleteAttachmentAsync(attachmentId);
        if (!deleted) return false;

        var uploadsFolder = Path.Combine(_environment.ContentRootPath, "uploads", "contacts");
        DeleteAttachmentFilesByPrefix(uploadsFolder, attachmentId);
        return true;
    }

    public async Task<AttachmentDownloadDto?> GetAttachmentDownloadAsync(int attachmentId)
    {
        var attachment = await _contactLogRepository.GetAttachmentByIdAsync(attachmentId);
        if (attachment == null) return null;

        var uploadsFolder = Path.Combine(_environment.ContentRootPath, "uploads", "contacts");
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

    public async Task<DeleteImpactResponseDto?> GetDeleteImpactAsync(int contactLogId)
    {
        var contactLog = await _contactLogRepository.GetByIdAsync(contactLogId);
        if (contactLog == null) return null;

        var attachmentCount = await _contactLogRepository.CountAttachmentsAsync(contactLogId);
        var tagCount = await _contactLogRepository.CountTagsAsync(contactLogId);
        var calendar = await _calendarAppointmentRepository.GetBySourceAsync(SourceType, contactLogId);

        return new DeleteImpactResponseDto
        {
            EntityId = contactLogId,
            AttachmentCount = attachmentCount,
            TagCount = tagCount,
            CalendarAppointmentCount = calendar == null ? 0 : 1
        };
    }

    private ContactLogResponseDto MapToContactLogResponseDto(ContactLog contactLog)
    {
        return new ContactLogResponseDto
        {
            ContactLogId = contactLog.ContactLogId,
            PropertyId = contactLog.PropertyId ?? 0,
            PropertyName = contactLog.Property?.PropertyName ?? string.Empty,
            PropertyGroupId = contactLog.Property?.PropertyGroupId,
            PropertyGroupName = contactLog.Property?.PropertyGroup?.PropertyGroupName,
            TenantId = contactLog.TenantId,
            TenantName = contactLog.Tenant != null ? $"{contactLog.Tenant.FirstName} {contactLog.Tenant.LastName}".Trim() : null,
            ContactLogTypeId = contactLog.ContactLogTypeId,
            ContactLogTypeName = contactLog.ContactLogType?.ContactLogTypeName ?? string.Empty,
            Subject = contactLog.Notes ?? string.Empty, // Subject is derived from Notes
            Notes = contactLog.Notes ?? string.Empty,
            ContactDate = contactLog.ContactDate,
            CreatedDate = DateTime.UtcNow,
            Attachments = contactLog.Attachments.Select(a => new AttachmentDto
            {
                AttachmentId = a.ContactLogAttachmentId,
                FileName = a.FileName ?? a.Description ?? "Unknown",
                FileType = a.FileType,
                FileSize = a.FileSize ?? 0,
                CreatedDate = DateTime.UtcNow
            }).ToList(),
            Tags = contactLog.TagLogs.Select(tl => new TagDto
            {
                TagLogId = tl.TagLogId,
                TagTypeId = tl.TagTypeId ?? 0,
                TagTypeName = tl.TagType?.TagTypeName ?? string.Empty,
                Color = null, // TagType doesn't have Color
                EntityType = tl.EntityType ?? "ContactLog",
                EntityId = tl.EntityId ?? contactLog.ContactLogId,
                CreatedDate = DateTime.UtcNow
            }).ToList()
        };
    }

    private async Task AttachCalendarLinksAsync(List<ContactLogResponseDto> rows)
    {
        if (rows.Count == 0) return;

        var appointmentMap = await _calendarAppointmentRepository.GetBySourceIdsAsync(
            SourceType,
            rows.Select(x => x.ContactLogId));

        foreach (var row in rows)
        {
            if (appointmentMap.TryGetValue(row.ContactLogId, out var appointment) && appointment.IsActive)
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

    private async Task SyncCalendarAppointmentAsync(int contactLogId, bool addToCalendar, DateTime? calendarDate)
    {
        if (!addToCalendar)
        {
            await _calendarAppointmentRepository.DeleteBySourceAsync(SourceType, contactLogId);
            return;
        }

        if (!calendarDate.HasValue)
            throw new InvalidOperationException("Calendar date is required when adding this contact log to calendar.");

        await _calendarAppointmentRepository.UpsertAsync(
            SourceType,
            contactLogId,
            calendarDate.Value.Date,
            isAllDay: true);
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
}
