using backend.DTOs;
using backend.Models;
using backend.Repositories;

namespace backend.Services;

public class ContactLogService : IContactLogService
{
    private readonly IContactLogRepository _contactLogRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IWebHostEnvironment _environment;

    public ContactLogService(
        IContactLogRepository contactLogRepository,
        IAuditLogRepository auditLogRepository,
        IWebHostEnvironment environment)
    {
        _contactLogRepository = contactLogRepository;
        _auditLogRepository = auditLogRepository;
        _environment = environment;
    }

    public async Task<List<ContactLogResponseDto>> GetAllContactLogsAsync()
    {
        var contactLogs = await _contactLogRepository.GetAllAsync();
        return contactLogs.Select(MapToContactLogResponseDto).ToList();
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
            Subject = request.Subject,
            Notes = request.Notes,
            ContactDate = request.ContactDate,
            CreatedDate = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
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
            NewValues = $"PropertyId: {contactLog.PropertyId}, Subject: {contactLog.Subject}",
            CreatedDate = DateTime.UtcNow
        });

        return MapToContactLogResponseDto(contactLog!);
    }

    public async Task<ContactLogResponseDto?> UpdateContactLogAsync(int contactLogId, UpdateContactLogRequest request, int modifiedByUserId)
    {
        var contactLog = await _contactLogRepository.GetByIdAsync(contactLogId);
        if (contactLog == null) return null;

        var oldValues = $"PropertyId: {contactLog.PropertyId}, Subject: {contactLog.Subject}";

        if (request.PropertyId.HasValue) contactLog.PropertyId = request.PropertyId.Value;
        if (request.TenantId.HasValue) contactLog.TenantId = request.TenantId;
        if (request.ContactLogTypeId.HasValue) contactLog.ContactLogTypeId = request.ContactLogTypeId.Value;
        if (request.Subject != null) contactLog.Subject = request.Subject;
        if (request.Notes != null) contactLog.Notes = request.Notes;
        if (request.ContactDate.HasValue) contactLog.ContactDate = request.ContactDate.Value;
        contactLog.ModifiedDate = DateTime.UtcNow;
        contactLog.ModifiedByUserId = modifiedByUserId;

        contactLog = await _contactLogRepository.UpdateAsync(contactLog);
        contactLog = await _contactLogRepository.GetByIdAsync(contactLogId);

        // Audit log
        var newValues = $"PropertyId: {contactLog!.PropertyId}, Subject: {contactLog.Subject}";
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
                OldValues = $"Subject: {contactLog.Subject}",
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
            ContactLogTypeName = clt.ContactLogTypeName,
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
            FileName = file.FileName,
            FilePath = filePath,
            FileType = file.ContentType,
            FileSize = file.Length,
            CreatedDate = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };

        attachment = await _contactLogRepository.AddAttachmentAsync(attachment);

        return new AttachmentDto
        {
            AttachmentId = attachment.ContactLogAttachmentId,
            FileName = attachment.FileName,
            FileType = attachment.FileType,
            FileSize = attachment.FileSize,
            CreatedDate = attachment.CreatedDate
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
            PropertyId = contactLog.PropertyId,
            PropertyName = contactLog.Property.PropertyName,
            TenantId = contactLog.TenantId,
            TenantName = contactLog.Tenant != null ? $"{contactLog.Tenant.FirstName} {contactLog.Tenant.LastName}".Trim() : null,
            ContactLogTypeId = contactLog.ContactLogTypeId,
            ContactLogTypeName = contactLog.ContactLogType.ContactLogTypeName,
            Subject = contactLog.Subject,
            Notes = contactLog.Notes,
            ContactDate = contactLog.ContactDate,
            CreatedDate = contactLog.CreatedDate,
            Attachments = contactLog.Attachments.Select(a => new AttachmentDto
            {
                AttachmentId = a.ContactLogAttachmentId,
                FileName = a.FileName,
                FileType = a.FileType,
                FileSize = a.FileSize,
                CreatedDate = a.CreatedDate
            }).ToList(),
            Tags = contactLog.TagLogs.Select(tl => new TagDto
            {
                TagLogId = tl.TagLogId,
                TagTypeId = tl.TagTypeId,
                TagTypeName = tl.TagType.TagTypeName,
                Color = tl.TagType.Color,
                EntityType = tl.EntityType ?? "ContactLog",
                EntityId = tl.EntityId ?? contactLog.ContactLogId,
                CreatedDate = tl.CreatedDate
            }).ToList()
        };
    }
}
