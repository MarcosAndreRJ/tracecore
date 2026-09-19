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
/// Fase 13/17: Gestão das configurações de provedores IA.
/// Suporta arquitetura desacoplada (llm_providers + llm_model_configs) e compatibilidade com fluxo legado (llm_provider_configs).
/// A credencial mora no ISecretStore (não no banco).
/// Auditoria: llm_provider_config.update / llm_provider_config.credential_removed / llm_provider.update / llm_model_config.update — NUNCA o valor da chave.
/// </summary>
public class LlmConfigurationService : ILlmConfigurationService
{
    private readonly ILlmProviderConfigRepository _legacyConfigRepository;
    private readonly ILlmProviderRepository _providerRepository;
    private readonly ILlmModelConfigRepository _modelConfigRepository;
    private readonly ILlmProviderResolver _resolver;
    private readonly ISecretStore _secretStore;
    private readonly IAuditService _auditService;
    private readonly ILlmModelCatalog _modelCatalog;

    public LlmConfigurationService(
        ILlmProviderConfigRepository legacyConfigRepository,
        ILlmProviderRepository providerRepository,
        ILlmModelConfigRepository modelConfigRepository,
        ILlmProviderResolver resolver,
        ISecretStore secretStore,
        IAuditService auditService,
        ILlmModelCatalog modelCatalog)
    {
        _legacyConfigRepository = legacyConfigRepository;
        _providerRepository = providerRepository;
        _modelConfigRepository = modelConfigRepository;
        _resolver = resolver;
        _secretStore = secretStore;
        _auditService = auditService;
        _modelCatalog = modelCatalog;
    }

    // ========== LEGADO (compatibilidade) ==========

    public async Task<IReadOnlyList<LlmProviderConfigDto>> GetLegacyConfigsAsync(CancellationToken ct = default)
    {
        var configs = await _legacyConfigRepository.GetAllAsync(ct);
        var dtos = new List<LlmProviderConfigDto>();
        foreach (var c in configs.OrderBy(x => x.Purpose).ThenBy(x => x.ProviderCode))
        {
            var credConfigured = await _secretStore.ExistsAsync(SecretKey(c.ProviderCode), ct)
                || _resolver.IsCredentialConfigured(c.ProviderCode);
            dtos.Add(new LlmProviderConfigDto(
                Id: c.Id,
                Purpose: c.Purpose,
                ProviderCode: c.ProviderCode,
                ModelName: c.ModelName,
                IsActive: c.IsActive,
                CredentialConfigured: credConfigured,
                UpdatedBy: c.UpdatedBy,
                UpdatedAt: c.UpdatedAt));
        }
        return dtos;
    }

    public async Task ActivateLegacyAsync(long configId, long? updatedBy, CancellationToken ct = default)
    {
        var config = await _legacyConfigRepository.GetByIdAsync(configId, ct)
            ?? throw new EntityNotFoundException("Configuração de provedor IA (legado)", configId);

        if (config.IsActive) return;

        foreach (var samePurpose in (await _legacyConfigRepository.GetAllAsync(ct))
                     .Where(c => c.Purpose.Equals(config.Purpose, StringComparison.OrdinalIgnoreCase)))
        {
            if (samePurpose.IsActive)
            {
                samePurpose.IsActive = false;
                samePurpose.UpdatedBy = updatedBy;
                samePurpose.UpdatedAt = DateTime.UtcNow;
                await _legacyConfigRepository.UpdateAsync(samePurpose, ct);
            }
        }

        config.IsActive = true;
        config.UpdatedBy = updatedBy;
        config.UpdatedAt = DateTime.UtcNow;
        await _legacyConfigRepository.UpdateAsync(config, ct);

        await _auditService.RecordAsync(
            "llm_provider_config.update",
            "LlmProviderConfig", config.Id.ToString(),
            actorUserId: updatedBy,
            metadata: new { Action = "Activate", Purpose = config.Purpose, ProviderCode = config.ProviderCode });
    }

    public async Task UpdateLegacyModelAsync(long configId, string modelName, long? updatedBy, CancellationToken ct = default)
    {
        var config = await _legacyConfigRepository.GetByIdAsync(configId, ct)
            ?? throw new EntityNotFoundException("Configuração de provedor IA (legado)", configId);

        if (string.IsNullOrWhiteSpace(modelName))
            throw new BusinessRuleValidationException("BR-080", "O nome do modelo é obrigatório.");

        config.ModelName = modelName.Trim();
        config.UpdatedBy = updatedBy;
        config.UpdatedAt = DateTime.UtcNow;
        await _legacyConfigRepository.UpdateAsync(config, ct);
    }

    public bool IsCredentialConfigured(string providerCode) =>
        _resolver.IsCredentialConfigured(providerCode);

    public async Task<LlmProviderConfigDto> UpsertLegacyConfigAsync(
        UpsertLlmProviderConfigCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.ProviderCode))
            throw new BusinessRuleValidationException("BR-080", "ProviderCode é obrigatório.");
        if (string.IsNullOrWhiteSpace(command.ModelName))
            throw new BusinessRuleValidationException("BR-080", "ModelName é obrigatório.");
        if (string.IsNullOrWhiteSpace(command.Purpose))
            throw new BusinessRuleValidationException("BR-080", "Purpose é obrigatório.");

        LlmProviderConfig config;
        bool isNew = false;

        if (command.Id.HasValue)
        {
            config = await _legacyConfigRepository.GetByIdAsync(command.Id.Value, ct)
                ?? throw new EntityNotFoundException("Configuração de provedor IA", command.Id.Value);
        }
        else
        {
            config = new LlmProviderConfig(command.Purpose, command.ProviderCode, command.ModelName,
                command.IsActive, command.UpdatedBy);
            isNew = true;
        }

        if (command.IsActive)
        {
            foreach (var other in (await _legacyConfigRepository.GetAllAsync(ct))
                         .Where(c => c.Purpose.Equals(command.Purpose, StringComparison.OrdinalIgnoreCase)
                                     && c.IsActive
                                     && (isNew || c.Id != config.Id)))
            {
                other.IsActive = false;
                other.UpdatedBy = command.UpdatedBy;
                other.UpdatedAt = DateTime.UtcNow;
                await _legacyConfigRepository.UpdateAsync(other, ct);
            }
        }

        config.ProviderCode = command.ProviderCode.Trim();
        config.ModelName = command.ModelName.Trim();
        config.IsActive = command.IsActive;
        config.UpdatedBy = command.UpdatedBy;
        config.UpdatedAt = DateTime.UtcNow;

        bool credentialChanged = false;
        if (!string.IsNullOrWhiteSpace(command.NewApiKey))
        {
            await _secretStore.SetSecretAsync(SecretKey(command.ProviderCode), command.NewApiKey, ct);
            credentialChanged = true;
        }

        long id;
        if (isNew)
        {
            id = await _legacyConfigRepository.AddAsync(config, ct);
        }
        else
        {
            await _legacyConfigRepository.UpdateAsync(config, ct);
            id = config.Id;
        }

        await _auditService.RecordAsync(
            "llm_provider_config.update",
            "LlmProviderConfig", id.ToString(),
            actorUserId: command.UpdatedBy,
            metadata: new
            {
                Action = isNew ? "Create" : "Update",
                Purpose = config.Purpose,
                ProviderCode = config.ProviderCode,
                ModelName = config.ModelName,
                IsActive = config.IsActive,
                CredentialChanged = credentialChanged
            });

        var credConfigured = await _secretStore.ExistsAsync(SecretKey(config.ProviderCode), ct)
            || _resolver.IsCredentialConfigured(config.ProviderCode);

        return new LlmProviderConfigDto(
            id, config.Purpose, config.ProviderCode, config.ModelName,
            config.IsActive, credConfigured, config.UpdatedBy, config.UpdatedAt);
    }

    public async Task SaveCredentialAsync(string providerCode, string apiKey, long? updatedBy, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(providerCode))
            throw new BusinessRuleValidationException("BR-080", "ProviderCode é obrigatório.");
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new BusinessRuleValidationException("BR-080", "API Key não pode ser vazia.");

        await _secretStore.SetSecretAsync(SecretKey(providerCode), apiKey.Trim(), ct);

        await _auditService.RecordAsync(
            "llm_provider_config.update",
            "LlmProviderCredential", providerCode,
            actorUserId: updatedBy,
            metadata: new { Action = "SaveCredential", ProviderCode = providerCode, CredentialChanged = true });
    }

    public async Task DeleteCredentialAsync(string providerCode, long? updatedBy, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(providerCode))
            throw new BusinessRuleValidationException("BR-080", "ProviderCode é obrigatório.");

        await _secretStore.DeleteSecretAsync(SecretKey(providerCode), ct);

        await _auditService.RecordAsync(
            "llm_provider_config.credential_removed",
            "LlmProviderCredential", providerCode,
            actorUserId: updatedBy,
            metadata: new { Action = "DeleteCredential", ProviderCode = providerCode });
    }

    public Task<ConnectionTestResult> TestLegacyConnectionAsync(string providerCode, string purpose, CancellationToken ct = default) =>
        _resolver.TestLegacyConnectionAsync(providerCode, purpose, ct);

    public IReadOnlyList<LlmModelEntry> GetLegacySupportedModels(string providerCode, string purpose) =>
        _modelCatalog.GetSupportedModels(providerCode, purpose);

    // ========== NOVA ARQUITETURA (Fase 17) ==========

    public async Task<IReadOnlyList<LlmProviderDto>> GetProvidersAsync(CancellationToken ct = default)
    {
        var providers = await _providerRepository.GetAllAsync(ct);
        var dtos = new List<LlmProviderDto>();
        foreach (var p in providers)
        {
            dtos.Add(await MapToDtoAsync(p, ct));
        }
        return dtos;
    }

    public async Task<LlmProviderDto?> GetProviderByIdAsync(long id, CancellationToken ct = default)
    {
        var provider = await _providerRepository.GetByIdAsync(id, ct);
        return provider != null ? await MapToDtoAsync(provider, ct) : null;
    }

    public async Task<LlmProviderDto?> GetProviderByCodeAsync(string code, CancellationToken ct = default)
    {
        var provider = await _providerRepository.GetByCodeAsync(code, ct);
        return provider != null ? await MapToDtoAsync(provider, ct) : null;
    }

    public async Task<LlmProviderDto> UpsertProviderAsync(UpsertLlmProviderCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            throw new BusinessRuleValidationException("BR-080", "Name é obrigatório.");
        if (string.IsNullOrWhiteSpace(command.Code))
            throw new BusinessRuleValidationException("BR-080", "Code é obrigatório.");
        if (string.IsNullOrWhiteSpace(command.Protocol))
            throw new BusinessRuleValidationException("BR-080", "Protocol é obrigatório.");
        if (string.IsNullOrWhiteSpace(command.BaseUrl))
            throw new BusinessRuleValidationException("BR-080", "BaseUrl é obrigatório.");
        if (!LlmProtocols.All.Contains(command.Protocol))
            throw new BusinessRuleValidationException("BR-080", $"Protocolo '{command.Protocol}' não suportado. Use: {string.Join(", ", LlmProtocols.All)}");
        if (!LlmAuthenticationTypes.All.Contains(command.AuthenticationType))
            throw new BusinessRuleValidationException("BR-080", $"Tipo de autenticação '{command.AuthenticationType}' não suportado. Use: {string.Join(", ", LlmAuthenticationTypes.All)}");
        if (!command.HasGenerationCapability && !command.HasEmbeddingCapability)
            throw new BusinessRuleValidationException("BR-080", "O provedor deve ter pelo menos uma capability (Generation ou Embedding).");

        // Validar URL: somente http e https
        if (!Uri.TryCreate(command.BaseUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            throw new BusinessRuleValidationException("BR-080", "BaseUrl deve ser uma URL absoluta http ou https.");
        }

        LlmProvider provider;
        bool isNew = false;

        if (command.Id.HasValue)
        {
            provider = await _providerRepository.GetByIdAsync(command.Id.Value, ct)
                ?? throw new EntityNotFoundException("Provedor IA", command.Id.Value);
        }
        else
        {
            // Verificar unicidade do code
            if (await _providerRepository.ExistsByCodeAsync(command.Code, ct))
                throw new BusinessRuleValidationException("BR-080", $"Já existe um provedor com o código '{command.Code}'.");

            provider = new LlmProvider(
                command.Name,
                command.Code,
                command.Protocol,
                command.BaseUrl,
                command.AuthenticationType,
                command.HasGenerationCapability,
                command.HasEmbeddingCapability,
                command.UpdatedBy);
            isNew = true;
        }

        provider.Update(
            command.Name,
            command.Protocol,
            command.BaseUrl,
            command.AuthenticationType,
            command.HasGenerationCapability,
            command.HasEmbeddingCapability,
            command.Status,
            command.UpdatedBy);

        long id;
        if (isNew)
        {
            id = await _providerRepository.AddAsync(provider, ct);
        }
        else
        {
            await _providerRepository.UpdateAsync(provider, ct);
            id = provider.Id;
        }

        if (!string.IsNullOrWhiteSpace(command.NewApiKey))
        {
            await _secretStore.SetSecretAsync(SecretKey(command.Code), command.NewApiKey, ct);
            await _auditService.RecordAsync(
                "llm_provider.credential_update",
                "LlmProvider", id.ToString(),
                actorUserId: command.UpdatedBy,
                metadata: new { Action = "SaveCredential", ProviderCode = command.Code });
        }

        await _auditService.RecordAsync(
            "llm_provider.update",
            "LlmProvider", id.ToString(),
            actorUserId: command.UpdatedBy,
            metadata: new
            {
                Action = isNew ? "Create" : "Update",
                Name = command.Name,
                Code = command.Code,
                Protocol = command.Protocol,
                BaseUrl = command.BaseUrl,
                AuthenticationType = command.AuthenticationType,
                HasGenerationCapability = command.HasGenerationCapability,
                HasEmbeddingCapability = command.HasEmbeddingCapability,
                Status = command.Status
            });

        var updated = await _providerRepository.GetByIdAsync(id, ct);
        return await MapToDtoAsync(updated!, ct);
    }

    public async Task<IReadOnlyList<LlmModelConfigDto>> GetModelConfigsAsync(CancellationToken ct = default)
    {
        var configs = await _modelConfigRepository.GetAllAsync(ct);
        var dtos = new List<LlmModelConfigDto>();

        foreach (var c in configs.OrderBy(x => x.Purpose).ThenBy(x => x.ModelName))
        {
            var provider = await _providerRepository.GetByIdAsync(c.ProviderId, ct);
            bool credConfigured = false;
            if (provider != null)
            {
                credConfigured = provider.AuthenticationType == LlmAuthenticationTypes.None
                    || await _secretStore.ExistsAsync(SecretKey(provider.Code), ct)
                    || _resolver.IsCredentialConfigured(provider.Code);
            }

            dtos.Add(new LlmModelConfigDto(
                Id: c.Id,
                Purpose: c.Purpose,
                ProviderId: c.ProviderId,
                ProviderName: provider?.Name ?? $"Provider #{c.ProviderId}",
                ProviderCode: provider?.Code ?? "unknown",
                Protocol: provider?.Protocol ?? "unknown",
                ModelName: c.ModelName,
                IsActive: c.IsActive,
                CredentialConfigured: credConfigured,
                CreatedBy: c.CreatedBy,
                CreatedAt: c.CreatedAt,
                UpdatedBy: c.UpdatedBy,
                UpdatedAt: c.UpdatedAt));
        }
        return dtos;
    }

    public async Task<LlmModelConfigDto?> GetActiveModelConfigAsync(string purpose, CancellationToken ct = default)
    {
        var config = await _modelConfigRepository.GetActiveByPurposeAsync(purpose, ct);
        if (config == null) return null;

        var provider = await _providerRepository.GetByIdAsync(config.ProviderId, ct);
        bool credConfigured = false;
        if (provider != null)
        {
            credConfigured = provider.AuthenticationType == LlmAuthenticationTypes.None
                || await _secretStore.ExistsAsync(SecretKey(provider.Code), ct)
                || _resolver.IsCredentialConfigured(provider.Code);
        }

        return new LlmModelConfigDto(
            Id: config.Id,
            Purpose: config.Purpose,
            ProviderId: config.ProviderId,
            ProviderName: provider?.Name ?? $"Provider #{config.ProviderId}",
            ProviderCode: provider?.Code ?? "unknown",
            Protocol: provider?.Protocol ?? "unknown",
            ModelName: config.ModelName,
            IsActive: config.IsActive,
            CredentialConfigured: credConfigured,
            CreatedBy: config.CreatedBy,
            CreatedAt: config.CreatedAt,
            UpdatedBy: config.UpdatedBy,
            UpdatedAt: config.UpdatedAt);
    }

    public async Task<LlmModelConfigDto> UpsertModelConfigAsync(UpsertLlmModelConfigCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Purpose))
            throw new BusinessRuleValidationException("BR-080", "Purpose é obrigatório.");
        if (string.IsNullOrWhiteSpace(command.ModelName))
            throw new BusinessRuleValidationException("BR-080", "ModelName é obrigatório.");

        var provider = await _providerRepository.GetByIdAsync(command.ProviderId, ct)
            ?? throw new EntityNotFoundException("Provedor IA", command.ProviderId);

        var requiredCapability = command.Purpose.Equals("Generation", StringComparison.OrdinalIgnoreCase)
            ? provider.HasGenerationCapability
            : provider.HasEmbeddingCapability;

        if (!requiredCapability)
            throw new BusinessRuleValidationException("BR-080", 
                $"Provedor '{provider.Name}' não possui capability '{command.Purpose}'.");

        LlmModelConfig config;
        bool isNew = false;

        if (command.Id.HasValue)
        {
            config = await _modelConfigRepository.GetByIdAsync(command.Id.Value, ct)
                ?? throw new EntityNotFoundException("Configuração de modelo IA", command.Id.Value);
        }
        else
        {
            config = new LlmModelConfig(command.Purpose, command.ProviderId, command.ModelName,
                command.IsActive, command.UpdatedBy);
            isNew = true;
        }

        if (command.IsActive)
        {
            foreach (var other in (await _modelConfigRepository.GetAllAsync(ct))
                         .Where(c => c.Purpose.Equals(command.Purpose, StringComparison.OrdinalIgnoreCase)
                                     && c.IsActive
                                     && (isNew || c.Id != config.Id)))
            {
                other.IsActive = false;
                other.UpdatedBy = command.UpdatedBy;
                other.UpdatedAt = DateTime.UtcNow;
                await _modelConfigRepository.UpdateAsync(other, ct);
            }
        }

        config.Purpose = command.Purpose.Trim();
        config.ProviderId = command.ProviderId;
        config.ModelName = command.ModelName.Trim();
        config.IsActive = command.IsActive;
        config.UpdatedBy = command.UpdatedBy;
        config.UpdatedAt = DateTime.UtcNow;

        long id;
        if (isNew)
            id = await _modelConfigRepository.AddAsync(config, ct);
        else
        {
            await _modelConfigRepository.UpdateAsync(config, ct);
            id = config.Id;
        }

        await _auditService.RecordAsync(
            "llm_model_config.update",
            "LlmModelConfig", id.ToString(),
            actorUserId: command.UpdatedBy,
            metadata: new
            {
                Action = isNew ? "Create" : "Update",
                Purpose = command.Purpose,
                ProviderId = command.ProviderId,
                ModelName = command.ModelName,
                IsActive = command.IsActive
            });

        var updated = await _modelConfigRepository.GetByIdAsync(id, ct);
        var p = await _providerRepository.GetByIdAsync(updated!.ProviderId, ct);
        bool credConfigured = false;
        if (p != null)
        {
            credConfigured = p.AuthenticationType == LlmAuthenticationTypes.None
                || await _secretStore.ExistsAsync(SecretKey(p.Code), ct)
                || _resolver.IsCredentialConfigured(p.Code);
        }
        return new LlmModelConfigDto(
            id, updated.Purpose, updated.ProviderId, p?.Name ?? "unknown", p?.Code ?? "unknown", p?.Protocol ?? "unknown",
            updated.ModelName, updated.IsActive, credConfigured, updated.CreatedBy, updated.CreatedAt, updated.UpdatedBy, updated.UpdatedAt);
    }

    public async Task ActivateModelConfigAsync(long configId, long? updatedBy, CancellationToken ct = default)
    {
        var config = await _modelConfigRepository.GetByIdAsync(configId, ct)
            ?? throw new EntityNotFoundException("Configuração de modelo IA", configId);

        if (config.IsActive) return;

        foreach (var other in (await _modelConfigRepository.GetAllAsync(ct))
                     .Where(c => c.Purpose.Equals(config.Purpose, StringComparison.OrdinalIgnoreCase) && c.IsActive))
        {
            other.IsActive = false;
            other.UpdatedBy = updatedBy;
            other.UpdatedAt = DateTime.UtcNow;
            await _modelConfigRepository.UpdateAsync(other, ct);
        }

        config.IsActive = true;
        config.UpdatedBy = updatedBy;
        config.UpdatedAt = DateTime.UtcNow;
        await _modelConfigRepository.UpdateAsync(config, ct);

        await _auditService.RecordAsync(
            "llm_model_config.update",
            "LlmModelConfig", config.Id.ToString(),
            actorUserId: updatedBy,
            metadata: new { Action = "Activate", Purpose = config.Purpose, ProviderId = config.ProviderId });
    }

    public Task<ConnectionTestResult> TestModelConfigConnectionAsync(long modelConfigId, CancellationToken ct = default) =>
        _resolver.TestModelConfigConnectionAsync(modelConfigId, ct);

    public Task<ConnectionTestResult> TestConnectionAsync(
        string providerCode, string purpose, CancellationToken ct = default) =>
        _resolver.TestLegacyConnectionAsync(providerCode, purpose, ct);

    public IReadOnlyList<LlmModelEntry> GetSupportedModels(string providerCode, string purpose) =>
        _modelCatalog.GetSupportedModels(providerCode, purpose);

    public IReadOnlyList<string> GetSupportedProtocols() => LlmProtocols.All;
    public IReadOnlyList<string> GetSupportedAuthenticationTypes() => LlmAuthenticationTypes.All;

    private async Task<LlmProviderDto> MapToDtoAsync(LlmProvider p, CancellationToken ct = default)
    {
        bool credConfigured = false;
        if (p.AuthenticationType == LlmAuthenticationTypes.None)
        {
            credConfigured = true;
        }
        else
        {
            credConfigured = await _secretStore.ExistsAsync(SecretKey(p.Code), ct)
                || _resolver.IsCredentialConfigured(p.Code);
        }

        return new LlmProviderDto(
            p.Id, p.Name, p.Code, p.Protocol, p.BaseUrl, p.AuthenticationType,
            p.HasGenerationCapability, p.HasEmbeddingCapability, p.Status,
            credConfigured,
            p.CreatedBy, p.CreatedAt, p.UpdatedBy, p.UpdatedAt);
    }

    private static string SecretKey(string providerCode) => $"llm_apikey_{providerCode}";
}
