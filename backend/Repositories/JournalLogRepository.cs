using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class JournalLogRepository : IJournalLogRepository
{
    private readonly ApplicationDbContext _context;

    public JournalLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<JournalLog>> GetAllAsync()
    {
        return await _context.JournalLogs
            .Include(j => j.Property)
                .ThenInclude(p => p.PropertyGroup)
            .Include(j => j.Tenancy)
            .Include(j => j.Tenant)
            .Include(j => j.JournalType)
            .Include(j => j.JournalSubType)
            // Temporarily commented out until column mapping is confirmed
            // .Include(j => j.Attachments)
            .OrderByDescending(j => j.TransactionDate)
            .ToListAsync();
    }

    public async Task<JournalLog?> GetByIdAsync(int journalLogId)
    {
        return await _context.JournalLogs
            .Include(j => j.Property)
                .ThenInclude(p => p.PropertyGroup)
            .Include(j => j.Tenancy)
            .Include(j => j.Tenant)
            .Include(j => j.JournalType)
            .Include(j => j.JournalSubType)
            // Temporarily commented out until column mapping is confirmed
            // .Include(j => j.Attachments)
            .FirstOrDefaultAsync(j => j.JournalLogId == journalLogId);
    }

    public async Task<List<JournalLog>> GetByPropertyIdAsync(int propertyId)
    {
        return await _context.JournalLogs
            .Include(j => j.Property)
            .Include(j => j.Tenancy)
            .Include(j => j.Tenant)
            .Include(j => j.JournalType)
            .Include(j => j.JournalSubType)
            // Temporarily commented out until column mapping is confirmed
            // .Include(j => j.Attachments)
            .Where(j => j.PropertyId == propertyId)
            .OrderByDescending(j => j.TransactionDate)
            .ToListAsync();
    }

    public async Task<JournalLog> CreateAsync(JournalLog journalLog)
    {
        _context.JournalLogs.Add(journalLog);
        await _context.SaveChangesAsync();
        return journalLog;
    }

    public async Task<JournalLog> UpdateAsync(JournalLog journalLog)
    {
        _context.JournalLogs.Update(journalLog);
        await _context.SaveChangesAsync();
        return journalLog;
    }

    public async Task<bool> DeleteAsync(int journalLogId)
    {
        var journalLog = await _context.JournalLogs.FindAsync(journalLogId);
        if (journalLog == null) return false;

        _context.JournalLogs.Remove(journalLog);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<JournalType>> GetAllJournalTypesAsync()
    {
        return await _context.JournalTypes
            .Include(jt => jt.JournalSubTypes)
            .ToListAsync();
    }

    public async Task<JournalType?> GetJournalTypeByIdAsync(int journalTypeId)
    {
        return await _context.JournalTypes
            .Include(jt => jt.JournalSubTypes)
            .FirstOrDefaultAsync(jt => jt.JournalTypeId == journalTypeId);
    }

    public async Task<List<JournalSubType>> GetJournalSubTypesByTypeAsync(int journalTypeId)
    {
        return await _context.JournalSubTypes
            .Where(jst => jst.JournalTypeId == journalTypeId)
            .ToListAsync();
    }

    public async Task<JournalLogAttachment> AddAttachmentAsync(JournalLogAttachment attachment)
    {
        _context.JournalLogAttachments.Add(attachment);
        await _context.SaveChangesAsync();
        return attachment;
    }

    public async Task<List<JournalLogAttachment>> GetAttachmentsByJournalLogIdAsync(int journalLogId)
    {
        return await _context.JournalLogAttachments
            .Where(a => a.JournalLogId == journalLogId)
            .ToListAsync();
    }

    public async Task<bool> DeleteAttachmentAsync(int attachmentId)
    {
        var attachment = await _context.JournalLogAttachments.FindAsync(attachmentId);
        if (attachment == null) return false;

        _context.JournalLogAttachments.Remove(attachment);
        await _context.SaveChangesAsync();
        return true;
    }
}
