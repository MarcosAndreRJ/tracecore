using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TraceCore.Application.DTOs;
using TraceCore.Application.Services;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;
using Xunit;

namespace TraceCore.IntegrationTests;

public class AuditAndContentPreparationIntegrationTests : IClassFixture<TraceCoreTestApplicationFactory>
{
    private readonly TraceCoreTestApplicationFactory _factory;

    public AuditAndContentPreparationIntegrationTests(TraceCoreTestApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    // ==========================================
    // FASE 11: AUDITORIA AMPLIADA
    // ==========================================

    [Fact]
    public void AuditRepository_IsAppendOnly_HasNoUpdateOrDeleteMethods()
    {
        var repoType = typeof(IAuditEventRepository);
        var methods = repoType.GetMethods().Select(m => m.Name).ToList();

        methods.Should().NotContain(m => m.StartsWith("Update") || m.StartsWith("Delete") || m.StartsWith("Remove"));
    }

    [Fact]
    public async Task AuditService_SanitizesSensitiveFields_WithRedactedPlaceholder()
    {
        using var scope = _factory.Services.CreateScope();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
        var auditRepo = scope.ServiceProvider.GetRequiredService<IAuditEventRepository>();

        var sensitivePayload = new
        {
            Username = "analista_teste",
            PasswordHash = "super_secret_hash_value",
            TokenSecret = "jwt_bearer_token_12345",
            ApiKey = "api_key_secret_value"
        };

        await auditService.RecordAsync(
            action: "user.security_test",
            entityType: "users",
            entityId: "999",
            actorUserId: 1L,
            before: sensitivePayload,
            after: new { Status = "Updated" },
            metadata: new { AccessToken = "secret_access_token_abc" }
        );

        var events = await auditRepo.GetByEntityAsync("users", "999");
        events.Should().ContainSingle();

        var evt = events.First();
        evt.BeforeJson.Should().NotBeNull();
        evt.BeforeJson.Should().Contain("***REDACTED***");
        evt.BeforeJson.Should().NotContain("super_secret_hash_value");
        evt.BeforeJson.Should().NotContain("jwt_bearer_token_12345");

        evt.MetadataJson.Should().NotBeNull();
        evt.MetadataJson.Should().Contain("***REDACTED***");
        evt.MetadataJson.Should().NotContain("secret_access_token_abc");
    }

    [Fact]
    public async Task CatalogService_Mutations_RecordAuditEventsWithProperNamingConvention()
    {
        using var scope = _factory.Services.CreateScope();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var auditRepo = scope.ServiceProvider.GetRequiredService<IAuditEventRepository>();

        // 1. Criar Produto
        var productId = await catalogService.CreateProductAsync("Sistema ERP", "ERP", "Módulo Central", false, currentUserId: 1L);

        // 2. Atualizar Produto
        await catalogService.UpdateProductAsync(productId, "Sistema ERP Enterprise", "ERP-ENT", "Atualizado", "Active", false, currentUserId: 1L);

        // 3. Criar Componentes
        var comp1Id = await catalogService.CreateComponentAsync("Módulo Fiscal", "Service", productId, "FSC", "Emissão de NFe", null, currentUserId: 1L);
        var comp2Id = await catalogService.CreateComponentAsync("Banco de Dados Fiscal", "Database", productId, "DB-FSC", "Base de dados", null, currentUserId: 1L);

        // 4. Criar Dependência
        var depId = await catalogService.AddComponentDependencyAsync(comp1Id, comp2Id, "Database", "Critical", "Dependência crítica", currentUserId: 1L);

        // Act: Obter eventos de auditoria recentes
        var recentAudits = await auditRepo.GetRecentAsync(limit: 50);

        // Assert: Ações gravadas
        recentAudits.Should().Contain(a => a.Action == "product.create" && a.EntityId == productId.ToString());
        recentAudits.Should().Contain(a => a.Action == "product.update" && a.EntityId == productId.ToString());
        recentAudits.Should().Contain(a => a.Action == "component.create" && a.EntityId == comp1Id.ToString());
        recentAudits.Should().Contain(a => a.Action == "component.dependency_create" && a.EntityId == depId.ToString());

        // Assert de Convenção de Nomenclatura corporativa: lowercase, entidade.verbo[_objeto]
        var newActions = new[] { "product.create", "product.update", "component.create", "component.dependency_create" };
        foreach (var action in newActions)
        {
            Regex.IsMatch(action, @"^[a-z0-9_]+\.[a-z0-9_]+$").Should().BeTrue($"Ação '{action}' deve seguir a convenção entidade.verbo[_objeto]");
        }
    }

    [Fact]
    public async Task AuditSearch_FiltersAccurately_ByActionAndDate()
    {
        using var scope = _factory.Services.CreateScope();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

        await auditService.RecordAsync("test.action_alpha", "TestEntity", "101", actorUserId: 1L);
        await auditService.RecordAsync("test.action_beta", "TestEntity", "102", actorUserId: 2L);

        var resultAlpha = await auditService.SearchAsync(new AuditFilterDto(Action: "test.action_alpha"));
        resultAlpha.Items.Should().ContainSingle(i => i.Action == "test.action_alpha" && i.EntityId == "101");
        resultAlpha.Items.Should().NotContain(i => i.Action == "test.action_beta");

        var resultActor2 = await auditService.SearchAsync(new AuditFilterDto(ActorUserId: 2L));
        resultActor2.Items.Should().Contain(i => i.ActorUserId == 2L && i.EntityId == "102");
    }

    // ==========================================
    // FASE 12: PREPARAÇÃO PARA IA
    // ==========================================

    [Fact]
    public async Task ContentPreparation_PreservesTechnicalIdentifiersLiterally()
    {
        using var scope = _factory.Services.CreateScope();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var prepService = scope.ServiceProvider.GetRequiredService<IContentPreparationService>();

        // Caso com termos técnicos sensíveis a alteração
        var technicalReport = "Falha ORA-12541 na conexão com listener do Oracle no endpoint /api/v1/auth/tokens (versão v8.2.1-rc1). Retorno HTTP 500 com código ERR_CONNECTION_TIMED_OUT.";
        var openedCase = await caseService.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: technicalReport,
            Severity: "Critical",
            ErrorCode: "ORA-12541",
            ErrorMessage: "TNS:no listener"
        ), currentUserId: 1L);

        // Ingestão pelo ContentPreparationService
        var entry = await prepService.PrepareCaseAsync(openedCase.Id, currentUserId: 1L);

        // Assert: Identificadores técnicos preservados literalmente (§12.8 / §12.9)
        entry.NormalizedContent.Should().Contain("ORA-12541");
        entry.NormalizedContent.Should().Contain("/api/v1/auth/tokens");
        entry.NormalizedContent.Should().Contain("v8.2.1-rc1");
        entry.NormalizedContent.Should().Contain("HTTP 500");
        entry.NormalizedContent.Should().Contain("ERR_CONNECTION_TIMED_OUT");
    }

    [Fact]
    public async Task ContentPreparation_IsIdempotent_DoesNotDuplicateOnReprocessing()
    {
        using var scope = _factory.Services.CreateScope();
        var knowledgeService = scope.ServiceProvider.GetRequiredService<IKnowledgeService>();
        var prepService = scope.ServiceProvider.GetRequiredService<IContentPreparationService>();
        var contentRepo = scope.ServiceProvider.GetRequiredService<ISearchableContentRepository>();

        // 1. Criar e Publicar Artigo de Conhecimento
        var articleId = await knowledgeService.CreateKnowledgeDraftAsync(new CreateKnowledgeDraftCommand(
            Title: "Procedimento de Recuperação de Listener Oracle",
            Summary: "Passos para reiniciar o listener em caso de ORA-12541",
            KnowledgeType: "Solution",
            ProvenanceType: "Documentation",
            ContentMarkdown: "Execute `lsnrctl start` e verifique o status com `lsnrctl status`.",
            ProblemDescription: "Listener do banco Oracle fica inoperante após reboot",
            RootCauseSummary: "Serviço não inicializado automaticamente",
            ValidationMethod: "Verificar porta 1521 aberta com netstat",
            RiskWarning: "Não executar em janela de pico",
            RollbackPlan: "Restaurar listener.ora anterior",
            Applicabilities: new List<CreateKnowledgeApplicabilityInput>
            {
                new CreateKnowledgeApplicabilityInput(ProductId: 1L, ApplicabilityType: "Applies")
            }
        ), userId: 1L);

        await knowledgeService.SubmitForReviewAsync(new SubmitForReviewCommand(articleId), userId: 1L);
        await knowledgeService.ApproveAndPublishAsync(new ApproveAndPublishCommand(articleId), userId: 1L);

        // 2. Primeira Ingestão
        var entry1 = await prepService.PrepareKnowledgeItemAsync(articleId, currentUserId: 1L);
        entry1.ValidationStatus.Should().Be("Validated");
        entry1.QualityStatus.Should().Be("Validated");
        entry1.ReadinessStatus.Should().Be("Ready");

        // 3. Segunda Ingestão com o mesmo conteúdo
        var entry2 = await prepService.PrepareKnowledgeItemAsync(articleId, currentUserId: 1L);

        // Assert: Idempotência garantida (§12.23)
        entry2.Id.Should().Be(entry1.Id);
        entry2.ContentHash.Should().Be(entry1.ContentHash);

        var (searchResult, total) = await contentRepo.SearchAsync(sourceType: "ValidatedKnowledge");
        searchResult.Count(s => s.SourceId == articleId).Should().Be(1);
    }

    [Fact]
    public async Task ContentPreparation_ReadinessMetrics_ReflectsRealCounts()
    {
        using var scope = _factory.Services.CreateScope();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var prepService = scope.ServiceProvider.GetRequiredService<IContentPreparationService>();

        // Caso aberto e incompleto (ainda não validado)
        var openedCase = await caseService.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Caso em aberto para teste de métricas",
            Severity: "Low"
        ), currentUserId: 1L);

        await prepService.PrepareCaseAsync(openedCase.Id, currentUserId: 1L);

        var metrics = await prepService.GetMetricsAsync();

        metrics.TotalCount.Should().BeGreaterThan(0);
        // Casos em aberto não validados caem em NotEligible ou NeedsMetadata
        (metrics.NotEligibleCount + metrics.NeedsMetadataCount + metrics.NeedsReviewCount + metrics.ReadyCount)
            .Should().Be(metrics.TotalCount);
    }
}
