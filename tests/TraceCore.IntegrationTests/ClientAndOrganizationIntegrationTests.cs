using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TraceCore.Application.DTOs;
using TraceCore.Application.Exceptions;
using TraceCore.Application.Services;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Enums;
using TraceCore.Domain.Repositories;
using Xunit;

namespace TraceCore.IntegrationTests;

public class ClientAndOrganizationIntegrationTests : IClassFixture<TraceCoreTestApplicationFactory>
{
    private readonly TraceCoreTestApplicationFactory _factory;

    public ClientAndOrganizationIntegrationTests(TraceCoreTestApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    /// <summary>
    /// Bloco 7.A.1 (Não-Silo): Confirma que um usuário do departamento "Suporte" consegue listar
    /// e ler casos e itens de conhecimento de OUTRO departamento (ex.: "Infraestrutura"),
    /// contanto que tenha permissão normal (caso.visualizar), sem filtro implícito de silo.
    /// </summary>
    [Fact]
    public async Task DepartmentNonSilo_UserFromSupport_CanReadCasesAndKnowledge_FromInfrastructureDepartment()
    {
        using var scope = _factory.Services.CreateScope();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var knowledgeService = scope.ServiceProvider.GetRequiredService<IKnowledgeService>();
        var deptService = scope.ServiceProvider.GetRequiredService<IDepartmentService>();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var roleService = scope.ServiceProvider.GetRequiredService<IRoleService>();

        var departments = await deptService.GetAllDepartmentsAsync();
        var suporteDept = departments.First(d => d.Name == "Suporte");
        var infraDept = departments.First(d => d.Name == "Infraestrutura");

        var roles = await roleService.GetAllRolesAsync();
        var techRole = roles.First(r => r.Name == "Usuário Técnico");

        // Usuário no departamento Suporte
        var suporteUser = await userService.CreateUserAsync(new CreateUserRequest(
            Name: "Maria Suporte",
            Email: "maria.suporte@empresa.com",
            Password: "Password123!",
            DepartmentIds: new List<long> { suporteDept.Id },
            RoleIds: new List<long> { techRole.Id }
        ));

        // Cria caso atribuído ao departamento Infraestrutura
        var createdCase = await caseService.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Falha de roteamento no cluster: Roteador de borda não responde a pacotes ICMP",
            CurrentDepartmentId: infraDept.Id,
            Severity: "High"
        ), currentUserId: suporteUser.Id);

        // Cria artigo de conhecimento atribuído a Infraestrutura
        var createdKnowledgeId = await knowledgeService.CreateKnowledgeDraftAsync(new CreateKnowledgeDraftCommand(
            Title: "Guia de Restauração de Roteadores",
            Summary: "Procedimento para reestabelecer tabela BGP",
            ProblemDescription: "Queda total de rotas de borda",
            ContentMarkdown: "1. Acessar console serial. 2. Reiniciar daemon zebra.",
            OwnerDepartmentId: infraDept.Id
        ), userId: suporteUser.Id);

        // Act: Consulta casos e artigos
        var cases = await caseService.GetAllCasesAsync();
        var knowledge = await knowledgeService.GetKnowledgeDetailAsync(createdKnowledgeId);

        // Assert: Usuário de suporte enxerga sem silos
        cases.Should().Contain(c => c.Id == createdCase.Id);
        knowledge.Should().NotBeNull();
        knowledge!.Title.Should().Be("Guia de Restauração de Roteadores");
    }

    /// <summary>
    /// Bloco 7.A.1 (BR-002): Bloqueia desativação ou suspensão do último usuário ativo com papel Admin.
    /// </summary>
    [Fact]
    public async Task AdminProtection_LastActiveAdmin_CannotBeDeactivatedOrSuspended()
    {
        using var scope = _factory.Services.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        // Localiza o admin padrão seedado (admin@tracecore.local)
        var admin = await userRepo.GetByEmailAsync("admin@tracecore.local");
        admin.Should().NotBeNull();

        // Tentativa de desativar o último admin
        var actDeactivate = async () => await userService.UpdateUserStatusAsync(admin!.Id, UserStatus.Inactive);
        await actDeactivate.Should().ThrowAsync<BusinessRuleValidationException>()
            .WithMessage("*último usuário ativo com o papel de Administrador*");

        // Tentativa de suspender o último admin
        var actSuspend = async () => await userService.UpdateUserStatusAsync(admin!.Id, UserStatus.Suspended);
        await actSuspend.Should().ThrowAsync<BusinessRuleValidationException>()
            .WithMessage("*último usuário ativo com o papel de Administrador*");
    }

    /// <summary>
    /// Bloco 7.A.1 (BR-002): Bloqueia remoção do papel Admin do último usuário ativo detentor deste papel.
    /// </summary>
    [Fact]
    public async Task AdminProtection_LastActiveAdmin_CannotHaveAdminRoleRemoved()
    {
        using var scope = _factory.Services.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var roleService = scope.ServiceProvider.GetRequiredService<IRoleService>();

        var admin = await userRepo.GetByEmailAsync("admin@tracecore.local");
        admin.Should().NotBeNull();

        var roles = await roleService.GetAllRolesAsync();
        var techRole = roles.First(r => r.Name == "Usuário Técnico");

        // Tenta atribuir somente papel Técnico ao Admin, removendo o papel Admin
        var actRemove = async () => await userService.AssignRolesAsync(admin!.Id, new[] { techRole.Id });
        await actRemove.Should().ThrowAsync<BusinessRuleValidationException>()
            .WithMessage("*remover o papel de Administrador (Admin) do último usuário ativo*");
    }

    /// <summary>
    /// Bloco 7.A.1 (Cliente & Contexto Técnico): Valida que product_version_id deve pertencer ao product_id.
    /// </summary>
    [Fact]
    public async Task ClientTechnicalContext_ProductVersionMustBelongToProduct_OrThrowsValidationException()
    {
        using var scope = _factory.Services.CreateScope();
        var clientService = scope.ServiceProvider.GetRequiredService<IClientService>();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();

        // Cria dois produtos e versões distintas
        var prod1Id = await catalogRepo.AddProductAsync(new Product("Produto Alpha", "PROD-A"));
        var prod1VerId = await catalogRepo.AddProductVersionAsync(new ProductVersion(prod1Id, "1.0"));

        var prod2Id = await catalogRepo.AddProductAsync(new Product("Produto Beta", "PROD-B"));
        var prod2VerId = await catalogRepo.AddProductVersionAsync(new ProductVersion(prod2Id, "2.0"));

        var clientId = await clientService.CreateClientAsync(new CreateClientRequest("Cliente Teste Alpha"));

        // Contexto com Produto 1 e versão legítima do Produto 1: Sucesso
        var okContextId = await clientService.AddTechnicalContextAsync(clientId, new CreateTechnicalContextRequest(
            ProductId: prod1Id,
            ProductVersionId: prod1VerId
        ));
        okContextId.Should().BeGreaterThan(0);

        // Contexto com Produto 1 e versão do Produto 2: Deve Falhar (BR-CLIENT-001)
        var actInvalid = async () => await clientService.AddTechnicalContextAsync(clientId, new CreateTechnicalContextRequest(
            ProductId: prod1Id,
            ProductVersionId: prod2VerId
        ));

        await actInvalid.Should().ThrowAsync<BusinessRuleValidationException>()
            .WithMessage("*não pertence ao produto selecionado*");
    }

    /// <summary>
    /// Bloco 7.A.1 (Cliente CRM ID): Não permite múltiplos clientes com o mesmo CRM ID.
    /// </summary>
    [Fact]
    public async Task Client_DuplicateExternalCrmId_ThrowsConflictException()
    {
        using var scope = _factory.Services.CreateScope();
        var clientService = scope.ServiceProvider.GetRequiredService<IClientService>();

        await clientService.CreateClientAsync(new CreateClientRequest(
            Name: "Cliente Original",
            Code: "ORIG-01",
            ExternalCrmId: "CRM-UNIQUE-100"
        ));

        // Tentar criar outro cliente com o mesmo ExternalCrmId
        var actDuplicate = async () => await clientService.CreateClientAsync(new CreateClientRequest(
            Name: "Cliente Clone",
            Code: "CLONE-01",
            ExternalCrmId: "CRM-UNIQUE-100"
        ));

        await actDuplicate.Should().ThrowAsync<ConflictException>()
            .WithMessage("*já existe um cliente cadastrado com o CRM ID*");
    }
}
