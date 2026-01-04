using backend.DTOs;

namespace backend.Services;

public interface ITagService
{
    Task<List<TagTypeResponseDto>> GetAllTagTypesAsync();
    Task<TagTypeResponseDto?> GetTagTypeByIdAsync(int tagTypeId);
    Task<TagTypeResponseDto> CreateTagTypeAsync(CreateTagTypeRequest request);
    Task<TagTypeResponseDto?> UpdateTagTypeAsync(int tagTypeId, UpdateTagTypeRequest request);
    Task<bool> DeleteTagTypeAsync(int tagTypeId);
    
    Task<TagDto> CreateTagLogAsync(CreateTagLogRequest request, int createdByUserId);
    Task<List<TagDto>> GetTagLogsByEntityAsync(string entityType, int entityId);
    Task<bool> DeleteTagLogAsync(int tagLogId, int deletedByUserId);
}
