using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TraceCore.Application.DTOs;
using TraceCore.Application.Services;
using TraceCore.Domain.Enums;
using Xunit;

namespace TraceCore.IntegrationTests;

public class StructuredEvidenceIntegrationTests : IClassFixture<TraceCoreTestApplicationFactory>
{
    private readonly TraceCoreTestApplicationFactory _factory;

    public StructuredEvidenceIntegrationTests(TraceCoreTestApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    [Fact]
    public async Task CreateEvidence_Manually_PersistsAndLoadsCorrectly()
    {
        using var scope = _factory.Services.CreateScope();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var investigationService = scope.ServiceProvider.GetRequiredService<ICaseInvestigationService>();

        var caseDto = await caseService.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Falha de conexão com a fila RabbitMQ"
        ), currentUserId: 1L);

        var hyp = await investigationService.RegisterHypothesisAsync(new RegisterHypothesisCommand(
            CaseId: caseDto.Id,
            Title: "Credenciais expiradas no broker"
        ), currentUserId: 1L);

        // Act: Registro manual de evidência
        var recordedEvidence = await investigationService.RecordEvidenceAsync(new RecordEvidenceCommand(
            CaseId: caseDto.Id,
            EvidenceType: "Log",
            Description: "2026-09-18 10:00:00 [ERROR] Authentication failure for user 'tracecore_app' on vhost '/'",
            HypothesisRelations: new List<HypothesisEvidenceRelationInputDto>
            {
                new(hyp.Id, nameof(EvidenceRelationType.Supports), "Log confirma falha explícita de autenticação no broker")
            }
        ), currentUserId: 1L);

        // Assert
        recordedEvidence.Should().NotBeNull();
        recordedEvidence.Id.Should().BeGreaterThan(0);
        recordedEvidence.EvidenceType.Should().Be("Log");
        recordedEvidence.HypothesisRelations.Should().HaveCount(1);
        recordedEvidence.HypothesisRelations![0].HypothesisId.Should().Be(hyp.Id);
        recordedEvidence.HypothesisRelations[0].RelationType.Should().Be("Supports");

        var caseEvidences = await investigationService.GetEvidencesByCaseIdAsync(caseDto.Id);
        caseEvidences.Should().ContainSingle(e => e.Id == recordedEvidence.Id);
    }

    [Fact]
    public async Task RegisterDiagnosticStep_GeneratesSuggestion_DoesNotAutoCreate_ManualConfirmationPersists()
    {
        using var scope = _factory.Services.CreateScope();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var investigationService = scope.ServiceProvider.GetRequiredService<ICaseInvestigationService>();

        var caseDto = await caseService.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Erro 500 ao consultar extrato bancário"
        ), currentUserId: 1L);

        var hyp = await investigationService.RegisterHypothesisAsync(new RegisterHypothesisCommand(
            CaseId: caseDto.Id,
            Title: "Timeout no endpoint bancário externo"
        ), currentUserId: 1L);

        // 1. Executa passo de diagnóstico associado à hipótese
        var stepDto = await investigationService.RegisterDiagnosticStepAsync(new RegisterDiagnosticStepCommand(
            CaseId: caseDto.Id,
            HypothesisId: hyp.Id,
            Title: "Ping no endpoint e verificação de rota HTTPS",
            Objective: "Validar se endpoint de integração está acessível da DMZ",
            Instruction: "Executar ping e curl a partir do nó de aplicação",
            InputEvidenceSummary: "Logs de timeout na aplicação",
            ResultSummary: "Traceroute atingiu o host externo sem perda de pacotes em 12ms",
            Outcome: nameof(DiagnosticStepOutcome.DidNotWork)
        ), currentUserId: 1L);

        // Assert: sugestão foi gerada
        stepDto.SuggestedEvidence.Should().NotBeNull();
        stepDto.SuggestedEvidence!.HypothesisId.Should().Be(hyp.Id);
        stepDto.SuggestedEvidence.DiagnosticStepId.Should().Be(stepDto.Id);
        stepDto.SuggestedEvidence.SuggestedRelationType.Should().Be(nameof(EvidenceRelationType.Contradicts));

        // Confirma que nenhuma evidência foi persistida automaticamente sem ação explícita
        var initialEvidences = await investigationService.GetEvidencesByCaseIdAsync(caseDto.Id);
        initialEvidences.Should().BeEmpty();

        // 2. Usuário confirma a sugestão explicitamente
        var recorded = await investigationService.RecordEvidenceAsync(new RecordEvidenceCommand(
            CaseId: caseDto.Id,
            EvidenceType: stepDto.SuggestedEvidence.SuggestedEvidenceType,
            Description: stepDto.SuggestedEvidence.SuggestedDescription,
            DiagnosticStepId: stepDto.SuggestedEvidence.DiagnosticStepId,
            HypothesisRelations: new List<HypothesisEvidenceRelationInputDto>
            {
                new(stepDto.SuggestedEvidence.HypothesisId.Value, stepDto.SuggestedEvidence.SuggestedRelationType, stepDto.SuggestedEvidence.SuggestedJustification)
            }
        ), currentUserId: 1L);

        recorded.Should().NotBeNull();
        recorded.DiagnosticStepId.Should().Be(stepDto.Id);

        var finalEvidences = await investigationService.GetEvidencesByCaseIdAsync(caseDto.Id);
        finalEvidences.Should().ContainSingle(e => e.Id == recorded.Id);
    }

    [Fact]
    public async Task SingleEvidence_CanBeRelatedToTwoHypotheses_WithDifferentRelationTypes()
    {
        using var scope = _factory.Services.CreateScope();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var investigationService = scope.ServiceProvider.GetRequiredService<ICaseInvestigationService>();

        var caseDto = await caseService.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Picos anormais de CPU no servidor de aplicação"
        ), currentUserId: 1L);

        var hypMemoria = await investigationService.RegisterHypothesisAsync(new RegisterHypothesisCommand(
            CaseId: caseDto.Id,
            Title: "Memory Leak provocando GC contínuo"
        ), currentUserId: 1L);

        var hypDeadlock = await investigationService.RegisterHypothesisAsync(new RegisterHypothesisCommand(
            CaseId: caseDto.Id,
            Title: "Deadlock em threads de processamento"
        ), currentUserId: 1L);

        // Uma única evidência: Dump de GC
        var evidence = await investigationService.RecordEvidenceAsync(new RecordEvidenceCommand(
            CaseId: caseDto.Id,
            EvidenceType: "Log",
            Description: "dotnet-dump indica 92% do tempo em Garbage Collection Gen2, threads ativas sem locks",
            HypothesisRelations: new List<HypothesisEvidenceRelationInputDto>
            {
                new(hypMemoria.Id, nameof(EvidenceRelationType.Supports), "Tempo de GC elevado aponta para pressão de memória"),
                new(hypDeadlock.Id, nameof(EvidenceRelationType.Contradicts), "Nenhuma thread em deadlock ou wait mútuo")
            }
        ), currentUserId: 1L);

        evidence.HypothesisRelations.Should().HaveCount(2);

        var relMemoria = evidence.HypothesisRelations!.First(r => r.HypothesisId == hypMemoria.Id);
        relMemoria.RelationType.Should().Be("Supports");

        var relDeadlock = evidence.HypothesisRelations!.First(r => r.HypothesisId == hypDeadlock.Id);
        relDeadlock.RelationType.Should().Be("Contradicts");
    }

    [Fact]
    public async Task QueryEvidence_Bidirectionally_ByCaseAndByHypothesis()
    {
        using var scope = _factory.Services.CreateScope();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var investigationService = scope.ServiceProvider.GetRequiredService<ICaseInvestigationService>();

        var caseDto = await caseService.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Investigação bidirecional de evidências"
        ), currentUserId: 1L);

        var hypA = await investigationService.RegisterHypothesisAsync(new RegisterHypothesisCommand(
            CaseId: caseDto.Id,
            Title: "Hipótese Alfa"
        ), currentUserId: 1L);

        var hypB = await investigationService.RegisterHypothesisAsync(new RegisterHypothesisCommand(
            CaseId: caseDto.Id,
            Title: "Hipótese Beta"
        ), currentUserId: 1L);

        // Evidência 1 ligada a Alfa e Beta
        await investigationService.RecordEvidenceAsync(new RecordEvidenceCommand(
            CaseId: caseDto.Id,
            EvidenceType: "Log",
            Description: "Log Alfa e Beta",
            HypothesisRelations: new List<HypothesisEvidenceRelationInputDto>
            {
                new(hypA.Id, nameof(EvidenceRelationType.Supports)),
                new(hypB.Id, nameof(EvidenceRelationType.Inconclusive))
            }
        ), currentUserId: 1L);

        // Evidência 2 ligada apenas a Beta
        await investigationService.RecordEvidenceAsync(new RecordEvidenceCommand(
            CaseId: caseDto.Id,
            EvidenceType: "DiagnosticTest",
            Description: "Teste específico de Beta",
            HypothesisRelations: new List<HypothesisEvidenceRelationInputDto>
            {
                new(hypB.Id, nameof(EvidenceRelationType.Confirms))
            }
        ), currentUserId: 1L);

        // Consultas bidirecionais
        var caseEvidences = await investigationService.GetEvidencesByCaseIdAsync(caseDto.Id);
        caseEvidences.Should().HaveCount(2);

        var evidencesHypA = await investigationService.GetEvidencesByHypothesisIdAsync(hypA.Id);
        evidencesHypA.Should().HaveCount(1);
        evidencesHypA[0].Description.Should().Be("Log Alfa e Beta");

        var evidencesHypB = await investigationService.GetEvidencesByHypothesisIdAsync(hypB.Id);
        evidencesHypB.Should().HaveCount(2);
    }
}
