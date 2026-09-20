using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TraceCore.Application.DTOs;
using TraceCore.Application.Services;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;
using TraceCore.Domain.Services;
using Xunit;

namespace TraceCore.IntegrationTests;

/// <summary>
/// Prompt 4 — enforcement real das políticas de pesquisa externa (Disabled/
/// OfficialOnly/AllowListed/OpenWeb) e do comportamento sem provider configurado.
/// O provedor de pesquisa é substituído por um fake com resultados fixos: o que está
/// sob teste é a camada de decisão (ExternalResearchService), não um provedor real.
/// </summary>
public class ExternalResearchPolicyIntegrationTests : IClassFixture<TraceCoreTestApplicationFactory>
{
    private readonly TraceCoreTestApplicationFactory _factory;

    public ExternalResearchPolicyIntegrationTests(TraceCoreTestApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    private sealed class FixedResultsProvider : IExternalResearchProvider
    {
        private readonly IReadOnlyList<ExternalSearchResult> _results;
        private readonly bool _configured;
        public FixedResultsProvider(IReadOnlyList<ExternalSearchResult> results, bool configured = true)
        {
            _results = results;
            _configured = configured;
        }
        public string ProviderCode => "FakeSearch";
        public bool IsConfigured => _configured;
        public Task<IReadOnlyList<ExternalSearchResult>> SearchAsync(string sanitizedQuery, int limit, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<ExternalSearchResult>>(_results.Take(limit).ToList());
    }

    private sealed class FakeProviderFactory : IExternalResearchProviderFactory
    {
        private readonly IExternalResearchProvider _provider;
        public FakeProviderFactory(IExternalResearchProvider provider) => _provider = provider;
        public Task<IExternalResearchProvider> GetProviderAsync(CancellationToken ct = default) => Task.FromResult(_provider);
    }

    private static readonly ExternalSearchResult MySqlDoc = new("MySQL 8.4 Reference", "https://dev.mysql.com/doc/refman/8.4/en/", "dev.mysql.com", "snippet", null, DateTime.UtcNow);
    private static readonly ExternalSearchResult MsLearn = new("Troubleshooting guide", "https://learn.microsoft.com/troubleshoot", "learn.microsoft.com", "snippet", null, DateTime.UtcNow);
    private static readonly ExternalSearchResult RandomBlog = new("Some blog post", "https://randomblog.example.com/post", "randomblog.example.com", "snippet", null, DateTime.UtcNow);

    private Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> BuildFactoryWithProvider(IExternalResearchProvider provider)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<IExternalResearchProviderFactory>(_ => new FakeProviderFactory(provider));
            });
        });
    }

    [Fact]
    public async Task SearchAsync_WithPolicyDisabled_RejectsEvenWithProviderConfigured()
    {
        var customFactory = BuildFactoryWithProvider(new FixedResultsProvider(new[] { MsLearn }));
        using var scope = customFactory.Services.CreateScope();

        var catalogRepository = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var productId = await catalogRepository.AddProductAsync(new Product("Sistema Disabled"));
        // Sem UpsertTechnicalProfileAsync => policy default = Disabled.

        var service = scope.ServiceProvider.GetRequiredService<IExternalResearchService>();
        var outcome = await service.SearchAsync(productId, "MySQL timeout", 5, userId: 1L);

        outcome.Rejected.Should().BeTrue();
        outcome.RejectionReason.Should().Contain("não está habilitada");
        outcome.Results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_WithOfficialOnlyPolicy_KeepsOnlyRegisteredOfficialSourceDomain()
    {
        var customFactory = BuildFactoryWithProvider(new FixedResultsProvider(new[] { MySqlDoc, RandomBlog }));
        using var scope = customFactory.Services.CreateScope();

        var catalogRepository = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var contextService = scope.ServiceProvider.GetRequiredService<IProductTechnicalContextService>();

        var productId = await catalogRepository.AddProductAsync(new Product("Sistema Oficial"));
        await contextService.UpsertTechnicalProfileAsync(new UpsertProductTechnicalProfileCommand(
            productId, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            ExternalResearchPolicy: "OfficialOnly"), null);
        await contextService.AddTechnicalSourceAsync(new CreateProductTechnicalSourceCommand(
            productId, "MySQL Docs", "OfficialDocumentation", "https://dev.mysql.com/doc/", null, "Official"), null);

        var service = scope.ServiceProvider.GetRequiredService<IExternalResearchService>();
        var outcome = await service.SearchAsync(productId, "MySQL timeout", 5, userId: 1L);

        outcome.Rejected.Should().BeFalse();
        outcome.Results.Should().ContainSingle(r => r.Domain == "dev.mysql.com");
        outcome.Results.Should().NotContain(r => r.Domain == "randomblog.example.com");
    }

    [Fact]
    public async Task SearchAsync_WithAllowListedPolicy_KeepsOnlyAllowedDomain()
    {
        var customFactory = BuildFactoryWithProvider(new FixedResultsProvider(new[] { MsLearn, RandomBlog }));
        using var scope = customFactory.Services.CreateScope();

        var catalogRepository = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var contextService = scope.ServiceProvider.GetRequiredService<IProductTechnicalContextService>();

        var productId = await catalogRepository.AddProductAsync(new Product("Sistema Allowlist"));
        await contextService.UpsertTechnicalProfileAsync(new UpsertProductTechnicalProfileCommand(
            productId, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            ExternalResearchPolicy: "AllowListed"), null);
        await contextService.AddAllowedDomainAsync(productId, "learn.microsoft.com", null, null);

        var service = scope.ServiceProvider.GetRequiredService<IExternalResearchService>();
        var outcome = await service.SearchAsync(productId, "troubleshooting", 5, userId: 1L);

        outcome.Results.Should().ContainSingle(r => r.Domain == "learn.microsoft.com");
        outcome.Results.Should().NotContain(r => r.Domain == "randomblog.example.com");
    }

    [Fact]
    public async Task SearchAsync_WithOpenWebPolicy_KeepsAllResultsAndLabelsTrust()
    {
        var customFactory = BuildFactoryWithProvider(new FixedResultsProvider(new[] { MsLearn, RandomBlog }));
        using var scope = customFactory.Services.CreateScope();

        var catalogRepository = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var contextService = scope.ServiceProvider.GetRequiredService<IProductTechnicalContextService>();

        var productId = await catalogRepository.AddProductAsync(new Product("Sistema OpenWeb"));
        await contextService.UpsertTechnicalProfileAsync(new UpsertProductTechnicalProfileCommand(
            productId, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            ExternalResearchPolicy: "OpenWeb"), null);

        var service = scope.ServiceProvider.GetRequiredService<IExternalResearchService>();
        var outcome = await service.SearchAsync(productId, "troubleshooting", 5, userId: 1L);

        outcome.Results.Should().HaveCount(2);
        outcome.Results.First(r => r.Domain == "learn.microsoft.com").TrustLevel.Should().Be("Official");
        outcome.Results.First(r => r.Domain == "randomblog.example.com").TrustLevel.Should().Be("Reference");
    }

    [Fact]
    public async Task SearchAsync_PolicyAllowsButProviderNotConfigured_ReturnsNonFatalRejection()
    {
        var customFactory = BuildFactoryWithProvider(new FixedResultsProvider(Array.Empty<ExternalSearchResult>(), configured: false));
        using var scope = customFactory.Services.CreateScope();

        var catalogRepository = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var contextService = scope.ServiceProvider.GetRequiredService<IProductTechnicalContextService>();

        var productId = await catalogRepository.AddProductAsync(new Product("Sistema Sem Provider"));
        await contextService.UpsertTechnicalProfileAsync(new UpsertProductTechnicalProfileCommand(
            productId, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            ExternalResearchPolicy: "OpenWeb"), null);

        var service = scope.ServiceProvider.GetRequiredService<IExternalResearchService>();
        var outcome = await service.SearchAsync(productId, "troubleshooting", 5, userId: 1L);

        outcome.Rejected.Should().BeTrue();
        outcome.RejectionReason.Should().Contain("não há provedor configurado");
    }

    [Fact]
    public async Task SearchAsync_OnSuccess_RecordsAuditEventWithSanitizedQuery()
    {
        var customFactory = BuildFactoryWithProvider(new FixedResultsProvider(new[] { MsLearn }));
        using var scope = customFactory.Services.CreateScope();

        var catalogRepository = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var contextService = scope.ServiceProvider.GetRequiredService<IProductTechnicalContextService>();
        var auditRepository = scope.ServiceProvider.GetRequiredService<IAuditEventRepository>();

        var productId = await catalogRepository.AddProductAsync(new Product("Sistema Auditoria"));
        await contextService.UpsertTechnicalProfileAsync(new UpsertProductTechnicalProfileCommand(
            productId, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            ExternalResearchPolicy: "OpenWeb"), null);

        var service = scope.ServiceProvider.GetRequiredService<IExternalResearchService>();
        await service.SearchAsync(productId, "password=SuperSecret1 troubleshooting", 5, userId: 1L);

        var events = await auditRepository.GetRecentAsync(20);
        var auditEvent = events.FirstOrDefault(e => e.Action == "external_research.executed");
        auditEvent.Should().NotBeNull();
        auditEvent!.AfterJson.Should().NotContain("SuperSecret1");
    }
}
