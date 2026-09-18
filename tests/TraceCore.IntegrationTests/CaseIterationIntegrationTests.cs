using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TraceCore.Application.DTOs;
using TraceCore.Application.Exceptions;
using TraceCore.Application.Services;
using TraceCore.Domain.Repositories;
using Xunit;

namespace TraceCore.IntegrationTests;

public class CaseIterationIntegrationTests : IClassFixture<TraceCoreTestApplicationFactory>
{
    private readonly TraceCoreTestApplicationFactory _factory;

    public CaseIterationIntegrationTests(TraceCoreTestApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    [Fact]
    public async Task CaseCreation_InitializesIteration1_WithOpenStatus()
    {
        using var scope = _factory.Services.CreateScope();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();

        var cmd = new OpenCaseCommand(
            OriginalReport: "Usuários relataram tela branca às 14:00 no checkout.",
            Severity: "High"
        );

        var caseDto = await caseService.OpenCaseAsync(cmd, currentUserId: 1L);

        caseDto.Should().NotBeNull();
        caseDto.Status.Should().Be("Open");
        caseDto.Iterations.Should().NotBeNull();
        caseDto.Iterations!.Should().HaveCount(1);

        var it1 = caseDto.Iterations![0];
        it1.SequenceNumber.Should().Be(1);
        it1.Status.Should().Be("Open");
        it1.ClosedAt.Should().BeNull();
    }

    [Fact]
    public async Task ReopenCase_ThrowsException_WhenCaseIsNotResolved()
    {
        using var scope = _factory.Services.CreateScope();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();

        var cmd = new OpenCaseCommand(
            OriginalReport: "Relato original de teste de bloqueio de reabertura",
            Severity: "Medium"
        );

        var caseDto = await caseService.OpenCaseAsync(cmd, currentUserId: 1L);

        // Act & Assert: Tentar reabrir caso ainda Open
        var act = async () => await caseService.ReopenCaseAsync(caseDto.Id, "Tentando reabrir caso aberto", reopenedBy: 1L);
        await act.Should().ThrowAsync<BusinessRuleValidationException>();
    }

    [Fact]
    public async Task ReopenCase_GeneratesIteration2_PreservesIteration1_AndSetsStatusReopened()
    {
        using var scope = _factory.Services.CreateScope();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var resolutionService = scope.ServiceProvider.GetRequiredService<ICaseResolutionService>();

        var caseDto = await caseService.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Socket timed out no Gateway",
            Severity: "High"
        ), currentUserId: 1L);
        var caseId = caseDto.Id;

        // Resolve Iteração 1
        await resolutionService.ResolveCaseAsync(new ResolveCaseCommand(
            CaseId: caseId,
            ResolutionSummary: "Ajustado timeout de rede de 5s para 30s",
            ValidationSummary: "Testes de carga executados durante 1 hora sem nenhum erro 504",
            RootCauseConfirmed: true,
            ResolutionType: "Definitive",
            RecurrenceRisk: "Low"
        ), currentUserId: 1L);

        var resolvedCase = await caseService.GetCaseByIdAsync(caseId);
        resolvedCase!.Status.Should().Be("Resolved");
        resolvedCase.Iterations!.Should().HaveCount(1);
        resolvedCase.Iterations![0].Status.Should().Be("Resolved");
        resolvedCase.Iterations![0].ClosedAt.Should().NotBeNull();

        // Reabre o caso -> Iteração 2
        await caseService.ReopenCaseAsync(caseId, "Sintomas voltaram após novo pico de tráfego", reopenedBy: 1L);

        var reopenedCase = await caseService.GetCaseByIdAsync(caseId);
        reopenedCase!.Status.Should().Be("Reopened");
        reopenedCase.Iterations!.Should().HaveCount(2);

        var it1 = reopenedCase.Iterations!.First(i => i.SequenceNumber == 1);
        it1.Status.Should().Be("Resolved");
        it1.ClosedAt.Should().NotBeNull();

        var it2 = reopenedCase.Iterations!.First(i => i.SequenceNumber == 2);
        it2.Status.Should().Be("Open");
        it2.Reason.Should().Be("Sintomas voltaram após novo pico de tráfego");
        it2.ClosedAt.Should().BeNull();
    }

    [Fact]
    public async Task Iteration2_CanBeResolved_IndependentlyFromIteration1()
    {
        using var scope = _factory.Services.CreateScope();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var resolutionService = scope.ServiceProvider.GetRequiredService<ICaseResolutionService>();
        var resolutionRepo = scope.ServiceProvider.GetRequiredService<ICaseResolutionRepository>();

        var caseDto = await caseService.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Usuários reclamando de lentidão crítica na consulta",
            Severity: "Critical"
        ), currentUserId: 1L);
        var caseId = caseDto.Id;

        // 1. Resolve Iteração 1
        await resolutionService.ResolveCaseAsync(new ResolveCaseCommand(
            CaseId: caseId,
            ResolutionSummary: "Criado índice composto na tabela de vendas",
            ValidationSummary: "Tempo de resposta caiu para 1.2s",
            RootCauseConfirmed: true,
            ResolutionType: "Definitive",
            RecurrenceRisk: "Low"
        ), currentUserId: 1L);

        var caseAfterIt1 = await caseService.GetCaseByIdAsync(caseId);
        var it1Id = caseAfterIt1!.Iterations!.First(i => i.SequenceNumber == 1).Id;

        // 2. Reabre caso
        await caseService.ReopenCaseAsync(caseId, "Índice gerou lock durante carga noturna", reopenedBy: 2L);

        var caseAfterReopen = await caseService.GetCaseByIdAsync(caseId);
        var it2Id = caseAfterReopen!.Iterations!.First(i => i.SequenceNumber == 2).Id;

        // 3. Resolve Iteração 2
        await resolutionService.ResolveCaseAsync(new ResolveCaseCommand(
            CaseId: caseId,
            ResolutionSummary: "Ajustada query para particionamento mensal e índice com INCLUDE",
            ValidationSummary: "Rotina noturna finalizou em 4 minutos sem nenhum lock de tabela",
            RootCauseConfirmed: true,
            ResolutionType: "Definitive",
            RecurrenceRisk: "Low"
        ), currentUserId: 2L);

        // 4. Assert: Ambas resoluções existem e estão preservadas
        var resIt1 = await resolutionRepo.GetByIterationIdAsync(it1Id);
        resIt1.Should().NotBeNull();
        resIt1!.ResolutionSummary.Should().Be("Criado índice composto na tabela de vendas");

        var resIt2 = await resolutionRepo.GetByIterationIdAsync(it2Id);
        resIt2.Should().NotBeNull();
        resIt2!.ResolutionSummary.Should().Be("Ajustada query para particionamento mensal e índice com INCLUDE");

        var finalCase = await caseService.GetCaseByIdAsync(caseId);
        finalCase!.Status.Should().Be("Resolved");
        finalCase.Iterations!.Should().HaveCount(2);
        finalCase.Iterations!.All(i => i.Status == "Resolved").Should().BeTrue();
    }
}
