using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TraceCore.Application.DTOs;
using TraceCore.Application.Services;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;
using Xunit;

namespace TraceCore.IntegrationTests;

/// <summary>
/// Testes de integração da Fase 5:
/// Histórico Versões × Casos, Analytics, Copiloto e Consolidação.
/// Validação de todos os Cenários Obrigatórios (A a I) e Ferramentas do Copiloto.
/// </summary>
public class VersionIntelligencePhase5Tests : IClassFixture<TraceCoreTestApplicationFactory>
{
    private readonly TraceCoreTestApplicationFactory _factory;

    public VersionIntelligencePhase5Tests(TraceCoreTestApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    private IServiceScope Scope() => _factory.Services.CreateScope();

    [Fact]
    public async Task ScenarioA_ClientInOlderVersion_DetectsFixInLaterVersion()
    {
        using var scope = Scope();
        var catalog = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var clients = scope.ServiceProvider.GetRequiredService<IClientService>();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        // 1. Setup Produto e Versões 5.17.9 (order 1) e 5.18.4 (order 2)
        var prodId = await catalogRepo.AddProductAsync(new Product("TMS Frotas", "TMS-FRT"));
        var v517Id = await catalog.CreateVersionAsync(prodId, "5.17.9");
        var v518Id = await catalog.CreateVersionAsync(prodId, "5.18.4");

        // 2. Setup Cliente na versão 5.17.9
        var clientId = await clients.CreateClientAsync(new CreateClientRequest("Atlas Logística"));
        await vm.ManualClientVersionUpdateAsync(
            clientId: clientId,
            productId: prodId,
            newProductVersionId: v517Id,
            clientUnitId: null,
            environmentId: null,
            effectiveFrom: DateTime.UtcNow.AddMonths(-2),
            notes: "Instalação inicial",
            currentUserId: null);

        // 3. Cadastra Fix na 5.18.4
        var changeId = await vm.CreateChangeAsync(
            productVersionId: v518Id,
            changeType: "Fix",
            title: "Corrigido Access Violation durante emissão de CT-e",
            description: "Correção de buffer overflow",
            componentId: null,
            errorCode: "Access Violation");

        // 4. Consulta possíveis correções posteriores (Fase 4 / Copiloto)
        var possibleFixes = await vm.SuggestPossibleFixesAsync(
            productId: prodId,
            currentProductVersionId: v517Id,
            title: "Access Violation na emissão de CT-e",
            description: "Travamento ao emitir",
            componentId: null,
            errorCode: "Access Violation");

        // Verificações Cenário A
        possibleFixes.Should().NotBeNull();
        possibleFixes.Should().HaveCount(1);
        possibleFixes[0].VersionLabel.Should().Be("5.18.4");
        possibleFixes[0].ChangeTitle.Should().Contain("Access Violation");
        possibleFixes[0].Score.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ScenarioB_ClientAlreadyOnLatestVersion_ReturnsNoLaterVersions()
    {
        using var scope = Scope();
        var catalog = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var clients = scope.ServiceProvider.GetRequiredService<IClientService>();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        var prodId = await catalogRepo.AddProductAsync(new Product("WMS Armazéns", "WMS-ARM"));
        var v1Id = await catalog.CreateVersionAsync(prodId, "1.0.0");
        var v2Id = await catalog.CreateVersionAsync(prodId, "2.0.0");

        var clientId = await clients.CreateClientAsync(new CreateClientRequest("Log Global"));
        await vm.ManualClientVersionUpdateAsync(
            clientId: clientId,
            productId: prodId,
            newProductVersionId: v2Id, // Já na mais recente
            clientUnitId: null,
            environmentId: null,
            effectiveFrom: DateTime.UtcNow.AddDays(-5),
            notes: "Versão mais atual",
            currentUserId: null);

        var copilotCtx = await vm.GetClientVersionCopilotContextAsync(clientId, prodId, null);

        copilotCtx.Should().NotBeNull();
        copilotCtx.CurrentVersion.Should().NotBeNull();
        copilotCtx.CurrentVersion!.VersionLabel.Should().Be("2.0.0");
        copilotCtx.LaterVersions.Should().BeEmpty();
        copilotCtx.LaterVersionFixes.Should().BeEmpty();
    }

    [Fact]
    public async Task ScenarioC_ClientPlannedRollout_ActiveVersionUnchangedUntilDeployment()
    {
        using var scope = Scope();
        var catalog = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var clients = scope.ServiceProvider.GetRequiredService<IClientService>();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        var prodId = await catalogRepo.AddProductAsync(new Product("ERP Vendas", "ERP-VND"));
        var v1Id = await catalog.CreateVersionAsync(prodId, "3.1.0");
        var v2Id = await catalog.CreateVersionAsync(prodId, "3.2.0");

        var clientId = await clients.CreateClientAsync(new CreateClientRequest("Distribuidora Central"));
        await vm.ManualClientVersionUpdateAsync(
            clientId: clientId,
            productId: prodId,
            newProductVersionId: v1Id,
            clientUnitId: null,
            environmentId: null,
            effectiveFrom: DateTime.UtcNow.AddMonths(-1),
            notes: "Produção",
            currentUserId: null);

        // Cria destinação planejada na v2
        await vm.CreateAssignmentAsync(
            productVersionId: v2Id,
            clientId: clientId,
            clientUnitId: null,
            notes: "Rollout agendado");

        // Indicadores da v2
        var indicators = await vm.GetVersionIndicatorsAsync(v2Id);
        indicators.PlannedClientsCount.Should().Be(1);
        indicators.DeployedClientsCount.Should().Be(0);
        indicators.PendingClientsCount.Should().Be(1);

        // Versão ativa do cliente ainda deve ser a 3.1.0
        var activeCtx = await vm.GetActiveVersionForClientAsync(clientId, prodId, null);
        activeCtx.Should().NotBeNull();
        activeCtx.ProductVersionId.Should().Be(v1Id);
        activeCtx.VersionLabel.Should().Be("3.1.0");
    }

    [Fact]
    public async Task ScenarioD_FixLinkedToCasesFromMultipleDifferentClients()
    {
        using var scope = Scope();
        var catalog = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var clients = scope.ServiceProvider.GetRequiredService<IClientService>();
        var cases = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        var prodId = await catalogRepo.AddProductAsync(new Product("Fiscal Cloud", "FSC-CLD"));
        var v1Id = await catalog.CreateVersionAsync(prodId, "1.0.0");
        var v2Id = await catalog.CreateVersionAsync(prodId, "2.0.0");

        var clientA = await clients.CreateClientAsync(new CreateClientRequest("Cliente Alpha"));
        var clientB = await clients.CreateClientAsync(new CreateClientRequest("Cliente Beta"));

        var caseA = await cases.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Erro ao gerar SPED Fiscal no encerramento",
            ClientId: clientA,
            ProductId: prodId,
            ProductVersionId: v1Id,
            ErrorCode: "SPED_ERR"
        ));

        var caseB = await cases.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "SPED Fiscal trava bloco C",
            ClientId: clientB,
            ProductId: prodId,
            ProductVersionId: v1Id,
            ErrorCode: "SPED_ERR"
        ));

        var fixId = await vm.CreateChangeAsync(
            productVersionId: v2Id,
            changeType: "Fix",
            title: "Correção de concorrência na geração do SPED Fiscal",
            description: "Thread safety",
            componentId: null,
            errorCode: "SPED_ERR");

        // Vincula ambos os casos à mesma correção
        await vm.LinkCaseAsync(fixId, caseA.Id, "FixedBy");
        await vm.LinkCaseAsync(fixId, caseB.Id, "FixedBy");

        // Visão Versão -> Casos
        var linkedCases = await vm.GetVersionLinkedCasesAsync(v2Id);
        linkedCases.Should().HaveCount(2);
        linkedCases.Select(c => c.ClientName).Should().Contain("Cliente Alpha").And.Contain("Cliente Beta");
        linkedCases.All(c => c.RelationType == "FixedBy").Should().BeTrue();
    }

    [Fact]
    public async Task ScenarioE_CaseWithoutVersion_HandledGracefully()
    {
        using var scope = Scope();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var cases = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        var prodId = await catalogRepo.AddProductAsync(new Product("App Mobile", "APP-MOB"));
        var c = await cases.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Dúvida geral sobre login",
            ProductId: prodId,
            ProductVersionId: null // Sem versão informada
        ));

        var caseCtx = await vm.GetVersionContextForCaseAsync(c.Id);
        caseCtx.Should().NotBeNull();
        caseCtx.ProductVersionId.Should().BeNull();
        caseCtx.VersionLabel.Should().BeNull();
        caseCtx.ReleaseOrder.Should().BeNull();
        caseCtx.PossibleFixes.Should().BeEmpty();
    }

    [Fact]
    public async Task ScenarioF_VersionWithoutChanges_ReturnsZeroIndicatorsGracefully()
    {
        using var scope = Scope();
        var catalog = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        var prodId = await catalogRepo.AddProductAsync(new Product("API Core", "API-COR"));
        var vId = await catalog.CreateVersionAsync(prodId, "1.0.0-rc1");

        var indicators = await vm.GetVersionIndicatorsAsync(vId);
        indicators.Should().NotBeNull();
        indicators.CasesOccurredCount.Should().Be(0);
        indicators.PublishedFixesCount.Should().Be(0);
        indicators.LinkedCasesCount.Should().Be(0);
        indicators.PlannedClientsCount.Should().Be(0);
        indicators.DeployedClientsCount.Should().Be(0);

        var linkedCases = await vm.GetVersionLinkedCasesAsync(vId);
        linkedCases.Should().BeEmpty();
    }

    [Fact]
    public async Task ScenarioG_NonLexicalVersionOrdering_SortedByReleaseOrder()
    {
        using var scope = Scope();
        var catalog = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var clients = scope.ServiceProvider.GetRequiredService<IClientService>();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        var prodId = await catalogRepo.AddProductAsync(new Product("Sistema Contábil", "SIS-CTB"));
        // 5.9 lançado antes de 5.10
        // Em ordenação léxica de string: "5.10" < "5.9", o que seria incorreto!
        // Em release_order numérico: 5.9 tem ReleaseOrder 1, 5.10 tem ReleaseOrder 2.
        var v59Id = await catalog.CreateVersionAsync(prodId, "5.9");
        var v510Id = await catalog.CreateVersionAsync(prodId, "5.10");

        var clientId = await clients.CreateClientAsync(new CreateClientRequest("Auditoria Contábil"));
        await vm.ManualClientVersionUpdateAsync(
            clientId: clientId,
            productId: prodId,
            newProductVersionId: v59Id,
            clientUnitId: null,
            environmentId: null,
            effectiveFrom: DateTime.UtcNow.AddMonths(-1),
            notes: "Em 5.9",
            currentUserId: null);

        var fixId = await vm.CreateChangeAsync(
            productVersionId: v510Id,
            changeType: "Fix",
            title: "Ajuste na apuração de PIS/COFINS",
            description: "Correção de fórmula",
            componentId: null,
            errorCode: "ERR_PIS_COFINS");

        var laterFixes = await vm.SuggestPossibleFixesAsync(
            productId: prodId,
            currentProductVersionId: v59Id,
            title: "apuração PIS COFINS",
            description: null,
            componentId: null,
            errorCode: "ERR_PIS_COFINS");

        laterFixes.Should().HaveCount(1);
        laterFixes[0].VersionLabel.Should().Be("5.10");
    }

    [Fact]
    public async Task ScenarioH_RecurrenceObservation_FactualObservationWithoutCausalClaim()
    {
        using var scope = Scope();
        var catalog = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var cases = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        var prodId = await catalogRepo.AddProductAsync(new Product("TMS Integrado", "TMS-INT"));
        var vId = await catalog.CreateVersionAsync(prodId, "5.18.4");

        var fixId = await vm.CreateChangeAsync(
            productVersionId: vId,
            changeType: "Fix",
            title: "Corrigido Access Violation durante emissão de CT-e",
            description: "Fix de memória",
            componentId: null,
            errorCode: "Access Violation");

        // Caso aberto após a liberação da versão com o mesmo código de erro
        await cases.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Ocorrência posterior com Access Violation ao transmitir CT-e",
            ProductId: prodId,
            ProductVersionId: vId,
            ErrorCode: "Access Violation"
        ));

        var recurrence = await vm.GetFixRecurrenceAsync(fixId);
        recurrence.Should().NotBeNull();
        recurrence.PostReleaseOccurrencesCount.Should().BeGreaterThanOrEqualTo(1);
        
        // Validação estrita do texto factual: NUNCA deve conter acusações causais
        recurrence.ObservationMessage.Should().Contain("ocorrência semelhante foi registrada após adoção da versão");
        recurrence.ObservationMessage.Should().NotContain("falhou");
        recurrence.ObservationMessage.Should().NotContain("culpada");
        recurrence.ObservationMessage.Should().NotContain("defeituosa");
    }

    [Fact]
    public async Task ScenarioI_ClientUnitOnDifferentVersionThanMatriz()
    {
        using var scope = Scope();
        var catalog = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var clients = scope.ServiceProvider.GetRequiredService<IClientService>();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        var prodId = await catalogRepo.AddProductAsync(new Product("ERP Varejo", "ERP-VAR"));
        var v1Id = await catalog.CreateVersionAsync(prodId, "1.0.0");
        var v2Id = await catalog.CreateVersionAsync(prodId, "2.0.0");

        var clientId = await clients.CreateClientAsync(new CreateClientRequest("Supermercados Unidos"));
        var filialId = await clients.AddUnitAsync(clientId, new CreateClientUnitRequest("Filial Rio", "RIO-01"));

        // Matriz na v2
        await vm.ManualClientVersionUpdateAsync(
            clientId: clientId,
            productId: prodId,
            newProductVersionId: v2Id,
            clientUnitId: null,
            environmentId: null,
            effectiveFrom: DateTime.UtcNow.AddMonths(-1),
            notes: "Matriz na v2",
            currentUserId: null);

        // Filial na v1
        await vm.ManualClientVersionUpdateAsync(
            clientId: clientId,
            productId: prodId,
            newProductVersionId: v1Id,
            clientUnitId: filialId,
            environmentId: null,
            effectiveFrom: DateTime.UtcNow.AddMonths(-2),
            notes: "Filial mantida em v1",
            currentUserId: null);

        // Contexto da Matriz
        var ctxMatriz = await vm.GetActiveVersionForClientAsync(clientId, prodId, null);
        ctxMatriz.Should().NotBeNull();
        ctxMatriz.ProductVersionId.Should().Be(v2Id);

        // Contexto da Filial
        var ctxFilial = await vm.GetActiveVersionForClientAsync(clientId, prodId, filialId);
        ctxFilial.Should().NotBeNull();
        ctxFilial.ProductVersionId.Should().Be(v1Id);
    }

    [Fact]
    public async Task CopilotTools_GetClientVersionContextAndSearchFixes_ReturnStructuredFacts()
    {
        using var scope = Scope();
        var catalog = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var clients = scope.ServiceProvider.GetRequiredService<IClientService>();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        var prodId = await catalogRepo.AddProductAsync(new Product("TMS Copilot", "TMS-COP"));
        var v1Id = await catalog.CreateVersionAsync(prodId, "5.17.9");
        var v2Id = await catalog.CreateVersionAsync(prodId, "5.18.4");

        var clientId = await clients.CreateClientAsync(new CreateClientRequest("Atlas Transportes"));
        await vm.ManualClientVersionUpdateAsync(
            clientId: clientId,
            productId: prodId,
            newProductVersionId: v1Id,
            clientUnitId: null,
            environmentId: null,
            effectiveFrom: DateTime.UtcNow.AddMonths(-3),
            notes: "Atlas na 5.17.9",
            currentUserId: null);

        await vm.CreateChangeAsync(
            productVersionId: v2Id,
            changeType: "Fix",
            title: "Corrigido Access Violation durante emissão de CT-e",
            description: "Correção de memory leak",
            componentId: null,
            errorCode: "Access Violation");

        // 1. Tool GetClientVersionContext
        var copilotContext = await vm.GetClientVersionCopilotContextAsync(clientId, prodId, null);
        copilotContext.Should().NotBeNull();
        copilotContext.CurrentVersion.Should().NotBeNull();
        copilotContext.CurrentVersion!.VersionLabel.Should().Be("5.17.9");
        copilotContext.LaterVersions.Should().NotBeEmpty();
        copilotContext.LaterVersionFixes.Should().NotBeEmpty();
        copilotContext.LaterVersionFixes[0].VersionLabel.Should().Be("5.18.4");

        // 2. Tool SearchVersionFixes
        var searchResults = await vm.SearchVersionFixesForCopilotAsync(prodId, currentProductVersionId: v1Id, query: "Access Violation", errorCode: "Access Violation");
        searchResults.Should().NotBeEmpty();
        searchResults[0].VersionLabel.Should().Be("5.18.4");
        searchResults[0].Title.Should().Contain("Access Violation");
    }
}
