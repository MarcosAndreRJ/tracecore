using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TraceCore.Application.DTOs;
using TraceCore.Application.Exceptions;
using TraceCore.Application.Services;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;
using Xunit;

namespace TraceCore.IntegrationTests;

/// <summary>
/// Fase 05 — Ajuste do Ecossistema: mecanismo único de teste de integração com contexto
/// (Diagnostic / SolutionValidation), vínculo estrutural IntegrationRun -> DiagnosticStep /
/// CaseEvidence, e regra de negócio que exige integração associada ao sistema do caso.
/// </summary>
public class IntegrationTestDuringInvestigationTests : IClassFixture<TraceCoreTestApplicationFactory>
{
    private readonly TraceCoreTestApplicationFactory _factory;

    public IntegrationTestDuringInvestigationTests(TraceCoreTestApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    private async Task<(long caseId, long integrationId)> SeedCaseWithLinkedIntegrationAsync(
        IServiceProvider services, ulong caseNumber, string integrationCode)
    {
        var catalogService = services.GetRequiredService<ICatalogService>();
        var integrationService = services.GetRequiredService<IIntegrationService>();
        var caseRepo = services.GetRequiredService<ICaseRepository>();

        var productId = await catalogService.CreateProductAsync($"Sistema Fase05 {integrationCode}", $"SYS-{integrationCode}", null, false);

        var integrationId = await integrationService.CreateIntegrationAsync(new CreateIntegrationCommand(
            Code: integrationCode,
            Name: $"Integração {integrationCode}",
            IntegrationType: "RestApi",
            TargetSystemDescription: null,
            OwnerDepartmentId: null,
            ContractNotes: null,
            CreatedBy: 1L,
            ProductId: productId));

        var @case = new Case
        {
            CaseNumber = caseNumber,
            ProductId = productId,
            OpenedAt = DateTime.UtcNow,
            Status = "Open"
        };
        var caseId = await caseRepo.AddAsync(@case);

        // Replica o comportamento real de abertura de caso (ICaseService.CreateCaseAsync
        // sempre abre a Iteração 1): sem isso, GetCurrentIterationAsync retorna null.
        await caseRepo.AddIterationAsync(new CaseIteration(caseId, 1, 1L, "Abertura inicial do caso"));

        return (caseId, integrationId);
    }

    [Fact]
    public async Task TestIntegrationDuringInvestigation_CreatesLinkedRunAndDiagnosticStep()
    {
        using var scope = _factory.Services.CreateScope();
        var (caseId, integrationId) = await SeedCaseWithLinkedIntegrationAsync(scope.ServiceProvider, 900001, "INT-F05-01");

        var investigationService = scope.ServiceProvider.GetRequiredService<ICaseInvestigationService>();
        var integrationRepo = scope.ServiceProvider.GetRequiredService<IIntegrationRepository>();

        var step = await investigationService.TestIntegrationDuringInvestigationAsync(
            caseId: caseId,
            integrationId: integrationId,
            currentUserId: 1L);

        // O passo diagnóstico deve ser um AutomatedCheck vinculado à IntegrationRun real.
        step.StepType.Should().Be("AutomatedCheck");
        step.IntegrationRunId.Should().NotBeNull();

        var runs = await integrationRepo.GetRunsByIntegrationIdAsync(integrationId);
        var run = runs.Should().ContainSingle().Subject;
        run.Id.Should().Be(step.IntegrationRunId!.Value);
        run.RunContext.Should().Be("Diagnostic");
        run.CaseId.Should().Be(caseId);

        // Sem URL de health-check configurada: falha segura (§26), não crash, não "Success" fantasma.
        run.Status.Should().Be("Failed");
        step.Outcome.Should().Be("DidNotWork");
    }

    [Fact]
    public async Task TestIntegrationDuringInvestigation_WithRecordAsEvidence_CreatesLinkedCaseEvidence()
    {
        using var scope = _factory.Services.CreateScope();
        var (caseId, integrationId) = await SeedCaseWithLinkedIntegrationAsync(scope.ServiceProvider, 900002, "INT-F05-02");

        var investigationService = scope.ServiceProvider.GetRequiredService<ICaseInvestigationService>();

        var step = await investigationService.TestIntegrationDuringInvestigationAsync(
            caseId: caseId,
            integrationId: integrationId,
            currentUserId: 1L,
            recordAsEvidence: true);

        var evidences = await investigationService.GetEvidencesByCaseIdAsync(caseId);
        var evidence = evidences.Should().ContainSingle().Subject;
        evidence.EvidenceType.Should().Be("DiagnosticTest");
        evidence.IntegrationRunId.Should().Be(step.IntegrationRunId);
        evidence.DiagnosticStepId.Should().Be(step.Id);
    }

    [Fact]
    public async Task TestIntegrationDuringInvestigation_IntegrationNotLinkedToCaseProduct_ThrowsBusinessRuleValidation()
    {
        using var scope = _factory.Services.CreateScope();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var integrationService = scope.ServiceProvider.GetRequiredService<IIntegrationService>();
        var caseRepo = scope.ServiceProvider.GetRequiredService<ICaseRepository>();
        var investigationService = scope.ServiceProvider.GetRequiredService<ICaseInvestigationService>();

        var caseProductId = await catalogService.CreateProductAsync("Sistema do Caso", "SYS-CASE-F05", null, false);
        var otherProductId = await catalogService.CreateProductAsync("Outro Sistema", "SYS-OTHER-F05", null, false);

        var integrationId = await integrationService.CreateIntegrationAsync(new CreateIntegrationCommand(
            Code: "INT-F05-03",
            Name: "Integração de Outro Sistema",
            IntegrationType: "RestApi",
            TargetSystemDescription: null,
            OwnerDepartmentId: null,
            ContractNotes: null,
            CreatedBy: 1L,
            ProductId: otherProductId));

        var @case = new Case
        {
            CaseNumber = 900003,
            ProductId = caseProductId,
            OpenedAt = DateTime.UtcNow,
            Status = "Open"
        };
        var caseId = await caseRepo.AddAsync(@case);

        var act = async () => await investigationService.TestIntegrationDuringInvestigationAsync(
            caseId: caseId,
            integrationId: integrationId,
            currentUserId: 1L);

        await act.Should().ThrowAsync<BusinessRuleValidationException>();
    }

    [Fact]
    public async Task TestIntegrationForSolutionValidation_CreatesEvidenceWithSolutionValidationContext()
    {
        using var scope = _factory.Services.CreateScope();
        var (caseId, integrationId) = await SeedCaseWithLinkedIntegrationAsync(scope.ServiceProvider, 900004, "INT-F05-04");

        var investigationService = scope.ServiceProvider.GetRequiredService<ICaseInvestigationService>();
        var integrationRepo = scope.ServiceProvider.GetRequiredService<IIntegrationRepository>();

        var evidence = await investigationService.TestIntegrationForSolutionValidationAsync(
            caseId: caseId,
            integrationId: integrationId,
            currentUserId: 1L);

        evidence.EvidenceType.Should().Be("DiagnosticTest");
        evidence.IntegrationRunId.Should().NotBeNull();

        var runs = await integrationRepo.GetRunsByIntegrationIdAsync(integrationId);
        var run = runs.Should().ContainSingle().Subject;
        run.Id.Should().Be(evidence.IntegrationRunId!.Value);
        run.RunContext.Should().Be("SolutionValidation");
        run.CaseId.Should().Be(caseId);
    }

    [Fact]
    public async Task TestIntegrationDuringInvestigation_FirstStepOnFreshCase_CreatesSessionWithCurrentIterationId()
    {
        // Regressão: EnsureOpenSessionAsync criava a DiagnosticSession sem popular
        // CaseIterationId (coluna NOT NULL em produção), quebrando o 1º teste/passo
        // diagnóstico de qualquer caso recém-aberto (sem sessão de diagnóstico ainda).
        using var scope = _factory.Services.CreateScope();
        var (caseId, integrationId) = await SeedCaseWithLinkedIntegrationAsync(scope.ServiceProvider, 900006, "INT-F05-06");

        var investigationService = scope.ServiceProvider.GetRequiredService<ICaseInvestigationService>();
        var diagnosticRepo = scope.ServiceProvider.GetRequiredService<IDiagnosticRepository>();
        var caseRepo = scope.ServiceProvider.GetRequiredService<ICaseRepository>();

        await investigationService.TestIntegrationDuringInvestigationAsync(
            caseId: caseId,
            integrationId: integrationId,
            currentUserId: 1L);

        var currentIteration = await caseRepo.GetCurrentIterationAsync(caseId);
        var session = await diagnosticRepo.GetOpenSessionByCaseIdAsync(caseId);

        session.Should().NotBeNull();
        session!.CaseIterationId.Should().BeGreaterThan(0);
        session.CaseIterationId.Should().Be(currentIteration!.Id);
    }

    [Fact]
    public async Task TestIntegrationDuringInvestigation_IsAuditedAsIntegrationRunRegister()
    {
        using var scope = _factory.Services.CreateScope();
        var (caseId, integrationId) = await SeedCaseWithLinkedIntegrationAsync(scope.ServiceProvider, 900005, "INT-F05-05");

        var investigationService = scope.ServiceProvider.GetRequiredService<ICaseInvestigationService>();
        var auditRepo = scope.ServiceProvider.GetRequiredService<IAuditEventRepository>();

        await investigationService.TestIntegrationDuringInvestigationAsync(
            caseId: caseId,
            integrationId: integrationId,
            currentUserId: 5L);

        var audit = await auditRepo.GetRecentAsync(50);
        var evt = audit.FirstOrDefault(e => e.Action == "integration.run_register" && e.EntityId == integrationId.ToString());
        evt.Should().NotBeNull();
        evt!.ActorUserId.Should().Be(5L);
        evt.AfterJson.Should().Contain("\"runContext\":\"Diagnostic\"");
        evt.AfterJson.Should().Contain($"\"caseId\":{caseId}");
    }
}
