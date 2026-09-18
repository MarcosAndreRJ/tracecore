using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using TraceCore.Application.DTOs;
using TraceCore.Application.Services;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;
using TraceCore.Infrastructure.Persistence.InMemory;
using Xunit;

namespace TraceCore.IntegrationTests;

public class HybridSearchIntegrationTests : IClassFixture<TraceCoreTestApplicationFactory>
{
    private readonly TraceCoreTestApplicationFactory _factory;

    public HybridSearchIntegrationTests(TraceCoreTestApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    [Fact]
    public async Task Search_ByExactCaseNumber_ShouldReturnInExactMatchesSection()
    {
        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<InMemoryDataStore>();
        var searchService = scope.ServiceProvider.GetRequiredService<ISearchService>();

        var testCase = new Case(
            originalReport: "Relato original do caso de teste 99801 para pesquisa exata.",
            caseNumber: 202699801,
            severity: "High"
        )
        {
            Id = 99801,
            Status = "Open",
            OpenedAt = DateTime.UtcNow
        };
        testCase.UpdateNormalizedSummary("Resumo normalizado do caso 99801 com falha de conexão", 1);
        store.Cases[testCase.Id] = testCase;

        var command = new ExecuteSearchCommand(
            QueryText: "202699801"
        );

        var result = await searchService.SearchAsync(command);

        Assert.NotNull(result);
        Assert.NotEmpty(result.ExactMatches);
        var exactMatch = result.ExactMatches.FirstOrDefault(m => m.Type == "Case" && m.Id == testCase.Id);
        Assert.NotNull(exactMatch);
        Assert.Equal(100.0, exactMatch.Score);
        Assert.Contains(exactMatch.MatchedFactors, f => f.Contains("Número exato do caso", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Search_ByExactErrorCode_ShouldReturnInExactMatchesSection()
    {
        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<InMemoryDataStore>();
        var searchService = scope.ServiceProvider.GetRequiredService<ISearchService>();

        var testCase = new Case(
            originalReport: "Relato original com erro de autenticação AUTH_TIMEOUT_504.",
            caseNumber: 202699802,
            severity: "Medium",
            errorCode: "AUTH_TIMEOUT_504"
        )
        {
            Id = 99802,
            Status = "Investigating",
            OpenedAt = DateTime.UtcNow
        };
        testCase.UpdateNormalizedSummary("Timeout na camada de auth", 1);
        store.Cases[testCase.Id] = testCase;

        var command = new ExecuteSearchCommand(
            QueryText: "AUTH_TIMEOUT_504"
        );

        var result = await searchService.SearchAsync(command);

        Assert.NotNull(result);
        Assert.NotEmpty(result.ExactMatches);
        var exactMatch = result.ExactMatches.FirstOrDefault(m => m.Type == "Case" && m.Id == testCase.Id);
        Assert.NotNull(exactMatch);
        Assert.Contains(exactMatch.MatchedFactors, f => f.Contains("Código de erro exato", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Search_FilterByTechnology_ShouldFilterSolutions()
    {
        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<InMemoryDataStore>();
        var searchService = scope.ServiceProvider.GetRequiredService<ISearchService>();
        var knowledgeService = scope.ServiceProvider.GetRequiredService<IKnowledgeService>();

        // Cadastra duas soluções usando o serviço oficial
        var sol1Id = await knowledgeService.CreateKnowledgeDraftAsync(new CreateKnowledgeDraftCommand(
            Title: "Solução específica para MariaDB",
            Summary: "Resumo 1",
            ValidationMethod: "Validação MariaDB",
            ContentMarkdown: "Conteúdo MariaDB",
            Applicabilities: new List<CreateKnowledgeApplicabilityInput> { new CreateKnowledgeApplicabilityInput(ProductId: 1) }
        ), userId: 1L);

        await knowledgeService.ApproveAndPublishAsync(new ApproveAndPublishCommand(KnowledgeItemId: sol1Id), userId: 1L);

        var sol2Id = await knowledgeService.CreateKnowledgeDraftAsync(new CreateKnowledgeDraftCommand(
            Title: "Solução geral para Redis Cache",
            Summary: "Resumo 2",
            ValidationMethod: "Validação Redis",
            ContentMarkdown: "Conteúdo Redis",
            Applicabilities: new List<CreateKnowledgeApplicabilityInput> { new CreateKnowledgeApplicabilityInput(ProductId: 1) }
        ), userId: 1L);

        await knowledgeService.ApproveAndPublishAsync(new ApproveAndPublishCommand(KnowledgeItemId: sol2Id), userId: 1L);

        // Associa sol1Id à tecnologia 101 (MariaDB)
        lock (store.KnowledgeTechnologies)
        {
            store.KnowledgeTechnologies.RemoveAll(kt => kt.KnowledgeItemId == sol1Id || kt.KnowledgeItemId == sol2Id);
            store.KnowledgeTechnologies.Add((sol1Id, 101));
            store.KnowledgeTechnologies.Add((sol2Id, 202));
        }

        var command = new ExecuteSearchCommand(
            QueryText: "",
            Filters: new SearchFilterCriteria(TechnologyId: 101),
            SelectedType: "Solution"
        );

        var result = await searchService.SearchAsync(command);

        Assert.NotNull(result);
        Assert.Contains(result.Items, i => i.Id == sol1Id);
        Assert.DoesNotContain(result.Items, i => i.Id == sol2Id);
    }

    [Fact]
    public async Task Search_FilterByClientAndUnit_ShouldFilterCases()
    {
        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<InMemoryDataStore>();
        var searchService = scope.ServiceProvider.GetRequiredService<ISearchService>();

        long targetClientId = 7701;
        long targetUnitId = 7702;

        var caseClientA = new Case(
            originalReport: "Caso do cliente A unidade matriz.",
            caseNumber: 202699803,
            severity: "Low",
            clientId: targetClientId
        )
        {
            Id = 99803,
            ClientUnitId = targetUnitId,
            Status = "Open",
            OpenedAt = DateTime.UtcNow
        };
        store.Cases[caseClientA.Id] = caseClientA;

        var caseClientB = new Case(
            originalReport: "Caso de outro cliente qualquer.",
            caseNumber: 202699804,
            severity: "Low",
            clientId: 9999
        )
        {
            Id = 99804,
            ClientUnitId = null,
            Status = "Open",
            OpenedAt = DateTime.UtcNow
        };
        store.Cases[caseClientB.Id] = caseClientB;

        var command = new ExecuteSearchCommand(
            QueryText: "",
            Filters: new SearchFilterCriteria(ClientId: targetClientId, ClientUnitId: targetUnitId),
            SelectedType: "Case"
        );

        var result = await searchService.SearchAsync(command);

        Assert.NotNull(result);
        Assert.Contains(result.Items, i => i.Id == caseClientA.Id);
        Assert.DoesNotContain(result.Items, i => i.Id == caseClientB.Id);
    }

    [Fact]
    public async Task Search_CatalogItems_MatchedFactorsReflectRealMatch()
    {
        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<InMemoryDataStore>();
        var searchService = scope.ServiceProvider.GetRequiredService<ISearchService>();

        var prod = new Product { Id = 6601, Code = "PORTAL-FIN", Name = "Portal Financeiro Corporativo", Description = "Sistema central de finanças" };
        store.Products[prod.Id] = prod;

        var comp = new ComponentEntity { Id = 6602, ProductId = prod.Id, Name = "GatewayPagamento", Description = "Módulo de pagamentos", ComponentType = "Microservice" };
        store.Components[comp.Id] = comp;

        var rc = new RootCause { Id = 6603, Name = "Deadlock no Banco", Category = "Infraestrutura", Description = "Conflito de transações concorrentes" };
        store.RootCauses[rc.Id] = rc;

        var command = new ExecuteSearchCommand(
            QueryText: "GatewayPagamento"
        );

        var result = await searchService.SearchAsync(command);

        Assert.NotNull(result);
        var compItem = result.Items.FirstOrDefault(i => i.Type == "Component" && i.Id == comp.Id);
        Assert.NotNull(compItem);
        Assert.Contains(compItem.MatchedFactors, f => f.Contains("Nome exato do componente", StringComparison.OrdinalIgnoreCase) || f.Contains("Nome do componente", StringComparison.OrdinalIgnoreCase));
    }
}
