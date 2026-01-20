using backend.DTOs;
using backend.Models;
using backend.Repositories;

namespace backend.Services;

public class JournalLogService : IJournalLogService
{
    private readonly IJournalLogRepository _journalLogRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IWebHostEnvironment _environment;

    public JournalLogService(
        IJournalLogRepository journalLogRepository,
        IAuditLogRepository auditLogRepository,
        IWebHostEnvironment environment)
    {
        _journalLogRepository = journalLogRepository;
        _auditLogRepository = auditLogRepository;
        _environment = environment;
    }

    public async Task<List<JournalLogResponseDto>> GetAllJournalLogsAsync()
    {
        var journalLogs = await _journalLogRepository.GetAllAsync();
        return journalLogs.Select(MapToJournalLogResponseDto).ToList();
    }

    public async Task<JournalLogResponseDto?> GetJournalLogByIdAsync(int journalLogId)
    {
        var journalLog = await _journalLogRepository.GetByIdAsync(journalLogId);
        if (journalLog == null) return null;

        return MapToJournalLogResponseDto(journalLog);
    }

    public async Task<List<JournalLogResponseDto>> GetJournalLogsByPropertyIdAsync(int propertyId)
    {
        var journalLogs = await _journalLogRepository.GetByPropertyIdAsync(propertyId);
        return journalLogs.Select(MapToJournalLogResponseDto).ToList();
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

        return MapToJournalLogResponseDto(journalLog!);
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

        return MapToJournalLogResponseDto(journalLog);
    }

    public async Task<bool> DeleteJournalLogAsync(int journalLogId, int deletedByUserId)
    {
        var journalLog = await _journalLogRepository.GetByIdAsync(journalLogId);
        if (journalLog == null) return false;

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
            SubTypes = jt.JournalSubTypes != null && jt.JournalSubTypes.Any()
                ? jt.JournalSubTypes.Select(jst => new JournalSubTypeDto
                {
                    JournalSubTypeId = jst.JournalSubTypeId,
                    JournalSubTypeName = jst.JournalSubTypeName ?? string.Empty,
                    Description = jst.Description
                }).ToList()
                : new List<JournalSubTypeDto>()
        }).ToList();
    }

    public async Task<AttachmentDto> AddAttachmentAsync(int journalLogId, IFormFile file, int createdByUserId)
    {
        var journalLog = await _journalLogRepository.GetByIdAsync(journalLogId);
        if (journalLog == null)
            throw new InvalidOperationException("Journal log not found");

        var uploadsFolder = Path.Combine(_environment.ContentRootPath, "uploads", "journals");
        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var attachment = new JournalLogAttachment
        {
            JournalLogId = journalLogId,
            FileName = file.FileName,
            FilePath = filePath,
            FileType = file.ContentType,
            FileSize = file.Length
        };

        attachment = await _journalLogRepository.AddAttachmentAsync(attachment);

        return new AttachmentDto
        {
            AttachmentId = attachment.JournalLogAttachmentId,
            FileName = attachment.FileName,
            FileType = attachment.FileType,
            FileSize = attachment.FileSize ?? 0,
            CreatedDate = DateTime.UtcNow
        };
    }

    public async Task<bool> DeleteAttachmentAsync(int attachmentId, int deletedByUserId)
    {
        // Note: File deletion from disk should be handled here as well
        return await _journalLogRepository.DeleteAttachmentAsync(attachmentId);
    }

    private JournalLogResponseDto MapToJournalLogResponseDto(JournalLog journalLog)
    {
        return new JournalLogResponseDto
        {
            JournalLogId = journalLog.JournalLogId,
            PropertyId = journalLog.PropertyId ?? 0,
            PropertyName = journalLog.Property?.PropertyName ?? string.Empty,
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
            // Temporarily disabled - load attachments separately if needed
            Attachments = new List<AttachmentDto>()
            // Attachments = journalLog.Attachments.Select(a => new AttachmentDto
            // {
            //     AttachmentId = a.JournalLogAttachmentId,
            //     FileName = a.FileName,
            //     FileType = a.FileType,
            //     FileSize = a.FileSize ?? 0,
            //     CreatedDate = DateTime.UtcNow
            // }).ToList()
        };
    }
}
