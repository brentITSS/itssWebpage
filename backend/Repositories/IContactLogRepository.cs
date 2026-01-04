using backend.Models;

namespace backend.Repositories;

public interface IContactLogRepository
{
    Task<List<ContactLog>> GetAllAsync();
    Task<ContactLog?> GetByIdAsync(int contactLogId);
    Task<List<ContactLog>> GetByPropertyIdAsync(int propertyId);
    Task<List<ContactLog>> GetByTenantIdAsync(int tenantId);
    Task<ContactLog> CreateAsync(ContactLog contactLog);
    Task<ContactLog> UpdateAsync(ContactLog contactLog);
    Task<bool> DeleteAsync(int contactLogId);
    
    Task<List<ContactLogType>> GetAllContactLogTypesAsync();
    Task<ContactLogType?> GetContactLogTypeByIdAsync(int contactLogTypeId);
    
    Task<ContactLogAttachment> AddAttachmentAsync(ContactLogAttachment attachment);
    Task<List<ContactLogAttachment>> GetAttachmentsByContactLogIdAsync(int contactLogId);
    Task<bool> DeleteAttachmentAsync(int attachmentId);
}
