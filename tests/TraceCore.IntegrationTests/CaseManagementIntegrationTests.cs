using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TraceCore.Application.DTOs;
using TraceCore.Application.Services;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;
using TraceCore.Domain.Services;
using Xunit;
using IAppAuthService = TraceCore.Application.Services.IAuthenticationService;

namespace TraceCore.IntegrationTests;

public class CaseManagementIntegrationTests : IClassFixture<TraceCoreTestApplicationFactory>
{
    private readonly TraceCoreTestApplicationFactory _factory;

    public CaseManagementIntegrationTests(TraceCoreTestApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    /// <summary>
    /// Critério de Aceite 1: Abrir um caso informando APENAS a descrição (todos os outros campos ausentes)
    /// e o sistema aceitar, sem exigir departamento ou área responsável (BR-022, FR-042).
    /// </summary>
    [Fact]
    public async Task AcceptanceCriterion1_OpenCaseWithOnlyDescription_SucceedsWithoutRequiringDepartmentOrContext()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var auditRepo = scope.ServiceProvider.GetRequiredService<IAuditEventRepository>();

        var command = new OpenCaseCommand(
            OriginalReport: "O cliente relatou que o sistema trava intermitentemente ao consultar o saldo."
        );

        // Act - Abre caso sem nenhum campo de contexto técnico ou organizacional
        var createdCase = await caseService.OpenCaseAsync(command);

        // Assert
        createdCase.Should().NotBeNull();
        createdCase.Id.Should().BeGreaterThan(0);
        createdCase.CaseNumber.Should().BeGreaterThan(0ul, "o case_number visível deve ser gerado automaticamente");
        createdCase.OriginalReport.Should().Be("O cliente relatou que o sistema trava intermitentemente ao consultar o saldo.");
        createdCase.Status.Should().Be("Open");

        // BR-022: Nenhum departamento ou usuário responsável é obrigatório
        createdCase.CurrentDepartmentId.Should().BeNull();
        createdCase.CurrentDepartmentName.Should().BeNull();
        createdCase.CurrentOwnerUserId.Should().BeNull();

        // FR-042: Contexto ausente permitido
        createdCase.ClientId.Should().BeNull();
        createdCase.ProductId.Should().BeNull();
        createdCase.EnvironmentId.Should().BeNull();
        createdCase.AffectedComponents.Should().BeEmpty();

        // BR-100: Auditoria imutável registrada
        var auditEvents = await auditRepo.GetByEntityAsync("Case", createdCase.Id.ToString());
        auditEvents.Should().ContainSingle(a => a.Action == "CaseOpened");
    }

    /// <summary>
    /// Critério de Aceite 2: Abrir um caso completo (cliente, sistema, ambiente, versão, múltiplos componentes,
    /// severidade, impacto, sintomas, evidência e anexo) e recuperá-lo com todos os dados íntegros (BR-023).
    /// </summary>
    [Fact]
    public async Task AcceptanceCriterion2_OpenCompleteCase_WithContextComponentsAndAttachment_RetrievesAllDataIntact()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();

        var clients = await caseService.GetClientsAsync();
        var clientAcme = clients.First(c => c.Code == "CLI-001");

        var products = await caseService.GetProductsAsync();
        var erpProduct = products.First(p => p.Code == "PRD-ERP");

        var versions = await caseService.GetVersionsByProductAsync(erpProduct.Id);
        var version = versions.First();

        var environments = await caseService.GetEnvironmentsAsync();
        var prodEnv = environments.First(e => e.Name == "Produção");

        var components = await caseService.GetComponentsAsync(erpProduct.Id);
        // BR-023: Múltiplos componentes afetados
        var compIds = components.Take(2).Select(c => c.Id).ToList();

        var fileContent = "2026-09-17 10:00:00 [ERROR] Database timeout expired in billing module.";
        var fileBytes = Encoding.UTF8.GetBytes(fileContent);
        using var fileStream = new MemoryStream(fileBytes);

        var command = new OpenCaseCommand(
            OriginalReport: "Falha crítica no módulo de faturamento gerando timeout na emissão.",
            Severity: "Critical",
            ImpactLevel: "Corporativo",
            ClientId: clientAcme.Id,
            ProductId: erpProduct.Id,
            ProductVersionId: version.Id,
            EnvironmentId: prodEnv.Id,
            ComponentIds: compIds,
            ErrorCode: "ERR-TIMEOUT-504",
            ErrorMessage: "TimeoutExpired: Database execution exceeded 30s",
            Symptoms: new List<string> { "Lentidão generalizada ao emitir fatura", "NF presa em processamento" },
            Evidences: new List<CaseEvidenceInputDto> { new("Log", "Log de erro do banco de dados anexado") },
            Attachments: new List<AttachmentInputDto>
            {
                new("billing_error.log", "text/plain", fileStream, "Internal")
            }
        );

        // Act - Abre caso completo
        var createdCase = await caseService.OpenCaseAsync(command, currentUserId: 1);

        // Recupera pelo ID para validar persistência e integridade
        var retrievedCase = await caseService.GetCaseByIdAsync(createdCase.Id);

        // Assert
        retrievedCase.Should().NotBeNull();
        retrievedCase!.CaseNumber.Should().Be(createdCase.CaseNumber);
        retrievedCase.ClientId.Should().Be(clientAcme.Id);
        retrievedCase.ClientName.Should().Be(clientAcme.Name);
        retrievedCase.ProductId.Should().Be(erpProduct.Id);
        retrievedCase.ProductName.Should().Be(erpProduct.Name);
        retrievedCase.EnvironmentId.Should().Be(prodEnv.Id);
        retrievedCase.EnvironmentName.Should().Be(prodEnv.Name);
        retrievedCase.Severity.Should().Be("Critical");
        retrievedCase.ImpactLevel.Should().Be("Corporativo");
        retrievedCase.ErrorCode.Should().Be("ERR-TIMEOUT-504");
        retrievedCase.ErrorMessage.Should().Be("TimeoutExpired: Database execution exceeded 30s");

        // BR-023: Confirma que ambos os componentes afetados estão vinculados
        retrievedCase.AffectedComponents.Should().HaveCount(2);
        retrievedCase.AffectedComponents.Select(c => c.ComponentId).Should().BeEquivalentTo(compIds);
        retrievedCase.AffectedComponents.Should().AllSatisfy(c => c.RelationType.Should().Be("Affected"));

        // Sintomas íntegros
        retrievedCase.Symptoms.Should().HaveCount(2);

        // Anexos íntegros
        retrievedCase.Attachments.Should().HaveCount(1);
        var att = retrievedCase.Attachments.First();
        att.FileName.Should().Be("billing_error.log");
        att.SizeBytes.Should().Be((ulong)fileBytes.Length);
    }

    /// <summary>
    /// Critério de Aceite 3: Tentar (via código/teste) alterar 'OriginalReport' de um caso já criado
    /// e confirmar que não existe caminho que permita isso (BR-020).
    /// </summary>
    [Fact]
    public void AcceptanceCriterion3_OriginalReport_IsStrictlyImmutable_CannotBeModifiedAfterCreation()
    {
        // 1. Verificação Estática via Reflection: propriedade OriginalReport NÃO possui setter público
        var propertyInfo = typeof(Case).GetProperty(nameof(Case.OriginalReport));
        propertyInfo.Should().NotBeNull();

        var setMethod = propertyInfo!.GetSetMethod(nonPublic: true);
        setMethod.Should().NotBeNull("deve existir setter interno para deserialização");
        propertyInfo.GetSetMethod(nonPublic: false).Should().BeNull("OriginalReport NUNCA pode expor setter público (BR-020)");

        // 2. Verificação de Métodos do Domínio: nenhum método em Case altera OriginalReport
        var methods = typeof(Case).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        var mutatingMethods = methods.Where(m => 
            (m.Name.StartsWith("set_", StringComparison.OrdinalIgnoreCase) && m.Name.Contains("OriginalReport"))
            || m.Name.Contains("SetOriginalReport")
            || m.Name.Contains("UpdateOriginalReport")
            || m.Name.Contains("ChangeOriginalReport")).ToList();
        mutatingMethods.Should().BeEmpty("não deve existir nenhum método de negócio capaz de alterar OriginalReport após a criação (BR-020)");

        // 3. Verificação em Tempo de Execução: atualizar resumo normalizado (BR-021) preserva o relato original intacto
        var originalText = "Relato original intocado do cliente";
        var @case = new Case(originalText);

        @case.UpdateNormalizedSummary("Novo resumo interpretado pelo suporte");

        @case.OriginalReport.Should().Be(originalText, "o relato original deve permanecer estritamente inalterado (BR-020)");
        @case.NormalizedSummary.Should().Be("Novo resumo interpretado pelo suporte", "o resumo normalizado é atualizável separadamente (BR-021)");
    }

    /// <summary>
    /// Critério de Aceite 4: Confirmar que o anexo enviado foi de fato gravado no storage configurado
    /// e que attachments.sha256 e size_bytes correspondem ao arquivo salvo (ADR-P004 / 12_SEGURANCA §8).
    /// </summary>
    [Fact]
    public async Task AcceptanceCriterion4_Attachment_IsSavedToConfiguredStorage_WithMatchingSha256AndSizeBytes()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var attachmentRepo = scope.ServiceProvider.GetRequiredService<IAttachmentRepository>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();

        var rawContent = "TraceCore diagnostic log payload for verification - " + Guid.NewGuid();
        var contentBytes = Encoding.UTF8.GetBytes(rawContent);

        // Calcula SHA-256 esperado dos bytes
        string expectedSha256;
        using (var sha256 = SHA256.Create())
        {
            expectedSha256 = Convert.ToHexString(sha256.ComputeHash(contentBytes)).ToLowerInvariant();
        }

        using var contentStream = new MemoryStream(contentBytes);

        var command = new OpenCaseCommand(
            OriginalReport: "Caso para validação de integridade física de anexo.",
            Attachments: new List<AttachmentInputDto>
            {
                new("evidence_trace.log", "text/plain", contentStream, "Internal")
            }
        );

        // Act - Abre caso e persiste anexo via IFileStorage
        var createdCase = await caseService.OpenCaseAsync(command);

        // Assert - Metadados em attachments
        var attachments = await attachmentRepo.GetByEntityAsync("Case", createdCase.Id);
        attachments.Should().ContainSingle();

        var attachment = attachments.First();
        attachment.FileName.Should().Be("evidence_trace.log");
        attachment.SizeBytes.Should().Be((ulong)contentBytes.Length);
        attachment.Sha256.Should().Be(expectedSha256);

        // Confirma existência física no storage configurado (ADR-P004)
        var fileExists = await fileStorage.ExistsAsync(attachment.StorageKey);
        fileExists.Should().BeTrue("o arquivo deve ter sido fisicamente gravado no filesystem via IFileStorage");

        // Lê o arquivo salvo e valida integridade bit a bit
        using var storedStream = await fileStorage.GetAsync(attachment.StorageKey);
        storedStream.Should().NotBeNull();

        using var memoryStream = new MemoryStream();
        await storedStream!.CopyToAsync(memoryStream);
        var storedBytes = memoryStream.ToArray();

        storedBytes.Should().Equal(contentBytes, "o conteúdo gravado no storage deve ser idêntico ao enviado");
    }

    /// <summary>
    /// Validação de Autorização no Servidor (DEV-AI-005 / BR-102):
    /// Um usuário autenticado SEM a permissão 'caso.criar' é bloqueado com 403 Forbidden ao tentar abrir um caso via API.
    /// </summary>
    [Fact]
    public async Task ServerAuthorization_UserWithoutCasoCriarPermission_IsForbidden()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var roleRepo = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
        var deptRepo = scope.ServiceProvider.GetRequiredService<IDepartmentRepository>();

        var roles = await roleRepo.GetAllAsync();
        // Papel "Admin Segurança" possui gerenciamento de usuários/segurança, mas NÃO possui 'caso.criar'
        var secRole = roles.First(r => r.Name == "Admin Segurança");
        var depts = await deptRepo.GetAllAsync();

        var user = await userService.CreateUserAsync(new CreateUserRequest(
            Name: "Auditor de TI",
            Email: "auditor.ti@empresa.com",
            Password: "Password123!",
            DepartmentIds: new List<long> { depts.First().Id },
            RoleIds: new List<long> { secRole.Id }
        ));

        var client = await CreateAuthenticatedClientAsync(user.Email, "Password123!");

        var payload = new
        {
            OriginalReport = "Tentativa de abertura de caso sem permissão"
        };

        // Act - Tenta abrir caso via POST /api/cases
        var response = await client.PostAsJsonAsync("/api/cases", payload);

        // Assert - O servidor DEVE bloquear com 403 Forbidden
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "o servidor deve validar autorização no backend e rejeitar usuários sem a capacidade 'caso.criar' (BR-102, DEV-AI-005)");
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

        var client = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
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
