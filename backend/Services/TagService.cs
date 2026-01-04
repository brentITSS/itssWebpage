using backend.DTOs;
using backend.Models;
using backend.Repositories;

namespace backend.Services;

public class TagService : ITagService
{
    private readonly ITagRepository _tagRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public TagService(ITagRepository tagRepository, IAuditLogRepository auditLogRepository)
    {
        _tagRepository = tagRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<List<TagTypeResponseDto>> GetAllTagTypesAsync()
    {
        var tagTypes = await _tagRepository.GetAllTagTypesAsync();
        return tagTypes.Select(MapToTagTypeResponseDto).ToList();
    }

    public async Task<TagTypeResponseDto?> GetTagTypeByIdAsync(int tagTypeId)
    {
        var tagType = await _tagRepository.GetTagTypeByIdAsync(tagTypeId);
        if (tagType == null) return null;

        return MapToTagTypeResponseDto(tagType);
    }

    public async Task<TagTypeResponseDto> CreateTagTypeAsync(CreateTagTypeRequest request)
    {
        var tagType = new TagType
        {
            TagTypeName = request.TagTypeName,
            Color = request.Color,
            Description = request.Description,
            CreatedDate = DateTime.UtcNow
        };

        tagType = await _tagRepository.CreateTagTypeAsync(tagType);
        return MapToTagTypeResponseDto(tagType);
    }

    public async Task<TagTypeResponseDto?> UpdateTagTypeAsync(int tagTypeId, UpdateTagTypeRequest request)
    {
        var tagType = await _tagRepository.GetTagTypeByIdAsync(tagTypeId);
        if (tagType == null) return null;

        if (request.TagTypeName != null) tagType.TagTypeName = request.TagTypeName;
        if (request.Color != null) tagType.Color = request.Color;
        if (request.Description != null) tagType.Description = request.Description;

        tagType = await _tagRepository.UpdateTagTypeAsync(tagType);
        return MapToTagTypeResponseDto(tagType);
    }

    public async Task<bool> DeleteTagTypeAsync(int tagTypeId)
    {
        return await _tagRepository.DeleteTagTypeAsync(tagTypeId);
    }

    public async Task<TagDto> CreateTagLogAsync(CreateTagLogRequest request, int createdByUserId)
    {
        var tagType = await _tagRepository.GetTagTypeByIdAsync(request.TagTypeId);
        if (tagType == null)
            throw new InvalidOperationException("Tag type not found");

        var tagLog = new TagLog
        {
            TagTypeId = request.TagTypeId,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            CreatedDate = DateTime.UtcNow,
            CreatedByUserId = createdByUserId
        };

        // Set the appropriate foreign key based on entity type
        switch (request.EntityType.ToLower())
        {
            case "property":
                tagLog.PropertyId = request.EntityId;
                break;
            case "propertygroup":
                tagLog.PropertyGroupId = request.EntityId;
                break;
            case "tenant":
                tagLog.TenantId = request.EntityId;
                break;
            case "contactlog":
                tagLog.ContactLogId = request.EntityId;
                break;
            case "journallog":
                // JournalLog doesn't have a direct FK in TagLog, using EntityType/EntityId only
                break;
        }

        tagLog = await _tagRepository.CreateTagLogAsync(tagLog);

        return new TagDto
        {
            TagLogId = tagLog.TagLogId,
            TagTypeId = tagLog.TagTypeId,
            TagTypeName = tagType.TagTypeName,
            Color = tagType.Color,
            EntityType = tagLog.EntityType ?? request.EntityType,
            EntityId = tagLog.EntityId ?? request.EntityId,
            CreatedDate = tagLog.CreatedDate
        };
    }

    public async Task<List<TagDto>> GetTagLogsByEntityAsync(string entityType, int entityId)
    {
        var tagLogs = await _tagRepository.GetTagLogsByEntityAsync(entityType, entityId);
        return tagLogs.Select(tl => new TagDto
        {
            TagLogId = tl.TagLogId,
            TagTypeId = tl.TagTypeId,
            TagTypeName = tl.TagType.TagTypeName,
            Color = tl.TagType.Color,
            EntityType = tl.EntityType ?? entityType,
            EntityId = tl.EntityId ?? entityId,
            CreatedDate = tl.CreatedDate
        }).ToList();
    }

    public async Task<bool> DeleteTagLogAsync(int tagLogId, int deletedByUserId)
    {
        var result = await _tagRepository.DeleteTagLogAsync(tagLogId);

        if (result)
        {
            await _auditLogRepository.CreateAsync(new AuditLog
            {
                UserId = deletedByUserId,
                Action = "Delete",
                EntityType = "TagLog",
                EntityId = tagLogId,
                CreatedDate = DateTime.UtcNow
            });
        }

        return result;
    }

    private TagTypeResponseDto MapToTagTypeResponseDto(TagType tagType)
    {
        return new TagTypeResponseDto
        {
            TagTypeId = tagType.TagTypeId,
            TagTypeName = tagType.TagTypeName,
            Color = tagType.Color,
            Description = tagType.Description,
            CreatedDate = tagType.CreatedDate
        };
    }
}
