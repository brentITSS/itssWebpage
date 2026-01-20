using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class ContactLogRepository : IContactLogRepository
{
    private readonly ApplicationDbContext _context;

    public ContactLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ContactLog>> GetAllAsync()
    {
        return await _context.ContactLogs
            .Include(c => c.Property)
                .ThenInclude(p => p.PropertyGroup)
            .Include(c => c.Tenant)
            .Include(c => c.ContactLogType)
            .Include(c => c.Attachments)
            .Include(c => c.TagLogs)
                .ThenInclude(tl => tl.TagType)
            .OrderByDescending(c => c.ContactDate)
            .ToListAsync();
    }

    public async Task<ContactLog?> GetByIdAsync(int contactLogId)
    {
        return await _context.ContactLogs
            .Include(c => c.Property)
                .ThenInclude(p => p.PropertyGroup)
            .Include(c => c.Tenant)
            .Include(c => c.ContactLogType)
            .Include(c => c.Attachments)
            .Include(c => c.TagLogs)
                .ThenInclude(tl => tl.TagType)
            .FirstOrDefaultAsync(c => c.ContactLogId == contactLogId);
    }

    public async Task<List<ContactLog>> GetByPropertyIdAsync(int propertyId)
    {
        return await _context.ContactLogs
            .Include(c => c.Property)
            .Include(c => c.Tenant)
            .Include(c => c.ContactLogType)
            .Include(c => c.Attachments)
            .Include(c => c.TagLogs)
                .ThenInclude(tl => tl.TagType)
            .Where(c => c.PropertyId == propertyId)
            .OrderByDescending(c => c.ContactDate)
            .ToListAsync();
    }

    public async Task<List<ContactLog>> GetByTenantIdAsync(int tenantId)
    {
        return await _context.ContactLogs
            .Include(c => c.Property)
            .Include(c => c.Tenant)
            .Include(c => c.ContactLogType)
            .Include(c => c.Attachments)
            .Include(c => c.TagLogs)
                .ThenInclude(tl => tl.TagType)
            .Where(c => c.TenantId == tenantId)
            .OrderByDescending(c => c.ContactDate)
            .ToListAsync();
    }

    public async Task<ContactLog> CreateAsync(ContactLog contactLog)
    {
        _context.ContactLogs.Add(contactLog);
        await _context.SaveChangesAsync();
        return contactLog;
    }

    public async Task<ContactLog> UpdateAsync(ContactLog contactLog)
    {
        _context.ContactLogs.Update(contactLog);
        await _context.SaveChangesAsync();
        return contactLog;
    }

    public async Task<bool> DeleteAsync(int contactLogId)
    {
        var contactLog = await _context.ContactLogs.FindAsync(contactLogId);
        if (contactLog == null) return false;

        _context.ContactLogs.Remove(contactLog);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<ContactLogType>> GetAllContactLogTypesAsync()
    {
        return await _context.ContactLogTypes.ToListAsync();
    }

    public async Task<ContactLogType?> GetContactLogTypeByIdAsync(int contactLogTypeId)
    {
        return await _context.ContactLogTypes.FindAsync(contactLogTypeId);
    }

    public async Task<ContactLogType> CreateContactLogTypeAsync(ContactLogType contactLogType)
    {
        _context.ContactLogTypes.Add(contactLogType);
        await _context.SaveChangesAsync();
        return contactLogType;
    }

    public async Task<ContactLogType> UpdateContactLogTypeAsync(ContactLogType contactLogType)
    {
        _context.ContactLogTypes.Update(contactLogType);
        await _context.SaveChangesAsync();
        return contactLogType;
    }

    public async Task<bool> DeleteContactLogTypeAsync(int contactLogTypeId)
    {
        var contactLogType = await _context.ContactLogTypes.FindAsync(contactLogTypeId);
        if (contactLogType == null) return false;

        _context.ContactLogTypes.Remove(contactLogType);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ContactLogAttachment> AddAttachmentAsync(ContactLogAttachment attachment)
    {
        _context.ContactLogAttachments.Add(attachment);
        await _context.SaveChangesAsync();
        return attachment;
    }

    public async Task<List<ContactLogAttachment>> GetAttachmentsByContactLogIdAsync(int contactLogId)
    {
        return await _context.ContactLogAttachments
            .Where(a => a.ContactLogId == contactLogId)
            .ToListAsync();
    }

    public async Task<bool> DeleteAttachmentAsync(int attachmentId)
    {
        var attachment = await _context.ContactLogAttachments.FindAsync(attachmentId);
        if (attachment == null) return false;

        _context.ContactLogAttachments.Remove(attachment);
        await _context.SaveChangesAsync();
        return true;
    }
}
