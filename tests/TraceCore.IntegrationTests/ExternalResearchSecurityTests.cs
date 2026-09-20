using System;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using TraceCore.Application.Services;
using TraceCore.Infrastructure.Services.ExternalResearch;
using Xunit;

namespace TraceCore.IntegrationTests;

/// <summary>
/// Prompt 4 — testes unitários (sem banco/host) para as duas camadas de segurança
/// da pesquisa externa: sanitização de query (§90) e proteção contra SSRF (§91-93).
/// </summary>
public class ExternalResearchSecurityTests
{
    private readonly ExternalResearchQuerySanitizer _sanitizer = new();
    private readonly ExternalUrlSafetyValidator _urlSafetyValidator = new();

    [Fact]
    public void Sanitize_RemovesClientNameHostAndPassword_KeepsTechnicalTerms()
    {
        var raw = "Cliente ABC em 10.0.0.4 com timeout. Connection string: Server=10.0.0.4;User=root;Password=secret123";

        var sanitized = _sanitizer.Sanitize(raw);

        sanitized.Should().NotContain("secret123");
        sanitized.Should().NotContain("10.0.0.4");
        sanitized.Should().Contain("timeout");
    }

    [Theory]
    [InlineData("Bearer abc123XYZ.token-value")]
    [InlineData("api_key=sk_live_12345")]
    [InlineData("password=SuperSecret1")]
    [InlineData("pwd=SuperSecret1")]
    public void Sanitize_RemovesCredentialPatterns(string secretFragment)
    {
        var raw = $"Erro ao autenticar. {secretFragment} não funcionou.";
        var sanitized = _sanitizer.Sanitize(raw);
        sanitized.Should().NotContain(secretFragment);
        sanitized.Should().Contain("[REDACTED]");
    }

    [Fact]
    public void Sanitize_RemovesEmailAndCpf()
    {
        var raw = "Usuário joao.silva@empresa.com.br, CPF 123.456.789-00 relatou o problema.";
        var sanitized = _sanitizer.Sanitize(raw);
        sanitized.Should().NotContain("joao.silva@empresa.com.br");
        sanitized.Should().NotContain("123.456.789-00");
    }

    [Fact]
    public void Sanitize_KeepsGenericTechnicalQuery_Unchanged()
    {
        var raw = "MySQL 8.4 connection timeout after upgrade";
        _sanitizer.Sanitize(raw).Should().Be(raw);
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.0.0.15")]
    [InlineData("172.16.5.5")]
    [InlineData("192.168.1.1")]
    [InlineData("169.254.169.254")] // metadata endpoint de nuvem
    [InlineData("localhost")]
    public void IsBlockedHost_RejectsPrivateLoopbackAndMetadataTargets(string host)
    {
        _urlSafetyValidator.IsBlockedHost(host).Should().BeTrue();
    }

    [Fact]
    public void IsBlockedHost_AllowsPublicIpLiteral()
    {
        _urlSafetyValidator.IsBlockedHost("8.8.8.8").Should().BeFalse();
    }

    [Theory]
    [InlineData("http://10.0.0.15/docs")]
    [InlineData("http://169.254.169.254/latest/meta-data")]
    [InlineData("http://127.0.0.1:8080/admin")]
    public async Task IsSafeAsync_RejectsPrivateAndMetadataUrls(string url)
    {
        var isSafe = await _urlSafetyValidator.IsSafeAsync(new Uri(url));
        isSafe.Should().BeFalse();
    }

    [Fact]
    public async Task IsSafeAsync_RejectsNonHttpScheme()
    {
        var isSafe = await _urlSafetyValidator.IsSafeAsync(new Uri("ftp://files.example.com/doc.txt"));
        isSafe.Should().BeFalse();
    }

    [Theory]
    [InlineData("10.0.0.1")]
    [InlineData("172.31.255.255")]
    [InlineData("192.168.255.255")]
    [InlineData("169.254.1.1")]
    [InlineData("127.0.0.1")]
    public void IsBlockedIp_CoversAllPrivateRanges(string ipLiteral)
    {
        ExternalUrlSafetyValidator.IsBlockedIp(IPAddress.Parse(ipLiteral)).Should().BeTrue();
    }

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("1.1.1.1")]
    [InlineData("172.32.0.1")] // fora do range 172.16-31 — não deve ser bloqueado
    public void IsBlockedIp_AllowsPublicAddresses(string ipLiteral)
    {
        ExternalUrlSafetyValidator.IsBlockedIp(IPAddress.Parse(ipLiteral)).Should().BeFalse();
    }
}
