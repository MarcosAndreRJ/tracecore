using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Enums;
using TraceCore.Domain.Repositories;
using TraceCore.Domain.Services;

namespace TraceCore.Application.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IUserSessionRepository _sessionRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditService _auditService;

    public AuthenticationService(
        IUserRepository userRepository,
        IUserSessionRepository sessionRepository,
        IPasswordHasher passwordHasher,
        IAuditService auditService)
    {
        _userRepository = userRepository;
        _sessionRepository = sessionRepository;
        _passwordHasher = passwordHasher;
        _auditService = auditService;
    }

    public async Task<LoginResultDto> LoginAsync(string email, string password, string? ipAddress = null, string? userAgent = null, CancellationToken ct = default)
    {
        var normalizedEmail = email?.Trim().ToLowerInvariant() ?? string.Empty;
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, ct);

        if (user == null || !_passwordHasher.VerifyPassword(password, user.PasswordHash))
        {
            // 12_SEGURANCA §5: Auditar falhas de login
            await _auditService.RecordAsync(
                action: "login.failure",
                entityType: "users",
                entityId: user?.Id.ToString() ?? "unknown",
                actorUserId: user?.Id,
                ipAddress: ipAddress,
                userAgent: userAgent,
                metadata: new { Email = normalizedEmail, Reason = "InvalidCredentials" },
                ct: ct
            );

            return new LoginResultDto(false, "Credenciais inválidas.", null, null, Array.Empty<string>());
        }

        if (user.Status != UserStatus.Active)
        {
            await _auditService.RecordAsync(
                action: "login.blocked",
                entityType: "users",
                entityId: user.Id.ToString(),
                actorUserId: user.Id,
                ipAddress: ipAddress,
                userAgent: userAgent,
                metadata: new { Status = user.Status.ToString() },
                ct: ct
            );

            return new LoginResultDto(false, "Usuário inativo ou suspenso.", null, null, Array.Empty<string>());
        }

        // Atualiza last_login_at
        user.RecordLogin(DateTime.UtcNow);
        await _userRepository.UpdateAsync(user, ct);

        // Cria sessão
        var session = new UserSession(user.Id, TimeSpan.FromHours(8), ipAddress, userAgent);
        var sessionId = await _sessionRepository.AddAsync(session, ct);
        session.Id = sessionId;

        // Permissões e vínculos
        var permissions = await _userRepository.GetUserEffectivePermissionCodesAsync(user.Id, ct);
        var departments = await _userRepository.GetUserDepartmentsAsync(user.Id, ct);
        var roles = await _userRepository.GetUserRolesAsync(user.Id, ct);

        var userDto = new UserDto(
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

        // 12_SEGURANCA §5 / BR-100: Auditar login bem-sucedido
        await _auditService.RecordAsync(
            action: "login.success",
            entityType: "users",
            entityId: user.Id.ToString(),
            actorUserId: user.Id,
            ipAddress: ipAddress,
            userAgent: userAgent,
            metadata: new { SessionId = sessionId },
            ct: ct
        );

        return new LoginResultDto(true, null, userDto, sessionId, permissions);
    }

    public async Task LogoutAsync(long userId, long? sessionId = null, string? ipAddress = null, CancellationToken ct = default)
    {
        if (sessionId.HasValue)
        {
            await _sessionRepository.RevokeAsync(sessionId.Value, ct);
        }

        // 12_SEGURANCA §5 / BR-100: Auditar logoff
        await _auditService.RecordAsync(
            action: "logout",
            entityType: "users",
            entityId: userId.ToString(),
            actorUserId: userId,
            ipAddress: ipAddress,
            metadata: new { SessionId = sessionId },
            ct: ct
        );
    }
}
