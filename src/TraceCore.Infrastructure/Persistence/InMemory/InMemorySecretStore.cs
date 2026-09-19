using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Services;

namespace TraceCore.Infrastructure.Persistence.InMemory;

/// <summary>
/// Fase 17 (Configuracoes): implementacao em memoria de ISecretStore para testes de integracao.
/// Nunca usa filesystem ou criptografia real — substitui ProtectedFileSecretStore em InMemory.
/// O valor e armazenado em texto claro internamente apenas em memoria de testes.
/// </summary>
public sealed class InMemorySecretStore : ISecretStore
{
    private readonly ConcurrentDictionary<string, string> _secrets = new();

    public Task<string?> GetSecretAsync(string key, CancellationToken ct = default)
    {
        _secrets.TryGetValue(key, out var value);
        return Task.FromResult<string?>(string.IsNullOrWhiteSpace(value) ? null : value);
    }

    public Task SetSecretAsync(string key, string value, CancellationToken ct = default)
    {
        _secrets[key] = value;
        return Task.CompletedTask;
    }

    public Task DeleteSecretAsync(string key, CancellationToken ct = default)
    {
        _secrets.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        var exists = _secrets.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value);
        return Task.FromResult(exists);
    }
}
