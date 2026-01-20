using backend.DTOs;

namespace backend.Services;

public interface IJournalLogService
{
    Task<List<JournalLogResponseDto>> GetAllJournalLogsAsync();
    Task<List<JournalLogResponseDto>> GetAllJournalLogsForUserAsync(int userId, bool isGlobalAdmin, bool isPropertyHubAdmin);
    Task<JournalLogResponseDto?> GetJournalLogByIdAsync(int journalLogId);
    Task<List<JournalLogResponseDto>> GetJournalLogsByPropertyIdAsync(int propertyId);
    Task<JournalLogResponseDto> CreateJournalLogAsync(CreateJournalLogRequest request, int createdByUserId);
    Task<JournalLogResponseDto?> UpdateJournalLogAsync(int journalLogId, UpdateJournalLogRequest request, int modifiedByUserId);
    Task<bool> DeleteJournalLogAsync(int journalLogId, int deletedByUserId);
    
    Task<List<JournalTypeDto>> GetAllJournalTypesAsync();
    Task<JournalTypeDto> CreateJournalTypeAsync(CreateJournalTypeRequest request);
    Task<JournalTypeDto?> UpdateJournalTypeAsync(int journalTypeId, UpdateJournalTypeRequest request);
    Task<bool> DeleteJournalTypeAsync(int journalTypeId);
    Task<AttachmentDto> AddAttachmentAsync(int journalLogId, IFormFile file, int createdByUserId);
    Task<bool> DeleteAttachmentAsync(int attachmentId, int deletedByUserId);
}
