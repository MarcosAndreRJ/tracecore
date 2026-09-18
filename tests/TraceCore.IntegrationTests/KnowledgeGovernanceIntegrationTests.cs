using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TraceCore.Application.DTOs;
using TraceCore.Application.Services;
using TraceCore.Domain.Repositories;
using Xunit;

namespace TraceCore.IntegrationTests;

public class KnowledgeGovernanceIntegrationTests : IClassFixture<TraceCoreTestApplicationFactory>
{
    private readonly TraceCoreTestApplicationFactory _factory;

    public KnowledgeGovernanceIntegrationTests(TraceCoreTestApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    [Fact]
    public async Task PublishV1_ThenCreateV2_MarksV1AsSuperseded_AndPreservesReadability()
    {
        using var scope = _factory.Services.CreateScope();
        var knowledgeService = scope.ServiceProvider.GetRequiredService<IKnowledgeService>();
        var knowledgeRepo = scope.ServiceProvider.GetRequiredService<IKnowledgeRepository>();

        // 1. Cria e publica v1
        var itemId = await knowledgeService.CreateKnowledgeDraftAsync(new CreateKnowledgeDraftCommand(
            Title: "Procedimento de Otimização de Pool de Conexões",
            Summary: "Configurações recomendadas para evitar esgotamento de conexões no backend",
            ValidationMethod: "Verificar métricas no Grafana: conexões ativas abaixo de 80%",
            ContentMarkdown: "### Procedimento v1\nAjustar MaxPoolSize=100",
            Applicabilities: new List<CreateKnowledgeApplicabilityInput>
            {
                new CreateKnowledgeApplicabilityInput(ProductId: 1)
            }
        ), userId: 1L);

        await knowledgeService.ApproveAndPublishAsync(new ApproveAndPublishCommand(
            KnowledgeItemId: itemId
        ), userId: 1L);

        var v1Before = await knowledgeRepo.GetVersionByItemAndNumberAsync(itemId, 1);
        v1Before.Should().NotBeNull();
        v1Before!.Status.Should().Be("Approved");

        // 2. Cria v2 a partir de modificação
        var newVerNo = await knowledgeService.CreateNewVersionAsync(new CreateNewVersionCommand(
            KnowledgeItemId: itemId,
            ContentMarkdown: "### Procedimento v2\nAjustar MaxPoolSize=200 e ConnectionTimeout=30",
            ValidationMethod: "Monitorar telemetria de connection pool em tempo real",
            ChangeSummary: "Aumento de capacidade de pool para absorver picos sazonais"
        ), userId: 1L);

        newVerNo.Should().Be(2);

        // 3. Assert: v1 agora está marcada como Superseded, mas permanece 100% legível
        var v1After = await knowledgeRepo.GetVersionByItemAndNumberAsync(itemId, 1);
        v1After.Should().NotBeNull();
        v1After!.Status.Should().Be("Superseded");
        v1After.ContentMarkdown.Should().Be("### Procedimento v1\nAjustar MaxPoolSize=100");
        v1After.ValidationMethod.Should().Be("Verificar métricas no Grafana: conexões ativas abaixo de 80%");

        // E a v2 foi criada como Draft
        var v2 = await knowledgeRepo.GetVersionByItemAndNumberAsync(itemId, 2);
        v2.Should().NotBeNull();
        v2!.Status.Should().Be("Draft");
        v2.ChangeSummary.Should().Be("Aumento de capacidade de pool para absorver picos sazonais");

        // Verifica que o detalhe consolidado continua legível com histórico de revisões
        var detail = await knowledgeService.GetKnowledgeDetailAsync(itemId);
        detail.Should().NotBeNull();
        detail!.Revisions.Should().HaveCount(2);
        detail.Revisions.Should().Contain(r => r.Version == "v1.0");
        detail.Revisions.Should().Contain(r => r.Version == "v2.0");
    }

    [Fact]
    public async Task ArchiveKnowledge_ChangesStatusToArchived_AndAuditsEvent()
    {
        using var scope = _factory.Services.CreateScope();
        var knowledgeService = scope.ServiceProvider.GetRequiredService<IKnowledgeService>();
        var knowledgeRepo = scope.ServiceProvider.GetRequiredService<IKnowledgeRepository>();
        var auditRepo = scope.ServiceProvider.GetRequiredService<IAuditEventRepository>();

        var itemId = await knowledgeService.CreateKnowledgeDraftAsync(new CreateKnowledgeDraftCommand(
            Title: "Procedimento Legado de Migração de Dados",
            Summary: "Rotina executada em sistemas que foram desativados",
            ValidationMethod: "Select count(*) nas tabelas de staging",
            Applicabilities: new List<CreateKnowledgeApplicabilityInput>
            {
                new CreateKnowledgeApplicabilityInput(ProductId: 1)
            }
        ), userId: 1L);

        await knowledgeService.ApproveAndPublishAsync(new ApproveAndPublishCommand(
            KnowledgeItemId: itemId
        ), userId: 1L);

        // Act: Arquiva formalmente o conhecimento
        await knowledgeService.ArchiveKnowledgeAsync(new ArchiveKnowledgeCommand(
            KnowledgeItemId: itemId,
            Reason: "Sistemas legados desativados na migração para cloud"
        ), userId: 1L);

        // Assert
        var item = await knowledgeRepo.GetByIdAsync(itemId);
        item.Should().NotBeNull();
        item!.Status.Should().Be("Archived");

        var auditEvents = await auditRepo.GetByEntityAsync("KnowledgeItem", itemId.ToString());
        auditEvents.Should().Contain(a => a.Action == "KnowledgeArchived");
    }
}
