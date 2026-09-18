using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Entities;

namespace TraceCore.Domain.Repositories;

public interface IDepartmentRepository
{
    Task<Department?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken ct = default);
    Task<long> AddAsync(Department department, CancellationToken ct = default);
    Task UpdateAsync(Department department, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, long? excludeId = null, CancellationToken ct = default);
}
