using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class TagRepository : ITagRepository
{
    private readonly ApplicationDbContext _context;

    public TagRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<TagType>> GetAllTagTypesAsync()
    {
        return await _context.TagTypes.ToListAsync();
    }

    public async Task<TagType?> GetTagTypeByIdAsync(int tagTypeId)
    {
        return await _context.TagTypes.FindAsync(tagTypeId);
    }

    public async Task<TagType> CreateTagTypeAsync(TagType tagType)
    {
        _context.TagTypes.Add(tagType);
        await _context.SaveChangesAsync();
        return tagType;
    }

    public async Task<TagType> UpdateTagTypeAsync(TagType tagType)
    {
        _context.TagTypes.Update(tagType);
        await _context.SaveChangesAsync();
        return tagType;
    }

    public async Task<bool> DeleteTagTypeAsync(int tagTypeId)
    {
        var tagType = await _context.TagTypes.FindAsync(tagTypeId);
        if (tagType == null) return false;

        _context.TagTypes.Remove(tagType);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<TagLog> CreateTagLogAsync(TagLog tagLog)
    {
        _context.TagLogs.Add(tagLog);
        await _context.SaveChangesAsync();
        return tagLog;
    }

    public async Task<List<TagLog>> GetTagLogsByEntityAsync(string entityType, int entityId)
    {
        return await _context.TagLogs
            .Include(tl => tl.TagType)
            .Where(tl => tl.EntityType == entityType && tl.EntityId == entityId)
            .ToListAsync();
    }

    public async Task<bool> DeleteTagLogAsync(int tagLogId)
    {
        var tagLog = await _context.TagLogs.FindAsync(tagLogId);
        if (tagLog == null) return false;

        _context.TagLogs.Remove(tagLog);
        await _context.SaveChangesAsync();
        return true;
    }
}
