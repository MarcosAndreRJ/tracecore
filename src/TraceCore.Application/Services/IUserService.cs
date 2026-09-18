using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;
using TraceCore.Domain.Enums;

namespace TraceCore.Application.Services;

public interface IUserService
{
    Task<UserDto> CreateUserAsync(CreateUserRequest request, long? currentUserId = null, CancellationToken ct = default);
    Task UpdateUserStatusAsync(long userId, UserStatus status, long? currentUserId = null, CancellationToken ct = default);
    Task ChangePasswordAsync(long userId, ChangePasswordRequest request, long? currentUserId = null, CancellationToken ct = default);
    Task<string> RequestPasswordResetAsync(string email, string? ipAddress = null, CancellationToken ct = default);
    Task ResetPasswordWithTokenAsync(ResetPasswordRequest request, string? ipAddress = null, CancellationToken ct = default);
    Task AssignDepartmentsAsync(long userId, IEnumerable<long> departmentIds, long? currentUserId = null, CancellationToken ct = default);
    Task AssignRolesAsync(long userId, IEnumerable<long> roleIds, long? currentUserId = null, CancellationToken ct = default);
    Task<UserDto?> GetUserByIdAsync(long userId, CancellationToken ct = default);
    Task<IReadOnlyList<UserDto>> GetAllUsersAsync(CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetUserEffectivePermissionsAsync(long userId, CancellationToken ct = default);
}
