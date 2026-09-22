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
/// Testes de integração da Fase 1 (Versionamento Inteligente):
/// itens de alteração por versão, vínculo com casos (sem nunca alterar a versão do caso)
/// e destinação/rollout de versões a clientes — deploy confirmado ATUALIZA a versão corrente;
/// criação de destinação NÃO.
/// </summary>
public class VersionManagementIntegrationTests : IClassFixture<TraceCoreTestApplicationFactory>
{
    private readonly TraceCoreTestApplicationFactory _factory;

    public VersionManagementIntegrationTests(TraceCoreTestApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    private IServiceScope Scope() => _factory.Services.CreateScope();

    // Produtos/versões/clientes do seed InMemory (ver InMemoryRepositories.Seed):
    // ERP Desktop (1) -> v1.0.0 (ReleaseOrder 1), v2.4.1 (ReleaseOrder 2)
    // Portal Web (2)  -> v3.0.0 (ReleaseOrder 1)
    // Clientes: Acme (1), Tech Solutions (2); unidade MATRIZ (1) da Acme.

    [Fact]
    public async Task ReleaseOrder_IsAssignedAutomatically_InCreationOrder()
    {
        using var scope = Scope();
        var catalog = scope.ServiceProvider.GetRequiredService<ICatalogService>();

        var erp = await catalog.GetProductByIdAsync(1);

        // Act: versões do ERP (seed já tem ReleaseOrder 1 e 2). Cria uma terceira.
        var nv = await catalog.CreateVersionAsync(erp!.Id, "v2.5.0");
        var versions = (await catalog.GetVersionsByProductIdAsync(erp.Id))
            .OrderBy(v => v.ReleaseOrder)
            .ToList();

        // Assert: ordem e ReleaseOrder incremental (1,[2],3), mesmo com rótulos arbitrarios.
        versions.Should().HaveCount(3);
        versions.Select(v => v.ReleaseOrder).Should().Equal(1, 2, 3);
        versions.Last().Id.Should().Be(nv);
        versions.Last().VersionLabel.Should().Be("v2.5.0");

        // Ordenacao independente de alfabetizacao/versao semantica: v10.0.0 entra depois de v2.4.1.
        await catalog.CreateVersionAsync(erp.Id, "v10.0.0");
        var after = (await catalog.GetVersionsByProductIdAsync(erp.Id))
            .OrderBy(v => v.ReleaseOrder)
            .Select(v => v.VersionLabel)
            .ToList();
        after.Should().ContainInOrder("v1.0.0", "v2.4.1", "v2.5.0", "v10.0.0");
    }

    [Fact]
    public async Task ProductVersionChange_Crud_AndTypeLabels()
    {
        using var scope = Scope();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        var changeId = await vm.CreateChangeAsync(
            productVersionId: 2,
            changeType: "NewFeature",
            title: "Dashboard de custos em tempo real",
            description: "Novo painel com consumo financeiro por filial",
            componentId: 2,
            errorCode: null);

        changeId.Should().BeGreaterThan(0);

        var changes = await vm.GetChangesByVersionIdAsync(2);
        changes.Should().ContainSingle();
        var c = changes.Single();
        c.ChangeTypeLabel.Should().Be("Novidade");
        c.ComponentName.Should().Be("Faturamento");

        // Update
        await vm.UpdateChangeAsync(changeId, "Fix", "Corrige recálculo de impostos", null, null, "E-1042", currentUserId: 1);

        var updated = (await vm.GetChangesByVersionIdAsync(2)).Single();
        updated.ChangeTypeLabel.Should().Be("Correção");
        updated.ErrorCode.Should().Be("E-1042");

        // Delete
        var deleted = await vm.DeleteChangeAsync(changeId);
        deleted.Should().BeTrue();
        (await vm.GetChangesByVersionIdAsync(2)).Should().BeEmpty();
    }

    [Fact]
    public async Task ChangeType_Invalid_IsRejected()
    {
        using var scope = Scope();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        Func<Task> act = async () => await vm.CreateChangeAsync(2, "Refactor", "Tipo inválido");
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Tipo de alteração inválido*");
    }

    [Fact]
    public async Task Case_CanBeLinkedToChange_WithoutChangingCaseVersion()
    {
        using var scope = Scope();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();
        var cases = scope.ServiceProvider.GetRequiredService<ICaseService>();

        // Caso reportado na v1.0.0 (versao da ocorrencia).
        var opened = await cases.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Relatório de fechamento falha periodicamente",
            ProductId: 1,
            ProductVersionId: 1,
            Severity: "High"));

        var changeId = await vm.CreateChangeAsync(2, "Fix", "Correção no relatório de fechamento");
        await vm.LinkCaseAsync(changeId, opened.Id, "FixedBy", matchScore: 0.9m);

        var links = await vm.GetLinkedCasesAsync(changeId);
        links.Should().ContainSingle();
        links.Single().CaseNumber.Should().Be(opened.CaseNumber);
        links.Single().RelationTypeLabel.Should().Be("Corrigido por");
        links.Single().MatchScore.Should().Be(0.9m);

        // REGRA 10: vincular a correção NUNCA muda a versao do caso.
        var after = await cases.GetCaseByIdAsync(opened.Id);
        after!.ProductVersionId.Should().Be(opened.ProductVersionId);
        after.VersionLabel.Should().Be("v1.0.0");

        // Desvincula
        var removed = await vm.UnlinkCaseAsync(changeId, opened.Id, "FixedBy");
        removed.Should().BeTrue();
        (await vm.GetLinkedCasesAsync(changeId)).Should().BeEmpty();
    }

    [Fact]
    public async Task VersionChangesByProduct_AreScoped_ToTheirOwnVersion()
    {
        using var scope = Scope();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        await vm.CreateChangeAsync(1, "Fix", "Fix que pertence a v1.0.0");
        await vm.CreateChangeAsync(2, "Improvement", "Melhoria de v2.4.1");
        await vm.CreateChangeAsync(2, "Fix", "Correcao extra de v2.4.1");

        var v1 = await vm.GetChangesByVersionIdAsync(1);
        var v2 = await vm.GetChangesByVersionIdAsync(2);

        v1.Should().ContainSingle().And.Subject.Single().Title.Should().Be("Fix que pertence a v1.0.0");
        v2.Should().HaveCount(2);
        v2.Should().OnlyContain(c => c.VersionLabel == "v2.4.1");
    }

    [Fact]
    public async Task Assignment_Planned_DoesNotChangeClientVersion()
    {
        using var scope = Scope();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();
        var clients = scope.ServiceProvider.GetRequiredService<IClientService>();

        // Contexto tecnico corrente da Acme para o ERP na v1.0.0.
        await clients.AddTechnicalContextAsync(1, new CreateTechnicalContextRequest(
            ProductId: 1,
            ClientUnitId: null,
            ProductVersionId: 1,
            EnvironmentId: 1));

        var assignmentId = await vm.CreateAssignmentAsync(2, clientId: 1, clientUnitId: null, notes: "Rollout planejado");

        assignmentId.Should().BeGreaterThan(0);

        // REGRA: criar destinação NÃO muda a versao corrente.
        var details = await clients.GetClientDetailsAsync(1);
        var ctx = details!.TechnicalContexts.Single(c => c.ProductId == 1 && c.EffectiveTo == null);
        ctx.ProductVersionId.Should().Be(1);
        ctx.VersionLabel.Should().Be("v1.0.0");

        var assignments = await vm.GetAssignmentsByVersionIdAsync(2);
        assignments.Should().ContainSingle();
        assignments.Single().StatusLabel.Should().Be("Planejado");
        assignments.Single().ClientName.Should().Be("Acme Corporação");
    }

    [Fact]
    public async Task Assignment_Duplicate_IsRejected_ByService()
    {
        using var scope = Scope();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        await vm.CreateAssignmentAsync(2, clientId: 1, clientUnitId: null);

        Func<Task> act = async () => await vm.CreateAssignmentAsync(2, clientId: 1, clientUnitId: null);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Já existe uma destinação*");
    }

    [Fact]
    public async Task ConfirmDeploy_UpdatesClientCurrentVersion_AndIsIdempotent()
    {
        using var scope = Scope();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();
        var clients = scope.ServiceProvider.GetRequiredService<IClientService>();

        await clients.AddTechnicalContextAsync(1, new CreateTechnicalContextRequest(
            ProductId: 1,
            ClientUnitId: null,
            ProductVersionId: 1,
            EnvironmentId: 1));

        var assignmentId = await vm.CreateAssignmentAsync(2, clientId: 1, clientUnitId: null);
        await vm.ConfirmAssignmentDeployedAsync(assignmentId, currentUserId: 1);

        // Deploy confirmado => versao corrente atualizada para v2.4.1 criando novo contexto e encerrando o anterior.
        var details = await clients.GetClientDetailsAsync(1);
        var contexts = details!.TechnicalContexts.Where(c => c.ProductId == 1).ToList();
        contexts.Should().HaveCount(2);

        var oldCtx = contexts.Single(c => c.EffectiveTo != null);
        oldCtx.ProductVersionId.Should().Be(1);

        var activeCtx = contexts.Single(c => c.EffectiveTo == null);
        activeCtx.ProductVersionId.Should().Be(2);
        activeCtx.VersionLabel.Should().Be("v2.4.1");

        var assignments = await vm.GetAssignmentsByVersionIdAsync(2);
        assignments.Single().StatusLabel.Should().Be("Implantado");
        assignments.Single().DeployedAt.Should().NotBeNull();

        // Idempotente: confirmar de novo nao duplica atualizacao.
        await vm.ConfirmAssignmentDeployedAsync(assignmentId, currentUserId: 1);
        var again = await clients.GetClientDetailsAsync(1);
        again!.TechnicalContexts.Count(c => c.ProductId == 1 && c.EffectiveTo == null).Should().Be(1);
        again!.TechnicalContexts.Count(c => c.ProductId == 1).Should().Be(2);
    }

    [Fact]
    public async Task SkipAssignment_Works_AndBlocksDuplicateDeploy()
    {
        using var scope = Scope();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        var assignmentId = await vm.CreateAssignmentAsync(3, clientId: 2, clientUnitId: null);
        await vm.SkipAssignmentAsync(assignmentId, currentUserId: 1);

        var assignments = await vm.GetAssignmentsByVersionIdAsync(3);
        assignments.Single().StatusLabel.Should().Be("Ignorado");

        // confirmar deploy após Skip é permitido (não bloqueia) — Skip apenas marca.
        await vm.ConfirmAssignmentDeployedAsync(assignmentId, currentUserId: 1);
        (await vm.GetAssignmentsByVersionIdAsync(3)).Single().StatusLabel.Should().Be("Implantado");
    }

    [Fact]
    public async Task ConfirmDeploy_WithMultipleEnvironments_RequiresExplicitEnvironment()
    {
        using var scope = Scope();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();
        var clients = scope.ServiceProvider.GetRequiredService<IClientService>();

        // Cliente 1 tem contexto em Prod (Env 1) e Homolog (Env 2)
        await clients.AddTechnicalContextAsync(1, new CreateTechnicalContextRequest(
            ProductId: 1,
            ClientUnitId: null,
            ProductVersionId: 1,
            EnvironmentId: 1));

        await clients.AddTechnicalContextAsync(1, new CreateTechnicalContextRequest(
            ProductId: 1,
            ClientUnitId: null,
            ProductVersionId: 1,
            EnvironmentId: 2));

        var assignmentId = await vm.CreateAssignmentAsync(2, clientId: 1, clientUnitId: null);

        // Sem informar environmentId -> lança BusinessRuleValidationException BR-VERSION-001
        Func<Task> actAmbiguous = async () => await vm.ConfirmAssignmentDeployedAsync(assignmentId, currentUserId: 1);
        var ex = await actAmbiguous.Should().ThrowAsync<TraceCore.Application.Exceptions.BusinessRuleValidationException>();
        ex.Which.RuleId.Should().Be("BR-VERSION-001");

        // Informando environmentId = 1 explicitamente -> sucesso
        await vm.ConfirmAssignmentDeployedAsync(assignmentId, environmentId: 1, currentUserId: 1);

        var details = await clients.GetClientDetailsAsync(1);
        var activeProd = details!.TechnicalContexts.Single(c => c.ProductId == 1 && c.EnvironmentId == 1 && c.EffectiveTo == null);
        activeProd.ProductVersionId.Should().Be(2);

        var activeHomolog = details.TechnicalContexts.Single(c => c.ProductId == 1 && c.EnvironmentId == 2 && c.EffectiveTo == null);
        activeHomolog.ProductVersionId.Should().Be(1); // Permanece inalterado
    }

    [Fact]
    public async Task ScheduleAssignment_UpdatesStatusToScheduled()
    {
        using var scope = Scope();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        var assignmentId = await vm.CreateAssignmentAsync(2, clientId: 1, clientUnitId: null);
        var targetDate = DateTime.UtcNow.AddDays(3);

        await vm.ScheduleAssignmentAsync(assignmentId, targetDate, currentUserId: 1);

        var assignments = await vm.GetAssignmentsByVersionIdAsync(2);
        var a = assignments.Single(x => x.Id == assignmentId);
        a.StatusLabel.Should().Be("Agendado");
        a.ScheduledAt.Should().BeCloseTo(targetDate, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task FailAssignment_UpdatesStatusToFailed()
    {
        using var scope = Scope();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        var assignmentId = await vm.CreateAssignmentAsync(2, clientId: 1, clientUnitId: null);
        await vm.FailAssignmentAsync(assignmentId, "Falha na execução de scripts de migração", currentUserId: 1);

        var assignments = await vm.GetAssignmentsByVersionIdAsync(2);
        var a = assignments.Single(x => x.Id == assignmentId);
        a.StatusLabel.Should().Be("Falhou");
        a.Notes.Should().Contain("Falha na execução de scripts de migração");
    }

    [Fact]
    public async Task RemoveAssignment_RemovesPlannedOrScheduled_AndRejectsDeployed()
    {
        using var scope = Scope();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();
        var clients = scope.ServiceProvider.GetRequiredService<IClientService>();

        await clients.AddTechnicalContextAsync(1, new CreateTechnicalContextRequest(
            ProductId: 1,
            ClientUnitId: null,
            ProductVersionId: 1,
            EnvironmentId: 1));

        var a1 = await vm.CreateAssignmentAsync(2, clientId: 1, clientUnitId: null);
        await vm.RemoveAssignmentAsync(a1, currentUserId: 1);

        (await vm.GetAssignmentsByVersionIdAsync(2)).Should().BeEmpty();

        // Criar novo e implantar
        var a2 = await vm.CreateAssignmentAsync(2, clientId: 1, clientUnitId: null);
        await vm.ConfirmAssignmentDeployedAsync(a2, currentUserId: 1);

        // Remover implantado deve lançar InvalidOperationException
        Func<Task> actDeployed = async () => await vm.RemoveAssignmentAsync(a2, currentUserId: 1);
        await actDeployed.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Não é possível remover uma destinação já implantada*");
    }

    [Fact]
    public async Task ReopenAssignment_ReopensFailedOrSkipped()
    {
        using var scope = Scope();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        var assignmentId = await vm.CreateAssignmentAsync(2, clientId: 1, clientUnitId: null);
        await vm.SkipAssignmentAsync(assignmentId, currentUserId: 1);

        var skipped = (await vm.GetAssignmentsByVersionIdAsync(2)).Single(x => x.Id == assignmentId);
        skipped.StatusLabel.Should().Be("Ignorado");

        await vm.ReopenAssignmentAsync(assignmentId, currentUserId: 1);

        var reopened = (await vm.GetAssignmentsByVersionIdAsync(2)).Single(x => x.Id == assignmentId);
        reopened.StatusLabel.Should().Be("Planejado");
    }

    [Fact]
    public async Task ManualClientVersionUpdate_UpdatesContext_AndReconcilesPendingAssignment()
    {
        using var scope = Scope();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();
        var clients = scope.ServiceProvider.GetRequiredService<IClientService>();

        // Contexto inicial na v1.0.0
        await clients.AddTechnicalContextAsync(1, new CreateTechnicalContextRequest(
            ProductId: 1,
            ClientUnitId: null,
            ProductVersionId: 1,
            EnvironmentId: 1));

        // Destinação pendente planejada para v2.4.1
        var assignmentId = await vm.CreateAssignmentAsync(2, clientId: 1, clientUnitId: null);

        // Atualização manual fora do rollout na tela do cliente
        await vm.ManualClientVersionUpdateAsync(
            clientId: 1,
            productId: 1,
            environmentId: 1,
            newProductVersionId: 2,
            clientUnitId: null,
            notes: "Atualizado manualmente via suporte",
            currentUserId: 1);

        // Verifica transição de contexto
        var details = await clients.GetClientDetailsAsync(1);
        var contexts = details!.TechnicalContexts.Where(c => c.ProductId == 1).ToList();
        contexts.Should().HaveCount(2);

        var oldCtx = contexts.Single(c => c.EffectiveTo != null);
        oldCtx.ProductVersionId.Should().Be(1);

        var activeCtx = contexts.Single(c => c.EffectiveTo == null);
        activeCtx.ProductVersionId.Should().Be(2);

        // Destinação pendente reconciliada para Deployed
        var assignments = await vm.GetAssignmentsByVersionIdAsync(2);
        var assignment = assignments.Single(x => x.Id == assignmentId);
        assignment.StatusLabel.Should().Be("Implantado");
        assignment.DeployedAt.Should().NotBeNull();
        assignment.Notes.Should().Contain("Atualizado manualmente via suporte");
    }

    [Fact]
    public async Task SameVersion_CanBeAssigned_ToMultipleClients()
    {
        using var scope = Scope();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        var a1 = await vm.CreateAssignmentAsync(2, clientId: 1, clientUnitId: null);
        var a2 = await vm.CreateAssignmentAsync(2, clientId: 2, clientUnitId: null);
        var a3 = await vm.CreateAssignmentAsync(2, clientId: 1, clientUnitId: 1);

        var assignments = await vm.GetAssignmentsByVersionIdAsync(2);
        assignments.Should().HaveCount(3);
        assignments.Select(a => a.ClientId).Should().OnlyContain(c => c == 1 || c == 2);
        assignments.Select(a => a.Id).Should().Contain(new[] { a1, a2, a3 });
    }

    [Fact]
    public async Task Assignment_InvalidClientUnit_IsRejected()
    {
        using var scope = Scope();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        // Unidade MATRIZ pertence a Acme (1). Tentar usar com Tech Solutions (2) deve falhar.
        Func<Task> act = async () => await vm.CreateAssignmentAsync(2, clientId: 2, clientUnitId: 1);
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*não pertence ao cliente*");
    }
}