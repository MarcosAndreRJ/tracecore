using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TraceCore.Application.Services;
using TraceCore.Domain.Repositories;
using Xunit;

namespace TraceCore.IntegrationTests;

/// <summary>
/// Fase 03 — Ajuste do Ecossistema: edição e inativação de componentes pela camada de
/// serviço (reaproveitando UpdateComponentAsync auditado), tipo alimentado pelo catálogo
/// component_types, e preservação do histórico ao inativar.
/// </summary>
public class ComponentEditAndDeactivateTests : IClassFixture<TraceCoreTestApplicationFactory>
{
    private readonly TraceCoreTestApplicationFactory _factory;

    public ComponentEditAndDeactivateTests(TraceCoreTestApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    [Fact]
    public async Task Component_Edit_PersistsAllEditableFields()
    {
        using var scope = _factory.Services.CreateScope();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var departmentService = scope.ServiceProvider.GetRequiredService<IDepartmentService>();

        var productId = await catalogService.CreateProductAsync("Portal Corporativo", "PORTAL", null, false);
        var dept = (await departmentService.GetAllDepartmentsAsync()).First();

        var id = await catalogService.CreateComponentAsync(
            name: "API Legada",
            componentType: "Servico",
            productId: null,
            code: "CMP-LEG",
            description: "Componente legado",
            ownerDepartmentId: null,
            currentUserId: 1L);

        // Edita todos os campos editáveis (tipo legado -> catálogo, produto, código, descrição, departamento)
        await catalogService.UpdateComponentAsync(
            id: id,
            name: "API Renomeada",
            componentType: "Module",
            productId: productId,
            code: "CMP-NEW",
            description: "Componente atualizado",
            ownerDepartmentId: dept.Id,
            status: "Active",
            currentUserId: 1L);

        var updated = await catalogService.GetComponentByIdAsync(id);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("API Renomeada");
        updated.ComponentType.Should().Be("Module", "o tipo agora vem do catálogo component_types");
        updated.ProductId.Should().Be(productId);
        updated.Code.Should().Be("CMP-NEW");
        updated.Description.Should().Be("Componente atualizado");
        updated.OwnerDepartmentId.Should().Be(dept.Id);
        updated.Status.Should().Be("Active");
    }

    [Fact]
    public async Task Component_Update_IsAuditedWithBeforeAfter()
    {
        using var scope = _factory.Services.CreateScope();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var auditRepo = scope.ServiceProvider.GetRequiredService<IAuditEventRepository>();

        var id = await catalogService.CreateComponentAsync("Componente Audit", "Api", null, null, null, null, currentUserId: 5L);

        await catalogService.UpdateComponentAsync(
            id: id,
            name: "Componente Audit Editado",
            componentType: "DesktopModule",
            productId: null,
            code: "CMP-AUD",
            description: null,
            ownerDepartmentId: null,
            status: "Active",
            currentUserId: 5L);

        var audit = await auditRepo.GetRecentAsync(50);
        var updateEvent = audit.FirstOrDefault(e => e.Action == "component.update" && e.EntityType == "components" && e.EntityId == id.ToString());
        updateEvent.Should().NotBeNull();
        updateEvent!.ActorUserId.Should().Be(5L);
        updateEvent.BeforeJson.Should().Contain("Componente Audit");
        updateEvent.BeforeJson.Should().Contain("Api");
        updateEvent.AfterJson.Should().Contain("Componente Audit Editado");
        updateEvent.AfterJson.Should().Contain("DesktopModule");
    }

    [Fact]
    public async Task Component_Deactivate_KeepsDependenciesAndAudits()
    {
        using var scope = _factory.Services.CreateScope();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var auditRepo = scope.ServiceProvider.GetRequiredService<IAuditEventRepository>();

        var id = await catalogService.CreateComponentAsync("Componente C", "Service", null, null, null, null, currentUserId: 2L);
        var id2 = await catalogService.CreateComponentAsync("Componente D", "Database", null, null, null, null, currentUserId: 2L);
        await catalogService.AddComponentDependencyAsync(id, id2, "Runtime", "High", "Depende em execução", currentUserId: 2L);

        await catalogService.UpdateComponentAsync(
            id: id,
            name: "Componente C",
            componentType: "Service",
            productId: null,
            code: null,
            description: null,
            ownerDepartmentId: null,
            status: "Inactive",
            currentUserId: 2L);

        var updated = await catalogService.GetComponentByIdAsync(id);
        updated!.Status.Should().Be("Inactive");

        // Inativar NÃO apaga dependências/histórico
        var dependencies = await catalogService.GetComponentDependenciesAsync();
        dependencies.Count(d => d.SourceComponentId == id || d.TargetComponentId == id).Should().Be(1, "inativação não pode remover dependências");

        var audit = await auditRepo.GetRecentAsync(50);
        var updateEvent = audit.FirstOrDefault(e => e.Action == "component.update" && e.EntityId == id.ToString());
        updateEvent.Should().NotBeNull();
        updateEvent!.AfterJson.Should().Contain("Inactive");
    }

    [Fact]
    public async Task ComponentTypes_Catalog_FeedsUiOptions()
    {
        using var scope = _factory.Services.CreateScope();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();

        var types = await catalogService.GetComponentTypesAsync();

        types.Should().NotBeNull();
        types.Count(t => t.Code == "Module").Should().Be(1, "o catálogo deve incluir Módulo");
        types.Count(t => t.Code == "DesktopModule").Should().Be(1, "o catálogo deve incluir Módulo Desktop");
        types.Count(t => t.Code == "Service").Should().Be(1, "o catálogo deve incluir Service (padrão atual)");
        types.Should().OnlyContain(t => t.IsActive, "todos os tipos ativos alimentam o select");
    }
}