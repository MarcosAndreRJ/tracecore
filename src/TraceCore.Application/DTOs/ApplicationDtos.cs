using System;
using System.Collections.Generic;
using TraceCore.Domain.Enums;

namespace TraceCore.Application.DTOs;

public record UserDto(
    long Id,
    string Name,
    string Email,
    UserStatus Status,
    DateTime? LastLoginAt,
    string? AvatarStorageKey,
    DateTime CreatedAt,
    IReadOnlyList<long> DepartmentIds,
    IReadOnlyList<long> RoleIds,
    IReadOnlyList<string> EffectivePermissions
);

public record CreateUserRequest(
    string Name,
    string Email,
    string Password,
    IReadOnlyList<long> DepartmentIds,
    IReadOnlyList<long> RoleIds
);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);

public record ResetPasswordRequest(
    string RawToken,
    string NewPassword
);

public record LoginRequest(
    string Email,
    string Password,
    string? IpAddress = null,
    string? UserAgent = null
);

public record LoginResultDto(
    bool IsSuccess,
    string? ErrorMessage,
    UserDto? User,
    long? SessionId,
    IReadOnlyList<string> Permissions
);

public record DepartmentDto(
    long Id,
    string Name,
    string Description,
    string Status
);

public record RoleDto(
    long Id,
    string Name,
    string Description,
    IReadOnlyList<PermissionDto> Permissions
);

public record PermissionDto(
    long Id,
    string Code,
    string Description
);
