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

public class IntegrationsModuleIntegrationTests : IClassFixture<TraceCoreTestApplicationFactory>
{
    private readonly TraceCoreTestApplicationFactory _factory;

    public IntegrationsModuleIntegrationTests(TraceCoreTestApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    [Fact]
    public async Task Integration_Create_PersistsAndLoadsWithConfiguredStatus()
    {
        using var scope = _factory.Services.CreateScope();
        var integrationService = scope.ServiceProvider.GetRequiredService<IIntegrationService>();
        var deptService = scope.ServiceProvider.GetRequiredService<IDepartmentService>();

        var departments = await deptService.GetAllDepartmentsAsync();
        var integracoesDept = departments.First(d => d.Name == "Integrações");

        // Act: Cadastra integração no catálogo
        var id = await integrationService.CreateIntegrationAsync(new CreateIntegrationCommand(
            Code: "INT-TEST-01",
            Name: "Integração de Teste",
            IntegrationType: "Sap",
            TargetSystemDescription: "ERP de teste — dados mestre",
            OwnerDepartmentId: integracoesDept.Id,
            ContractNotes: "Conector ainda não construído (catálogo).",
            CreatedBy: null));

        // Assert
        id.Should().BeGreaterThan(0);
        var created = await integrationService.GetIntegrationByIdAsync(id);
        created.Should().NotBeNull();
        created!.Code.Should().Be("INT-TEST-01");
        created.Status.Should().Be("Configured", "integração nova entra como catálogo, nunca conexão ativa");
    }

    [Fact]
    public async Task IntegrationStatus_OnlyChangesByManualAction()
    {
        using var scope = _factory.Services.CreateScope();
        var integrationService = scope.ServiceProvider.GetRequiredService<IIntegrationService>();

        var id = await integrationService.CreateIntegrationAsync(new CreateIntegrationCommand(
            Code: "INT-STATUS-01",
            Name: "Integração de Status",
            IntegrationType: "Ticketing",
            TargetSystemDescription: null,
            OwnerDepartmentId: null,
            ContractNotes: null,
            CreatedBy: null));

        // Toda integração nasce como 'Configured' (catálogo, nunca conexão ativa)
        var before = await integrationService.GetIntegrationByIdAsync(id);
        before!.Status.Should().Be("Configured");

        // Act: Ação manual de usuário altera o status
        await integrationService.UpdateIntegrationStatusAsync(id, "Active", updatedBy: null);

        var after = await integrationService.GetIntegrationByIdAsync(id);
        after!.Status.Should().Be("Active");

        // E volta a ser manual: Inactive
        await integrationService.UpdateIntegrationStatusAsync(id, "Inactive", updatedBy: null);
        var final = await integrationService.GetIntegrationByIdAsync(id);
        final!.Status.Should().Be("Inactive");
    }

    [Fact]
    public async Task IntegrationRun_RegisterManualRun_RecordsHistoryForIntegration()
    {
        using var scope = _factory.Services.CreateScope();
        var integrationService = scope.ServiceProvider.GetRequiredService<IIntegrationService>();

        var id = await integrationService.CreateIntegrationAsync(new CreateIntegrationCommand(
            Code: "INT-RUN-01",
            Name: "Integração com Execuções",
            IntegrationType: "Monitoring",
            TargetSystemDescription: null,
            OwnerDepartmentId: null,
            ContractNotes: null,
            CreatedBy: null));

        // Act: Registra log manual de execução bem-sucedida e uma falha
        var successRunId = await integrationService.RegisterRunAsync(new RegisterIntegrationRunCommand(
            IntegrationId: id,
            Status: "Success",
            StartedAt: DateTime.UtcNow.AddHours(-2),
            RecordsProcessed: 1250,
            ErrorMessage: null,
            RecordedBy: null));

        await integrationService.RegisterRunAsync(new RegisterIntegrationRunCommand(
            IntegrationId: id,
            Status: "Failed",
            StartedAt: DateTime.UtcNow.AddHours(-1),
            RecordsProcessed: 0,
            ErrorMessage: "Timeout ao autenticar no sistema externo.",
            RecordedBy: null));

        // Assert: histórico aparece na consulta da integração (ordem mais recente primeiro)
        successRunId.Should().BeGreaterThan(0);
        var integration = await integrationService.GetIntegrationByIdAsync(id);
        integration!.Runs.Should().HaveCount(2);
        integration.Runs.First().Status.Should().Be("Failed");
        integration.Runs.First().ErrorMessage.Should().Contain("Timeout");
        integration.Runs.Should().Contain(r => r.Status == "Success" && r.RecordsProcessed == 1250);
    }

    [Fact]
    public async Task Permission_IntegracaoGerenciar_IsGrantedToAdminAndAdminFuncional()
    {
        using var scope = _factory.Services.CreateScope();
        var roleRepository = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
        var permRepository = scope.ServiceProvider.GetRequiredService<IPermissionRepository>();

        var allPerms = await permRepository.GetAllAsync();
        var intPerm = allPerms.FirstOrDefault(p => p.Code == "integracao.gerenciar");
        intPerm.Should().NotBeNull("Permissão integracao.gerenciar deve existir no catálogo de permissões");

        var adminFuncionalRole = await roleRepository.GetByNameAsync("Admin Funcional");
        adminFuncionalRole.Should().NotBeNull();
        var adminFuncPerms = await roleRepository.GetRolePermissionsAsync(adminFuncionalRole!.Id);
        adminFuncPerms.Should().Contain(p => p.Code == "integracao.gerenciar");

        var adminRole = await roleRepository.GetByNameAsync("Admin");
        adminRole.Should().NotBeNull();
        var adminPerms = await roleRepository.GetRolePermissionsAsync(adminRole!.Id);
        adminPerms.Should().Contain(p => p.Code == "integracao.gerenciar");
    }
}