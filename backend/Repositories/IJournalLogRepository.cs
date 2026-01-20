using backend.Models;

namespace backend.Repositories;

public interface IJournalLogRepository
{
    Task<List<JournalLog>> GetAllAsync();
    Task<JournalLog?> GetByIdAsync(int journalLogId);
    Task<List<JournalLog>> GetByPropertyIdAsync(int propertyId);
    Task<JournalLog> CreateAsync(JournalLog journalLog);
    Task<JournalLog> UpdateAsync(JournalLog journalLog);
    Task<bool> DeleteAsync(int journalLogId);
    
    Task<List<JournalType>> GetAllJournalTypesAsync();
    Task<JournalType?> GetJournalTypeByIdAsync(int journalTypeId);
    Task<JournalType> CreateJournalTypeAsync(JournalType journalType);
    Task<JournalType> UpdateJournalTypeAsync(JournalType journalType);
    Task<bool> DeleteJournalTypeAsync(int journalTypeId);
    Task<List<JournalSubType>> GetJournalSubTypesByTypeAsync(int journalTypeId);
    Task<JournalSubType?> GetJournalSubTypeByIdAsync(int journalSubTypeId);
    Task<JournalSubType> CreateJournalSubTypeAsync(JournalSubType journalSubType);
    Task<JournalSubType> UpdateJournalSubTypeAsync(JournalSubType journalSubType);
    Task<bool> DeleteJournalSubTypeAsync(int journalSubTypeId);
    
    Task<JournalLogAttachment> AddAttachmentAsync(JournalLogAttachment attachment);
    Task<List<JournalLogAttachment>> GetAttachmentsByJournalLogIdAsync(int journalLogId);
    Task<bool> DeleteAttachmentAsync(int attachmentId);
}
