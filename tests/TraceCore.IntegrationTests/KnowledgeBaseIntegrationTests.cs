using System;
using System.Collections.Generic;
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

public class KnowledgeBaseIntegrationTests : IClassFixture<TraceCoreTestApplicationFactory>
{
    private readonly TraceCoreTestApplicationFactory _factory;

    public KnowledgeBaseIntegrationTests(TraceCoreTestApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    #region Débito da Fase 5 (BR-027 e BR-028)

    [Fact]
    public async Task Phase5_BR027_ResolutionBlocked_WhenResolutionSummaryOrValidationMissing()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var resolutionService = scope.ServiceProvider.GetRequiredService<ICaseResolutionService>();

        var openedCase = await caseService.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Falha intermitente de comunicação no barramento de integração."
        ), currentUserId: 1);

        // Act & Assert 1: Sem ResolutionSummary deve falhar com BR-027
        var actMissingResolution = async () => await resolutionService.ResolveCaseAsync(new ResolveCaseCommand(
            CaseId: openedCase.Id,
            ResolutionSummary: "",
            ValidationSummary: "Executado teste de carga por 10 minutos sem perdas de pacotes.",
            RootCauseConfirmed: false
        ), currentUserId: 1);

        var ex1 = await actMissingResolution.Should().ThrowAsync<BusinessRuleValidationException>();
        ex1.Which.RuleId.Should().Be("BR-027");

        // Act & Assert 2: Sem ValidationSummary deve falhar com BR-027
        var actMissingValidation = async () => await resolutionService.ResolveCaseAsync(new ResolveCaseCommand(
            CaseId: openedCase.Id,
            ResolutionSummary: "Ajustado timeout do pool de conexões para 30s.",
            ValidationSummary: "   ",
            RootCauseConfirmed: false
        ), currentUserId: 1);

        var ex2 = await actMissingValidation.Should().ThrowAsync<BusinessRuleValidationException>();
        ex2.Which.RuleId.Should().Be("BR-027");
    }

    [Fact]
    public async Task Phase5_BR028_ResolveCase_WithoutConfirmedRootCause_ExplicitlySetsNotConfirmed()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var resolutionService = scope.ServiceProvider.GetRequiredService<ICaseResolutionService>();
        var auditRepo = scope.ServiceProvider.GetRequiredService<IAuditEventRepository>();

        var openedCase = await caseService.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Instabilidade não reproduzível em produção que normalizou após reinício."
        ), currentUserId: 1);

        // Act: Encerra com RootCauseConfirmed = false (BR-028)
        var resolution = await resolutionService.ResolveCaseAsync(new ResolveCaseCommand(
            CaseId: openedCase.Id,
            ResolutionSummary: "Realizado restart gracioso dos nós de aplicação.",
            ValidationSummary: "Health checks retornando 200 OK em todos os nós por 30 minutos.",
            RootCauseConfirmed: false,
            ResolutionType: "Workaround",
            RecurrenceRisk: "Medium"
        ), currentUserId: 1);

        // Assert
        resolution.Should().NotBeNull();
        resolution.RootCauseConfirmed.Should().BeFalse("BR-028 exige marcar explicitamente causa não confirmada");

        var updatedCase = await caseService.GetCaseByIdAsync(openedCase.Id);
        updatedCase.Should().NotBeNull();
        updatedCase!.Status.Should().Be("Resolved");

        var audits = await auditRepo.GetByEntityAsync("Case", openedCase.Id.ToString());
        audits.Should().Contain(a => a.Action == "CaseResolved");
    }

    [Fact]
    public async Task Phase5_BR028_ResolveCase_WithConfirmedRootCause_ExplicitlySetsConfirmed()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var resolutionService = scope.ServiceProvider.GetRequiredService<ICaseResolutionService>();

        var openedCase = await caseService.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Vazamento de memória identificado na rota de exportação de relatórios."
        ), currentUserId: 1);

        // Act: Encerra com RootCauseConfirmed = true (BR-028)
        var resolution = await resolutionService.ResolveCaseAsync(new ResolveCaseCommand(
            CaseId: openedCase.Id,
            ResolutionSummary: "Corrigido descarte de streams em loop de geração de PDF.",
            ValidationSummary: "Teste de estresse com 1.000 requisições simultâneas manteve uso de memória estável.",
            RootCauseConfirmed: true,
            NewRootCauseName: "Memory Leak no gerador de relatórios",
            NewRootCauseCategory: "CodeDefect",
            ResolutionType: "Definitive",
            RecurrenceRisk: "Low"
        ), currentUserId: 1);

        // Assert
        resolution.Should().NotBeNull();
        resolution.RootCauseConfirmed.Should().BeTrue("BR-028 exige confirmação explícita da causa raiz");
        resolution.RootCauseName.Should().Be("Memory Leak no gerador de relatórios");
    }

    #endregion

    #region Fase 6 — Base de Conhecimento e Soluções (BR-040 a BR-049)

    [Fact]
    public async Task BR046_CreateDraftFromCase_CopiesResolutionAndComponent_SetsDraftStatus()
    {
        // Arrange: Cria e resolve um caso para alimentar o conhecimento
        using var scope = _factory.Services.CreateScope();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var resolutionService = scope.ServiceProvider.GetRequiredService<ICaseResolutionService>();
        var knowledgeService = scope.ServiceProvider.GetRequiredService<IKnowledgeService>();

        var openedCase = await caseService.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Lentidão crítica nas consultas de faturamento mensal."
        ), currentUserId: 1);

        await resolutionService.ResolveCaseAsync(new ResolveCaseCommand(
            CaseId: openedCase.Id,
            ResolutionSummary: "Criação de índice composto na tabela faturamento (cliente_id, mes_competencia).",
            ValidationSummary: "Plano de execução validado com Explain Analyze: Index Scan em vez de Seq Scan.",
            RootCauseConfirmed: true,
            NewRootCauseName: "Falta de indexação na tabela de faturamento",
            NewRootCauseCategory: "DatabaseOptimization",
            ResponsibleComponentId: 1, // Componente afetado
            PreventiveActions: "Incluir auditoria de queries lentas nas migrações de banco."
        ), currentUserId: 1);

        // Act: Cria rascunho de conhecimento a partir do caso (BR-046)
        var knowledgeId = await knowledgeService.CreateDraftFromCaseAsync(new CreateDraftFromCaseCommand(
            CaseId: openedCase.Id
        ), userId: 1);

        // Assert
        knowledgeId.Should().BeGreaterThan(0);
        var detail = await knowledgeService.GetKnowledgeDetailAsync(knowledgeId);
        detail.Should().NotBeNull();

        // BR-046: O artigo nasce obrigatoriamente como Rascunho (Draft), nunca Published diretamente
        detail!.LifecycleStatus.Should().Be("Draft");
        detail.ProvenanceType.ToLowerInvariant().Should().Be("case");
        detail.SourceCaseId.Should().Be(openedCase.Id);
        detail.ProblemDescription.Should().Contain("Lentidão crítica nas consultas de faturamento");
        detail.ValidationMethod.Should().Be("Plano de execução validado com Explain Analyze: Index Scan em vez de Seq Scan.");
        detail.ApplicableEnvironments.Should().NotBeEmpty();
    }

    [Fact]
    public async Task BR043_Publish_Blocked_WhenApplicabilityMissing()
    {
        // Arrange: Criar rascunho sem aplicabilidade
        using var scope = _factory.Services.CreateScope();
        var knowledgeService = scope.ServiceProvider.GetRequiredService<IKnowledgeService>();

        var draftId = await knowledgeService.CreateKnowledgeDraftAsync(new CreateKnowledgeDraftCommand(
            Title: "Procedimento sem matriz de aplicabilidade",
            Summary: "Tentativa de publicação sem definir sistemas afetados",
            ValidationMethod: "Teste de conectividade via curl",
            Applicabilities: new List<CreateKnowledgeApplicabilityInput>() // Lista vazia (violação BR-043)
        ), userId: 1);

        // Act & Assert: Aprovação bloqueada por falta de aplicabilidade (BR-043)
        var actPublish = async () => await knowledgeService.ApproveAndPublishAsync(new ApproveAndPublishCommand(
            KnowledgeItemId: draftId
        ), userId: 1);

        var ex = await actPublish.Should().ThrowAsync<BusinessRuleValidationException>();
        ex.Which.RuleId.Should().Be("BR-043");
        ex.Which.Message.Should().Contain("aplicabilidade");
    }

    [Fact]
    public async Task BR044_Publish_Blocked_WhenValidationMethodMissing()
    {
        // Arrange: Criar rascunho com aplicabilidade mas sem método de validação (BR-044)
        using var scope = _factory.Services.CreateScope();
        var knowledgeService = scope.ServiceProvider.GetRequiredService<IKnowledgeService>();

        var draftId = await knowledgeService.CreateKnowledgeDraftAsync(new CreateKnowledgeDraftCommand(
            Title: "Procedimento sem método de validação formal",
            Summary: "Aplicação de script de limpeza sem validação técnica",
            ValidationMethod: "   ", // Vazio (violação BR-044)
            Applicabilities: new List<CreateKnowledgeApplicabilityInput>
            {
                new CreateKnowledgeApplicabilityInput(ProductId: 1)
            }
        ), userId: 1);

        // Act & Assert: Publicação bloqueada por BR-044
        var actPublish = async () => await knowledgeService.ApproveAndPublishAsync(new ApproveAndPublishCommand(
            KnowledgeItemId: draftId
        ), userId: 1);

        var ex = await actPublish.Should().ThrowAsync<BusinessRuleValidationException>();
        ex.Which.RuleId.Should().Be("BR-044");
        ex.Which.Message.Should().Contain("validação");
    }

    [Fact]
    public async Task BR045_Publish_Blocked_WhenRiskWarningPresent_WithoutRollbackPlan()
    {
        // Arrange: Criar rascunho com aviso de risco porém SEM rollback plan (BR-045)
        using var scope = _factory.Services.CreateScope();
        var knowledgeService = scope.ServiceProvider.GetRequiredService<IKnowledgeService>();

        var draftId = await knowledgeService.CreateKnowledgeDraftAsync(new CreateKnowledgeDraftCommand(
            Title: "Procedimento de Alto Risco Operacional",
            Summary: "Reindexação e drop de tabelas temporárias",
            ValidationMethod: "Verificar integridade referencial com checksum",
            RiskWarning: "Pode causar lock exclusivo na base de dados por até 5 minutos",
            RollbackPlan: "", // Vazio (violação BR-045)
            Applicabilities: new List<CreateKnowledgeApplicabilityInput>
            {
                new CreateKnowledgeApplicabilityInput(ProductId: 1)
            }
        ), userId: 1);

        // Act & Assert: Publicação bloqueada por BR-045
        var actPublish = async () => await knowledgeService.ApproveAndPublishAsync(new ApproveAndPublishCommand(
            KnowledgeItemId: draftId
        ), userId: 1);

        var ex = await actPublish.Should().ThrowAsync<BusinessRuleValidationException>();
        ex.Which.RuleId.Should().Be("BR-045");
        ex.Which.Message.Should().Contain("rollback");
    }

    [Fact]
    public async Task BR040_BR041_FullLifecycle_DraftToReviewToPublished_RecordsApproverAndDate()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var knowledgeService = scope.ServiceProvider.GetRequiredService<IKnowledgeService>();
        var auditRepo = scope.ServiceProvider.GetRequiredService<IAuditEventRepository>();

        var draftId = await knowledgeService.CreateKnowledgeDraftAsync(new CreateKnowledgeDraftCommand(
            Title: "Procedimento Oficial de Recuperação de Deadlock",
            Summary: "Passo a passo auditado para desbloqueio de sessões presas",
            ValidationMethod: "Consultar sys.dm_exec_requests e confirmar liberação de locks",
            RiskWarning: "Encerramento de sessão pode causar rollback de transação ativa",
            RollbackPlan: "Reexecutar a transação cancelada pelo scheduler automático",
            Applicabilities: new List<CreateKnowledgeApplicabilityInput>
            {
                new CreateKnowledgeApplicabilityInput(ProductId: 1, ComponentId: 1)
            }
        ), userId: 2);

        // 1. Ciclo de Vida: Draft -> Review (BR-040)
        await knowledgeService.SubmitForReviewAsync(new SubmitForReviewCommand(draftId), userId: 2);
        var inReview = await knowledgeService.GetKnowledgeDetailAsync(draftId);
        inReview!.LifecycleStatus.Should().Be("Review");

        // 2. Ciclo de Vida: Review -> Published (BR-041)
        await knowledgeService.ApproveAndPublishAsync(new ApproveAndPublishCommand(
            KnowledgeItemId: draftId
        ), userId: 1);

        var published = await knowledgeService.GetKnowledgeDetailAsync(draftId);
        published!.LifecycleStatus.Should().Be("Published");
        published.Reviewer.Should().NotBeNullOrWhiteSpace();
        published.PublishedAt.Should().NotBeNull();
        published.Version.Should().Be("v1.0");

        // Auditoria
        var audits = await auditRepo.GetByEntityAsync("KnowledgeItem", draftId.ToString());
        audits.Should().Contain(a => a.Action == "KnowledgePublished");
    }

    [Fact]
    public async Task BR042_CreateNewVersion_CreatesVersion2_KeepsVersion1Intact()
    {
        // Arrange: Publica uma versão inicial
        using var scope = _factory.Services.CreateScope();
        var knowledgeService = scope.ServiceProvider.GetRequiredService<IKnowledgeService>();

        var itemId = await knowledgeService.CreateKnowledgeDraftAsync(new CreateKnowledgeDraftCommand(
            Title: "Diretriz de Configuração de Pool",
            Summary: "Tuning inicial",
            ContentMarkdown: "Versão 1.0 com MaxPoolSize=100",
            ValidationMethod: "Conectar 50 clientes",
            Applicabilities: new List<CreateKnowledgeApplicabilityInput> { new(ProductId: 1) }
        ), userId: 1);

        await knowledgeService.ApproveAndPublishAsync(new ApproveAndPublishCommand(itemId), userId: 1);

        // Act: Cria versão 2 (BR-042)
        var newVersion = await knowledgeService.CreateNewVersionAsync(new CreateNewVersionCommand(
            KnowledgeItemId: itemId,
            ContentMarkdown: "Versão 2.0 com MaxPoolSize=250 e minPoolSize=20",
            ValidationMethod: "Conectar 150 clientes em teste de carga",
            ChangeSummary: "Ajuste de capacidade para alta volumetria"
        ), userId: 1);

        // Assert
        newVersion.Should().Be(2);

        var detail = await knowledgeService.GetKnowledgeDetailAsync(itemId);
        detail.Should().NotBeNull();
        detail!.Version.Should().Be("v2.0");
        detail.Revisions.Should().HaveCount(2, "Histórico deve preservar versão 1 e versão 2 intactas para auditoria");
        detail.Revisions.Should().Contain(r => r.Version == "v1.0");
        detail.Revisions.Should().Contain(r => r.Version == "v2.0");
    }

    [Fact]
    public async Task BR047_BR048_RecordUsage_CalculatesSuccessRate_WithSampleSize()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var knowledgeService = scope.ServiceProvider.GetRequiredService<IKnowledgeService>();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();

        var itemId = await knowledgeService.CreateKnowledgeDraftAsync(new CreateKnowledgeDraftCommand(
            Title: "Script de Reindexação Automática",
            Summary: "Script executado em incidentes de lentidão",
            ValidationMethod: "Monitoramento de latência",
            Applicabilities: new List<CreateKnowledgeApplicabilityInput> { new(ProductId: 1) }
        ), userId: 1);

        await knowledgeService.ApproveAndPublishAsync(new ApproveAndPublishCommand(itemId), userId: 1);

        // Cria 4 casos para registrar usos
        var c1 = await caseService.OpenCaseAsync(new OpenCaseCommand(OriginalReport: "Caso 1"), 1);
        var c2 = await caseService.OpenCaseAsync(new OpenCaseCommand(OriginalReport: "Caso 2"), 1);
        var c3 = await caseService.OpenCaseAsync(new OpenCaseCommand(OriginalReport: "Caso 3"), 1);
        var c4 = await caseService.OpenCaseAsync(new OpenCaseCommand(OriginalReport: "Caso 4"), 1);

        // Act: Registra 2 Worked, 1 PartiallyWorked, 1 NotWorked (BR-047)
        await knowledgeService.RecordUsageAsync(new RecordKnowledgeUsageCommand(itemId, c1.Id, "Worked"), 1);
        await knowledgeService.RecordUsageAsync(new RecordKnowledgeUsageCommand(itemId, c2.Id, "Worked"), 1);
        await knowledgeService.RecordUsageAsync(new RecordKnowledgeUsageCommand(itemId, c3.Id, "PartiallyWorked"), 1);
        await knowledgeService.RecordUsageAsync(new RecordKnowledgeUsageCommand(itemId, c4.Id, "NotWorked"), 1);

        // Assert: 3 sucessos em 4 usos = 75% com amostra (4 casos) (BR-048)
        var detail = await knowledgeService.GetKnowledgeDetailAsync(itemId);
        detail.Should().NotBeNull();
        detail!.TotalUsages.Should().Be(4);
        detail.SuccessfulUsages.Should().Be(3);
        detail.SuccessRate.Should().Contain("75");
        detail.AssociatedCases.Should().HaveCount(4);
    }

    [Fact]
    public async Task BR049_Deprecate_ChangesStatusToDeprecated_KeepsHistoryIntact()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var knowledgeService = scope.ServiceProvider.GetRequiredService<IKnowledgeService>();

        var itemId = await knowledgeService.CreateKnowledgeDraftAsync(new CreateKnowledgeDraftCommand(
            Title: "Procedimento Legado de Backup em Fita",
            Summary: "Procedimento antigo que foi substituído",
            ValidationMethod: "Conferir integridade do drive",
            Applicabilities: new List<CreateKnowledgeApplicabilityInput> { new(ProductId: 1) }
        ), userId: 1);

        await knowledgeService.ApproveAndPublishAsync(new ApproveAndPublishCommand(itemId), userId: 1);

        // Act: Descontinua o procedimento (BR-049)
        await knowledgeService.DeprecateKnowledgeAsync(new DeprecateKnowledgeCommand(itemId), userId: 1);

        // Assert
        var detail = await knowledgeService.GetKnowledgeDetailAsync(itemId);
        detail.Should().NotBeNull();
        detail!.LifecycleStatus.Should().Be("Deprecated");
        detail.Title.Should().Be("Procedimento Legado de Backup em Fita");
        // O histórico e versões devem permanecer intactos
        detail.Revisions.Should().NotBeEmpty();
    }

    #endregion
}
