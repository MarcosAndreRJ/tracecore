using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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
using TraceCore.Domain.Entities;
using TraceCore.Domain.Enums;
using TraceCore.Domain.Repositories;
using Xunit;
using IAppAuthService = TraceCore.Application.Services.IAuthenticationService;

namespace TraceCore.IntegrationTests;

public class AuthorizationIntegrationTests : IClassFixture<TraceCoreTestApplicationFactory>
{
    private readonly TraceCoreTestApplicationFactory _factory;

    public AuthorizationIntegrationTests(TraceCoreTestApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    /// <summary>
    /// Critério de Aceite 4: Confirmar que uma operação protegida por uma permissão que o usuário NÃO tem
    /// é efetivamente bloqueada no servidor com 403 Forbidden (BR-102, DEV-AI-005).
    /// </summary>
    [Fact]
    public async Task OperationProtectedByPermission_WhenUserDoesNotHavePermission_IsBlockedByServerWithForbidden()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var deptService = scope.ServiceProvider.GetRequiredService<IDepartmentService>();
        var roleService = scope.ServiceProvider.GetRequiredService<IRoleService>();

        var departments = await deptService.GetAllDepartmentsAsync();
        var webDept = departments.First(d => d.Name == "Desenvolvimento Web");

        var roles = await roleService.GetAllRolesAsync();
        // Papel "Usuário Técnico" possui: caso.visualizar, caso.criar, caso.editar, solucao.criar
        // NÃO possui: usuario.gerenciar
        var techRole = roles.First(r => r.Name == "Usuário Técnico");

        var user = await userService.CreateUserAsync(new CreateUserRequest(
            Name: "Carlos Técnico",
            Email: "carlos.tecnico@empresa.com",
            Password: "Password123!",
            DepartmentIds: new List<long> { webDept.Id },
            RoleIds: new List<long> { techRole.Id }
        ));

        // Cliente HTTP autenticado com as permissões efetivas de Carlos
        var client = await CreateAuthenticatedClientAsync(user.Email, "Password123!");

        // Act - Tenta executar operação que exige 'usuario.gerenciar'
        var response = await client.PostAsync("/api/test/operacao-protegida", new StringContent(""));

        // Assert - Servidor DEVE bloquear com 403 Forbidden
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, 
            "o servidor deve rejeitar a requisição porque o usuário técnico não possui a permissão 'usuario.gerenciar' (BR-102)");
    }

    /// <summary>
    /// Critério de Aceite 5: Confirmar que a mesma operação protegida funciona com 200 OK para um usuário que TEM a permissão.
    /// </summary>
    [Fact]
    public async Task OperationProtectedByPermission_WhenUserHasPermission_SucceedsWithOk()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var deptService = scope.ServiceProvider.GetRequiredService<IDepartmentService>();
        var roleService = scope.ServiceProvider.GetRequiredService<IRoleService>();

        var departments = await deptService.GetAllDepartmentsAsync();
        var gestaoDept = departments.First(d => d.Name == "Gestão");

        var roles = await roleService.GetAllRolesAsync();
        // Papel "Admin Segurança" possui: usuario.gerenciar, permissao.gerenciar, auditoria.visualizar
        var secRole = roles.First(r => r.Name == "Admin Segurança");

        var user = await userService.CreateUserAsync(new CreateUserRequest(
            Name: "Paula Gestora de Segurança",
            Email: "paula.seg@empresa.com",
            Password: "Password123!",
            DepartmentIds: new List<long> { gestaoDept.Id },
            RoleIds: new List<long> { secRole.Id }
        ));

        // Cliente HTTP autenticado com as permissões de Paula (inclui usuario.gerenciar)
        var client = await CreateAuthenticatedClientAsync(user.Email, "Password123!");

        // Act - Executa operação protegida
        var response = await client.PostAsync("/api/test/operacao-protegida", new StringContent(""));

        // Assert - Servidor deve autorizar com 200 OK
        response.StatusCode.Should().Be(HttpStatusCode.OK, 
            "o servidor deve permitir a operação porque o usuário possui a permissão 'usuario.gerenciar'");
    }

    /// <summary>
    /// Critérios de Aceite 1, 2 e 3: Fluxo completo de ponta a ponta:
    /// 1. Criar usuário;
    /// 2. Atribuí-lo a departamento(s) (N:N, BR-002);
    /// 3. Atribuir papel com conjunto específico de permissões;
    /// 4. Validar sessão e auditoria imutável (BR-004, BR-100).
    /// </summary>
    [Fact]
    public async Task EndToEnd_CreateUser_AssignDepartment_AssignRole_Login_And_Audit()
    {
        using var scope = _factory.Services.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var deptService = scope.ServiceProvider.GetRequiredService<IDepartmentService>();
        var roleService = scope.ServiceProvider.GetRequiredService<IRoleService>();
        var authService = scope.ServiceProvider.GetRequiredService<IAppAuthService>();
        var auditRepo = scope.ServiceProvider.GetRequiredService<IAuditEventRepository>();

        // 1. Departamentos (BR-002: múltiplos departamentos)
        var depts = await deptService.GetAllDepartmentsAsync();
        var deptWeb = depts.First(d => d.Name == "Desenvolvimento Web");
        var deptMobile = depts.First(d => d.Name == "Mobile");

        // 2. Papel Especialista
        var roles = await roleService.GetAllRolesAsync();
        var specialistRole = roles.First(r => r.Name == "Especialista");

        // 3. Criar usuário vinculado a 2 departamentos (N:N) e ao papel Especialista
        var createdUser = await userService.CreateUserAsync(new CreateUserRequest(
            Name: "Roberto Engenheiro",
            Email: "roberto.eng@empresa.com",
            Password: "SecurePassword2026!",
            DepartmentIds: new List<long> { deptWeb.Id, deptMobile.Id },
            RoleIds: new List<long> { specialistRole.Id }
        ));

        createdUser.Should().NotBeNull();
        createdUser.Status.Should().Be(UserStatus.Active);
        createdUser.DepartmentIds.Should().HaveCount(2).And.Contain(new[] { deptWeb.Id, deptMobile.Id });
        createdUser.RoleIds.Should().Contain(specialistRole.Id);

        // 4. Permissões efetivas herdadas do papel
        createdUser.EffectivePermissions.Should().Contain(new[] { "caso.visualizar", "solucao.validar" });
        createdUser.EffectivePermissions.Should().NotContain("usuario.gerenciar");

        // 5. Autenticar usuário
        var loginResult = await authService.LoginAsync(
            email: "roberto.eng@empresa.com",
            password: "SecurePassword2026!",
            ipAddress: "127.0.0.1",
            userAgent: "TraceCore-IntegrationTest/1.0"
        );

        loginResult.IsSuccess.Should().BeTrue();
        loginResult.SessionId.Should().NotBeNull();

        // 6. Validar registro de auditoria imutável (BR-004 / BR-100)
        var recentAudit = await auditRepo.GetRecentAsync(10);
        recentAudit.Should().Contain(a => a.Action == "user.create" && a.EntityId == createdUser.Id.ToString());
        recentAudit.Should().Contain(a => a.Action == "login.success" && a.EntityId == createdUser.Id.ToString());
    }

    /// <summary>
    /// BR-001: Todo usuário ativo deve possuir identidade única, nome, e-mail/login
    /// e pelo menos um vínculo organizacional ou papel global.
    /// </summary>
    [Fact]
    public async Task CreateUser_WithoutDepartmentAndWithoutRole_ThrowsBusinessRuleValidationException_BR001()
    {
        using var scope = _factory.Services.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        var act = async () => await userService.CreateUserAsync(new CreateUserRequest(
            Name: "Usuario Solto",
            Email: "solto@empresa.com",
            Password: "Password123!",
            DepartmentIds: new List<long>(),
            RoleIds: new List<long>()
        ));

        var ex = await act.Should().ThrowAsync<BusinessRuleValidationException>();
        ex.Which.RuleId.Should().Be("BR-001");
    }

    /// <summary>
    /// BR-005: Desativar usuário não apaga autoria nem histórico, apenas altera status e invalida sessões.
    /// </summary>
    [Fact]
    public async Task DeactivateUser_PreservesData_ChangesStatus_And_RevokesSessions_BR005()
    {
        using var scope = _factory.Services.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var deptService = scope.ServiceProvider.GetRequiredService<IDepartmentService>();
        var sessionRepo = scope.ServiceProvider.GetRequiredService<IUserSessionRepository>();
        var authService = scope.ServiceProvider.GetRequiredService<IAppAuthService>();

        var depts = await deptService.GetAllDepartmentsAsync();
        var user = await userService.CreateUserAsync(new CreateUserRequest(
            Name: "Joao Suporte",
            Email: "joao.suporte@empresa.com",
            Password: "Password123!",
            DepartmentIds: new List<long> { depts.First().Id },
            RoleIds: new List<long>()
        ));

        // Login inicial
        var loginRes = await authService.LoginAsync("joao.suporte@empresa.com", "Password123!");
        loginRes.IsSuccess.Should().BeTrue();

        // Desativar usuário
        await userService.UpdateUserStatusAsync(user.Id, UserStatus.Inactive);

        // Verificar status
        var updatedUser = await userService.GetUserByIdAsync(user.Id);
        updatedUser!.Status.Should().Be(UserStatus.Inactive);

        // Verificar sessões revogadas
        var activeSessions = await sessionRepo.GetActiveSessionsByUserIdAsync(user.Id);
        activeSessions.Should().BeEmpty();

        // Tentar login novamente -> deve ser bloqueado
        var secondLogin = await authService.LoginAsync("joao.suporte@empresa.com", "Password123!");
        secondLogin.IsSuccess.Should().BeFalse();
        secondLogin.ErrorMessage.Should().Contain("inativo");
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

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ClaimsPrincipal _principal;

    public TestAuthHandler(
        Microsoft.Extensions.Options.IOptionsMonitor<AuthenticationSchemeOptions> options,
        Microsoft.Extensions.Logging.ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder,
        ClaimsPrincipal principal)
        : base(options, logger, encoder)
    {
        _principal = principal;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var ticket = new AuthenticationTicket(_principal, "TestScheme");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
