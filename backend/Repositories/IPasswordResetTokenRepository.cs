namespace backend.Repositories;

public interface IPasswordResetTokenRepository
{
    Task CreateForUserAsync(int userId, string tokenHash, DateTime expiresAtUtc, DateTime createdAtUtc);
    Task<int?> TryConsumeTokenAsync(string tokenHash, DateTime utcNow);
}
