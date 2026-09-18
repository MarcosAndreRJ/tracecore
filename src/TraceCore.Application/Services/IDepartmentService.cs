using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;

namespace TraceCore.Application.Services;

public interface IDepartmentService
{
    Task<DepartmentDto> CreateDepartmentAsync(string name, string description, long? currentUserId = null, CancellationToken ct = default);
    Task UpdateDepartmentAsync(long id, string name, string description, string status, long? currentUserId = null, CancellationToken ct = default);
    Task<IReadOnlyList<DepartmentDto>> GetAllDepartmentsAsync(CancellationToken ct = default);
    Task<DepartmentDto?> GetDepartmentByIdAsync(long id, CancellationToken ct = default);
}
