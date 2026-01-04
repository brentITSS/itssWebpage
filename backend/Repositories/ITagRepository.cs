using backend.Models;

namespace backend.Repositories;

public interface ITagRepository
{
    Task<List<TagType>> GetAllTagTypesAsync();
    Task<TagType?> GetTagTypeByIdAsync(int tagTypeId);
    Task<TagType> CreateTagTypeAsync(TagType tagType);
    Task<TagType> UpdateTagTypeAsync(TagType tagType);
    Task<bool> DeleteTagTypeAsync(int tagTypeId);
    
    Task<TagLog> CreateTagLogAsync(TagLog tagLog);
    Task<List<TagLog>> GetTagLogsByEntityAsync(string entityType, int entityId);
    Task<bool> DeleteTagLogAsync(int tagLogId);
}
