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
/// Fase 02 — Ajuste do Ecossistema: cadastro, edição completa e relacionamento da
/// Integração com Sistema (ProductId), desvinculação preservando histórico, e auditoria
/// de integração.update / integration.unlink_from_product.
/// </summary>
public class IntegrationsCrudAndProductLinkIntegrationTests : IClassFixture<TraceCoreTestApplicationFactory>
{
    private readonly TraceCoreTestApplicationFactory _factory;

    public IntegrationsCrudAndProductLinkIntegrationTests(TraceCoreTestApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    [Fact]
    public async Task Integration_CreateWithProduct_PersistsProductAssociation()
    {
        using var scope = _factory.Services.CreateScope();
        var integrationService = scope.ServiceProvider.GetRequiredService<IIntegrationService>();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var deptService = scope.ServiceProvider.GetRequiredService<IDepartmentService>();

        var productId = await catalogService.CreateProductAsync("Sistema Logístico", "SIS-LOG", null, false);
        var integracoesDept = (await deptService.GetAllDepartmentsAsync()).First(d => d.Name == "Integrações");

        var id = await integrationService.CreateIntegrationAsync(new CreateIntegrationCommand(
            Code: "INT-PROD-01",
            Name: "Integração do Logístico",
            IntegrationType: "Sap",
            TargetSystemDescription: "ERP SAP",
            OwnerDepartmentId: integracoesDept.Id,
            ContractNotes: null,
            CreatedBy: null,
            ProductId: productId,
            Responsibility: "Cliente",
            HostingLocation: "Cliente",
            Direction: "Entrada"));

        var created = await integrationService.GetIntegrationByIdAsync(id);
        created.Should().NotBeNull();
        created!.ProductId.Should().Be(productId);
        created.Responsibility.Should().Be("Cliente");
        created.HostingLocation.Should().Be("Cliente");
        created.Direction.Should().Be("Entrada");
    }

    [Fact]
    public async Task Integration_Edit_PersistsAllEditableFields()
    {
        using var scope = _factory.Services.CreateScope();
        var integrationService = scope.ServiceProvider.GetRequiredService<IIntegrationService>();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();

        var productA = await catalogService.CreateProductAsync("Sistema Alpha", "ALPHA", null, false);
        var productB = await catalogService.CreateProductAsync("Sistema Beta", "BETA", null, false);

        var id = await integrationService.CreateIntegrationAsync(new CreateIntegrationCommand(
            Code: "INT-OLD",
            Name: "Nome Antigo",
            IntegrationType: "Sap",
            TargetSystemDescription: "Descrição antiga",
            OwnerDepartmentId: null,
            ContractNotes: "Nota antiga",
            CreatedBy: 1L,
            ProductId: productA,
            Responsibility: "NossaEmpresa",
            HostingLocation: "Empresa",
            Direction: "Bidirecional"));

        // Edita TODOS os campos editáveis (tipo, sistema, descrição, notas, estruturais)
        await integrationService.UpdateIntegrationAsync(new UpdateIntegrationCommand(
            Id: id,
            Code: "INT-NEW",
            Name: "Nome Novo",
            IntegrationType: "Edi",
            ProductId: productB,
            TargetSystemDescription: "Descrição nova",
            OwnerDepartmentId: null,
            ContractNotes: "Nota nova",
            Responsibility: "Cliente",
            HostingLocation: "Cloud-SaaS",
            Direction: "Saida"), currentUserId: 1L);

        var updated = await integrationService.GetIntegrationByIdAsync(id);
        updated.Should().NotBeNull();
        updated!.Code.Should().Be("INT-NEW");
        updated.Name.Should().Be("Nome Novo");
        updated.IntegrationType.Should().Be("Edi");
        updated.ProductId.Should().Be(productB);
        updated.TargetSystemDescription.Should().Be("Descrição nova");
        updated.ContractNotes.Should().Be("Nota nova");
        updated.Responsibility.Should().Be("Cliente");
        updated.HostingLocation.Should().Be("Cloud-SaaS");
        updated.Direction.Should().Be("Saida");
    }

    [Fact]
    public async Task Integration_Update_IsAuditedWithBeforeAfter()
    {
        using var scope = _factory.Services.CreateScope();
        var integrationService = scope.ServiceProvider.GetRequiredService<IIntegrationService>();
        var auditRepo = scope.ServiceProvider.GetRequiredService<IAuditEventRepository>();

        var id = await integrationService.CreateIntegrationAsync(new CreateIntegrationCommand(
            Code: "INT-AUD2",
            Name: "NomeFase2Antigo",
            IntegrationType: "Sap",
            TargetSystemDescription: null,
            OwnerDepartmentId: null,
            ContractNotes: null,
            CreatedBy: 7L));

        await integrationService.UpdateIntegrationAsync(new UpdateIntegrationCommand(
            Id: id,
            Code: "INT-AUD2",
            Name: "NomeFase2Novo",
            IntegrationType: "Webhook",
            ProductId: null,
            TargetSystemDescription: null,
            OwnerDepartmentId: null,
            ContractNotes: null,
            Responsibility: null,
            HostingLocation: null,
            Direction: null), currentUserId: 7L);

        var audit = await auditRepo.GetRecentAsync(50);
        var updateEvent = audit.FirstOrDefault(e => e.Action == "integration.update" && e.EntityType == "integrations" && e.EntityId == id.ToString());
        updateEvent.Should().NotBeNull();
        updateEvent!.ActorUserId.Should().Be(7L);
        updateEvent.BeforeJson.Should().Contain("NomeFase2Antigo");
        updateEvent.BeforeJson.Should().Contain("Sap");
        updateEvent.AfterJson.Should().Contain("NomeFase2Novo");
        updateEvent.AfterJson.Should().Contain("Webhook");
    }

    [Fact]
    public async Task Integration_UnlinkFromProduct_ZeroesProductKeepsRunsAndAudits()
    {
        using var scope = _factory.Services.CreateScope();
        var integrationService = scope.ServiceProvider.GetRequiredService<IIntegrationService>();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var auditRepo = scope.ServiceProvider.GetRequiredService<IAuditEventRepository>();

        var productId = await catalogService.CreateProductAsync("Sistema Desvincular", "DESV", null, false);
        var id = await integrationService.CreateIntegrationAsync(new CreateIntegrationCommand(
            Code: "INT-UNLINK",
            Name: "Integração a Desvincular",
            IntegrationType: "Monitoring",
            TargetSystemDescription: null,
            OwnerDepartmentId: null,
            ContractNotes: null,
            CreatedBy: 2L,
            ProductId: productId));

        await integrationService.RegisterRunAsync(new RegisterIntegrationRunCommand(
            IntegrationId: id,
            Status: "Success",
            StartedAt: DateTime.UtcNow.AddMinutes(-30),
            RecordsProcessed: 100,
            ErrorMessage: null,
            RecordedBy: 2L));

        // Atua: desvincula
        await integrationService.UnlinkIntegrationFromProductAsync(id, currentUserId: 2L);

        var integration = await integrationService.GetIntegrationByIdAsync(id);
        integration!.ProductId.Should().BeNull("desvincular deve zerar a associação com o sistema");
        integration.Runs.Should().HaveCount(1, "desvincular não pode apagar o histórico de execuções");

        var audit = await auditRepo.GetRecentAsync(50);
        audit.Should().Contain(e => e.Action == "integration.unlink_from_product" && e.EntityType == "integrations" && e.EntityId == id.ToString());

        // Idempotência: desvincular de novo não gera evento de auditoria
        await integrationService.UnlinkIntegrationFromProductAsync(id, currentUserId: 2L);
        var afterSecond = await auditRepo.GetRecentAsync(50);
        afterSecond.Count(e => e.Action == "integration.unlink_from_product" && e.EntityId == id.ToString()).Should().Be(1);
    }

    [Fact]
    public async Task Integration_Edit_DoesNotBreakCopilotInvestigationContext()
    {
        using var scope = _factory.Services.CreateScope();
        var integrationService = scope.ServiceProvider.GetRequiredService<IIntegrationService>();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var contextService = scope.ServiceProvider.GetRequiredService<IProductTechnicalContextService>();

        var productId = await catalogService.CreateProductAsync("Copiloto Contexto", "CTX", null, false);
        var id = await integrationService.CreateIntegrationAsync(new CreateIntegrationCommand(
            Code: "INT-CTX",
            Name: "Integração Contexto",
            IntegrationType: "Notification",
            TargetSystemDescription: null,
            OwnerDepartmentId: null,
            ContractNotes: null,
            CreatedBy: 3L,
            ProductId: productId));

        // Edita nome e tipo — a consulta usada pelo Copiloto deve refletir a mudança
        await integrationService.UpdateIntegrationAsync(new UpdateIntegrationCommand(
            Id: id,
            Code: "INT-CTX",
            Name: "Integração Contexto Atualizada",
            IntegrationType: "Telemetry",
            ProductId: productId,
            TargetSystemDescription: null,
            OwnerDepartmentId: null,
            ContractNotes: null,
            Responsibility: null,
            HostingLocation: null,
            Direction: null), currentUserId: 3L);

        var context = await contextService.GetInvestigationContextAsync(productId, null);
        context.Should().NotBeNull();
        var summary = context!.Integrations.FirstOrDefault(i => i.Id == id);
        summary.Should().NotBeNull();
        summary!.Name.Should().Be("Integração Contexto Atualizada");
        summary.IntegrationType.Should().Be("Telemetry");
    }
}