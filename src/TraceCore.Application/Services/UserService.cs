using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;
using TraceCore.Application.Exceptions;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Enums;
using TraceCore.Domain.Repositories;
using TraceCore.Domain.Services;

namespace TraceCore.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserSessionRepository _sessionRepository;
    private readonly IPasswordResetTokenRepository _resetTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditService _auditService;

    public UserService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IUserSessionRepository sessionRepository,
        IPasswordResetTokenRepository resetTokenRepository,
        IPasswordHasher _hasher,
        IAuditService auditService)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _sessionRepository = sessionRepository;
        _resetTokenRepository = resetTokenRepository;
        _passwordHasher = _hasher;
        _auditService = auditService;
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request, long? currentUserId = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new BusinessRuleValidationException("BR-001", "O nome do usuário é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.Email))
            throw new BusinessRuleValidationException("BR-001", "O e-mail do usuário é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            throw new BusinessRuleValidationException("BR-101", "A senha deve conter no mínimo 6 caracteres.");

        // BR-001: Todo usuário ativo deve possuir identidade única, estado, nome, e-mail/login e pelo menos um vínculo organizacional ou papel global.
        var hasDepartment = request.DepartmentIds != null && request.DepartmentIds.Any();
        var hasRole = request.RoleIds != null && request.RoleIds.Any();
        if (!hasDepartment && !hasRole)
        {
            throw new BusinessRuleValidationException("BR-001", "Todo usuário ativo deve possuir pelo menos um vínculo organizacional (departamento) ou papel global.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _userRepository.ExistsByEmailAsync(email, null, ct))
        {
            throw new ConflictException($"Já existe um usuário cadastrado com o e-mail '{email}'.");
        }

        var passwordHash = _passwordHasher.HashPassword(request.Password);
        var user = new User(request.Name, email, passwordHash, currentUserId);

        var userId = await _userRepository.AddAsync(user, ct);
        user.Id = userId;

        if (hasDepartment)
        {
            await _userRepository.SetUserDepartmentsAsync(userId, request.DepartmentIds!, ct);
        }

        if (hasRole)
        {
            await _userRepository.SetUserRolesAsync(userId, request.RoleIds!, ct);
        }

        // BR-004: Auditoria imutável de criação
        await _auditService.RecordAsync(
            action: "user.create",
            entityType: "users",
            entityId: userId.ToString(),
            actorUserId: currentUserId,
            after: new { user.Id, user.Name, user.Email, user.Status, request.DepartmentIds, request.RoleIds },
            ct: ct
        );

        return await MapToDtoAsync(user, ct);
    }

    public async Task UpdateUserStatusAsync(long userId, UserStatus status, long? currentUserId = null, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user == null)
            throw new EntityNotFoundException("Usuário", userId);

        var previousStatus = user.Status;
        if (previousStatus == status)
            return;

        if (status == UserStatus.Active)
        {
            // BR-001: Valida se o usuário tem departamento ou papel para poder estar ativo
            var deps = await _userRepository.GetUserDepartmentsAsync(userId, ct);
            var roles = await _userRepository.GetUserRolesAsync(userId, ct);
            if (deps.Count == 0 && roles.Count == 0)
            {
                throw new BusinessRuleValidationException("BR-001", "Usuário ativo deve possuir pelo menos um vínculo departamental ou papel atribuído.");
            }
            user.Activate(currentUserId);
        }
        else if (status == UserStatus.Inactive || status == UserStatus.Suspended)
        {
            // Bloco 7.A.1 (BR-002): Bloqueia desativação/suspensão do último Admin ativo
            var adminRole = await _roleRepository.GetByNameAsync("Admin", ct);
            if (adminRole != null)
            {
                var userRoles = await _userRepository.GetUserRolesAsync(userId, ct);
                if (userRoles.Any(r => r.Id == adminRole.Id))
                {
                    var activeAdminsCount = await _userRepository.CountActiveUsersWithRoleAsync(adminRole.Id, ct);
                    if (activeAdminsCount <= 1)
                    {
                        throw new BusinessRuleValidationException("BR-002", "Não é permitido desativar ou suspender o último usuário ativo com o papel de Administrador (Admin).");
                    }
                }
            }

            if (status == UserStatus.Inactive)
            {
                // BR-005: Desativar não apaga dados históricos nem remove associações
                user.Deactivate(currentUserId);
            }
            else
            {
                user.Suspend(currentUserId);
            }

            // Invalida sessões ativas
            await _sessionRepository.RevokeAllForUserAsync(userId, ct);
        }

        await _userRepository.UpdateAsync(user, ct);

        // BR-004: Auditoria
        await _auditService.RecordAsync(
            action: "user.status_change",
            entityType: "users",
            entityId: userId.ToString(),
            actorUserId: currentUserId,
            before: new { Status = previousStatus },
            after: new { Status = user.Status },
            ct: ct
        );
    }

    public async Task ChangePasswordAsync(long userId, ChangePasswordRequest request, long? currentUserId = null, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user == null)
            throw new EntityNotFoundException("Usuário", userId);

        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            throw new BusinessRuleValidationException("BR-101", "A senha atual informada é incorreta.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
        {
            throw new BusinessRuleValidationException("BR-101", "A nova senha deve possuir pelo menos 6 caracteres.");
        }

        var newHash = _passwordHasher.HashPassword(request.NewPassword);
        user.UpdatePassword(newHash, currentUserId ?? userId);
        await _userRepository.UpdateAsync(user, ct);

        // Invalida sessões ativas existentes por segurança
        await _sessionRepository.RevokeAllForUserAsync(userId, ct);

        // BR-004: Auditoria
        await _auditService.RecordAsync(
            action: "user.change_password",
            entityType: "users",
            entityId: userId.ToString(),
            actorUserId: currentUserId ?? userId,
            metadata: new { Reason = "User requested password change" },
            ct: ct
        );
    }

    public async Task<string> RequestPasswordResetAsync(string email, string? ipAddress = null, CancellationToken ct = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, ct);
        if (user == null)
        {
            // BR-101 / 12_SEGURANCA: Resposta genérica para evitar enumeração de usuários
            return string.Empty;
        }

        // Gera token aleatório seguro
        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var tokenHash = ComputeSha256(rawToken);

        // Invalida tokens anteriores
        await _resetTokenRepository.InvalidateAllForUserAsync(user.Id, ct);

        // Expiração curta (15 minutos)
        var resetToken = new PasswordResetToken(user.Id, tokenHash, TimeSpan.FromMinutes(15));
        await _resetTokenRepository.AddAsync(resetToken, ct);

        // BR-004 / BR-100: Auditoria
        await _auditService.RecordAsync(
            action: "user.password_reset_request",
            entityType: "users",
            entityId: user.Id.ToString(),
            actorUserId: user.Id,
            ipAddress: ipAddress,
            metadata: new { ExpiresAt = resetToken.ExpiresAt },
            ct: ct
        );

        return rawToken;
    }

    public async Task ResetPasswordWithTokenAsync(ResetPasswordRequest request, string? ipAddress = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.RawToken))
            throw new BusinessRuleValidationException("BR-101", "Token de recuperação é obrigatório.");

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            throw new BusinessRuleValidationException("BR-101", "A nova senha deve possuir pelo menos 6 caracteres.");

        var tokenHash = ComputeSha256(request.RawToken);
        var token = await _resetTokenRepository.GetByTokenHashAsync(tokenHash, ct);
        if (token == null || !token.IsValid)
        {
            throw new BusinessRuleValidationException("BR-101", "Token de recuperação inválido ou expirado.");
        }

        var user = await _userRepository.GetByIdAsync(token.UserId, ct);
        if (user == null)
            throw new EntityNotFoundException("Usuário", token.UserId);

        var newHash = _passwordHasher.HashPassword(request.NewPassword);
        user.UpdatePassword(newHash, user.Id);
        await _userRepository.UpdateAsync(user, ct);

        await _resetTokenRepository.MarkAsUsedAsync(token.Id, ct);
        await _sessionRepository.RevokeAllForUserAsync(user.Id, ct);

        // BR-004 / BR-100: Auditoria
        await _auditService.RecordAsync(
            action: "user.password_reset_complete",
            entityType: "users",
            entityId: user.Id.ToString(),
            actorUserId: user.Id,
            ipAddress: ipAddress,
            ct: ct
        );
    }

    public async Task AssignDepartmentsAsync(long userId, IEnumerable<long> departmentIds, long? currentUserId = null, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user == null)
            throw new EntityNotFoundException("Usuário", userId);

        var deps = departmentIds.Distinct().ToList();
        if (user.Status == UserStatus.Active && deps.Count == 0)
        {
            var roles = await _userRepository.GetUserRolesAsync(userId, ct);
            if (roles.Count == 0)
            {
                throw new BusinessRuleValidationException("BR-001", "Usuário ativo deve possuir pelo menos um vínculo organizacional ou papel.");
            }
        }

        var previousDeps = (await _userRepository.GetUserDepartmentsAsync(userId, ct)).Select(d => d.Id).ToList();
        await _userRepository.SetUserDepartmentsAsync(userId, deps, ct);

        // BR-004: Auditoria
        await _auditService.RecordAsync(
            action: "user.assign_departments",
            entityType: "users",
            entityId: userId.ToString(),
            actorUserId: currentUserId,
            before: new { Departments = previousDeps },
            after: new { Departments = deps },
            ct: ct
        );
    }

    public async Task AssignRolesAsync(long userId, IEnumerable<long> roleIds, long? currentUserId = null, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user == null)
            throw new EntityNotFoundException("Usuário", userId);

        var roles = roleIds.Distinct().ToList();
        if (user.Status == UserStatus.Active && roles.Count == 0)
        {
            var deps = await _userRepository.GetUserDepartmentsAsync(userId, ct);
            if (deps.Count == 0)
            {
                throw new BusinessRuleValidationException("BR-001", "Usuário ativo deve possuir pelo menos um vínculo organizacional ou papel.");
            }
        }

        // Bloco 7.A.1 (BR-002): Bloqueia remoção do papel Admin do último Admin ativo
        var adminRole = await _roleRepository.GetByNameAsync("Admin", ct);
        if (adminRole != null && user.Status == UserStatus.Active)
        {
            var currentRoles = await _userRepository.GetUserRolesAsync(userId, ct);
            if (currentRoles.Any(r => r.Id == adminRole.Id) && !roles.Contains(adminRole.Id))
            {
                var activeAdminsCount = await _userRepository.CountActiveUsersWithRoleAsync(adminRole.Id, ct);
                if (activeAdminsCount <= 1)
                {
                    throw new BusinessRuleValidationException("BR-002", "Não é permitido remover o papel de Administrador (Admin) do último usuário ativo detentor deste papel.");
                }
            }
        }

        var previousRoles = (await _userRepository.GetUserRolesAsync(userId, ct)).Select(r => r.Id).ToList();
        await _userRepository.SetUserRolesAsync(userId, roles, ct);

        // BR-004: Auditoria
        await _auditService.RecordAsync(
            action: "user.assign_roles",
            entityType: "users",
            entityId: userId.ToString(),
            actorUserId: currentUserId,
            before: new { Roles = previousRoles },
            after: new { Roles = roles },
            ct: ct
        );
    }

    public async Task<UserDto?> GetUserByIdAsync(long userId, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user == null) return null;
        return await MapToDtoAsync(user, ct);
    }

    public async Task<IReadOnlyList<UserDto>> GetAllUsersAsync(CancellationToken ct = default)
    {
        var users = await _userRepository.GetAllAsync(ct);
        var result = new List<UserDto>();
        foreach (var user in users)
        {
            result.Add(await MapToDtoAsync(user, ct));
        }
        return result;
    }

    public async Task<IReadOnlyList<string>> GetUserEffectivePermissionsAsync(long userId, CancellationToken ct = default)
    {
        return await _userRepository.GetUserEffectivePermissionCodesAsync(userId, ct);
    }

    private async Task<UserDto> MapToDtoAsync(User user, CancellationToken ct)
    {
        var departments = await _userRepository.GetUserDepartmentsAsync(user.Id, ct);
        var roles = await _userRepository.GetUserRolesAsync(user.Id, ct);
        var permissions = await _userRepository.GetUserEffectivePermissionCodesAsync(user.Id, ct);

        return new UserDto(
            user.Id,
            user.Name,
            user.Email,
            user.Status,
            user.LastLoginAt,
            user.AvatarStorageKey,
            user.CreatedAt,
            departments.Select(d => d.Id).ToList(),
            roles.Select(r => r.Id).ToList(),
            permissions
        );
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
