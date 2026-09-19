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
using TraceCore.Domain.Services;
using Xunit;

namespace TraceCore.IntegrationTests;

public class IntegrationAndAnalyticsIntelligenceTests : IClassFixture<TraceCoreTestApplicationFactory>
{
    private readonly TraceCoreTestApplicationFactory _factory;

    public IntegrationAndAnalyticsIntelligenceTests(TraceCoreTestApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    [Fact]
    public async Task HealthCheck_WhenHostUnreachable_RecordsFailedStatusNeverSuccess_SafeFailure()
    {
        // Princípio §26: Falha segura — erro de rede/timeout/porta fechada NUNCA pode fingir sucesso.
        using var scope = _factory.Services.CreateScope();
        var integrationService = scope.ServiceProvider.GetRequiredService<IIntegrationService>();
        var healthCheckService = scope.ServiceProvider.GetRequiredService<IIntegrationHealthCheckService>();

        // 1. Cria integração apontando para endpoint local inacessível
        var id = await integrationService.CreateIntegrationAsync(new CreateIntegrationCommand(
            Code: "INT-FAIL-SAFE",
            Name: "Integração Teste Falha Segura",
            IntegrationType: "Monitoring",
            TargetSystemDescription: "Serviço que não existe para testar falha segura",
            OwnerDepartmentId: null,
            ContractNotes: null,
            CreatedBy: 1L,
            HealthCheckUrl: "http://127.0.0.1:54321/health",
            HealthCheckMethod: "GET",
            HealthCheckTimeoutSeconds: 1,
            HealthCheckExpectedStatusCode: 200
        ));

        // 2. Executa o health-check
        var run = await healthCheckService.ExecuteHealthCheckAsync(id);

        // 3. Assert: Falha segura estrita (§26)
        run.Should().NotBeNull();
        run.Status.Should().Be("Failed", "Conexão recusada/inacessível NUNCA pode ser gravada como Success");
        run.TriggeredBy.Should().Be("Automated");
        run.ErrorMessage.Should().NotBeNullOrWhiteSpace();

        // 4. Verifica histórico gravado no repositório
        var integration = await integrationService.GetIntegrationByIdAsync(id);
        integration!.Runs.Should().ContainSingle();
        integration.Runs[0].Status.Should().Be("Failed");
        integration.Runs[0].TriggeredBy.Should().Be("Automated");
    }

    [Fact]
    public async Task DiagnosticEngine_WhenAutomatedCheckHasIntegration_ExecutesAutomatically()
    {
        // BR-073: Check do tipo AutomatedCheck vinculado a uma integração executa automaticamente sem intervenção humana.
        using var scope = _factory.Services.CreateScope();
        var engineService = scope.ServiceProvider.GetRequiredService<IDiagnosticEngineService>();
        var integrationService = scope.ServiceProvider.GetRequiredService<IIntegrationService>();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var diagRepo = scope.ServiceProvider.GetRequiredService<IDiagnosticRepository>();

        // 1. Cria uma integração com endpoint inacessível (para executar de verdade e registrar o teste)
        var integrationId = await integrationService.CreateIntegrationAsync(new CreateIntegrationCommand(
            Code: "INT-AUTO-DIAG",
            Name: "Integração Diagnóstico Automático",
            IntegrationType: "Monitoring",
            TargetSystemDescription: null,
            OwnerDepartmentId: null,
            ContractNotes: null,
            CreatedBy: 1L,
            HealthCheckUrl: "http://127.0.0.1:54322/health",
            HealthCheckMethod: "GET",
            HealthCheckTimeoutSeconds: 1,
            HealthCheckExpectedStatusCode: 200
        ));

        // 2. Cria fluxo com verificação automática vinculada a essa integração
        var flowId = await engineService.CreateFlowAsync(new CreateDiagnosticFlowCommand(
            Code: "FLOW-AUTO-01",
            Name: "Fluxo com Checagem Automática",
            EntryKeywords: "banco,conexao,oracle"
        ), 1L);

        var hypId = await engineService.AddFlowHypothesisAsync(flowId, "Instabilidade no Gateway de Integração", "Suspeita de indisponibilidade", null);

        var checkId = await engineService.AddCheckAsync(new CreateDiagnosticCheckCommand(
            FlowId: flowId,
            Code: "CHK-AUTO-GW",
            Title: "Health-Check do Gateway",
            QuestionText: "Verificando saúde do gateway de integração...",
            CheckType: "AutomatedCheck",
            Cost: 1,
            RiskLevel: "Low",
            IntegrationId: integrationId
        ));

        await engineService.AddCheckOptionWithImpactsAsync(checkId, "Falha na conexão", 1, new List<(long HypothesisId, string ImpactType, decimal Weight)>
        {
            (hypId, "Favors", 2.0m)
        });

        // 3. Cria caso com palavras-chave correspondentes
        var openedCase = await caseService.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Erro de conexao com banco e gateway oracle",
            Severity: "High"
        ), 1L);

        await engineService.StartFlowAsync(openedCase.Id, flowId, 1L);

        // 4. Act: Solicita o estado do motor de diagnóstico
        // O motor deve detectar que o próximo check é AutomatedCheck com IntegrationId configurado,
        // executar o health-check automaticamente (BR-073) e avançar o estado!
        var state = await engineService.GetEngineStateAsync(openedCase.Id);

        // 5. Assert: o passo foi executado e gravado como AutomatedCheck
        state.Should().NotBeNull();
        var steps = await diagRepo.GetStepsByCaseIdAsync(openedCase.Id);
        steps.Should().Contain(s => s.StepType == "AutomatedCheck");
    }

    [Fact]
    public async Task DiagnosticEngine_WhenAutomatedCheckHasNoIntegration_FallsBackGracefully()
    {
        // Se o check for AutomatedCheck mas não tiver IntegrationId associado, não pode quebrar a investigação.
        using var scope = _factory.Services.CreateScope();
        var engineService = scope.ServiceProvider.GetRequiredService<IDiagnosticEngineService>();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();

        var flowId = await engineService.CreateFlowAsync(new CreateDiagnosticFlowCommand(
            Code: "FLOW-FALLBACK-01",
            Name: "Fluxo Fallback Manual",
            EntryKeywords: "fallback,teste"
        ), 1L);

        var hypId = await engineService.AddFlowHypothesisAsync(flowId, "Hipótese A", "Descrição", null);

        var checkId = await engineService.AddCheckAsync(new CreateDiagnosticCheckCommand(
            FlowId: flowId,
            Code: "CHK-NO-INT",
            Title: "Checagem Sem Integração",
            QuestionText: "Por favor informe manualmente o resultado:",
            CheckType: "AutomatedCheck",
            Cost: 1,
            RiskLevel: "Low",
            IntegrationId: null // Sem integração vinculada
        ));

        await engineService.AddCheckOptionWithImpactsAsync(checkId, "Opção 1", 1, new List<(long HypothesisId, string ImpactType, decimal Weight)>
        {
            (hypId, "Favors", 1.0m)
        });

        var openedCase = await caseService.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Caso para teste fallback",
            Severity: "Medium"
        ), 1L);

        await engineService.StartFlowAsync(openedCase.Id, flowId, 1L);

        // Act & Assert: não pode lançar exceção, deve cair para pergunta manual
        var state = await engineService.GetEngineStateAsync(openedCase.Id);
        state.Should().NotBeNull();
        state.CurrentRecommendation.Should().NotBeNull();
        state.CurrentRecommendation!.CheckCode.Should().Be("CHK-NO-INT");
    }

    [Fact]
    public async Task AnalyticsIntelligence_GetTrendAfterVersion_ComputesDeterministically()
    {
        // Fase 16 (§31): Métricas antes e depois da publicação da versão de ajuste determinísticas
        using var scope = _factory.Services.CreateScope();
        var analyticsService = scope.ServiceProvider.GetRequiredService<IManagementAnalyticsService>();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var caseRepo = scope.ServiceProvider.GetRequiredService<ICaseRepository>();

        // 1. Cadastra produto e versão com ReleaseDate definida
        var prod = new Product("Produto Tendência", "PRD-TREND");
        long prodId = await catalogRepo.AddProductAsync(prod);

        var releaseDate = DateTime.UtcNow.AddDays(-30);
        var version = new ProductVersion(prodId, "v3.0.0")
        {
            ReleasedAt = releaseDate,
            Status = "Released"
        };
        long versionId = await catalogRepo.AddProductVersionAsync(version);

        // 2. Insere 3 casos ANTES da versão (opened_at = releaseDate - 10 dias)
        for (int i = 1; i <= 3; i++)
        {
            var c = new Case
            {
                CaseNumber = (ulong)(1000 + i),
                ProductId = prodId,
                OpenedAt = releaseDate.AddDays(-10 + i),
                Status = "Resolved"
            };
            long cid = await caseRepo.AddAsync(c);
            var iter = new CaseIteration(cid, 1, 1L, "Diagnóstico", c.OpenedAt, "Resolved")
            {
                ClosedAt = c.OpenedAt.AddMinutes(40)
            };
            await caseRepo.AddIterationAsync(iter);
        }

        // 3. Insere 3 casos DEPOIS da versão (opened_at = releaseDate + 10 dias)
        for (int i = 1; i <= 3; i++)
        {
            var c = new Case
            {
                CaseNumber = (ulong)(2000 + i),
                ProductId = prodId,
                ProductVersionId = versionId,
                OpenedAt = releaseDate.AddDays(i),
                Status = "Resolved"
            };
            long cid = await caseRepo.AddAsync(c);
            var iter = new CaseIteration(cid, 1, 1L, "Diagnóstico", c.OpenedAt, "Resolved")
            {
                ClosedAt = c.OpenedAt.AddMinutes(20)
            };
            await caseRepo.AddIterationAsync(iter);
        }

        // 4. Act: Consulta a tendência determinística
        var trend = await analyticsService.GetTrendAfterVersionAsync(versionId);

        // 5. Assert:
        trend.Should().NotBeNull();
        trend.VersionLabel.Should().Be("v3.0.0");
        trend.BeforeCount.Should().Be(3);
        trend.AfterCount.Should().Be(3);
        trend.TotalSample.Should().Be(6);
        trend.PercentageChange.Should().Be(0.0);
        trend.HasSufficientData.Should().BeTrue();
    }

    [Fact]
    public async Task AnalyticsIntelligence_GetSolutionEffectiveness_ReturnsInsufficientDataWhenSampleTooSmall()
    {
        // Princípio §31: Quando N < 3 (ou amostra muito pequena), o sistema declara dados insuficientes sem inventar.
        using var scope = _factory.Services.CreateScope();
        var analyticsService = scope.ServiceProvider.GetRequiredService<IManagementAnalyticsService>();
        var knowledgeRepo = scope.ServiceProvider.GetRequiredService<IKnowledgeRepository>();

        // 1. Cadastra artigo de conhecimento
        var ki = new KnowledgeItem(
            knowledgeCode: "SOL-AMOST-01",
            title: "Solução com Amostra Insuficiente",
            summary: "Artigo novo com pouco uso",
            knowledgeType: "Solution",
            provenanceType: "Case",
            createdBy: 1L
        );
        long kiId = await knowledgeRepo.CreateItemAsync(ki);

        // 2. Act: Consulta efetividade da solução
        var result = await analyticsService.GetSolutionEffectivenessComparisonAsync(kiId, new AnalyticsFilterDto());

        // 3. Assert:
        result.Should().NotBeNull();
        result.HasSufficientData.Should().BeFalse("Nenhum caso usou esta solução ainda (N=0)");
        result.SampleWithCount.Should().Be(0);
        result.Observation.Should().Contain("insuficientes");
    }

    [Fact]
    public async Task AnalyticsIntelligence_GetComponentAssociationPercentage_CalculatesExactDistribution()
    {
        // Fase 16 (§31): Distribuição de componentes calculada deterministicamente sobre casos filtrados
        using var scope = _factory.Services.CreateScope();
        var analyticsService = scope.ServiceProvider.GetRequiredService<IManagementAnalyticsService>();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();

        // 1. Cadastra componente no catálogo
        var comp = new ComponentEntity { Code = "CMP-AUTH", Name = "Módulo Autenticação", ComponentType = "Module", Status = "Active" };
        long compId = await catalogRepo.AddComponentAsync(comp);

        // 2. Insere 5 casos vinculados ao componente via OpenCaseAsync (atinge o limiar de suficiência N>=5)
        for (int i = 1; i <= 5; i++)
        {
            await caseService.OpenCaseAsync(new OpenCaseCommand(
                OriginalReport: $"Falha de autenticação caso {i}",
                Severity: "High",
                ComponentIds: new List<long> { compId }
            ), 1L);
        }

        // 3. Act: Consulta associação
        var assoc = await analyticsService.GetComponentAssociationPercentageAsync(new AnalyticsFilterDto { Period = "all" });

        // 4. Assert:
        assoc.Should().NotBeNull();
        assoc.TotalCases.Should().Be(5);
        assoc.Components.Should().ContainSingle(c => c.ComponentId == compId);
        assoc.Components[0].Percentage.Should().Be(100.0);
        assoc.HasSufficientData.Should().BeTrue();
    }

    [Fact]
    public async Task AiToolDefinitions_AnalyzeManagementTrend_IsStrictlyReadOnly()
    {
        // Ferramenta analítica é estritamente de leitura (não altera dados, não requer confirmação de escrita)
        var readingTool = AiToolDefinitions.ReadingTools.FirstOrDefault(t => t.Name == "AnalyzeManagementTrend");
        readingTool.Should().NotBeNull("AnalyzeManagementTrend deve estar cadastrado em ReadingTools");
        readingTool!.ParametersJsonSchema.Should().Contain("metricType");

        var writingTool = AiToolDefinitions.WritingTools.FirstOrDefault(t => t.Name == "AnalyzeManagementTrend");
        writingTool.Should().BeNull("AnalyzeManagementTrend NÃO pode ser ferramenta de escrita (é somente leitura)");
    }
}
