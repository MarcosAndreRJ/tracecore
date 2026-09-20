using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TraceCore.Application.DTOs;
using TraceCore.Application.Services;
using Xunit;

namespace TraceCore.IntegrationTests;

public class ProductTechnicalContextIntegrationTests : IClassFixture<TraceCoreTestApplicationFactory>
{
    private readonly TraceCoreTestApplicationFactory _factory;

    public ProductTechnicalContextIntegrationTests(TraceCoreTestApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    [Fact]
    public async Task Product_WithoutTechnicalProfile_ContinuesWorkingNormally()
    {
        using var scope = _factory.Services.CreateScope();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var contextService = scope.ServiceProvider.GetRequiredService<IProductTechnicalContextService>();

        var productId = await catalogService.CreateProductAsync("Sistema Sem Perfil", "SEM-PERFIL", null, false);

        var profile = await contextService.GetTechnicalProfileAsync(productId);
        profile.Should().BeNull();

        var context = await contextService.GetInvestigationContextAsync(productId, null);
        context.Should().NotBeNull();
        context!.TechnicalProfile.Should().BeNull();
        context.ExternalResearchPolicy.Should().Be("Disabled");
        context.Technologies.Should().BeEmpty();
    }

    [Fact]
    public async Task UpsertTechnicalProfile_PersistsAndIsRetrievable()
    {
        using var scope = _factory.Services.CreateScope();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var contextService = scope.ServiceProvider.GetRequiredService<IProductTechnicalContextService>();

        var productId = await catalogService.CreateProductAsync("Sistema X", "SIS-X", "Sistema de teste", false);

        await contextService.UpsertTechnicalProfileAsync(new UpsertProductTechnicalProfileCommand(
            ProductId: productId,
            BusinessPurpose: "Gestão de pedidos e vendas",
            ArchitectureSummary: "Frontend Web -> API -> MySQL",
            FrontendStack: "React",
            BackendStack: ".NET 10",
            PrimaryDatabase: "MySQL 8.4",
            RuntimePlatform: ".NET 10",
            HostingModel: "Docker / Linux",
            AuthenticationModel: "OAuth2 / Keycloak",
            ObservabilityStack: "Grafana",
            DeploymentModel: "GitHub Actions",
            Vendor: "Interno",
            SupportNotes: null,
            KnownConstraints: null,
            InvestigationNotes: null,
            ExternalResearchPolicy: "OfficialOnly"), currentUserId: null);

        var profile = await contextService.GetTechnicalProfileAsync(productId);
        profile.Should().NotBeNull();
        profile!.BusinessPurpose.Should().Be("Gestão de pedidos e vendas");
        profile.ExternalResearchPolicy.Should().Be("OfficialOnly");

        // Atualização (upsert sobre o mesmo produto)
        await contextService.UpsertTechnicalProfileAsync(new UpsertProductTechnicalProfileCommand(
            ProductId: productId,
            BusinessPurpose: "Gestão de pedidos, vendas e faturamento",
            ArchitectureSummary: profile.ArchitectureSummary,
            FrontendStack: profile.FrontendStack,
            BackendStack: profile.BackendStack,
            PrimaryDatabase: profile.PrimaryDatabase,
            RuntimePlatform: profile.RuntimePlatform,
            HostingModel: profile.HostingModel,
            AuthenticationModel: profile.AuthenticationModel,
            ObservabilityStack: profile.ObservabilityStack,
            DeploymentModel: profile.DeploymentModel,
            Vendor: profile.Vendor,
            SupportNotes: null,
            KnownConstraints: null,
            InvestigationNotes: null,
            ExternalResearchPolicy: "AllowListed"), currentUserId: null);

        var updated = await contextService.GetTechnicalProfileAsync(productId);
        updated!.BusinessPurpose.Should().Be("Gestão de pedidos, vendas e faturamento");
        updated.ExternalResearchPolicy.Should().Be("AllowListed");
    }

    [Fact]
    public async Task UpsertTechnicalProfile_WithInvalidPolicy_Throws()
    {
        using var scope = _factory.Services.CreateScope();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var contextService = scope.ServiceProvider.GetRequiredService<IProductTechnicalContextService>();

        var productId = await catalogService.CreateProductAsync("Sistema Y", null, null, false);

        var act = async () => await contextService.UpsertTechnicalProfileAsync(new UpsertProductTechnicalProfileCommand(
            productId, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            ExternalResearchPolicy: "Anything"), currentUserId: null);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SetProductTechnologies_PersistsAndReusesExistingTechnologyByName()
    {
        using var scope = _factory.Services.CreateScope();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var contextService = scope.ServiceProvider.GetRequiredService<IProductTechnicalContextService>();

        var productId = await catalogService.CreateProductAsync("Sistema Z", null, null, false);

        await contextService.SetProductTechnologiesAsync(productId, new[] { ".NET", "MySQL", "Redis" }, null);

        var technologies = await contextService.GetProductTechnologiesAsync(productId);
        technologies.Should().BeEquivalentTo(new[] { ".NET", "MySQL", "Redis" });

        // Redefinir substitui a lista anterior
        await contextService.SetProductTechnologiesAsync(productId, new[] { "MySQL", "Docker" }, null);
        var updated = await contextService.GetProductTechnologiesAsync(productId);
        updated.Should().BeEquivalentTo(new[] { "MySQL", "Docker" });
    }

    [Fact]
    public async Task TechnicalSource_AddAndDisable_KeepsHistoryButHidesFromDefaultList()
    {
        using var scope = _factory.Services.CreateScope();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var contextService = scope.ServiceProvider.GetRequiredService<IProductTechnicalContextService>();

        var productId = await catalogService.CreateProductAsync("Sistema Fontes", null, null, false);

        var sourceId = await contextService.AddTechnicalSourceAsync(new CreateProductTechnicalSourceCommand(
            productId, "MySQL Docs", "OfficialDocumentation", "https://dev.mysql.com/doc/refman/8.4/en/", null, "Official"), null);

        var sources = await contextService.GetTechnicalSourcesAsync(productId);
        sources.Should().ContainSingle(s => s.Id == sourceId && s.IsActive);

        await contextService.DisableTechnicalSourceAsync(sourceId, null);

        var afterDisable = await contextService.GetTechnicalSourcesAsync(productId);
        afterDisable.Should().ContainSingle(s => s.Id == sourceId);
        afterDisable.First(s => s.Id == sourceId).IsActive.Should().BeFalse("desativar não remove o histórico, só marca como inativa");
    }

    [Fact]
    public async Task AllowedDomain_AddAndRemove_Works()
    {
        using var scope = _factory.Services.CreateScope();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var contextService = scope.ServiceProvider.GetRequiredService<IProductTechnicalContextService>();

        var productId = await catalogService.CreateProductAsync("Sistema Allowlist", null, null, false);

        var domainId = await contextService.AddAllowedDomainAsync(productId, "learn.microsoft.com", "Documentação Microsoft", null);
        var domains = await contextService.GetAllowedDomainsAsync(productId);
        domains.Should().ContainSingle(d => d.Id == domainId && d.Domain == "learn.microsoft.com");

        await contextService.RemoveAllowedDomainAsync(domainId, null);
        var afterRemoval = await contextService.GetAllowedDomainsAsync(productId);
        afterRemoval.Should().NotContain(d => d.Id == domainId);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://files.example.com")]
    [InlineData("not a domain")]
    public async Task AllowedDomain_WithInvalidValue_Throws(string invalidDomain)
    {
        using var scope = _factory.Services.CreateScope();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var contextService = scope.ServiceProvider.GetRequiredService<IProductTechnicalContextService>();

        var productId = await catalogService.CreateProductAsync("Sistema Allowlist Invalido", null, null, false);

        var act = async () => await contextService.AddAllowedDomainAsync(productId, invalidDomain, null, null);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetInvestigationContext_ConsolidatesProductProfileTechnologiesComponentsDependenciesIntegrationsAndSources()
    {
        using var scope = _factory.Services.CreateScope();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var contextService = scope.ServiceProvider.GetRequiredService<IProductTechnicalContextService>();
        var integrationService = scope.ServiceProvider.GetRequiredService<IIntegrationService>();

        // Cenário do §42 do Prompt 2: ERP Corporativo
        var productId = await catalogService.CreateProductAsync("ERP Corporativo", "ERP-CORP", null, false);

        await contextService.UpsertTechnicalProfileAsync(new UpsertProductTechnicalProfileCommand(
            productId, null, null, null, ".NET", "MySQL", null, null, "Keycloak", null, null, null, null, null, null,
            ExternalResearchPolicy: "OfficialOnly"), null);

        await contextService.SetProductTechnologiesAsync(productId, new[] { ".NET", "MySQL", "Keycloak" }, null);

        var apiId = await catalogService.CreateComponentAsync("API", "API", productId, null, null, null);
        var authId = await catalogService.CreateComponentAsync("Auth", "Service", productId, null, null, null);
        var dbId = await catalogService.CreateComponentAsync("Database", "Database", productId, null, null, null);

        await catalogService.AddComponentDependencyAsync(apiId, authId, "Synchronous", "High", null);
        await catalogService.AddComponentDependencyAsync(apiId, dbId, "Database", "Critical", null);

        await integrationService.CreateIntegrationAsync(new CreateIntegrationCommand(
            Code: "INT-SAP-ERP",
            Name: "SAP",
            IntegrationType: "Sap",
            TargetSystemDescription: null,
            OwnerDepartmentId: null,
            ContractNotes: null,
            CreatedBy: null,
            ProductId: productId));

        await contextService.AddTechnicalSourceAsync(new CreateProductTechnicalSourceCommand(
            productId, "Microsoft Learn", "OfficialDocumentation", "https://learn.microsoft.com", null, "Official"), null);
        await contextService.AddTechnicalSourceAsync(new CreateProductTechnicalSourceCommand(
            productId, "MySQL Documentation", "OfficialDocumentation", "https://dev.mysql.com/doc/", null, "Official"), null);

        var context = await contextService.GetInvestigationContextAsync(productId, null);

        context.Should().NotBeNull();
        context!.ProductName.Should().Be("ERP Corporativo");
        context.TechnicalProfile.Should().NotBeNull();
        context.TechnicalProfile!.ExternalResearchPolicy.Should().Be("OfficialOnly");
        context.Technologies.Should().Contain(new[] { ".NET", "MySQL", "Keycloak" });
        context.Components.Should().HaveCount(3);
        context.Dependencies.Should().HaveCount(2);
        context.Integrations.Should().ContainSingle(i => i.Name == "SAP");
        context.TechnicalSources.Should().HaveCount(2);
        context.ExternalResearchPolicy.Should().Be("OfficialOnly");
    }
}
