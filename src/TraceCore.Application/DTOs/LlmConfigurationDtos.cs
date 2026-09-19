using System;
using System.Collections.Generic;
using TraceCore.Domain.Services;

namespace TraceCore.Application.DTOs;

/// <summary>
/// Fase 13/17: DTO para configurações legadas (llm_provider_configs) — compatibilidade durante migração.
/// </summary>
public record LlmProviderConfigDto(
    long Id,
    string Purpose,
    string ProviderCode,
    string ModelName,
    bool IsActive,
    bool CredentialConfigured,
    long? UpdatedBy,
    DateTime? UpdatedAt);

/// <summary>
/// Fase 17: DTO para provedor administrativo (llm_providers).
/// </summary>
public record LlmProviderDto(
    long Id,
    string Name,
    string Code,
    string Protocol,
    string BaseUrl,
    string AuthenticationType,
    bool HasGenerationCapability,
    bool HasEmbeddingCapability,
    string Status,
    bool CredentialConfigured,
    long? CreatedBy,
    DateTime CreatedAt,
    long? UpdatedBy,
    DateTime? UpdatedAt);

/// <summary>
/// Fase 17: DTO para configuração de uso de modelo por propósito (llm_model_configs).
/// </summary>
public record LlmModelConfigDto(
    long Id,
    string Purpose,
    long ProviderId,
    string ProviderName,
    string ProviderCode,
    string Protocol,
    string ModelName,
    bool IsActive,
    bool CredentialConfigured,
    long? CreatedBy,
    DateTime CreatedAt,
    long? UpdatedBy,
    DateTime? UpdatedAt);

// ConnectionTestResult está definido no domínio (TraceCore.Domain.Services)

/// <summary>
/// Fase 13/17: Comando para criar/atualizar configuração legada.
/// </summary>
public record UpsertLlmProviderConfigCommand(
    long? Id,
    string Purpose,
    string ProviderCode,
    string ModelName,
    bool IsActive,
    /// <summary>
    /// Nova API Key a ser gravada no ISecretStore. Null ou vazio = nao alterar credencial existente.
    /// NUNCA incluir em logs ou eventos de auditoria — apenas o flag CredentialChanged.
    /// </summary>
    string? NewApiKey,
    long? UpdatedBy);

/// <summary>
/// Fase 17: Comando para criar/atualizar provedor administrativo.
/// </summary>
public record UpsertLlmProviderCommand(
    long? Id,
    string Name,
    string Code,
    string Protocol,
    string BaseUrl,
    string AuthenticationType,
    bool HasGenerationCapability,
    bool HasEmbeddingCapability,
    string Status,
    string? NewApiKey,
    long? UpdatedBy);

/// <summary>
/// Fase 17: Comando para criar/atualizar configuração de modelo por propósito.
/// </summary>
public record UpsertLlmModelConfigCommand(
    long? Id,
    string Purpose,
    long ProviderId,
    string ModelName,
    bool IsActive,
    long? UpdatedBy);
