using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TraceCore.Application;
using TraceCore.Application.DTOs;
using TraceCore.Application.Services;
using TraceCore.Infrastructure;
using TraceCore.Web.Security;

var builder = WebApplication.CreateBuilder(args);

// Injeção de dependência das camadas limpas (Clean Architecture §6)
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Autenticação via Cookie conforme 12_SEGURANCA §8
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "TraceCore.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/acesso-negado";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Políticas de autorização granulares no servidor (BR-102 / DEV-AI-005)
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddAuthorization();

builder.Services.AddRazorPages();

var app = builder.Build();

// Bloco 7.A.0 §4.3: DatabaseMigrationRunner estava registrado mas nunca era chamado.
// AutoMigrate é explícito por ambiente (appsettings.{Environment}.json) — em produção
// não assumir automaticamente que migrations rodam no startup (padrão seguro: false).
if (string.Equals(app.Configuration["Persistence:Provider"], "MySql", StringComparison.OrdinalIgnoreCase)
    && app.Configuration.GetValue<bool>("Persistence:AutoMigrate"))
{
    using var migrationScope = app.Services.CreateScope();
    var migrationRunner = migrationScope.ServiceProvider.GetRequiredService<TraceCore.Infrastructure.Migrations.DatabaseMigrationRunner>();
    var connectionString = app.Configuration.GetConnectionString("TraceCoreDb")
        ?? throw new InvalidOperationException("ConnectionStrings:TraceCoreDb ausente ao tentar executar AutoMigrate.");
    migrationRunner.EnsureDatabaseCreated(connectionString);
    migrationRunner.MigrateUp();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Endpoint de teste protegido no servidor (Critérios de aceite 4 e 5)
app.MapPost("/api/test/operacao-protegida", [Authorize(Policy = "usuario.gerenciar")] () =>
    Results.Ok(new
    {
        message = "Operação autorizada com sucesso pelo servidor!",
        timestampUtc = DateTime.UtcNow
    }));

// Endpoints da Fase 3 — Cadastro de Ocorrências (DEV-AI-005, BR-102)
app.MapPost("/api/cases", [Authorize(Policy = "caso.criar")] async (
    OpenCaseCommand command,
    ICaseService caseService,
    HttpContext httpContext) =>
{
    long? currentUserId = null;
    var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (long.TryParse(userIdClaim, out var parsedId))
    {
        currentUserId = parsedId;
    }

    var createdCase = await caseService.OpenCaseAsync(command, currentUserId);
    return Results.Created($"/api/cases/{createdCase.Id}", createdCase);
});

app.MapGet("/api/cases/{id:long}", [Authorize(Policy = "caso.visualizar")] async (
    long id,
    ICaseService caseService) =>
{
    var @case = await caseService.GetCaseByIdAsync(id);
    return @case != null ? Results.Ok(@case) : Results.NotFound();
});

// Endpoints da Fase 4 — Investigação do Caso (DEV-AI-005, BR-102, BR-023 a BR-026)
app.MapPost("/api/cases/{caseId:long}/hypotheses", [Authorize(Policy = "caso.diagnosticar")] async (
    long caseId,
    RegisterHypothesisRequest request,
    ICaseInvestigationService investigationService,
    HttpContext httpContext) =>
{
    long? currentUserId = null;
    var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (long.TryParse(userIdClaim, out var parsedId))
    {
        currentUserId = parsedId;
    }

    var command = new RegisterHypothesisCommand(caseId, request.Title, request.Description, request.ComponentId);
    var created = await investigationService.RegisterHypothesisAsync(command, currentUserId);
    return Results.Created($"/api/cases/{caseId}/hypotheses/{created.Id}", created);
});

app.MapPost("/api/cases/{caseId:long}/diagnostic-steps", [Authorize(Policy = "caso.diagnosticar")] async (
    long caseId,
    RegisterDiagnosticStepRequest request,
    ICaseInvestigationService investigationService,
    HttpContext httpContext) =>
{
    long? currentUserId = null;
    var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (long.TryParse(userIdClaim, out var parsedId))
    {
        currentUserId = parsedId;
    }

    var command = new RegisterDiagnosticStepCommand(
        CaseId: caseId,
        HypothesisId: request.HypothesisId,
        Title: request.Title,
        Objective: request.Objective,
        Instruction: request.Instruction,
        InputEvidenceSummary: request.InputEvidenceSummary,
        ResultSummary: request.ResultSummary,
        Outcome: request.Outcome,
        StepType: request.StepType,
        RiskLevel: request.RiskLevel,
        DurationSeconds: request.DurationSeconds
    );

    var created = await investigationService.RegisterDiagnosticStepAsync(command, currentUserId);
    return Results.Created($"/api/cases/{caseId}/diagnostic-steps/{created.Id}", created);
});

app.MapPost("/api/hypotheses/{hypothesisId:long}/evaluate", [Authorize(Policy = "caso.diagnosticar")] async (
    long hypothesisId,
    EvaluateHypothesisRequest request,
    ICaseInvestigationService investigationService,
    HttpContext httpContext) =>
{
    long? currentUserId = null;
    var userIdClaim = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (long.TryParse(userIdClaim, out var parsedId))
    {
        currentUserId = parsedId;
    }

    var command = new EvaluateHypothesisCommand(hypothesisId, request.NewStatus, request.Justification, request.EvidenceStepId);
    var updated = await investigationService.EvaluateHypothesisAsync(command, currentUserId);
    return Results.Ok(updated);
});

app.MapGet("/api/cases/{caseId:long}/investigation-timeline", [Authorize(Policy = "caso.visualizar")] async (
    long caseId,
    ICaseInvestigationService investigationService) =>
{
    var timeline = await investigationService.GetInvestigationTimelineAsync(caseId);
    return Results.Ok(timeline);
});

app.MapRazorPages();

app.Run();

public partial class Program { }
