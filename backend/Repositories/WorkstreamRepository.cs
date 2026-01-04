using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class WorkstreamRepository : IWorkstreamRepository
{
    private readonly ApplicationDbContext _context;

    public WorkstreamRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Workstream>> GetAllAsync()
    {
        return await _context.Workstreams.ToListAsync();
    }

    public async Task<Workstream?> GetByIdAsync(int workstreamId)
    {
        return await _context.Workstreams.FindAsync(workstreamId);
    }

    public async Task<List<PermissionType>> GetAllPermissionTypesAsync()
    {
        return await _context.PermissionTypes.ToListAsync();
    }

    public async Task<PermissionType?> GetPermissionTypeByIdAsync(int permissionTypeId)
    {
        return await _context.PermissionTypes.FindAsync(permissionTypeId);
    }

    public async Task<Workstream> CreateAsync(Workstream workstream)
    {
        _context.Workstreams.Add(workstream);
        await _context.SaveChangesAsync();
        return workstream;
    }

    public async Task<Workstream> UpdateAsync(Workstream workstream)
    {
        _context.Workstreams.Update(workstream);
        await _context.SaveChangesAsync();
        return workstream;
    }

    public async Task<bool> DeleteAsync(int workstreamId)
    {
        var workstream = await _context.Workstreams.FindAsync(workstreamId);
        if (workstream == null) return false;

        _context.Workstreams.Remove(workstream);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<WorkstreamUser>> GetWorkstreamUsersAsync(int workstreamId)
    {
        return await _context.WorkstreamUsers
            .Include(wu => wu.User)
            .Include(wu => wu.PermissionType)
            .Where(wu => wu.WorkstreamId == workstreamId)
            .ToListAsync();
    }

    public async Task<List<WorkstreamUser>> GetUserWorkstreamsAsync(int userId)
    {
        return await _context.WorkstreamUsers
            .Include(wu => wu.Workstream)
            .Include(wu => wu.PermissionType)
            .Where(wu => wu.UserId == userId)
            .ToListAsync();
    }

    public async Task<WorkstreamUser> AddWorkstreamUserAsync(WorkstreamUser workstreamUser)
    {
        _context.WorkstreamUsers.Add(workstreamUser);
        await _context.SaveChangesAsync();
        return workstreamUser;
    }

    public async Task<bool> RemoveWorkstreamUserAsync(int workstreamId, int userId)
    {
        var workstreamUser = await _context.WorkstreamUsers
            .FirstOrDefaultAsync(wu => wu.WorkstreamId == workstreamId && wu.UserId == userId);
        
        if (workstreamUser != null)
        {
            _context.WorkstreamUsers.Remove(workstreamUser);
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }
}
