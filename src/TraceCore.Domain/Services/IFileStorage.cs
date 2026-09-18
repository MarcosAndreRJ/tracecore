using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TraceCore.Domain.Services;

/// <summary>
/// Abstração de armazenamento de arquivos e anexos.
/// ADR-P004 fechado para o MVP com implementação em Filesystem local/corporativo.
/// </summary>
public interface IFileStorage
{
    Task<string> SaveAsync(string fileName, Stream contentStream, string contentType, CancellationToken ct = default);
    Task<Stream?> GetAsync(string storageKey, CancellationToken ct = default);
    Task DeleteAsync(string storageKey, CancellationToken ct = default);
    Task<bool> ExistsAsync(string storageKey, CancellationToken ct = default);
}
