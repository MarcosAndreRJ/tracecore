using System.Threading;
using System.Threading.Tasks;

namespace TraceCore.Domain.Services;

/// <summary>
/// Fase 17 (Configuracoes): abstração para armazenamento seguro de segredos em tempo de execucao
/// (ex.: API Keys de provedores de IA). O valor NUNCA é logado, nunca é devolvido à UI,
/// nunca aparece em texto claro fora da camada de infraestrutura que o criptografa.
///
/// Implementação padrao: ProtectedFileSecretStore (ASP.NET Core Data Protection, App_Data/Secrets/).
/// Substituivel por Azure Key Vault, AWS Secrets Manager ou HashiCorp Vault sem alterar o dominio.
///
/// Precedencia de credencial em LlmProviderResolver:
///   1. ISecretStore (chave por provedor: llm_apikey_{providerCode})
///   2. IConfiguration (Llm:{providerCode}:ApiKey — User Secrets / env — fallback)
///   3. Ausente: fail-fast com mensagem clara na UI.
/// </summary>
public interface ISecretStore
{
    /// <summary>
    /// Recupera o valor do segredo identificado por key.
    /// Retorna null se nao existir.
    /// NUNCA expor o valor retornado a UI — use apenas internamente na infraestrutura.
    /// </summary>
    Task<string?> GetSecretAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Persiste o segredo value sob a chave key.
    /// Substitui silenciosamente se ja existir.
    /// </summary>
    Task SetSecretAsync(string key, string value, CancellationToken ct = default);

    /// <summary>
    /// Remove o segredo identificado por key.
    /// Sem erro se a chave nao existir (idempotente).
    /// </summary>
    Task DeleteSecretAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Retorna true se o segredo existir e tiver valor nao nulo/vazio.
    /// Use este metodo na UI — nunca GetSecretAsync para exibir estado.
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
}
