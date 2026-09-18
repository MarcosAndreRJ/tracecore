using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Entities;

namespace TraceCore.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default);
    Task<long> AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, long? excludeUserId = null, CancellationToken ct = default);

    // Vínculos organizacionais e papéis
    Task<IReadOnlyList<Department>> GetUserDepartmentsAsync(long userId, CancellationToken ct = default);
    Task SetUserDepartmentsAsync(long userId, IEnumerable<long> departmentIds, CancellationToken ct = default);

    Task<IReadOnlyList<Role>> GetUserRolesAsync(long userId, CancellationToken ct = default);
    Task SetUserRolesAsync(long userId, IEnumerable<long> roleIds, CancellationToken ct = default);

    Task<IReadOnlyList<string>> GetUserEffectivePermissionCodesAsync(long userId, CancellationToken ct = default);
    Task<int> CountActiveUsersWithRoleAsync(long roleId, CancellationToken ct = default);
}
