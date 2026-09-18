using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TraceCore.Application.DTOs;
using TraceCore.Application.Exceptions;
using TraceCore.Application.Services;
using TraceCore.Domain.Enums;
using TraceCore.Domain.Repositories;
using Xunit;
using IAppAuthService = TraceCore.Application.Services.IAuthenticationService;

namespace TraceCore.IntegrationTests;

public class CaseInvestigationIntegrationTests : IClassFixture<TraceCoreTestApplicationFactory>
{
    private readonly TraceCoreTestApplicationFactory _factory;

    public CaseInvestigationIntegrationTests(TraceCoreTestApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    /// <summary>
    /// Critério de Aceite Principal (Seção 5 / Bloco 4.1):
    /// Reproduz literalmente o exemplo do Bloco 4.1:
    /// 1. registrar a hipótese "Servidor indisponível" num caso existente;
    /// 2. registrar o teste "Ping no servidor" ligado a essa hipótese, com resultado "Servidor responde normalmente" e outcome Worked;
    /// 3. avaliar a hipótese como Discarded com justificativa referenciando o teste;
    /// 4. registrar uma segunda hipótese "API de autenticação indisponível" no mesmo caso (confirmando BR-023: múltiplas hipóteses simultâneas);
    /// 5. registrar o teste "Health endpoint" com resultado "HTTP 500" e avaliar essa hipótese como Supported;
    /// 6. consultar a timeline do caso e ver as duas hipóteses, cada uma com seu(s) teste(s) e status final corretos, na ordem em que ocorreram.
    /// </summary>
    [Fact]
    public async Task AcceptanceCriterion_FullInvestigationFlow_Bloco41_ReplicatesHypothesisTestResultConclusion()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var investigationService = scope.ServiceProvider.GetRequiredService<ICaseInvestigationService>();
        var auditRepo = scope.ServiceProvider.GetRequiredService<IAuditEventRepository>();

        // Abre caso base da Fase 3
        var openedCase = await caseService.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Usuários relatam instabilidade no login e mensagens de timeout."
        ), currentUserId: 1);

        openedCase.Should().NotBeNull();
        var caseId = openedCase.Id;

        // -------------------------------------------------------------
        // Passo 1: Registrar a hipótese "Servidor indisponível"
        // -------------------------------------------------------------
        var hyp1 = await investigationService.RegisterHypothesisAsync(new RegisterHypothesisCommand(
            CaseId: caseId,
            Title: "Servidor indisponível",
            Description: "Suspeita de queda de infraestrutura ou hardware do host"
        ), currentUserId: 1);

        hyp1.Should().NotBeNull();
        hyp1.Id.Should().BeGreaterThan(0);
        hyp1.Title.Should().Be("Servidor indisponível");
        hyp1.Status.Should().Be("Proposed", "o status inicial de qualquer hipótese deve ser Proposed");

        // -------------------------------------------------------------
        // Passo 2: Registrar o teste "Ping no servidor" ligado a essa hipótese,
        // com resultado "Servidor responde normalmente" e outcome Worked
        // -------------------------------------------------------------
        var step1 = await investigationService.RegisterDiagnosticStepAsync(new RegisterDiagnosticStepCommand(
            CaseId: caseId,
            HypothesisId: hyp1.Id,
            Title: "Ping no servidor",
            Objective: "Verificar conectividade ICMP e resposta da máquina física",
            Instruction: "ping -c 4 192.168.1.50",
            InputEvidenceSummary: "Relato de instabilidade e timeout no sistema",
            ResultSummary: "Servidor responde normalmente",
            Outcome: "Worked",
            RiskLevel: "Low",
            DurationSeconds: 4
        ), currentUserId: 1);

        step1.Should().NotBeNull();
        step1.Id.Should().BeGreaterThan(0);
        step1.HypothesisId.Should().Be(hyp1.Id);
        step1.Title.Should().Be("Ping no servidor");
        step1.ResultSummary.Should().Be("Servidor responde normalmente");
        step1.Outcome.Should().Be("Worked");
        step1.SequenceNo.Should().Be(1);

        // -------------------------------------------------------------
        // Passo 3: Avaliar a hipótese como Discarded com justificativa referenciando o teste
        // -------------------------------------------------------------
        var evalHyp1 = await investigationService.EvaluateHypothesisAsync(new EvaluateHypothesisCommand(
            HypothesisId: hyp1.Id,
            NewStatus: "Discarded",
            Justification: "Servidor respondeu normalmente ao ping de rede sem perda de pacotes.",
            EvidenceStepId: step1.Id
        ), currentUserId: 1);

        evalHyp1.Status.Should().Be("Discarded");
        evalHyp1.Justification.Should().Be("Servidor respondeu normalmente ao ping de rede sem perda de pacotes.");

        // -------------------------------------------------------------
        // Passo 4: Registrar uma segunda hipótese "API de autenticação indisponível" no mesmo caso
        // (confirmando BR-023: múltiplas hipóteses simultâneas convivendo)
        // -------------------------------------------------------------
        var hyp2 = await investigationService.RegisterHypothesisAsync(new RegisterHypothesisCommand(
            CaseId: caseId,
            Title: "API de autenticação indisponível",
            Description: "Suspeita de falha no microsserviço de autenticação ou dependência interna"
        ), currentUserId: 1);

        hyp2.Should().NotBeNull();
        hyp2.Id.Should().BeGreaterThan(hyp1.Id);
        hyp2.Title.Should().Be("API de autenticação indisponível");
        hyp2.Status.Should().Be("Proposed");

        // Confirma BR-023: ambas hipóteses coexistem no caso
        var currentHypotheses = await investigationService.GetHypothesesByCaseIdAsync(caseId);
        currentHypotheses.Should().HaveCount(2, "BR-023: o caso deve comportar múltiplas hipóteses simultaneamente");
        currentHypotheses.Select(h => h.Title).Should().Contain(new[] { "Servidor indisponível", "API de autenticação indisponível" });

        // -------------------------------------------------------------
        // Passo 5: Registrar o teste "Health endpoint" com resultado "HTTP 500"
        // e avaliar essa hipótese como Supported
        // -------------------------------------------------------------
        var step2 = await investigationService.RegisterDiagnosticStepAsync(new RegisterDiagnosticStepCommand(
            CaseId: caseId,
            HypothesisId: hyp2.Id,
            Title: "Health endpoint",
            Objective: "Consultar endpoint de verificação /health do serviço de autenticação",
            Instruction: "curl -i http://auth.service.internal/health",
            InputEvidenceSummary: "Erros de login persistentes",
            ResultSummary: "HTTP 500",
            Outcome: "Worked",
            RiskLevel: "Low",
            DurationSeconds: 2
        ), currentUserId: 1);

        step2.Should().NotBeNull();
        step2.HypothesisId.Should().Be(hyp2.Id);
        step2.Title.Should().Be("Health endpoint");
        step2.ResultSummary.Should().Be("HTTP 500");
        step2.Outcome.Should().Be("Worked");
        step2.SequenceNo.Should().Be(2);

        var evalHyp2 = await investigationService.EvaluateHypothesisAsync(new EvaluateHypothesisCommand(
            HypothesisId: hyp2.Id,
            NewStatus: "Supported",
            Justification: "Health endpoint retornou HTTP 500 confirmando falha interna no serviço de autenticação.",
            EvidenceStepId: step2.Id
        ), currentUserId: 1);

        evalHyp2.Status.Should().Be("Supported", "evidência aponta para ela como possível origem identificada");
        evalHyp2.Justification.Should().Contain("HTTP 500");

        // -------------------------------------------------------------
        // Passo 6: Consultar a timeline do caso e ver as duas hipóteses,
        // cada uma com seu(s) teste(s) e status final corretos, na ordem em que ocorreram
        // -------------------------------------------------------------
        var timeline = await investigationService.GetInvestigationTimelineAsync(caseId);

        timeline.Should().NotBeNull();
        timeline.CaseId.Should().Be(caseId);
        timeline.ActiveSession.Should().NotBeNull();
        timeline.ActiveSession!.Status.Should().Be("Open");

        // Hipóteses
        timeline.Hypotheses.Should().HaveCount(2);

        var timelineHyp1 = timeline.Hypotheses.First(h => h.Id == hyp1.Id);
        timelineHyp1.Title.Should().Be("Servidor indisponível");
        timelineHyp1.Status.Should().Be("Discarded");
        timelineHyp1.Steps.Should().ContainSingle(s => s.Title == "Ping no servidor" && s.ResultSummary == "Servidor responde normalmente" && s.Outcome == "Worked");

        var timelineHyp2 = timeline.Hypotheses.First(h => h.Id == hyp2.Id);
        timelineHyp2.Title.Should().Be("API de autenticação indisponível");
        timelineHyp2.Status.Should().Be("Supported");
        timelineHyp2.Steps.Should().ContainSingle(s => s.Title == "Health endpoint" && s.ResultSummary == "HTTP 500" && s.Outcome == "Worked");

        // Passos globais
        timeline.Steps.Should().HaveCount(2);
        timeline.Steps[0].SequenceNo.Should().Be(1);
        timeline.Steps[1].SequenceNo.Should().Be(2);

        // Timeline cronológica
        timeline.Timeline.Should().HaveCount(4, "2 hipóteses + 2 passos/testes");
        timeline.Timeline.Select(t => t.Title).Should().ContainInOrder(
            "Hipótese: Servidor indisponível",
            "Teste #1: Ping no servidor",
            "Hipótese: API de autenticação indisponível",
            "Teste #2: Health endpoint"
        );

        // -------------------------------------------------------------
        // BR-100: Auditoria imutável dos eventos de investigação
        // -------------------------------------------------------------
        var auditHyp1 = await auditRepo.GetByEntityAsync("CaseHypothesis", hyp1.Id.ToString());
        auditHyp1.Should().Contain(a => a.Action == "HypothesisRegistered");
        auditHyp1.Should().Contain(a => a.Action == "HypothesisEvaluated");

        var auditStep1 = await auditRepo.GetByEntityAsync("DiagnosticStep", step1.Id.ToString());
        auditStep1.Should().ContainSingle(a => a.Action == "DiagnosticStepRegistered");

        var auditHyp2 = await auditRepo.GetByEntityAsync("CaseHypothesis", hyp2.Id.ToString());
        auditHyp2.Should().Contain(a => a.Action == "HypothesisRegistered");
        auditHyp2.Should().Contain(a => a.Action == "HypothesisEvaluated");
    }

    /// <summary>
    /// BR-024: Não permitir voltar de Discarded/Supported para Proposed apagando a decisão histórica.
    /// </summary>
    [Fact]
    public async Task BusinessRule24_CannotRevertHypothesisFromDiscardedOrSupportedToProposed()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var investigationService = scope.ServiceProvider.GetRequiredService<ICaseInvestigationService>();

        var @case = await caseService.OpenCaseAsync(new OpenCaseCommand(OriginalReport: "Caso para teste BR-024"));
        var hyp = await investigationService.RegisterHypothesisAsync(new RegisterHypothesisCommand(
            CaseId: @case.Id,
            Title: "Hipótese a ser descartada"
        ), currentUserId: 1);

        // Avalia como Discarded
        await investigationService.EvaluateHypothesisAsync(new EvaluateHypothesisCommand(
            HypothesisId: hyp.Id,
            NewStatus: "Discarded",
            Justification: "Evidência confirmou descarte"
        ), currentUserId: 1);

        // Act & Assert: Tentar voltar para Proposed deve lançar BusinessRuleValidationException
        var act = async () => await investigationService.EvaluateHypothesisAsync(new EvaluateHypothesisCommand(
            HypothesisId: hyp.Id,
            NewStatus: "Proposed",
            Justification: "Tentando voltar para estado inicial"
        ), currentUserId: 1);

        await act.Should().ThrowAsync<BusinessRuleValidationException>()
            .WithMessage("*Proposed*");
    }

    /// <summary>
    /// BR-025: Toda tentativa de diagnóstico relevante deve registrar: ação, autor, data/hora, objetivo,
    /// evidência anterior, resultado e classificação do resultado. A validação deve ocorrer na fronteira da aplicação.
    /// </summary>
    [Theory]
    [InlineData("", "Objetivo válido", "Evidência", "Resultado", "Worked", "ação/título")]
    [InlineData("Título válido", "", "Evidência", "Resultado", "Worked", "objetivo")]
    [InlineData("Título válido", "Objetivo válido", "", "Resultado", "Worked", "evidência anterior")]
    [InlineData("Título válido", "Objetivo válido", "Evidência", "", "Worked", "resultado observado")]
    public async Task BusinessRule25_DiagnosticStepRequiresAllMandatoryFieldsBeforePersisting(
        string title, string objective, string evidence, string result, string outcome, string fieldName)
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var investigationService = scope.ServiceProvider.GetRequiredService<ICaseInvestigationService>();

        var @case = await caseService.OpenCaseAsync(new OpenCaseCommand(OriginalReport: "Caso para teste BR-025"));

        var command = new RegisterDiagnosticStepCommand(
            CaseId: @case.Id,
            HypothesisId: null,
            Title: title,
            Objective: objective,
            Instruction: null,
            InputEvidenceSummary: evidence,
            ResultSummary: result,
            Outcome: outcome
        );

        // Act & Assert
        var act = async () => await investigationService.RegisterDiagnosticStepAsync(command, currentUserId: 1);

        await act.Should().ThrowAsync<BusinessRuleValidationException>(
            $"o campo '{fieldName}' é obrigatório por BR-025");
    }

    /// <summary>
    /// BR-026: Vocabulário fixo de classificação do resultado da ação em si:
    /// Worked, PartiallyWorked, DidNotWork, NotApplicable, Inconclusive.
    /// Outros valores devem ser rejeitados.
    /// </summary>
    [Fact]
    public async Task BusinessRule26_OutcomeVocabulary_IsStrictlyEnforced()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var investigationService = scope.ServiceProvider.GetRequiredService<ICaseInvestigationService>();

        var @case = await caseService.OpenCaseAsync(new OpenCaseCommand(OriginalReport: "Caso para teste BR-026"));

        var command = new RegisterDiagnosticStepCommand(
            CaseId: @case.Id,
            HypothesisId: null,
            Title: "Ação de teste",
            Objective: "Objetivo válido",
            Instruction: null,
            InputEvidenceSummary: "Evidência válida",
            ResultSummary: "Resultado válido",
            Outcome: "Confirmed" // Inválido! Confirmed não é outcome de diagnostic_steps
        );

        // Act & Assert
        var act = async () => await investigationService.RegisterDiagnosticStepAsync(command, currentUserId: 1);

        await act.Should().ThrowAsync<BusinessRuleValidationException>()
            .WithMessage("*Classificação do resultado*inválida*");
    }

    /// <summary>
    /// Validação de Autorização no Servidor (DEV-AI-005 / BR-102):
    /// Um usuário autenticado SEM a permissão 'caso.diagnosticar' (como Admin de Segurança)
    /// é bloqueado com 403 Forbidden ao tentar registrar hipóteses ou passos de diagnóstico via API.
    /// </summary>
    [Fact]
    public async Task ServerAuthorization_UserWithoutCasoDiagnosticarPermission_IsForbidden()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var roleRepo = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
        var deptRepo = scope.ServiceProvider.GetRequiredService<IDepartmentRepository>();

        var @case = await caseService.OpenCaseAsync(new OpenCaseCommand(OriginalReport: "Caso para teste de autorização"));

        var roles = await roleRepo.GetAllAsync();
        // Papel "Admin Segurança" tem 'caso.visualizar', mas NÃO tem 'caso.diagnosticar' (25_MATRIZ_PERFIS_PERMISSOES.md)
        var secRole = roles.First(r => r.Name == "Admin Segurança");
        var depts = await deptRepo.GetAllAsync();

        var user = await userService.CreateUserAsync(new CreateUserRequest(
            Name: "Auditor de Segurança",
            Email: "auditor.seg@tracecore.local",
            Password: "Password123!",
            DepartmentIds: new List<long> { depts.First().Id },
            RoleIds: new List<long> { secRole.Id }
        ));

        var client = await CreateAuthenticatedClientAsync(user.Email, "Password123!");

        // Act 1: Tenta registrar hipótese
        var hypPayload = new RegisterHypothesisRequest(Title: "Hipótese não autorizada");
        var hypResponse = await client.PostAsJsonAsync($"/api/cases/{@case.Id}/hypotheses", hypPayload);

        // Assert 1
        hypResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "o servidor deve validar autorização no backend e rejeitar usuários sem a permissão 'caso.diagnosticar' (DEV-AI-005, BR-102)");

        // Act 2: Tenta registrar passo de diagnóstico
        var stepPayload = new RegisterDiagnosticStepRequest(
            HypothesisId: null,
            Title: "Teste não autorizado",
            Objective: "Objetivo",
            Instruction: null,
            InputEvidenceSummary: "Evidência",
            ResultSummary: "Resultado",
            Outcome: "Worked"
        );
        var stepResponse = await client.PostAsJsonAsync($"/api/cases/{@case.Id}/diagnostic-steps", stepPayload);

        // Assert 2
        stepResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// Validação de Autorização no Servidor:
    /// Um usuário autenticado COM a permissão 'caso.diagnosticar' (como Usuário Técnico)
    /// executa os endpoints com sucesso (201 Created).
    /// </summary>
    [Fact]
    public async Task ServerAuthorization_UserWithCasoDiagnosticarPermission_Succeeds()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var roleRepo = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
        var deptRepo = scope.ServiceProvider.GetRequiredService<IDepartmentRepository>();

        var @case = await caseService.OpenCaseAsync(new OpenCaseCommand(OriginalReport: "Caso para teste técnico autorizado"));

        var roles = await roleRepo.GetAllAsync();
        // Papel "Usuário Técnico" possui 'caso.diagnosticar'
        var techRole = roles.First(r => r.Name == "Usuário Técnico");
        var depts = await deptRepo.GetAllAsync();

        var user = await userService.CreateUserAsync(new CreateUserRequest(
            Name: "Técnico de Suporte",
            Email: "tecnico.suporte@tracecore.local",
            Password: "Password123!",
            DepartmentIds: new List<long> { depts.First().Id },
            RoleIds: new List<long> { techRole.Id }
        ));

        var client = await CreateAuthenticatedClientAsync(user.Email, "Password123!");

        // Act: Registra hipótese via API
        var hypPayload = new RegisterHypothesisRequest(Title: "Hipótese criada via API autorizada");
        var hypResponse = await client.PostAsJsonAsync($"/api/cases/{@case.Id}/hypotheses", hypPayload);

        // Assert: Sucesso 201 Created
        hypResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdHyp = await hypResponse.Content.ReadFromJsonAsync<CaseHypothesisDto>();
        createdHyp.Should().NotBeNull();
        createdHyp!.Title.Should().Be("Hipótese criada via API autorizada");
        createdHyp.Status.Should().Be("Proposed");
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IAppAuthService>();

        var loginResult = await authService.LoginAsync(email, password);
        if (!loginResult.IsSuccess || loginResult.User == null)
            throw new InvalidOperationException($"Falha no login de teste para '{email}': {loginResult.ErrorMessage}");

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, loginResult.User.Id.ToString()),
            new Claim(ClaimTypes.Name, loginResult.User.Name),
            new Claim(ClaimTypes.Email, loginResult.User.Email),
            new Claim("status", loginResult.User.Status.ToString())
        };

        foreach (var p in loginResult.Permissions)
        {
            claims.Add(new Claim("permission", p));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var inMemoryStore = _factory.Services.GetRequiredService<TraceCore.Infrastructure.Persistence.InMemory.InMemoryDataStore>();

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddSingleton(inMemoryStore);
                services.AddAuthentication(defaultScheme: "TestScheme")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", options => { });
                services.AddSingleton(principal);
            });
        }).CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        return client;
    }
}
