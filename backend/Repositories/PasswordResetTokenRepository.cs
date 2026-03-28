using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly ApplicationDbContext _context;

    public PasswordResetTokenRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task CreateForUserAsync(int userId, string tokenHash, DateTime expiresAtUtc, DateTime createdAtUtc)
    {
        var existing = await _context.PasswordResetTokens
            .Where(t => t.UserId == userId)
            .ToListAsync();
        _context.PasswordResetTokens.RemoveRange(existing);

        _context.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = createdAtUtc
        });

        await _context.SaveChangesAsync();
    }

    public async Task<int?> TryConsumeTokenAsync(string tokenHash, DateTime utcNow)
    {
        var token = await _context.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.ExpiresAtUtc > utcNow);

        if (token == null)
            return null;

        var userId = token.UserId;
        _context.PasswordResetTokens.Remove(token);
        await _context.SaveChangesAsync();
        return userId;
    }
}
