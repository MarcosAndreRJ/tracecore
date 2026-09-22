using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TraceCore.Application.DTOs;
using TraceCore.Application.Services;
using TraceCore.Domain.Repositories;
using Xunit;

namespace TraceCore.IntegrationTests;

/// <summary>
/// Fase 04 — Ajuste do Ecossistema: cadastro/edição de Sistema com contexto técnico
/// básico (Tipo do Sistema), operações das abas da tela de detalhe (componentes e
/// integrações com ProductId pré-preenchido) e vínculo de integração existente.
/// </summary>
public class ProductCrudAndDetailsTabsIntegrationTests : IClassFixture<TraceCoreTestApplicationFactory>
{
    private readonly TraceCoreTestApplicationFactory _factory;

    public ProductCrudAndDetailsTabsIntegrationTests(TraceCoreTestApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    [Fact]
    public async Task CreateProduct_WithBasicTechnicalContext_CreatesProfileWithSystemType()
    {
        using var scope = _factory.Services.CreateScope();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var contextService = scope.ServiceProvider.GetRequiredService<IProductTechnicalContextService>();

        var productId = await catalogService.CreateProductAsync("Portal do Cliente", "PRD-WEB-01", "Portal web de clientes", false);

        await contextService.UpsertTechnicalProfileAsync(new UpsertProductTechnicalProfileCommand(
            productId,
            BusinessPurpose: "Autosserviço de clientes e faturamento",
            ArchitectureSummary: "Frontend Web -> API .NET -> SQL Server",
            FrontendStack: null,
            BackendStack: ".NET 8 + Angular 17",
            PrimaryDatabase: "SQL Server",
            RuntimePlatform: null,
            HostingModel: "Cloud (Azure)",
            AuthenticationModel: null,
            ObservabilityStack: null,
            DeploymentModel: null,
            Vendor: null,
            SupportNotes: null,
            KnownConstraints: null,
            InvestigationNotes: null,
            ExternalResearchPolicy: "Disabled",
            SystemType: "Web"), currentUserId: null);

        var profile = await contextService.GetTechnicalProfileAsync(productId);
        profile.Should().NotBeNull();
        profile!.SystemType.Should().Be("Web");
        profile.BackendStack.Should().Be(".NET 8 + Angular 17");
        profile.PrimaryDatabase.Should().Be("SQL Server");
        profile.HostingModel.Should().Be("Cloud (Azure)");
        profile.BusinessPurpose.Should().Be("Autosserviço de clientes e faturamento");

        // O contexto consolidado do Copiloto expõe o Tipo do Sistema
        var context = await contextService.GetInvestigationContextAsync(productId, null);
        context.Should().NotBeNull();
        context!.TechnicalProfile.Should().NotBeNull();
        context.TechnicalProfile!.SystemType.Should().Be("Web");
    }

    [Fact]
    public async Task CreateProduct_IdentificationOnly_DoesNotRequireTechnicalFields()
    {
        using var scope = _factory.Services.CreateScope();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();

        var productId = await catalogService.CreateProductAsync("Sistema Legado", "SIS-LEG-01", null, false);

        var created = await catalogService.GetProductByIdAsync(productId);
        created.Should().NotBeNull();
        created!.Name.Should().Be("Sistema Legado");
    }

    [Fact]
    public async Task UpsertTechnicalProfile_WithInvalidSystemType_Throws()
    {
        using var scope = _factory.Services.CreateScope();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var contextService = scope.ServiceProvider.GetRequiredService<IProductTechnicalContextService>();

        var productId = await catalogService.CreateProductAsync("Sistema Tipo Invalido", null, null, false);

        var act = async () => await contextService.UpsertTechnicalProfileAsync(new UpsertProductTechnicalProfileCommand(
            productId,
            BusinessPurpose: null,
            ArchitectureSummary: null,
            FrontendStack: null,
            BackendStack: null,
            PrimaryDatabase: null,
            RuntimePlatform: null,
            HostingModel: null,
            AuthenticationModel: null,
            ObservabilityStack: null,
            DeploymentModel: null,
            Vendor: null,
            SupportNotes: null,
            KnownConstraints: null,
            InvestigationNotes: null,
            ExternalResearchPolicy: "Disabled",
            SystemType: "Mainframe"), currentUserId: null);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetTechnicalProfilesFor_ReturnsMapOnlyForExistingProfiles()
    {
        using var scope = _factory.Services.CreateScope();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var contextService = scope.ServiceProvider.GetRequiredService<IProductTechnicalContextService>();

        var withProfile = await catalogService.CreateProductAsync("Com Perfil", null, null, false);
        var withoutProfile = await catalogService.CreateProductAsync("Sem Perfil", null, null, false);

        await contextService.UpsertTechnicalProfileAsync(new UpsertProductTechnicalProfileCommand(
            withProfile, null, null, null, ".NET", null, null, null, null, null, null, null, null, null, null,
            ExternalResearchPolicy: "Disabled",
            SystemType: "Api"), null);

        var map = await contextService.GetTechnicalProfilesForAsync(new[] { withProfile, withoutProfile });

        map.Should().HaveCount(1);
        map.Should().ContainKey(withProfile);
        map[withProfile].SystemType.Should().Be("Api");
        map.Should().NotContainKey(withoutProfile);
    }

    [Fact]
    public async Task LinkIntegrationToProduct_LinksPreservesRunsAndAudits()
    {
        using var scope = _factory.Services.CreateScope();
        var integrationService = scope.ServiceProvider.GetRequiredService<IIntegrationService>();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var auditRepo = scope.ServiceProvider.GetRequiredService<IAuditEventRepository>();

        var productId = await catalogService.CreateProductAsync("Sistema Alvo", "ALVO", null, false);
        var id = await integrationService.CreateIntegrationAsync(new CreateIntegrationCommand(
            Code: "INT-EXIST",
            Name: "Integração Existente",
            IntegrationType: "RestApi",
            TargetSystemDescription: null,
            OwnerDepartmentId: null,
            ContractNotes: null,
            CreatedBy: 1L));

        await integrationService.RegisterRunAsync(new RegisterIntegrationRunCommand(
            IntegrationId: id,
            Status: "Success",
            StartedAt: DateTime.UtcNow.AddHours(-1),
            RecordsProcessed: 50,
            ErrorMessage: null,
            RecordedBy: 1L));

        // Atua: vincula a integração já cadastrada ao sistema da tela de detalhe
        await integrationService.LinkIntegrationToProductAsync(id, productId, currentUserId: 1L);

        var integration = await integrationService.GetIntegrationByIdAsync(id);
        integration!.ProductId.Should().Be(productId, "vincular deve associar a integração ao sistema");
        integration.Runs.Should().HaveCount(1, "vincular não pode apagar o histórico de execuções");

        var audit = await auditRepo.GetRecentAsync(50);
        audit.Should().Contain(e => e.Action == "integration.link_to_product" && e.EntityType == "integrations" && e.EntityId == id.ToString());
        var linkEvent = audit.First(e => e.Action == "integration.link_to_product" && e.EntityId == id.ToString());
        linkEvent.ActorUserId.Should().Be(1L);
        linkEvent.AfterJson.Should().Contain("\"productId\":" + productId.ToString());
    }

    [Fact]
    public async Task LinkIntegrationToProduct_ThenUnlink_RoundTripPreservesHistory()
    {
        using var scope = _factory.Services.CreateScope();
        var integrationService = scope.ServiceProvider.GetRequiredService<IIntegrationService>();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();

        var productId = await catalogService.CreateProductAsync("Sistema Ida e Volta", "IDAVOLTA", null, false);
        var id = await integrationService.CreateIntegrationAsync(new CreateIntegrationCommand(
            Code: "INT-RT",
            Name: "Integração Round-Trip",
            IntegrationType: "Sftp",
            TargetSystemDescription: null,
            OwnerDepartmentId: null,
            ContractNotes: null,
            CreatedBy: 2L,
            ProductId: productId));

        await integrationService.LinkIntegrationToProductAsync(id, productId, currentUserId: 2L); // idempotente
        await integrationService.UnlinkIntegrationFromProductAsync(id, currentUserId: 2L);

        var integration = await integrationService.GetIntegrationByIdAsync(id);
        integration!.ProductId.Should().BeNull();
        integration.Runs.Should().BeEmpty();
    }

    [Fact]
    public async Task LinkIntegrationToProduct_Idempotent_DoesNotDuplicateAudit()
    {
        using var scope = _factory.Services.CreateScope();
        var integrationService = scope.ServiceProvider.GetRequiredService<IIntegrationService>();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var auditRepo = scope.ServiceProvider.GetRequiredService<IAuditEventRepository>();

        var productId = await catalogService.CreateProductAsync("Sistema Idempotente", "IDEMP", null, false);
        var id = await integrationService.CreateIntegrationAsync(new CreateIntegrationCommand(
            Code: "INT-IDEMP",
            Name: "Integração Idempotente",
            IntegrationType: "MessageQueue",
            TargetSystemDescription: null,
            OwnerDepartmentId: null,
            ContractNotes: null,
            CreatedBy: 3L));

        await integrationService.LinkIntegrationToProductAsync(id, productId, currentUserId: 3L);
        await integrationService.LinkIntegrationToProductAsync(id, productId, currentUserId: 3L);

        var audit = await auditRepo.GetRecentAsync(50);
        audit.Count(e => e.Action == "integration.link_to_product" && e.EntityId == id.ToString()).Should().Be(1);
    }

    [Fact]
    public async Task Component_AddEditDeactivateUnlink_FromProductPersist()
    {
        using var scope = _factory.Services.CreateScope();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();

        var productId = await catalogService.CreateProductAsync("Sistema Componentes", "COMP", null, false);

        var componentId = await catalogService.CreateComponentAsync("API de Cadastro", "Api", productId, "CMP-API-01", null, null);
        var scoped = await catalogService.GetAllComponentsAsync(productId);
        scoped.Should().ContainSingle(c => c.Id == componentId);
        scoped.First(c => c.Id == componentId).ProductId.Should().Be(productId);

        // Editar preservando o vínculo (como faz o modal da aba Componentes)
        await catalogService.UpdateComponentAsync(componentId, "API de Cadastro v2", "Api", productId, "CMP-API-01", "API de cadastro de clientes", null, "Active");
        var edited = await catalogService.GetComponentByIdAsync(componentId);
        edited!.Name.Should().Be("API de Cadastro v2");
        edited.ProductId.Should().Be(productId);

        // Inativar preserva o vínculo (componente continua visível na aba, como Inativo)
        await catalogService.UpdateComponentAsync(componentId, edited.Name, edited.ComponentType, productId, edited.Code, edited.Description, edited.OwnerDepartmentId, "Inactive");
        var inactive = await catalogService.GetAllComponentsAsync(productId);
        inactive.Should().ContainSingle(c => c.Id == componentId && c.Status == "Inactive");

        // Desvincular: componente sai da aba mas permanece no catálogo geral
        await catalogService.UpdateComponentAsync(componentId, edited.Name, edited.ComponentType, null, edited.Code, edited.Description, edited.OwnerDepartmentId, "Inactive");
        var afterUnlink = await catalogService.GetAllComponentsAsync(productId);
        afterUnlink.Should().NotContain(c => c.Id == componentId);
        var globalList = await catalogService.GetAllComponentsAsync();
        globalList.Should().Contain(c => c.Id == componentId);
    }
}