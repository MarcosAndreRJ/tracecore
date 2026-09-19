using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Entities;

namespace TraceCore.Domain.Repositories;

/// <summary>
/// Fase 13/17: Repositório para configurações legadas (llm_provider_configs).
/// Mantido para compatibilidade durante migração.
/// </summary>
public interface ILlmProviderConfigRepository
{
    Task<LlmProviderConfig?> GetActiveByPurposeAsync(string purpose, CancellationToken ct = default);
    Task<LlmProviderConfig?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<LlmProviderConfig>> GetAllAsync(CancellationToken ct = default);
    Task<LlmProviderConfig?> GetByPurposeAndProviderAsync(string purpose, string providerCode, CancellationToken ct = default);
    Task<long> AddAsync(LlmProviderConfig config, CancellationToken ct = default);
    Task UpdateAsync(LlmProviderConfig config, CancellationToken ct = default);
}

/// <summary>
/// Fase 17: Repositório para provedores administrativos (llm_providers).
/// Permite cadastro dinâmico de qualquer provedor sem alteração de código.
/// </summary>
public interface ILlmProviderRepository
{
    Task<LlmProvider?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<LlmProvider?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<IReadOnlyList<LlmProvider>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<LlmProvider>> GetByProtocolAsync(string protocol, CancellationToken ct = default);
    Task<IReadOnlyList<LlmProvider>> GetByCapabilityAsync(string capability, CancellationToken ct = default); // "Generation" ou "Embedding"
    Task<long> AddAsync(LlmProvider provider, CancellationToken ct = default);
    Task UpdateAsync(LlmProvider provider, CancellationToken ct = default);
    Task<bool> ExistsByCodeAsync(string code, CancellationToken ct = default);
}

/// <summary>
/// Fase 17: Repositório para configurações de uso de modelo por propósito (llm_model_configs).
/// Substitui a antiga LlmProviderConfig para o novo fluxo.
/// </summary>
public interface ILlmModelConfigRepository
{
    Task<LlmModelConfig?> GetActiveByPurposeAsync(string purpose, CancellationToken ct = default);
    Task<LlmModelConfig?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<LlmModelConfig>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<LlmModelConfig>> GetByProviderAsync(long providerId, CancellationToken ct = default);
    Task<LlmModelConfig?> GetByPurposeAndProviderAsync(string purpose, long providerId, CancellationToken ct = default);
    Task<long> AddAsync(LlmModelConfig config, CancellationToken ct = default);
    Task UpdateAsync(LlmModelConfig config, CancellationToken ct = default);
}