using backend.DTOs;

namespace backend.Services;

public interface IUserService
{
    Task<List<UserResponseDto>> GetAllUsersAsync();
    Task<UserResponseDto?> GetUserByIdAsync(int userId);
    Task<UserResponseDto> CreateUserAsync(CreateUserRequest request, int createdByUserId);
    Task<UserResponseDto?> UpdateUserAsync(int userId, UpdateUserRequest request, int modifiedByUserId);
    Task<bool> DeleteUserAsync(int userId, int deletedByUserId);
    Task<bool> ResetPasswordAsync(int userId, ResetPasswordRequest request, int modifiedByUserId);
}
