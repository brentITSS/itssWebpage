using backend.Models;

namespace backend.Repositories;

public interface IAuditLogRepository
{
    Task<AuditLog> CreateAsync(AuditLog auditLog);
    Task<List<AuditLog>> GetByEntityAsync(string entityType, int? entityId = null);
    Task<List<AuditLog>> GetByUserAsync(int userId);
    Task<List<AuditLog>> GetAllAsync(int? limit = null);
}
