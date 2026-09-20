using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TraceCore.Domain.Services;

/// <summary>
/// Prompt 4 — resultado normalizado de uma pesquisa externa, independente do
/// provedor concreto (Bing/Brave/Tavily/SerpAPI/etc.). A Application layer nunca
/// conhece o formato de resposta de um provedor específico.
/// </summary>
public record ExternalSearchResult(
    string Title,
    string Url,
    string Domain,
    string? Snippet,
    DateTime? PublishedAt,
    DateTime RetrievedAt
);

/// <summary>
/// Abstração de pesquisa externa (Prompt 4, §14/§15). A Application layer
/// (IExternalResearchService) decide política, sanitização e limites — este
/// contrato só sabe pesquisar e normalizar o resultado. Implementação concreta
/// em Infrastructure via HttpClient puro, mesmo padrão de ILlmProvider (DEV-AI-003).
/// </summary>
public interface IExternalResearchProvider
{
    string ProviderCode { get; }
    bool IsConfigured { get; }
    Task<IReadOnlyList<ExternalSearchResult>> SearchAsync(string sanitizedQuery, int limit, CancellationToken ct = default);
}

/// <summary>
/// Resolve o provedor de pesquisa externa configurado, seguindo a mesma precedência de
/// credencial já usada para LLM (<see cref="ILlmProviderResolver"/>): ISecretStore
/// primeiro, IConfiguration como fallback — nunca API key em texto puro no banco.
/// BaseUrl/Provider/AuthType não são segredo e vêm só de IConfiguration (Prompt 4,
/// §16: "ExternalResearch:Provider" / "ExternalResearch:BaseUrl").
/// </summary>
public interface IExternalResearchProviderFactory
{
    Task<IExternalResearchProvider> GetProviderAsync(CancellationToken ct = default);
}
