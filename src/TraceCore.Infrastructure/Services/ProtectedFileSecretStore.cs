using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using TraceCore.Domain.Services;

namespace TraceCore.Infrastructure.Services;

/// <summary>
/// Fase 17: implementação de ISecretStore usando ASP.NET Core Data Protection.
/// Armazena segredos criptografados em App_Data/Secrets/{key}.dat, fora de wwwroot
/// e fora do controle de versao (.gitignore exclui App_Data/Secrets/).
///
/// A chave de protecao e gerenciada pelo sistema de Data Protection do ASP.NET Core
/// (padrao: App_Data/DataProtection-Keys/). Substituivel por Azure Key Vault,
/// AWS KMS ou qualquer IDataProtectionProvider compativel sem alterar ISecretStore.
///
/// Seguranca:
///   - NUNCA loga o valor de um segredo.
///   - NUNCA expoe o valor em texto claro para fora da infraestrutura.
///   - ExistsAsync e o unico metodo que a UI pode chamar — retorna apenas bool.
/// </summary>
public sealed class ProtectedFileSecretStore : ISecretStore
{
    private const string Purpose = "TraceCore.SecretStore.v1";
    private readonly IDataProtector _protector;
    private readonly string _secretsDirectory;

    public ProtectedFileSecretStore(IDataProtectionProvider dataProtectionProvider, IConfiguration configuration)
    {
        _protector = dataProtectionProvider.CreateProtector(Purpose);

        // Caminho analogo ao Storage:BasePath (App_Data/Storage) — mantendo a convencao do projeto.
        var basePath = configuration["Storage:BasePath"] ?? "App_Data/Storage";
        var appDataRoot = Path.GetDirectoryName(basePath) ?? "App_Data";
        _secretsDirectory = Path.Combine(appDataRoot, "Secrets");
    }

    public Task<string?> GetSecretAsync(string key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var filePath = GetFilePath(key);
        if (!File.Exists(filePath))
            return Task.FromResult<string?>(null);

        try
        {
            var encrypted = File.ReadAllText(filePath);
            var plaintext = _protector.Unprotect(encrypted);
            return Task.FromResult<string?>(plaintext);
        }
        catch
        {
            // Arquivo corrompido ou chave de protecao rotacionada: trata como ausente.
            // Nao loga o erro para nao vazar contexto do segredo.
            return Task.FromResult<string?>(null);
        }
    }

    public Task SetSecretAsync(string key, string value, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        EnsureDirectoryExists();
        var encrypted = _protector.Protect(value);
        File.WriteAllText(GetFilePath(key), encrypted);
        return Task.CompletedTask;
    }

    public Task DeleteSecretAsync(string key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var filePath = GetFilePath(key);
        if (File.Exists(filePath))
            File.Delete(filePath);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var filePath = GetFilePath(key);
        if (!File.Exists(filePath))
            return Task.FromResult(false);

        try
        {
            var encrypted = File.ReadAllText(filePath);
            var plaintext = _protector.Unprotect(encrypted);
            return Task.FromResult(!string.IsNullOrWhiteSpace(plaintext));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private string GetFilePath(string key)
    {
        // Sanitiza a chave para uso seguro como nome de arquivo.
        var safeKey = string.Concat(key.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_secretsDirectory, safeKey + ".dat");
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_secretsDirectory))
            Directory.CreateDirectory(_secretsDirectory);
    }
}
