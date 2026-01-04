using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly ApplicationDbContext _context;

    public AuditLogRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AuditLog> CreateAsync(AuditLog auditLog)
    {
        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync();
        return auditLog;
    }

    public async Task<List<AuditLog>> GetByEntityAsync(string entityType, int? entityId = null)
    {
        var query = _context.AuditLogs
            .Include(al => al.User)
            .Where(al => al.EntityType == entityType);

        if (entityId.HasValue)
        {
            query = query.Where(al => al.EntityId == entityId);
        }

        return await query
            .OrderByDescending(al => al.CreatedDate)
            .ToListAsync();
    }

    public async Task<List<AuditLog>> GetByUserAsync(int userId)
    {
        return await _context.AuditLogs
            .Include(al => al.User)
            .Where(al => al.UserId == userId)
            .OrderByDescending(al => al.CreatedDate)
            .ToListAsync();
    }

    public async Task<List<AuditLog>> GetAllAsync(int? limit = null)
    {
        var query = _context.AuditLogs
            .Include(al => al.User)
            .OrderByDescending(al => al.CreatedDate);

        if (limit.HasValue)
        {
            query = (IOrderedQueryable<AuditLog>)query.Take(limit.Value);
        }

        return await query.ToListAsync();
    }
}
