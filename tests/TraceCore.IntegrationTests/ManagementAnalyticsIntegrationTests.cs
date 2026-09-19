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

public class ManagementAnalyticsIntegrationTests : IClassFixture<TraceCoreTestApplicationFactory>
{
    private readonly TraceCoreTestApplicationFactory _factory;

    public ManagementAnalyticsIntegrationTests(TraceCoreTestApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    [Fact]
    public async Task Mttr_Calculation_PerIteration_AveragesIndividualIterationsNotCumulativeTime()
    {
        using var scope = _factory.Services.CreateScope();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var analyticsService = scope.ServiceProvider.GetRequiredService<IManagementAnalyticsService>();
        var caseRepo = scope.ServiceProvider.GetRequiredService<ICaseRepository>();

        // 1. Criar um caso base
        var openedCase = await caseService.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Falha de conexão com banco de dados durante pico de carga",
            Severity: "High"
        ), currentUserId: 1L);

        // 2. Atualizar Iteração 1 existente para duração de 30 minutos (OpenedAt = Agora - 50m, ClosedAt = Agora - 20m)
        var now = DateTime.UtcNow;
        var iters = await caseRepo.GetIterationsByCaseIdAsync(openedCase.Id);
        var iter1 = iters.First();
        iter1.OpenedAt = now.AddMinutes(-50);
        await caseRepo.UpdateIterationStatusAsync(iter1.Id, "Resolved", now.AddMinutes(-20));

        // 3. Simular Iteração 2 (após reabertura) com duração de 10 minutos (OpenedAt = Agora - 15m, ClosedAt = Agora - 5m)
        var iter2 = new CaseIteration(openedCase.Id, 2, openedBy: 1L, reason: "Reaberto para ajuste", openedAt: now.AddMinutes(-15), status: "Resolved");
        iter2.ClosedAt = now.AddMinutes(-5); // 10 minutos
        await caseRepo.AddIterationAsync(iter2);

        // Atualiza o status do caso para Resolved
        await caseRepo.UpdateCaseResolutionStatusAsync(openedCase.Id, "Resolved", "Confirmed", DateTime.UtcNow, 1L);

        // Act: Consultar analytics do período
        var overview = await analyticsService.GetOverviewAnalyticsAsync(new AnalyticsFilterDto { Period = "all" });

        // Assert:
        // O MTTR por iteração DEVE ser a média das iterações individuais ((30 + 10) / 2 = 20 minutos).
        // Não pode ser 40 minutos (soma acumulada do caso multi-iteração).
        overview.MttrMetric.Value.Should().BeApproximately(20.0, 0.5);
        overview.MttrMetric.FormattedValue.Should().Be("20 min");
    }

    [Fact]
    public void CalculateMedian_DeterministicValues_WorksForEvenAndOddSamples()
    {
        // 1. Amostra ímpar: [10, 20, 30] -> Mediana = 20
        var oddList = new List<double> { 10.0, 20.0, 30.0 };
        double oddMedian = ManagementAnalyticsService.CalculateMedian(oddList);
        oddMedian.Should().Be(20.0);

        // 2. Amostra par: [10, 20, 30, 40] -> Mediana = (20 + 30) / 2 = 25
        var evenList = new List<double> { 10.0, 20.0, 30.0, 40.0 };
        double evenMedian = ManagementAnalyticsService.CalculateMedian(evenList);
        evenMedian.Should().Be(25.0);

        // 3. Amostra vazia: [] -> Mediana = 0
        double emptyMedian = ManagementAnalyticsService.CalculateMedian(new List<double>());
        emptyMedian.Should().Be(0.0);
    }

    [Fact]
    public async Task RecurrentCases_CountsOnlyRecurrenceAndCommonCause_ExcludesSimilar()
    {
        using var scope = _factory.Services.CreateScope();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var analyticsService = scope.ServiceProvider.GetRequiredService<IManagementAnalyticsService>();
        var relationRepo = scope.ServiceProvider.GetRequiredService<ICaseRelationRepository>();

        // 1. Criar Caso A, Caso B e Caso C
        var caseA = await caseService.OpenCaseAsync(new OpenCaseCommand(OriginalReport: "Falha de rede A"), currentUserId: 1L);
        var caseB = await caseService.OpenCaseAsync(new OpenCaseCommand(OriginalReport: "Falha de rede B"), currentUserId: 1L);
        var caseC = await caseService.OpenCaseAsync(new OpenCaseCommand(OriginalReport: "Falha de rede C"), currentUserId: 1L);

        // 2. Criar relação 'Similar' entre A e B (relação puramente textual, NÃO deve contar como recorrência)
        await relationRepo.SaveSimilarRelationsAsync(caseA.Id, new[] {
            new CaseRelation(caseA.Id, caseB.Id, CaseRelationType.Similar, similarityScore: 85.0)
        });

        // 3. Criar relação 'Recurrence' entre A e C (relação de recorrência de verdade)
        await relationRepo.AddManualRelationAsync(new CaseRelation(caseA.Id, caseC.Id, CaseRelationType.Recurrence, createdBy: 1L));

        // Act: Consultar métricas gerais
        var overview = await analyticsService.GetOverviewAnalyticsAsync(new AnalyticsFilterDto { Period = "all" });

        // Assert:
        // Apenas casos vinculados por Recurrence/CommonCause (Casos A e C) devem pontuar na métrica de recorrência.
        // O caso B (que só tem relação 'Similar') NÃO conta como recorrente.
        overview.RecurrentCasesMetric.Value.Should().Be(2); // Casos A e C
    }

    [Fact]
    public async Task OverviewMetrics_IdentifiesCasesWithoutRootCauseAndWithoutKnowledge()
    {
        using var scope = _factory.Services.CreateScope();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var analyticsService = scope.ServiceProvider.GetRequiredService<IManagementAnalyticsService>();
        var resolutionService = scope.ServiceProvider.GetRequiredService<ICaseResolutionService>();
        var resolutionRepo = scope.ServiceProvider.GetRequiredService<ICaseResolutionRepository>();

        // 1. Criar e resolver Caso 1 SEM causa raiz confirmada
        var case1 = await caseService.OpenCaseAsync(new OpenCaseCommand(OriginalReport: "Erro de autenticação esporádico"), currentUserId: 1L);
        await resolutionService.ResolveCaseAsync(new ResolveCaseCommand(
            CaseId: case1.Id,
            ResolutionSummary: "Reiniciado o serviço de autenticação",
            ValidationSummary: "Login testado com sucesso",
            RootCauseConfirmed: false // Sem confirmação de causa raiz
        ), currentUserId: 1L);

        // 2. Criar e resolver Caso 2 COM causa raiz confirmada
        long rootId = await resolutionRepo.AddRootCauseAsync(new RootCause("TIMEOUT-DB", "Timeout na conexão com o banco"));

        var case2 = await caseService.OpenCaseAsync(new OpenCaseCommand(OriginalReport: "Lentidão nas consultas SQL"), currentUserId: 1L);
        await resolutionService.ResolveCaseAsync(new ResolveCaseCommand(
            CaseId: case2.Id,
            ResolutionSummary: "Criado índice composto na tabela",
            ValidationSummary: "Consultas reduziram de 5s para 100ms",
            RootCauseId: rootId,
            RootCauseConfirmed: true
        ), currentUserId: 1L);

        // Act: Consultar overview
        var overview = await analyticsService.GetOverviewAnalyticsAsync(new AnalyticsFilterDto { Period = "all" });

        // Assert:
        // Caso 1 está resolvido e NÃO tem causa raiz confirmada
        overview.UnconfirmedRootCauseMetric.Value.Should().BeGreaterThanOrEqualTo(1);

        // Nenhum dos dois casos teve artigo criado na Base de Conhecimento ainda
        overview.UndocumentedKnowledgeMetric.Value.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task OpenCases_CountsBothOpenAndReopenedStatuses()
    {
        using var scope = _factory.Services.CreateScope();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var analyticsService = scope.ServiceProvider.GetRequiredService<IManagementAnalyticsService>();
        var caseRepo = scope.ServiceProvider.GetRequiredService<ICaseRepository>();

        // 1. Caso Open
        var caseOpen = await caseService.OpenCaseAsync(new OpenCaseCommand(OriginalReport: "Caso em aberto"), currentUserId: 1L);

        // 2. Caso Reopened
        var caseReopened = await caseService.OpenCaseAsync(new OpenCaseCommand(OriginalReport: "Caso reaberto"), currentUserId: 1L);
        await caseRepo.UpdateCaseReopenStatusAsync(caseReopened.Id, "Reopened", 1L);

        // 3. Caso Resolved
        var caseResolved = await caseService.OpenCaseAsync(new OpenCaseCommand(OriginalReport: "Caso resolvido"), currentUserId: 1L);
        await caseRepo.UpdateCaseResolutionStatusAsync(caseResolved.Id, "Resolved", "Confirmed", DateTime.UtcNow, 1L);

        // Act:
        var overview = await analyticsService.GetOverviewAnalyticsAsync(new AnalyticsFilterDto { Period = "all" });

        // Assert:
        // Casos abertos = Open + Reopened (mínimo 2)
        overview.OpenCasesMetric.Value.Should().BeGreaterThanOrEqualTo(2);
        overview.ResolvedCasesMetric.Value.Should().BeGreaterThanOrEqualTo(1);
    }
}
