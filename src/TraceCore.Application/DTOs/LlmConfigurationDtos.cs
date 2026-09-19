using System.Collections.Generic;

namespace TraceCore.Application.DTOs;

public record LlmProviderConfigDto(
    long Id,
    string Purpose,
    string ProviderCode,
    string ModelName,
    bool IsActive,
    bool CredentialConfigured,
    long? UpdatedBy,
    System.DateTime? UpdatedAt);