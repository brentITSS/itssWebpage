using backend.DTOs;

namespace backend.Services;

public interface IContactLogService
{
    Task<List<ContactLogResponseDto>> GetAllContactLogsAsync();
    Task<List<ContactLogResponseDto>> GetAllContactLogsForUserAsync(int userId, bool isGlobalAdmin, bool isPropertyHubAdmin);
    Task<ContactLogResponseDto?> GetContactLogByIdAsync(int contactLogId);
    Task<List<ContactLogResponseDto>> GetContactLogsByPropertyIdAsync(int propertyId);
    Task<List<ContactLogResponseDto>> GetContactLogsByTenantIdAsync(int tenantId);
    Task<ContactLogResponseDto> CreateContactLogAsync(CreateContactLogRequest request, int createdByUserId);
    Task<ContactLogResponseDto?> UpdateContactLogAsync(int contactLogId, UpdateContactLogRequest request, int modifiedByUserId);
    Task<bool> DeleteContactLogAsync(int contactLogId, int deletedByUserId);
    
    Task<List<ContactLogTypeDto>> GetAllContactLogTypesAsync();
    Task<AttachmentDto> AddAttachmentAsync(int contactLogId, IFormFile file, int createdByUserId);
    Task<bool> DeleteAttachmentAsync(int attachmentId, int deletedByUserId);
}
