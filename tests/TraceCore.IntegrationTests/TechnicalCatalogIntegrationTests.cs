using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TraceCore.Application.Services;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;
using Xunit;

namespace TraceCore.IntegrationTests;

public class TechnicalCatalogIntegrationTests : IClassFixture<TraceCoreTestApplicationFactory>
{
    private readonly TraceCoreTestApplicationFactory _factory;

    public TechnicalCatalogIntegrationTests(TraceCoreTestApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    [Fact]
    public async Task Product_WithIsExternalFlag_PersistsAndLoadsCorrectly()
    {
        using var scope = _factory.Services.CreateScope();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();

        // Act: Cadastra produto externo (ex.: Gateway de Pagamento Terceiro)
        var externalProdId = await catalogService.CreateProductAsync(
            name: "Stripe API Gateway",
            code: "EXT-STRIPE",
            description: "Gateway de cobrança e pagamentos externo",
            isExternal: true);

        // Cadastra produto interno
        var internalProdId = await catalogService.CreateProductAsync(
            name: "TraceCore Core App",
            code: "TC-CORE",
            description: "Plataforma principal interna",
            isExternal: false);

        // Assert
        var extProduct = await catalogService.GetProductByIdAsync(externalProdId);
        extProduct.Should().NotBeNull();
        extProduct!.IsExternal.Should().BeTrue();
        extProduct.Code.Should().Be("EXT-STRIPE");

        var intProduct = await catalogService.GetProductByIdAsync(internalProdId);
        intProduct.Should().NotBeNull();
        intProduct!.IsExternal.Should().BeFalse();
    }

    [Fact]
    public async Task ComponentDependency_DomainRule_BlocksSelfDependency()
    {
        using var scope = _factory.Services.CreateScope();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();

        var compId = await catalogService.CreateComponentAsync(
            name: "Serviço de Cobrança",
            componentType: "Service",
            productId: null,
            code: "CMP-COB",
            description: null,
            ownerDepartmentId: null);

        // Act & Assert: Bloqueia source == target
        Func<Task> act = async () =>
        {
            await catalogService.AddComponentDependencyAsync(
                sourceComponentId: compId,
                targetComponentId: compId,
                dependencyType: "Synchronous",
                criticality: "High",
                description: "Auto-dependência inválida");
        };

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*não pode depender de si mesmo*");
    }

    [Fact]
    public async Task ComponentDependency_Crud_CreatesQueriesAndDeletesCorrectly()
    {
        using var scope = _factory.Services.CreateScope();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();

        var apiCompId = await catalogService.CreateComponentAsync(
            name: "TraceCore Web API",
            componentType: "Gateway",
            productId: null,
            code: "API-01",
            description: null,
            ownerDepartmentId: null);

        var dbCompId = await catalogService.CreateComponentAsync(
            name: "MariaDB Cluster",
            componentType: "Database",
            productId: null,
            code: "DB-01",
            description: null,
            ownerDepartmentId: null);

        // Act: Adiciona dependência API -> DB
        var depId = await catalogService.AddComponentDependencyAsync(
            sourceComponentId: apiCompId,
            targetComponentId: dbCompId,
            dependencyType: "Database",
            criticality: "Critical",
            description: "Persistência relacional direta via TCP 3306");

        depId.Should().BeGreaterThan(0);

        // Consulta dependências
        var deps = await catalogService.GetComponentDependenciesAsync(apiCompId);
        deps.Should().ContainSingle(d => d.Id == depId);
        var dep = deps.First();
        dep.SourceComponentId.Should().Be(apiCompId);
        dep.TargetComponentId.Should().Be(dbCompId);
        dep.DependencyType.Should().Be("Database");
        dep.Criticality.Should().Be("Critical");

        // Remove dependência
        var deleted = await catalogService.DeleteComponentDependencyAsync(depId);
        deleted.Should().BeTrue();

        var depsAfter = await catalogService.GetComponentDependenciesAsync(apiCompId);
        depsAfter.Should().BeEmpty();
    }

    [Fact]
    public async Task ComponentOwner_OfficialDepartment_AssociatesAndQueriesRolesCorrectly()
    {
        using var scope = _factory.Services.CreateScope();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var deptService = scope.ServiceProvider.GetRequiredService<IDepartmentService>();

        var departments = await deptService.GetAllDepartmentsAsync();
        var devDept = departments.First(d => d.Name == "Desenvolvimento Web");
        var infraDept = departments.First(d => d.Name == "Infraestrutura");

        var compId = await catalogService.CreateComponentAsync(
            name: "Portal de Relatórios",
            componentType: "Frontend",
            productId: null,
            code: "REP-FRONT",
            description: null,
            ownerDepartmentId: null);

        // Act: Vincula Desenvolvimento Web como Primary
        var owner1Id = await catalogService.AddComponentOwnerAsync(compId, devDept.Id, "Primary");
        // Vincula Infraestrutura como Escalation
        var owner2Id = await catalogService.AddComponentOwnerAsync(compId, infraDept.Id, "Escalation");

        // Assert
        var owners = await catalogService.GetComponentOwnersAsync(compId);
        owners.Should().HaveCount(2);

        owners.Should().Contain(o => o.DepartmentId == devDept.Id && o.OwnershipRole == "Primary");
        owners.Should().Contain(o => o.DepartmentId == infraDept.Id && o.OwnershipRole == "Escalation");

        // Remove um vínculo
        var deleted = await catalogService.DeleteComponentOwnerAsync(owner2Id);
        deleted.Should().BeTrue();

        var ownersAfter = await catalogService.GetComponentOwnersAsync(compId);
        ownersAfter.Should().ContainSingle(o => o.DepartmentId == devDept.Id);
    }

    [Fact]
    public async Task Permission_CatalogoGerenciar_IsGrantedToAdminAndAdminFuncional()
    {
        using var scope = _factory.Services.CreateScope();
        var roleRepository = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
        var permRepository = scope.ServiceProvider.GetRequiredService<IPermissionRepository>();

        var allPerms = await permRepository.GetAllAsync();
        var catPerm = allPerms.FirstOrDefault(p => p.Code == "catalogo.gerenciar");
        catPerm.Should().NotBeNull("Permissão catalogo.gerenciar deve existir no catálogo de permissões");

        var adminFuncionalRole = await roleRepository.GetByNameAsync("Admin Funcional");
        adminFuncionalRole.Should().NotBeNull();
        var adminFuncPerms = await roleRepository.GetRolePermissionsAsync(adminFuncionalRole!.Id);
        adminFuncPerms.Should().Contain(p => p.Code == "catalogo.gerenciar");

        var adminRole = await roleRepository.GetByNameAsync("Admin");
        adminRole.Should().NotBeNull();
        var adminPerms = await roleRepository.GetRolePermissionsAsync(adminRole!.Id);
        adminPerms.Should().Contain(p => p.Code == "catalogo.gerenciar");
    }
}
