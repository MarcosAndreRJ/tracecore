using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace TraceCore.Infrastructure.Services.ExternalResearch;

/// <summary>
/// Prompt 4, §57/§58 — proteção contra SSRF na pesquisa externa. Antes de qualquer
/// chamada HTTP de saída (para o provedor de pesquisa configurado, ou para uma futura
/// fonte específica), o alvo precisa ser validado: só http/https, sem IP privado/
/// loopback/link-local/metadata-endpoint, resolvendo o hostname de verdade (não confia
/// só na string) para não ser enganado por DNS apontando para rede interna.
/// Não é reaproveitado de IntegrationHealthCheckService — aquele código não tem
/// nenhuma proteção equivalente hoje.
/// </summary>
public interface IExternalUrlSafetyValidator
{
    Task<bool> IsSafeAsync(Uri uri, CancellationToken ct = default);
    bool IsBlockedHost(string host);
}

public class ExternalUrlSafetyValidator : IExternalUrlSafetyValidator
{
    // Endpoints de metadata de nuvem — nunca acessíveis via pesquisa externa.
    private static readonly string[] BlockedHostLiterals = { "169.254.169.254", "metadata.google.internal" };

    public bool IsBlockedHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host)) return true;

        host = host.Trim().ToLowerInvariant();

        if (BlockedHostLiterals.Contains(host)) return true;
        if (host == "localhost") return true;

        if (IPAddress.TryParse(host, out var ip))
        {
            return IsBlockedIp(ip);
        }

        return false;
    }

    public async Task<bool> IsSafeAsync(Uri uri, CancellationToken ct = default)
    {
        if (uri == null) return false;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return false;

        if (IsBlockedHost(uri.Host)) return false;

        // Resolve DNS de verdade — um hostname público pode ter sido configurado para
        // apontar para um IP interno (DNS rebinding / erro de configuração).
        IPAddress[] addresses;
        try
        {
            addresses = await Dns.GetHostAddressesAsync(uri.Host, ct);
        }
        catch
        {
            // Falha de resolução não é "seguro por padrão" — trata como inseguro.
            return false;
        }

        if (addresses.Length == 0) return false;

        return addresses.All(a => !IsBlockedIp(a));
    }

    public static bool IsBlockedIp(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip)) return true;

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = ip.GetAddressBytes();

            // 10.0.0.0/8
            if (bytes[0] == 10) return true;
            // 172.16.0.0/12
            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
            // 192.168.0.0/16
            if (bytes[0] == 192 && bytes[1] == 168) return true;
            // 169.254.0.0/16 (link-local, inclui metadata endpoints de nuvem)
            if (bytes[0] == 169 && bytes[1] == 254) return true;
            // 0.0.0.0/8
            if (bytes[0] == 0) return true;
            // 100.64.0.0/10 (carrier-grade NAT)
            if (bytes[0] == 100 && bytes[1] >= 64 && bytes[1] <= 127) return true;

            return false;
        }

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6Multicast) return true;

            var bytes = ip.GetAddressBytes();
            // fc00::/7 (unique local address)
            if ((bytes[0] & 0xFE) == 0xFC) return true;

            return false;
        }

        return true; // família de endereço desconhecida — não confiar.
    }
}
