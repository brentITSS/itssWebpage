using backend.DTOs;
using backend.Models;
using backend.Repositories;

namespace backend.Services;

public class ContactLogService : IContactLogService
{
    private readonly IContactLogRepository _contactLogRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IPropertyRepository _propertyRepository;
    private readonly IWebHostEnvironment _environment;

    public ContactLogService(
        IContactLogRepository contactLogRepository,
        IAuditLogRepository auditLogRepository,
        IPropertyRepository propertyRepository,
        IWebHostEnvironment environment)
    {
        _contactLogRepository = contactLogRepository;
        _auditLogRepository = auditLogRepository;
        _propertyRepository = propertyRepository;
        _environment = environment;
    }

    public async Task<List<ContactLogResponseDto>> GetAllContactLogsAsync()
    {
        var contactLogs = await _contactLogRepository.GetAllAsync();
        return contactLogs.Select(MapToContactLogResponseDto).ToList();
    }

    public async Task<List<ContactLogResponseDto>> GetAllContactLogsForUserAsync(int userId, bool isGlobalAdmin, bool isPropertyHubAdmin)
    {
        var allContactLogs = await _contactLogRepository.GetAllAsync();
        
        // Global Admins and Property Hub Admins see all contact logs
        if (isGlobalAdmin || isPropertyHubAdmin)
        {
            return allContactLogs.Select(MapToContactLogResponseDto).ToList();
        }

        // Regular users: get their assigned property group IDs
        var userPropertyGroupIds = await _propertyRepository.GetUserPropertyGroupIdsAsync(userId);
        
        // If user has no specific assignments, show all (backward compatible)
        if (userPropertyGroupIds.Count == 0)
        {
            return allContactLogs.Select(MapToContactLogResponseDto).ToList();
        }

        // Get all properties in user's accessible property groups
        var allProperties = await _propertyRepository.GetAllPropertiesAsync();
        var accessiblePropertyIds = allProperties
            .Where(p => p.PropertyGroupId.HasValue && userPropertyGroupIds.Contains(p.PropertyGroupId.Value))
            .Select(p => p.PropertyId)
            .ToList();

        // Filter contact logs to only those for accessible properties
        return allContactLogs
            .Where(cl => cl.PropertyId.HasValue && accessiblePropertyIds.Contains(cl.PropertyId.Value))
            .Select(MapToContactLogResponseDto)
            .ToList();
    }

    public async Task<ContactLogResponseDto?> GetContactLogByIdAsync(int contactLogId)
    {
        var contactLog = await _contactLogRepository.GetByIdAsync(contactLogId);
        if (contactLog == null) return null;

        return MapToContactLogResponseDto(contactLog);
    }

    public async Task<List<ContactLogResponseDto>> GetContactLogsByPropertyIdAsync(int propertyId)
    {
        var contactLogs = await _contactLogRepository.GetByPropertyIdAsync(propertyId);
        return contactLogs.Select(MapToContactLogResponseDto).ToList();
    }

    public async Task<List<ContactLogResponseDto>> GetContactLogsByTenantIdAsync(int tenantId)
    {
        var contactLogs = await _contactLogRepository.GetByTenantIdAsync(tenantId);
        return contactLogs.Select(MapToContactLogResponseDto).ToList();
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

        return MapToContactLogResponseDto(contactLog!);
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

        return MapToContactLogResponseDto(contactLog);
    }

    public async Task<bool> DeleteContactLogAsync(int contactLogId, int deletedByUserId)
    {
        var contactLog = await _contactLogRepository.GetByIdAsync(contactLogId);
        if (contactLog == null) return false;

        var result = await _contactLogRepository.DeleteAsync(contactLogId);

        if (result)
        {
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
            Description = clt.Description
        }).ToList();
    }

    public async Task<AttachmentDto> AddAttachmentAsync(int contactLogId, IFormFile file, int createdByUserId)
    {
        var contactLog = await _contactLogRepository.GetByIdAsync(contactLogId);
        if (contactLog == null)
            throw new InvalidOperationException("Contact log not found");

        var uploadsFolder = Path.Combine(_environment.ContentRootPath, "uploads", "contacts");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var attachment = new ContactLogAttachment
        {
            ContactLogId = contactLogId,
            Description = file.FileName, // Store filename in Description
            FilePath = filePath, // Store in computed property (not in DB)
            FileType = file.ContentType, // Store in computed property (not in DB)
            FileSize = file.Length // Store in computed property (not in DB)
        };

        attachment = await _contactLogRepository.AddAttachmentAsync(attachment);

        return new AttachmentDto
        {
            AttachmentId = attachment.ContactLogAttachmentId,
            FileName = attachment.FileName ?? attachment.Description ?? "Unknown",
            FileType = attachment.FileType,
            FileSize = attachment.FileSize ?? 0,
            CreatedDate = DateTime.UtcNow
        };
    }

    public async Task<bool> DeleteAttachmentAsync(int attachmentId, int deletedByUserId)
    {
        // Note: File deletion from disk should be handled here as well
        return await _contactLogRepository.DeleteAttachmentAsync(attachmentId);
    }

    private ContactLogResponseDto MapToContactLogResponseDto(ContactLog contactLog)
    {
        return new ContactLogResponseDto
        {
            ContactLogId = contactLog.ContactLogId,
            PropertyId = contactLog.PropertyId ?? 0,
            PropertyName = contactLog.Property?.PropertyName ?? string.Empty,
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
}
