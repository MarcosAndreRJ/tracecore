using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TraceCore.Domain.Services;

namespace TraceCore.Infrastructure.Services;

/// <summary>
/// Implementação concreta do ADR-P004 (filesystem local/corporativo).
/// Grava arquivos em pasta configurável (Storage:BasePath), com nome físico baseado em GUID/Hash,
/// garantindo que o nome original do arquivo informado pelo usuário nunca seja usado no disco.
/// </summary>
public class LocalFileStorage : IFileStorage
{
    private readonly string _basePath;

    public LocalFileStorage(IConfiguration configuration)
    {
        var configuredPath = configuration["Storage:BasePath"];
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            // Fallback para App_Data/Storage caso não configurado explicitamente
            _basePath = Path.Combine(AppContext.BaseDirectory, "App_Data", "Storage");
        }
        else
        {
            _basePath = configuredPath;
        }

        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public async Task<string> SaveAsync(string fileName, Stream contentStream, string contentType, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(fileName);
        // Gera chave física opaca baseada em GUID para evitar path traversal ou injeção de nomes
        var uniqueFileId = Guid.NewGuid().ToString("N");
        var physicalName = $"{uniqueFileId}{extension}";
        var fullPath = Path.Combine(_basePath, physicalName);

        using (var destinationStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            if (contentStream.CanSeek)
            {
                contentStream.Position = 0;
            }
            await contentStream.CopyToAsync(destinationStream, ct);
        }

        // storageKey representa o identificador único físico gravado no storage
        return physicalName;
    }

    public Task<Stream?> GetAsync(string storageKey, CancellationToken ct = default)
    {
        // Higieniza para prevenir traversal
        var safeKey = Path.GetFileName(storageKey);
        var fullPath = Path.Combine(_basePath, safeKey);

        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct = default)
    {
        var safeKey = Path.GetFileName(storageKey);
        var fullPath = Path.Combine(_basePath, safeKey);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string storageKey, CancellationToken ct = default)
    {
        var safeKey = Path.GetFileName(storageKey);
        var fullPath = Path.Combine(_basePath, safeKey);

        return Task.FromResult(File.Exists(fullPath));
    }
}
