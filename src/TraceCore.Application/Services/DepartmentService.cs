using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;
using TraceCore.Application.Exceptions;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Application.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IAuditService _auditService;

    public DepartmentService(IDepartmentRepository departmentRepository, IAuditService auditService)
    {
        _departmentRepository = departmentRepository;
        _auditService = auditService;
    }

    public async Task<DepartmentDto> CreateDepartmentAsync(string name, string description, long? currentUserId = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleValidationException("BR-002", "O nome do departamento é obrigatório.");

        var trimmedName = name.Trim();
        if (await _departmentRepository.ExistsByNameAsync(trimmedName, null, ct))
        {
            throw new ConflictException($"Já existe um departamento com o nome '{trimmedName}'.");
        }

        var dept = new Department(trimmedName, description, "Active");
        var id = await _departmentRepository.AddAsync(dept, ct);
        dept.Id = id;

        // BR-004: Auditoria
        await _auditService.RecordAsync(
            action: "department.create",
            entityType: "departments",
            entityId: id.ToString(),
            actorUserId: currentUserId,
            after: new { dept.Id, dept.Name, dept.Description, dept.Status },
            ct: ct
        );

        return new DepartmentDto(dept.Id, dept.Name, dept.Description, dept.Status);
    }

    public async Task UpdateDepartmentAsync(long id, string name, string description, string status, long? currentUserId = null, CancellationToken ct = default)
    {
        var dept = await _departmentRepository.GetByIdAsync(id, ct);
        if (dept == null)
            throw new EntityNotFoundException("Departamento", id);

        var trimmedName = name.Trim();
        if (await _departmentRepository.ExistsByNameAsync(trimmedName, id, ct))
        {
            throw new ConflictException($"Já existe um departamento com o nome '{trimmedName}'.");
        }

        var before = new { dept.Name, dept.Description, dept.Status };
        dept.Name = trimmedName;
        dept.Description = description;
        dept.Status = status;

        await _departmentRepository.UpdateAsync(dept, ct);

        // BR-004: Auditoria
        await _auditService.RecordAsync(
            action: "department.update",
            entityType: "departments",
            entityId: id.ToString(),
            actorUserId: currentUserId,
            before: before,
            after: new { dept.Name, dept.Description, dept.Status },
            ct: ct
        );
    }

    public async Task<IReadOnlyList<DepartmentDto>> GetAllDepartmentsAsync(CancellationToken ct = default)
    {
        var list = await _departmentRepository.GetAllAsync(ct);
        return list.Select(d => new DepartmentDto(d.Id, d.Name, d.Description, d.Status)).ToList();
    }

    public async Task<DepartmentDto?> GetDepartmentByIdAsync(long id, CancellationToken ct = default)
    {
        var dept = await _departmentRepository.GetByIdAsync(id, ct);
        if (dept == null) return null;
        return new DepartmentDto(dept.Id, dept.Name, dept.Description, dept.Status);
    }
}
