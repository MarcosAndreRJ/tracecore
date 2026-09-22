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
/// Fase 01 — Ajuste do Ecossistema: catálogos administráveis de tipos de componente
/// e integração (component_types / integration_types), campos estruturais de integração
/// (responsibility/hosting_location/direction) e auditoria das operações de integração.
/// </summary>
public class CatalogTypesAndIntegrationAuditTests : IClassFixture<TraceCoreTestApplicationFactory>
{
    private readonly TraceCoreTestApplicationFactory _factory;

    public CatalogTypesAndIntegrationAuditTests(TraceCoreTestApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    [Fact]
    public async Task CatalogTypes_AreSeededAndListableThroughServices()
    {
        using var scope = _factory.Services.CreateScope();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var integrationService = scope.ServiceProvider.GetRequiredService<IIntegrationService>();

        // Component types semeado (paridade com migration M20260921_24)
        var componentTypes = await catalogService.GetComponentTypesAsync();
        componentTypes.Should().Contain(t => t.Code == "Service" && t.Name == "Serviço");
        componentTypes.Should().Contain(t => t.Code == "Module");
        componentTypes.All(t => t.IsActive).Should().BeTrue();
        componentTypes.Select(t => t.Code).Distinct().Count().Should().Be(componentTypes.Count, "códigos do catálogo devem ser únicos");

        // Integration types semeado
        var integrationTypes = await integrationService.GetIntegrationTypesAsync();
        integrationTypes.Should().Contain(t => t.Code == "Sap" && t.Name == "SAP / ERP");
        integrationTypes.Should().Contain(t => t.Code == "Edi");
        integrationTypes.All(t => t.IsActive).Should().BeTrue();
    }

    [Fact]
    public async Task ComponentType_CreateAndDeactivate_PersistsAndAudits()
    {
        using var scope = _factory.Services.CreateScope();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var auditRepo = scope.ServiceProvider.GetRequiredService<IAuditEventRepository>();

        var id = await catalogService.CreateComponentTypeAsync("LogSystem", "Log System", currentUserId: 1L);
        id.Should().BeGreaterThan(0);

        var active = await catalogService.GetComponentTypesAsync();
        active.Should().Contain(t => t.Id == id && t.Code == "LogSystem" && t.IsActive);

        // Desativa: some da lista padrão, permanece com includeInactive
        await catalogService.DeactivateComponentTypeAsync(id, currentUserId: 1L);
        var afterDefault = await catalogService.GetComponentTypesAsync();
        afterDefault.Should().NotContain(t => t.Id == id);
        var withInactive = await catalogService.GetComponentTypesAsync(includeInactive: true);
        withInactive.Should().Contain(t => t.Id == id && !t.IsActive);

        var audit = await auditRepo.GetRecentAsync(50);
        audit.Should().Contain(e => e.Action == "component_type.create" && e.EntityType == "component_types" && e.EntityId == id.ToString());
        audit.Should().Contain(e => e.Action == "component_type.deactivate" && e.EntityType == "component_types" && e.EntityId == id.ToString());
    }

    [Fact]
    public async Task ComponentType_DeactivateWhenInUse_ThrowsInvalidOperation()
    {
        using var scope = _factory.Services.CreateScope();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();

        // Cria tipo e um componente que o usa (code 'Service' já é usado nos seeds de base)
        var productId = await catalogService.CreateProductAsync("Sistema Bloqueio", "BLOQ", null, false);
        await catalogService.CreateComponentAsync("Módulo de Bloqueio", "Module", productId, "BLOQ-MOD", "Usa tipo Module", null);

        var moduleType = (await catalogService.GetComponentTypesAsync()).First(t => t.Code == "Module");
        var act = () => catalogService.DeactivateComponentTypeAsync(moduleType.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*não pode ser inativado*");
    }

    [Fact]
    public async Task IntegrationType_CreateAndDeactivate_PersistsAndAudits()
    {
        using var scope = _factory.Services.CreateScope();
        var integrationService = scope.ServiceProvider.GetRequiredService<IIntegrationService>();
        var auditRepo = scope.ServiceProvider.GetRequiredService<IAuditEventRepository>();

        var id = await integrationService.CreateIntegrationTypeAsync("Ldap", "LDAP / AD", currentUserId: 2L);
        id.Should().BeGreaterThan(0);

        var active = await integrationService.GetIntegrationTypesAsync();
        active.Should().Contain(t => t.Id == id && t.Code == "Ldap" && t.IsActive);

        await integrationService.DeactivateIntegrationTypeAsync(id, currentUserId: 2L);
        var afterDefault = await integrationService.GetIntegrationTypesAsync();
        afterDefault.Should().NotContain(t => t.Id == id);
        var withInactive = await integrationService.GetIntegrationTypesAsync(includeInactive: true);
        withInactive.Should().Contain(t => t.Id == id && !t.IsActive);

        var audit = await auditRepo.GetRecentAsync(50);
        audit.Should().Contain(e => e.Action == "integration_type.create" && e.EntityType == "integration_types" && e.EntityId == id.ToString());
        audit.Should().Contain(e => e.Action == "integration_type.deactivate" && e.EntityType == "integration_types" && e.EntityId == id.ToString());
    }

    [Fact]
    public async Task IntegrationType_DeactivateWhenInUse_ThrowsInvalidOperation()
    {
        using var scope = _factory.Services.CreateScope();
        var integrationService = scope.ServiceProvider.GetRequiredService<IIntegrationService>();

        await integrationService.CreateIntegrationAsync(new CreateIntegrationCommand(
            Code: "INT-BLOQ",
            Name: "Integração Bloqueio",
            IntegrationType: "Sap",
            TargetSystemDescription: null,
            OwnerDepartmentId: null,
            ContractNotes: null,
            CreatedBy: null));

        var sapType = (await integrationService.GetIntegrationTypesAsync()).First(t => t.Code == "Sap");
        var act = () => integrationService.DeactivateIntegrationTypeAsync(sapType.Id);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*não pode ser inativado*");
    }

    [Fact]
    public async Task Integration_NewStructuralFields_ValidatedAndPersisted()
    {
        using var scope = _factory.Services.CreateScope();
        var integrationService = scope.ServiceProvider.GetRequiredService<IIntegrationService>();

        // Aceita valores válidos (captura case-insensitive; armazena o valor como enviado)
        var id = await integrationService.CreateIntegrationAsync(new CreateIntegrationCommand(
            Code: "INT-STR-01",
            Name: "Integração Estrutural",
            IntegrationType: "Sap",
            TargetSystemDescription: null,
            OwnerDepartmentId: null,
            ContractNotes: null,
            CreatedBy: 3L,
            Responsibility: "nossaempresa",
            HostingLocation: "Cloud-SaaS",
            Direction: "saida"));

        var created = await integrationService.GetIntegrationByIdAsync(id);
        created.Should().NotBeNull();
        created!.Responsibility.Should().Be("nossaempresa");
        created.HostingLocation.Should().Be("Cloud-SaaS");
        created.Direction.Should().Be("saida");

        // Rejeita valores fora do conjunto controlado
        var invalid = () => integrationService.CreateIntegrationAsync(new CreateIntegrationCommand(
            Code: "INT-STR-INV",
            Name: "Integração Inválida",
            IntegrationType: "Sap",
            TargetSystemDescription: null,
            OwnerDepartmentId: null,
            ContractNotes: null,
            CreatedBy: null,
            Responsibility: "Fornecedor"));
        await invalid.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Responsabilidade*");

        var invalidHost = () => integrationService.CreateIntegrationAsync(new CreateIntegrationCommand(
            Code: "INT-HOST-INV",
            Name: "Integração Hospedagem Inválida",
            IntegrationType: "Sap",
            TargetSystemDescription: null,
            OwnerDepartmentId: null,
            ContractNotes: null,
            CreatedBy: null,
            HostingLocation: "OnPremLocal"));
        await invalidHost.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Hospedagem*");

        var invalidDir = () => integrationService.CreateIntegrationAsync(new CreateIntegrationCommand(
            Code: "INT-DIR-INV",
            Name: "Integração Direção Inválida",
            IntegrationType: "Sap",
            TargetSystemDescription: null,
            OwnerDepartmentId: null,
            ContractNotes: null,
            CreatedBy: null,
            Direction: "Diagonal"));
        await invalidDir.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Direção*");

        // Sem os campos, continua criando normalmente (opcional)
        var plainId = await integrationService.CreateIntegrationAsync(new CreateIntegrationCommand(
            Code: "INT-PLAIN",
            Name: "Integração Sem Campos",
            IntegrationType: "Ticketing",
            TargetSystemDescription: null,
            OwnerDepartmentId: null,
            ContractNotes: null,
            CreatedBy: null));
        var plain = await integrationService.GetIntegrationByIdAsync(plainId);
        plain!.Responsibility.Should().BeNull();
        plain.HostingLocation.Should().BeNull();
        plain.Direction.Should().BeNull();
    }

    [Fact]
    public async Task Integration_Operations_AreAudited()
    {
        using var scope = _factory.Services.CreateScope();
        var integrationService = scope.ServiceProvider.GetRequiredService<IIntegrationService>();
        var auditRepo = scope.ServiceProvider.GetRequiredService<IAuditEventRepository>();

        var id = await integrationService.CreateIntegrationAsync(new CreateIntegrationCommand(
            Code: "INT-AUDIT-01",
            Name: "Integração Auditada",
            IntegrationType: "Monitoring",
            TargetSystemDescription: null,
            OwnerDepartmentId: null,
            ContractNotes: null,
            CreatedBy: 4L,
            Responsibility: "NossaEmpresa",
            HostingLocation: "Empresa",
            Direction: "Entrada"));

        await integrationService.UpdateIntegrationStatusAsync(id, "Active", updatedBy: 4L);
        await integrationService.ConfigureHealthCheckAsync(new ConfigureIntegrationHealthCheckCommand(
            IntegrationId: id,
            HealthCheckUrl: "http://localhost/hc",
            HealthCheckMethod: "Http",
            HealthCheckTimeoutSeconds: 10,
            HealthCheckExpectedStatusCode: 200,
            UpdatedBy: 4L));
        await integrationService.RegisterRunAsync(new RegisterIntegrationRunCommand(
            IntegrationId: id,
            Status: "Success",
            StartedAt: DateTime.UtcNow.AddMinutes(-5),
            RecordsProcessed: 500,
            ErrorMessage: null,
            RecordedBy: 4L));

        var audit = await auditRepo.GetRecentAsync(50);
        audit.Should().Contain(e => e.Action == "integration.create" && e.EntityType == "integrations" && e.EntityId == id.ToString());
        audit.Should().Contain(e => e.Action == "integration.status_update" && e.EntityType == "integrations" && e.EntityId == id.ToString());
        audit.Should().Contain(e => e.Action == "integration.healthcheck_configure" && e.EntityType == "integrations" && e.EntityId == id.ToString());
        audit.Should().Contain(e => e.Action == "integration.run_register" && e.EntityType == "integration_runs");

        var createEvent = audit.First(e => e.Action == "integration.create");
        createEvent.ActorUserId.Should().Be(4L);
    }
}