using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Application.DTOs;
using TraceCore.Application.Exceptions;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;
using TraceCore.Domain.Services;

namespace TraceCore.Application.Services;

/// <summary>
/// Fase 13 (M12): gestão das configurações de provedores IA (llm_provider_configs).
/// Somente uma configuração ativa por propósito; a credencial (API Key) continua sempre
/// em configuração de ambiente e aparece aqui apenas como estado (presente/ausente).
/// </summary>
public class LlmConfigurationService : ILlmConfigurationService
{
    private readonly ILlmProviderConfigRepository _configRepository;
    private readonly ILlmProviderResolver _resolver;

    public LlmConfigurationService(
        ILlmProviderConfigRepository configRepository,
        ILlmProviderResolver resolver)
    {
        _configRepository = configRepository;
        _resolver = resolver;
    }

    public async Task<IReadOnlyList<LlmProviderConfigDto>> GetConfigsAsync(CancellationToken ct = default)
    {
        var configs = await _configRepository.GetAllAsync(ct);
        return configs
            .OrderBy(c => c.Purpose)
            .ThenBy(c => c.ProviderCode)
            .Select(c => new LlmProviderConfigDto(
                Id: c.Id,
                Purpose: c.Purpose,
                ProviderCode: c.ProviderCode,
                ModelName: c.ModelName,
                IsActive: c.IsActive,
                CredentialConfigured: _resolver.IsCredentialConfigured(c.ProviderCode),
                UpdatedBy: c.UpdatedBy,
                UpdatedAt: c.UpdatedAt))
            .ToList();
    }

    public async Task ActivateAsync(long configId, long? updatedBy, CancellationToken ct = default)
    {
        var config = await _configRepository.GetByIdAsync(configId, ct)
            ?? throw new EntityNotFoundException("Configuração de provedor IA", configId);

        if (config.IsActive)
            return;

        // Somente uma configuração ativa por propósito
        foreach (var samePurpose in (await _configRepository.GetAllAsync(ct))
                     .Where(c => c.Purpose.Equals(config.Purpose, StringComparison.OrdinalIgnoreCase)))
        {
            if (samePurpose.IsActive)
            {
                samePurpose.IsActive = false;
                samePurpose.UpdatedBy = updatedBy;
                await _configRepository.UpdateAsync(samePurpose, ct);
            }
        }

        config.IsActive = true;
        config.UpdatedBy = updatedBy;
        await _configRepository.UpdateAsync(config, ct);
    }

    public async Task UpdateModelAsync(long configId, string modelName, long? updatedBy, CancellationToken ct = default)
    {
        var config = await _configRepository.GetByIdAsync(configId, ct)
            ?? throw new EntityNotFoundException("Configuração de provedor IA", configId);

        if (string.IsNullOrWhiteSpace(modelName))
            throw new BusinessRuleValidationException("BR-080", "O nome do modelo é obrigatório.");

        config.ModelName = modelName.Trim();
        config.UpdatedBy = updatedBy;
        await _configRepository.UpdateAsync(config, ct);
    }

    public bool IsCredentialConfigured(string providerCode) => _resolver.IsCredentialConfigured(providerCode);
}