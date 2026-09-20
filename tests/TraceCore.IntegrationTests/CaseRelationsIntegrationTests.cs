using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TraceCore.Application.DTOs;
using TraceCore.Application.Services;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Enums;
using TraceCore.Domain.Repositories;
using Xunit;

namespace TraceCore.IntegrationTests;

public class CaseRelationsIntegrationTests : IClassFixture<TraceCoreTestApplicationFactory>
{
    private readonly TraceCoreTestApplicationFactory _factory;

    public CaseRelationsIntegrationTests(TraceCoreTestApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    [Fact]
    public async Task ComputeSimilarCases_MatchesOnContextSignals_ReturnsDeterministicScoreAndFactors()
    {
        using var scope = _factory.Services.CreateScope();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var relationService = scope.ServiceProvider.GetRequiredService<ICaseRelationService>();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();

        // 1. Criar sistema e versão de teste no catálogo
        long prodId = await catalogRepo.AddProductAsync(new Product("Gateway de Pagamentos", "GW-PAY"));
        long verId = await catalogRepo.AddProductVersionAsync(new ProductVersion(prodId, "v2.5.0"));

        // 2. Criar Caso A (anterior, já fechado/resolvido)
        var caseA = await caseService.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Instabilidade com timeout na conexão HTTP com o adquirente",
            ProductId: prodId,
            ProductVersionId: verId,
            ErrorCode: "ERR_TIMEOUT_GATEWAY"
        ), currentUserId: 1L);

        // 3. Criar Caso B (novo caso investigado com mesmo erro, produto e versão)
        var caseB = await caseService.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Timeout intermitente na conexão HTTP com adquirente durante checkout",
            ProductId: prodId,
            ProductVersionId: verId,
            ErrorCode: "ERR_TIMEOUT_GATEWAY"
        ), currentUserId: 1L);

        // Act: Computar similaridades para o Caso B
        var similarCases = await relationService.ComputeSimilarCasesAsync(caseB.Id);

        // Assert: Princípio P-006 (Similaridade determinística com MatchedFactors, nunca probabilidade)
        similarCases.Should().NotBeEmpty();
        var matchedA = similarCases.FirstOrDefault(c => c.TargetCaseId == caseA.Id);
        matchedA.Should().NotBeNull();
        matchedA!.SimilarityScore.Should().BeGreaterThan(50); // Múltiplos sinais coincidentes
        matchedA.MatchedFactors.Should().Contain(f => f.Contains("Mesmo erro", StringComparison.OrdinalIgnoreCase));
        matchedA.MatchedFactors.Should().Contain(f => f.Contains("Mesmo Produto", StringComparison.OrdinalIgnoreCase));
        matchedA.MatchedFactors.Should().Contain(f => f.Contains("Mesma Versão", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AggregatedInsight_AppearsOnlyWithMinimumSample_AndReflectsMostFrequentAction()
    {
        using var scope = _factory.Services.CreateScope();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var investigationService = scope.ServiceProvider.GetRequiredService<ICaseInvestigationService>();
        var resolutionService = scope.ServiceProvider.GetRequiredService<ICaseResolutionService>();
        var relationService = scope.ServiceProvider.GetRequiredService<ICaseRelationService>();

        const string sharedErrorCode = "ERR_DATABASE_DEADLOCK";

        // Helper local para criar caso resolvido com passo 'Worked'
        async Task<CaseDto> CreateResolvedCaseWithStep(string successfulActionTitle)
        {
            var c = await caseService.OpenCaseAsync(new OpenCaseCommand(
                OriginalReport: "Travamento de transações simultâneas por deadlock no banco de dados",
                ErrorCode: sharedErrorCode
            ), currentUserId: 1L);

            // Registra hipótese e passo investigativo com Worked
            var hyp = await investigationService.RegisterHypothesisAsync(new RegisterHypothesisCommand(
                CaseId: c.Id,
                Title: "Gargalo de concorrência ou falta de índices"
            ), currentUserId: 1L);

            await investigationService.RegisterDiagnosticStepAsync(new RegisterDiagnosticStepCommand(
                CaseId: c.Id,
                HypothesisId: hyp.Id,
                Title: successfulActionTitle,
                Objective: "Validar resolução de deadlock",
                Instruction: "Executar plano de indexação e medição de locks",
                InputEvidenceSummary: "Alertas de concorrência e traces de bloqueio",
                ResultSummary: "Deadlock eliminado após aplicar alteração nos índices",
                Outcome: nameof(DiagnosticStepOutcome.Worked)
            ), currentUserId: 1L);

            // Encerra o caso
            await resolutionService.ResolveCaseAsync(new ResolveCaseCommand(
                CaseId: c.Id,
                ResolutionSummary: $"Aplicada ação: {successfulActionTitle}",
                ValidationSummary: "Carga de 100 requisições simultâneas sem deadlocks",
                RootCauseConfirmed: true
            ), currentUserId: 1L);

            return c;
        }

        // 1. Criar apenas 2 casos similares resolvidos (Amostra M = 2, insuficiente para BR-048)
        var case1 = await CreateResolvedCaseWithStep("Otimizar índices da tabela de pedidos");
        var case2 = await CreateResolvedCaseWithStep("Otimizar índices da tabela de pedidos");

        var targetCase = await caseService.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Novo relato de deadlock em concorrência de banco",
            ErrorCode: sharedErrorCode
        ), currentUserId: 1L);

        // Act com M = 2: Overview não deve exibir Insight Agregado
        var overviewM2 = await relationService.GetCaseRelationsOverviewAsync(targetCase.Id);
        overviewM2.Insight.Should().BeNull("conforme BR-048, amostra mínima confiável para insight é M >= 3");

        // 2. Adicionar o 3º caso resolvido com a mesma ação frequente (M = 3)
        var case3 = await CreateResolvedCaseWithStep("Otimizar índices da tabela de pedidos");

        // Act com M = 3
        var overviewM3 = await relationService.GetCaseRelationsOverviewAsync(targetCase.Id);

        // Assert: Insight deve ser exibido com os dados agregados corretos
        overviewM3.Insight.Should().NotBeNull();
        overviewM3.Insight!.SampleCount.Should().Be(3);
        overviewM3.Insight.SuccessCount.Should().Be(3);
        overviewM3.Insight.ActionOrComponent.Should().Be("Otimizar índices da tabela de pedidos");
        overviewM3.Insight.Text.Should().Contain("3 de 3 casos semelhantes foram resolvidos verificando/agindo sobre: Otimizar índices da tabela de pedidos");
    }

    [Fact]
    public async Task ManualRelation_CreatesAuditEvent_AndEnforcesValidationConstraints()
    {
        using var scope = _factory.Services.CreateScope();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var relationService = scope.ServiceProvider.GetRequiredService<ICaseRelationService>();
        var auditRepo = scope.ServiceProvider.GetRequiredService<IAuditEventRepository>();

        var caseSource = await caseService.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Erro de parsing de payload XML"
        ), currentUserId: 1L);

        var caseTarget = await caseService.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Falha de validação de esquema XSD em integração"
        ), currentUserId: 1L);

        // 1. Bloqueio de autorreferência
        var selfRelAction = () => relationService.CreateManualRelationAsync(new CreateCaseRelationCommand(
            SourceCaseId: caseSource.Id,
            TargetCaseId: caseSource.Id,
            RelationType: "Duplicate"
        ), userId: 1L);

        await selfRelAction.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ele próprio*");

        // 2. Criação válida de relacionamento manual
        long relationId = await relationService.CreateManualRelationAsync(new CreateCaseRelationCommand(
            SourceCaseId: caseSource.Id,
            TargetCaseNumber: caseTarget.CaseNumber,
            RelationType: "Duplicate"
        ), userId: 1L);

        relationId.Should().BeGreaterThan(0);

        // 3. Auditoria obrigatória (audit_events)
        var auditEvents = await auditRepo.GetRecentAsync(10);
        var relationAudit = auditEvents.FirstOrDefault(a => a.Action == "CaseRelationCreated" && a.ActorUserId == 1L);
        relationAudit.Should().NotBeNull();
        relationAudit!.EntityType.Should().Be("CaseRelation");
        relationAudit.EntityId.Should().Be(relationId.ToString());

        // 4. Bloqueio de relacionamento duplicado
        var duplicateRelAction = () => relationService.CreateManualRelationAsync(new CreateCaseRelationCommand(
            SourceCaseId: caseSource.Id,
            TargetCaseId: caseTarget.Id,
            RelationType: "Duplicate"
        ), userId: 1L);

        await duplicateRelAction.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Já existe um relacionamento*");

        // 5. Consulta via Overview
        var overview = await relationService.GetCaseRelationsOverviewAsync(caseSource.Id);
        overview.ManualRelations.Should().ContainSingle(m => m.TargetCaseId == caseTarget.Id && m.RelationType == "Duplicate");
    }

    [Fact]
    public async Task ComputeSimilarCases_IsIdempotent_DoesNotDuplicateRowsUponRecalculation()
    {
        using var scope = _factory.Services.CreateScope();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var relationService = scope.ServiceProvider.GetRequiredService<ICaseRelationService>();

        var c1 = await caseService.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Lentidão crítica de I/O de disco",
            ErrorCode: "DISK_IO_SLOW"
        ), currentUserId: 1L);

        var c2 = await caseService.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Alerta de latência de leitura em disco",
            ErrorCode: "DISK_IO_SLOW"
        ), currentUserId: 1L);

        // Act: Executar computação determinística duas vezes
        var run1 = await relationService.ComputeSimilarCasesAsync(c2.Id);
        var run2 = await relationService.ComputeSimilarCasesAsync(c2.Id);

        // Assert: Idempotência garantida pela restrição UNIQUE e upsert
        run1.Should().NotBeEmpty();
        run2.Should().NotBeEmpty();
        run2.Count.Should().Be(run1.Count);

        var overview = await relationService.GetCaseRelationsOverviewAsync(c2.Id);
        overview.SimilarCases.Count(s => s.TargetCaseId == c1.Id).Should().Be(1, "não deve haver duplicatas de relação Similar");
    }

    [Fact]
    public async Task ComputeSimilarCases_FindsOlderRelevantCase_EvenOutsideTheMostRecent50()
    {
        // Regressão do bug (Prompt 3, §4/§80): GetPotentialSimilarCandidatesAsync
        // recebia productId/errorCode mas ignorava os dois, trazendo apenas os 50
        // casos mais recentes do sistema. Um caso realmente relevante (mesmo produto
        // e erro), porém mais antigo, nunca era considerado como candidato.
        using var scope = _factory.Services.CreateScope();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var relationService = scope.ServiceProvider.GetRequiredService<ICaseRelationService>();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();

        const string sharedErrorCode = "ERR_LEGACY_RELEVANT";
        long prodId = await catalogRepo.AddProductAsync(new Product("Sistema Legado Relevante", "SIS-LEG"));

        // 1. Caso antigo e relevante — criado primeiro (mais antigo por OpenedAt)
        var oldRelevantCase = await caseService.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Falha antiga no mesmo componente com o mesmo código de erro",
            ProductId: prodId,
            ErrorCode: sharedErrorCode
        ), currentUserId: 1L);

        // 2. 60 casos recentes e totalmente não relacionados, para empurrar o caso
        // relevante para fora dos "50 mais recentes" se a busca continuar por data.
        for (int i = 0; i < 60; i++)
        {
            await caseService.OpenCaseAsync(new OpenCaseCommand(
                OriginalReport: $"Caso recente sem relação nenhuma #{i}"
            ), currentUserId: 1L);
        }

        // 3. Novo caso investigado, mesmo produto e mesmo erro do caso antigo
        var newCase = await caseService.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Nova ocorrência do mesmo problema neste sistema",
            ProductId: prodId,
            ErrorCode: sharedErrorCode
        ), currentUserId: 1L);

        // Act
        var similarCases = await relationService.ComputeSimilarCasesAsync(newCase.Id);

        // Assert: o caso antigo relevante deve ser encontrado apesar de não estar
        // entre os casos mais recentes do sistema.
        similarCases.Should().Contain(s => s.TargetCaseId == oldRelevantCase.Id);
    }
}
