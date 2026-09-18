using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Enums;
using TraceCore.Domain.Repositories;

namespace TraceCore.Infrastructure.Persistence.InMemory;

public class InMemoryDataStore
{
    public ConcurrentDictionary<long, User> Users { get; } = new();
    public ConcurrentDictionary<long, Department> Departments { get; } = new();
    public ConcurrentDictionary<long, Role> Roles { get; } = new();
    public ConcurrentDictionary<long, Permission> Permissions { get; } = new();
    public ConcurrentDictionary<long, UserSession> Sessions { get; } = new();
    public ConcurrentDictionary<long, PasswordResetToken> ResetTokens { get; } = new();
    public ConcurrentDictionary<long, AuditEvent> AuditEvents { get; } = new();
    public ConcurrentDictionary<long, Client> Clients { get; } = new();
    public ConcurrentDictionary<long, ClientUnit> ClientUnits { get; } = new();
    public ConcurrentDictionary<long, ClientTechnicalContext> ClientTechnicalContexts { get; } = new();
    public ConcurrentDictionary<long, Product> Products { get; } = new();
    public ConcurrentDictionary<long, ProductVersion> ProductVersions { get; } = new();
    public ConcurrentDictionary<long, EnvironmentEntity> Environments { get; } = new();
    public ConcurrentDictionary<long, ComponentEntity> Components { get; } = new();
    public ConcurrentDictionary<long, ComponentDependency> ComponentDependencies { get; } = new();
    public ConcurrentDictionary<long, ComponentOwner> ComponentOwners { get; } = new();
    public ConcurrentDictionary<long, Case> Cases { get; } = new();
    public ConcurrentDictionary<long, CaseIteration> CaseIterations { get; } = new();
    public ConcurrentDictionary<long, CaseEvidence> CaseEvidences { get; } = new();
    public ConcurrentDictionary<long, CaseHypothesisEvidence> CaseHypothesisEvidences { get; } = new();
    public ConcurrentDictionary<long, Attachment> Attachments { get; } = new();
    public ConcurrentDictionary<long, DiagnosticSession> DiagnosticSessions { get; } = new();
    public ConcurrentDictionary<long, CaseHypothesis> CaseHypotheses { get; } = new();
    public ConcurrentDictionary<long, DiagnosticStep> DiagnosticSteps { get; } = new();
    public ConcurrentDictionary<long, RootCause> RootCauses { get; } = new();
    public ConcurrentDictionary<long, CaseResolution> CaseResolutions { get; } = new();
    public ConcurrentDictionary<long, CaseRelation> CaseRelations { get; } = new();
    public ConcurrentDictionary<long, KnowledgeItem> KnowledgeItems { get; } = new();
    public ConcurrentDictionary<long, KnowledgeVersion> KnowledgeVersions { get; } = new();
    public ConcurrentDictionary<long, KnowledgeApplicability> KnowledgeApplicabilities { get; } = new();
    public ConcurrentDictionary<long, KnowledgeStep> KnowledgeSteps { get; } = new();
    public ConcurrentDictionary<long, KnowledgeSymptom> KnowledgeSymptoms { get; } = new();
    public ConcurrentDictionary<long, Technology> Technologies { get; } = new();
    public ConcurrentDictionary<long, Tag> Tags { get; } = new();
    public List<(long KnowledgeItemId, long TechnologyId)> KnowledgeTechnologies { get; } = new();
    public List<(long KnowledgeItemId, long TagId)> KnowledgeTags { get; } = new();
    public ConcurrentDictionary<long, KnowledgeUsage> KnowledgeUsages { get; } = new();

    public List<UserDepartment> UserDepartments { get; } = new();
    public List<UserRole> UserRoles { get; } = new();
    public List<RolePermission> RolePermissions { get; } = new();

    public List<SearchSession> SearchSessions { get; } = new();
    public List<SearchQueryRecord> SearchQueries { get; } = new();
    public List<SearchResultInteraction> SearchResultInteractions { get; } = new();

    private long _userIdSeq = 0;
    private long _deptIdSeq = 0;
    private long _roleIdSeq = 0;
    private long _permIdSeq = 0;
    private long _sessionIdSeq = 0;
    private long _tokenIdSeq = 0;
    private long _auditIdSeq = 0;
    private long _clientIdSeq = 0;
    private long _clientUnitIdSeq = 0;
    private long _clientTechnicalContextIdSeq = 0;
    private long _productIdSeq = 0;
    private long _versionIdSeq = 0;
    private long _envIdSeq = 0;
    private long _compIdSeq = 0;
    private long _caseIdSeq = 0;
    private long _caseNumberSeq = 0;
    private long _attachmentIdSeq = 0;
    private long _diagnosticSessionIdSeq = 0;
    private long _caseHypothesisIdSeq = 0;
    private long _diagnosticStepIdSeq = 0;
    private long _caseEvidenceIdSeq = 0;
    private long _caseHypothesisEvidenceIdSeq = 0;

    public long NextUserId() => Interlocked.Increment(ref _userIdSeq);
    public long NextDeptId() => Interlocked.Increment(ref _deptIdSeq);
    public long NextRoleId() => Interlocked.Increment(ref _roleIdSeq);
    public long NextPermId() => Interlocked.Increment(ref _permIdSeq);
    public long NextSessionId() => Interlocked.Increment(ref _sessionIdSeq);
    public long NextTokenId() => Interlocked.Increment(ref _tokenIdSeq);
    public long NextAuditId() => Interlocked.Increment(ref _auditIdSeq);
    public long NextClientId() => Interlocked.Increment(ref _clientIdSeq);
    public long NextClientUnitId() => Interlocked.Increment(ref _clientUnitIdSeq);
    public long NextClientTechnicalContextId() => Interlocked.Increment(ref _clientTechnicalContextIdSeq);
    public long NextProductId() => Interlocked.Increment(ref _productIdSeq);
    public long NextVersionId() => Interlocked.Increment(ref _versionIdSeq);
    public long NextEnvId() => Interlocked.Increment(ref _envIdSeq);
    public long NextCompId() => Interlocked.Increment(ref _compIdSeq);
    public long NextComponentDependencyId() => Interlocked.Increment(ref _componentDependencyIdSeq);
    public long NextComponentOwnerId() => Interlocked.Increment(ref _componentOwnerIdSeq);
    public long NextCaseId() => Interlocked.Increment(ref _caseIdSeq);
    public long NextCaseIterationId() => Interlocked.Increment(ref _caseIterationIdSeq);
    public ulong NextCaseNumber() => (ulong)Interlocked.Increment(ref _caseNumberSeq);
    public long NextAttachmentId() => Interlocked.Increment(ref _attachmentIdSeq);
    public long NextDiagnosticSessionId() => Interlocked.Increment(ref _diagnosticSessionIdSeq);
    public long NextCaseHypothesisId() => Interlocked.Increment(ref _caseHypothesisIdSeq);
    public long NextDiagnosticStepId() => Interlocked.Increment(ref _diagnosticStepIdSeq);
    public long NextCaseEvidenceId() => Interlocked.Increment(ref _caseEvidenceIdSeq);
    public long NextCaseHypothesisEvidenceId() => Interlocked.Increment(ref _caseHypothesisEvidenceIdSeq);
    public long NextCaseRelationId() => Interlocked.Increment(ref _caseRelationIdSeq);
    public long NextRootCauseId() => Interlocked.Increment(ref _rootCauseIdSeq);
    public long NextCaseResolutionId() => Interlocked.Increment(ref _caseResolutionIdSeq);
    public long NextKnowledgeItemId() => Interlocked.Increment(ref _knowledgeItemIdSeq);
    public long NextKnowledgeVersionId() => Interlocked.Increment(ref _knowledgeVersionIdSeq);
    public long NextKnowledgeApplicabilityId() => Interlocked.Increment(ref _knowledgeApplicabilityIdSeq);
    public long NextKnowledgeStepId() => Interlocked.Increment(ref _knowledgeStepIdSeq);
    public long NextKnowledgeSymptomId() => Interlocked.Increment(ref _knowledgeSymptomIdSeq);
    public long NextTechnologyId() => Interlocked.Increment(ref _technologyIdSeq);
    public long NextTagId() => Interlocked.Increment(ref _tagIdSeq);
    public long NextKnowledgeUsageId() => Interlocked.Increment(ref _knowledgeUsageIdSeq);
    public long NextSearchSessionId() => Interlocked.Increment(ref _searchSessionIdSeq);
    public long NextSearchQueryId() => Interlocked.Increment(ref _searchQueryIdSeq);
    public long NextSearchResultInteractionId() => Interlocked.Increment(ref _searchResultInteractionIdSeq);

    private long _rootCauseIdSeq = 0;
    private long _caseResolutionIdSeq = 0;
    private long _knowledgeItemIdSeq = 0;
    private long _knowledgeVersionIdSeq = 0;
    private long _knowledgeApplicabilityIdSeq = 0;
    private long _knowledgeStepIdSeq = 0;
    private long _knowledgeSymptomIdSeq = 0;
    private long _technologyIdSeq = 0;
    private long _tagIdSeq = 0;
    private long _knowledgeUsageIdSeq = 0;
    private long _searchSessionIdSeq = 0;
    private long _searchQueryIdSeq = 0;
    private long _searchResultInteractionIdSeq = 0;
    private long _componentDependencyIdSeq = 0;
    private long _componentOwnerIdSeq = 0;
    private long _caseIterationIdSeq = 0;
    private long _caseRelationIdSeq = 0;

    public InMemoryDataStore()
    {
        Seed();
    }

    public void Reset()
    {
        Users.Clear();
        Departments.Clear();
        Roles.Clear();
        Permissions.Clear();
        Sessions.Clear();
        ResetTokens.Clear();
        AuditEvents.Clear();
        Clients.Clear();
        ClientUnits.Clear();
        ClientTechnicalContexts.Clear();
        Products.Clear();
        ProductVersions.Clear();
        Environments.Clear();
        Components.Clear();
        ComponentDependencies.Clear();
        ComponentOwners.Clear();
        Cases.Clear();
        CaseIterations.Clear();
        CaseRelations.Clear();
        Attachments.Clear();
        DiagnosticSessions.Clear();
        CaseHypotheses.Clear();
        DiagnosticSteps.Clear();
        CaseEvidences.Clear();
        CaseHypothesisEvidences.Clear();
        RootCauses.Clear();
        CaseResolutions.Clear();
        KnowledgeItems.Clear();
        KnowledgeVersions.Clear();
        KnowledgeApplicabilities.Clear();
        KnowledgeSteps.Clear();
        KnowledgeSymptoms.Clear();
        Technologies.Clear();
        Tags.Clear();
        lock (KnowledgeTechnologies) KnowledgeTechnologies.Clear();
        lock (KnowledgeTags) KnowledgeTags.Clear();
        KnowledgeUsages.Clear();
        lock (SearchSessions) SearchSessions.Clear();
        lock (SearchQueries) SearchQueries.Clear();
        lock (SearchResultInteractions) SearchResultInteractions.Clear();
        lock (UserDepartments) UserDepartments.Clear();
        lock (UserRoles) UserRoles.Clear();
        lock (RolePermissions) RolePermissions.Clear();
        _userIdSeq = 0;
        _deptIdSeq = 0;
        _roleIdSeq = 0;
        _permIdSeq = 0;
        _sessionIdSeq = 0;
        _tokenIdSeq = 0;
        _auditIdSeq = 0;
        _clientIdSeq = 0;
        _clientUnitIdSeq = 0;
        _clientTechnicalContextIdSeq = 0;
        _productIdSeq = 0;
        _versionIdSeq = 0;
        _envIdSeq = 0;
        _compIdSeq = 0;
        _caseIdSeq = 0;
        _caseIterationIdSeq = 0;
        _caseRelationIdSeq = 0;
        _caseNumberSeq = 0;
        _attachmentIdSeq = 0;
        _diagnosticSessionIdSeq = 0;
        _caseHypothesisIdSeq = 0;
        _diagnosticStepIdSeq = 0;
        _rootCauseIdSeq = 0;
        _caseResolutionIdSeq = 0;
        _knowledgeItemIdSeq = 0;
        _knowledgeVersionIdSeq = 0;
        _knowledgeApplicabilityIdSeq = 0;
        _knowledgeStepIdSeq = 0;
        _knowledgeSymptomIdSeq = 0;
        _technologyIdSeq = 0;
        _tagIdSeq = 0;
        _knowledgeUsageIdSeq = 0;
        _componentDependencyIdSeq = 0;
        _componentOwnerIdSeq = 0;
        Seed();
    }

    private void Seed()
    {
        // 1. Permissions (português, dominio.acao)
        var perms = new[]
        {
            ("caso.visualizar", "Visualizar casos e incidentes"),
            ("caso.criar", "Criar novos casos"),
            ("caso.editar", "Editar casos existentes"),
            ("caso.encerrar", "Encerrar casos"),
            ("caso.relacionar", "Permite estabelecer relacionamentos determinísticos ou manuais entre casos"),
            ("solucao.criar", "Criar soluções de conhecimento"),
            ("solucao.validar", "Validar e revisar soluções técnicas"),
            ("solucao.publicar", "Publicar soluções oficiais de conhecimento"),
            ("analytics.visualizar", "Visualizar relatórios e painéis analíticos"),
            ("analytics.departamento", "Visualizar analytics departamentais"),
            ("usuario.gerenciar", "Gerenciar usuários, status e vínculos"),
            ("permissao.gerenciar", "Gerenciar papéis e permissões"),
            ("auditoria.visualizar", "Visualizar trilhas de auditoria"),
            ("caso.diagnosticar", "Diagnosticar caso, registrar hipóteses, testes e avaliações"),
            ("cliente.gerenciar", "Gerenciar clientes, unidades e contextos técnicos"),
            ("catalogo.gerenciar", "Gerenciar produtos, componentes, dependências e ownership no catálogo técnico")
        };

        var permLookup = new Dictionary<string, Permission>();
        foreach (var (code, desc) in perms)
        {
            var p = new Permission(code, desc) { Id = NextPermId() };
            Permissions[p.Id] = p;
            permLookup[code] = p;
        }

        // 2. Departments
        var depts = new[]
        {
            ("Desenvolvimento Web", "Equipes focadas em soluções web e front-end"),
            ("Desenvolvimento Desktop", "Equipes focadas em aplicações desktop legadas e modernas"),
            ("Mobile", "Aplicações corporativas móveis"),
            ("Infraestrutura", "Servidores, redes, nuvem e sustentação de TI"),
            ("Banco de Dados", "Administração, performance e sustentação de dados"),
            ("Suporte", "Atendimento ao cliente e suporte operacional de 1º/2º nível"),
            ("Integrações", "Conectores e integrações corporativas (SAP, mensageria)"),
            ("Gestão", "Liderança de projetos, produtos e governança")
        };

        Department? gestaoDept = null;
        foreach (var (name, desc) in depts)
        {
            var d = new Department(name, desc, "Active") { Id = NextDeptId() };
            Departments[d.Id] = d;
            if (name == "Gestão") gestaoDept = d;
        }

        // 3. Roles
        var roleDefs = new (string Name, string Desc, string[] Codes)[]
        {
            ("Usuário Técnico", "Pesquisa conhecimento e registra casos", new[] { "caso.visualizar", "caso.criar", "caso.editar", "solucao.criar", "caso.diagnosticar" }),
            ("Especialista", "Atua no diagnóstico e validação", new[] { "caso.visualizar", "caso.criar", "caso.editar", "solucao.criar", "solucao.validar", "caso.diagnosticar" }),
            ("Revisor", "Revisa e publica artigos na base", new[] { "caso.visualizar", "caso.criar", "caso.editar", "solucao.criar", "solucao.validar", "solucao.publicar", "caso.diagnosticar" }),
            ("Gestor", "Acompanha indicadores e métricas", new[] { "caso.visualizar", "caso.criar", "caso.editar", "solucao.criar", "solucao.validar", "solucao.publicar", "analytics.visualizar", "analytics.departamento", "auditoria.visualizar", "caso.diagnosticar" }),
            ("Admin Funcional", "Administra catálogo e usuários", new[] { "caso.visualizar", "caso.criar", "caso.editar", "caso.encerrar", "solucao.criar", "solucao.validar", "solucao.publicar", "analytics.visualizar", "analytics.departamento", "usuario.gerenciar", "auditoria.visualizar", "caso.diagnosticar", "cliente.gerenciar", "catalogo.gerenciar" }),
            ("Admin Segurança", "Gestão de acessos, papéis e segurança", new[] { "usuario.gerenciar", "permissao.gerenciar", "auditoria.visualizar", "analytics.visualizar", "caso.visualizar" })
        };

        Role? adminSecRole = null;
        foreach (var (name, desc, codes) in roleDefs)
        {
            var r = new Role(name, desc) { Id = NextRoleId() };
            Roles[r.Id] = r;
            if (name == "Admin Segurança") adminSecRole = r;

            foreach (var code in codes)
            {
                if (permLookup.TryGetValue(code, out var p))
                {
                    RolePermissions.Add(new RolePermission(r.Id, p.Id));
                }
            }
        }

        // 4. Admin User
        var adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!", workFactor: 10);
        var admin = new User("Administrador de Segurança", "admin@tracecore.local", adminPasswordHash)
        {
            Id = NextUserId(),
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            RowVersion = 1
        };
        Users[admin.Id] = admin;

        if (gestaoDept != null)
        {
            UserDepartments.Add(new UserDepartment(admin.Id, gestaoDept.Id));
        }

        if (adminSecRole != null)
        {
            UserRoles.Add(new UserRole(admin.Id, adminSecRole.Id));
        }

        // Bloco 7.A.1: Papel Admin com TODAS as permissões da plataforma
        var adminRole = new Role("Admin", "Administrador com controle total sobre configurações e permissões da plataforma.")
        {
            Id = NextRoleId()
        };
        Roles[adminRole.Id] = adminRole;

        foreach (var p in Permissions.Values)
        {
            RolePermissions.Add(new RolePermission(adminRole.Id, p.Id));
        }

        UserRoles.Add(new UserRole(admin.Id, adminRole.Id));

        // 5. Clients
        var clientSeeds = new[]
        {
            ("CLI-001", "Acme Corporação", "CRM-ACME-001", "Cliente corporativo enterprise"),
            ("CLI-002", "Tech Solutions Brasil", "CRM-TECH-002", "Parceiro de integração tecnológica"),
            ("CLI-003", "Logística Global S.A.", null, "Operador logístico multimodal")
        };
        foreach (var (code, name, crm, notes) in clientSeeds)
        {
            var c = new Client(name, code, "Active", crm, notes) { Id = NextClientId(), CreatedAt = DateTime.UtcNow };
            Clients[c.Id] = c;
        }

        var acmeUnit = new ClientUnit(1, "MATRIZ", "Matriz São Paulo", "Active", "CRM-ACME-SP") { Id = NextClientUnitId() };
        ClientUnits[acmeUnit.Id] = acmeUnit;

        // 6. Environments
        var envSeeds = new[]
        {
            ("Produção", "Production"),
            ("Homologação", "Staging"),
            ("Desenvolvimento", "Development")
        };
        foreach (var (name, envType) in envSeeds)
        {
            var env = new EnvironmentEntity(name, envType) { Id = NextEnvId() };
            Environments[env.Id] = env;
        }

        // 7. Products & Versions & Components
        var erp = new Product("ERP Desktop", "PRD-ERP", "Sistema ERP Desktop corporativo legado e moderno") { Id = NextProductId() };
        Products[erp.Id] = erp;
        var web = new Product("Portal Web", "PRD-WEB", "Portal de atendimento e autosserviço") { Id = NextProductId() };
        Products[web.Id] = web;
        var api = new Product("API Comercial", "PRD-API", "Gateway e serviços de integração comercial") { Id = NextProductId() };
        Products[api.Id] = api;

        var v1 = new ProductVersion(erp.Id, "v1.0.0") { Id = NextVersionId() };
        ProductVersions[v1.Id] = v1;
        var v2 = new ProductVersion(erp.Id, "v2.4.1") { Id = NextVersionId() };
        ProductVersions[v2.Id] = v2;
        var v3 = new ProductVersion(web.Id, "v3.0.0") { Id = NextVersionId() };
        ProductVersions[v3.Id] = v3;

        var c1 = new ComponentEntity("Módulo Financeiro", "Desktop", erp.Id, "MOD-FIN") { Id = NextCompId() };
        Components[c1.Id] = c1;
        var c2 = new ComponentEntity("Faturamento", "Desktop", erp.Id, "MOD-FAT") { Id = NextCompId() };
        Components[c2.Id] = c2;
        var c3 = new ComponentEntity("Autenticação Web", "Web", web.Id, "MOD-AUTH-WEB") { Id = NextCompId() };
        Components[c3.Id] = c3;
        var c4 = new ComponentEntity("Gateway de Vendas", "API", api.Id, "API-GATEWAY") { Id = NextCompId() };
        Components[c4.Id] = c4;

        // 8. Technologies & Tags
        var techs = new[] { "Kubernetes", "Kafka", "Redis", "PostgreSQL", "MySQL", ".NET Core", "Docker", "RabbitMQ" };
        foreach (var t in techs)
        {
            var tech = new Technology(t) { Id = NextTechnologyId() };
            Technologies[tech.Id] = tech;
        }

        var tagList = new[] { "deadlock", "connection-pool", "timeout", "latency", "cache", "memory-leak", "mtls", "sre" };
        foreach (var tg in tagList)
        {
            var tag = new Tag(tg) { Id = NextTagId() };
            Tags[tag.Id] = tag;
        }
    }
}

public class InMemoryUserRepository : IUserRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryUserRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task<User?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        _store.Users.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var norm = email.Trim().ToLowerInvariant();
        var user = _store.Users.Values.FirstOrDefault(u => u.Email.Equals(norm, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user);
    }

    public Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default)
    {
        IReadOnlyList<User> list = _store.Users.Values.OrderBy(u => u.Name).ToList();
        return Task.FromResult(list);
    }

    public Task<long> AddAsync(User user, CancellationToken ct = default)
    {
        user.Id = _store.NextUserId();
        _store.Users[user.Id] = user;
        return Task.FromResult(user.Id);
    }

    public Task UpdateAsync(User user, CancellationToken ct = default)
    {
        _store.Users[user.Id] = user;
        return Task.CompletedTask;
    }

    public Task<bool> ExistsByEmailAsync(string email, long? excludeUserId = null, CancellationToken ct = default)
    {
        var norm = email.Trim().ToLowerInvariant();
        var exists = _store.Users.Values.Any(u => u.Email.Equals(norm, StringComparison.OrdinalIgnoreCase) && (!excludeUserId.HasValue || u.Id != excludeUserId.Value));
        return Task.FromResult(exists);
    }

    public Task<IReadOnlyList<Department>> GetUserDepartmentsAsync(long userId, CancellationToken ct = default)
    {
        lock (_store.UserDepartments)
        {
            var deptIds = _store.UserDepartments.Where(ud => ud.UserId == userId).Select(ud => ud.DepartmentId).ToList();
            IReadOnlyList<Department> list = _store.Departments.Values.Where(d => deptIds.Contains(d.Id)).ToList();
            return Task.FromResult(list);
        }
    }

    public Task SetUserDepartmentsAsync(long userId, IEnumerable<long> departmentIds, CancellationToken ct = default)
    {
        lock (_store.UserDepartments)
        {
            _store.UserDepartments.RemoveAll(ud => ud.UserId == userId);
            foreach (var dId in departmentIds.Distinct())
            {
                _store.UserDepartments.Add(new UserDepartment(userId, dId));
            }
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Role>> GetUserRolesAsync(long userId, CancellationToken ct = default)
    {
        lock (_store.UserRoles)
        {
            var roleIds = _store.UserRoles.Where(ur => ur.UserId == userId).Select(ur => ur.RoleId).ToList();
            IReadOnlyList<Role> list = _store.Roles.Values.Where(r => roleIds.Contains(r.Id)).ToList();
            return Task.FromResult(list);
        }
    }

    public Task SetUserRolesAsync(long userId, IEnumerable<long> roleIds, CancellationToken ct = default)
    {
        lock (_store.UserRoles)
        {
            _store.UserRoles.RemoveAll(ur => ur.UserId == userId);
            foreach (var rId in roleIds.Distinct())
            {
                _store.UserRoles.Add(new UserRole(userId, rId));
            }
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> GetUserEffectivePermissionCodesAsync(long userId, CancellationToken ct = default)
    {
        lock (_store.UserRoles)
        lock (_store.RolePermissions)
        {
            var roleIds = _store.UserRoles.Where(ur => ur.UserId == userId).Select(ur => ur.RoleId).ToList();
            var permIds = _store.RolePermissions.Where(rp => roleIds.Contains(rp.RoleId)).Select(rp => rp.PermissionId).Distinct().ToList();
            IReadOnlyList<string> codes = _store.Permissions.Values.Where(p => permIds.Contains(p.Id)).Select(p => p.Code).Distinct().ToList();
            return Task.FromResult(codes);
        }
    }

    public Task<int> CountActiveUsersWithRoleAsync(long roleId, CancellationToken ct = default)
    {
        lock (_store.UserRoles)
        {
            var count = _store.Users.Values
                .Where(u => u.Status == UserStatus.Active)
                .Count(u => _store.UserRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == roleId));
            return Task.FromResult(count);
        }
    }
}

public class InMemoryDepartmentRepository : IDepartmentRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryDepartmentRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task<Department?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        _store.Departments.TryGetValue(id, out var dept);
        return Task.FromResult(dept);
    }

    public Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken ct = default)
    {
        IReadOnlyList<Department> list = _store.Departments.Values.OrderBy(d => d.Name).ToList();
        return Task.FromResult(list);
    }

    public Task<long> AddAsync(Department department, CancellationToken ct = default)
    {
        department.Id = _store.NextDeptId();
        _store.Departments[department.Id] = department;
        return Task.FromResult(department.Id);
    }

    public Task UpdateAsync(Department department, CancellationToken ct = default)
    {
        _store.Departments[department.Id] = department;
        return Task.CompletedTask;
    }

    public Task<bool> ExistsByNameAsync(string name, long? excludeId = null, CancellationToken ct = default)
    {
        var norm = name.Trim();
        var exists = _store.Departments.Values.Any(d => d.Name.Equals(norm, StringComparison.OrdinalIgnoreCase) && (!excludeId.HasValue || d.Id != excludeId.Value));
        return Task.FromResult(exists);
    }
}

public class InMemoryRoleRepository : IRoleRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryRoleRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task<Role?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        _store.Roles.TryGetValue(id, out var role);
        return Task.FromResult(role);
    }

    public Task<Role?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var norm = name.Trim();
        var role = _store.Roles.Values.FirstOrDefault(r => r.Name.Equals(norm, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(role);
    }

    public Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken ct = default)
    {
        IReadOnlyList<Role> list = _store.Roles.Values.OrderBy(r => r.Name).ToList();
        return Task.FromResult(list);
    }

    public Task<long> AddAsync(Role role, CancellationToken ct = default)
    {
        role.Id = _store.NextRoleId();
        _store.Roles[role.Id] = role;
        return Task.FromResult(role.Id);
    }

    public Task<IReadOnlyList<Permission>> GetRolePermissionsAsync(long roleId, CancellationToken ct = default)
    {
        lock (_store.RolePermissions)
        {
            var permIds = _store.RolePermissions.Where(rp => rp.RoleId == roleId).Select(rp => rp.PermissionId).ToList();
            IReadOnlyList<Permission> list = _store.Permissions.Values.Where(p => permIds.Contains(p.Id)).ToList();
            return Task.FromResult(list);
        }
    }

    public Task SetRolePermissionsAsync(long roleId, IEnumerable<long> permissionIds, CancellationToken ct = default)
    {
        lock (_store.RolePermissions)
        {
            _store.RolePermissions.RemoveAll(rp => rp.RoleId == roleId);
            foreach (var pId in permissionIds.Distinct())
            {
                _store.RolePermissions.Add(new RolePermission(roleId, pId));
            }
        }
        return Task.CompletedTask;
    }
}

public class InMemoryPermissionRepository : IPermissionRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryPermissionRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task<Permission?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        _store.Permissions.TryGetValue(id, out var perm);
        return Task.FromResult(perm);
    }

    public Task<Permission?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var norm = code.Trim().ToLowerInvariant();
        var perm = _store.Permissions.Values.FirstOrDefault(p => p.Code.Equals(norm, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(perm);
    }

    public Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken ct = default)
    {
        IReadOnlyList<Permission> list = _store.Permissions.Values.OrderBy(p => p.Code).ToList();
        return Task.FromResult(list);
    }

    public Task<long> AddAsync(Permission permission, CancellationToken ct = default)
    {
        permission.Id = _store.NextPermId();
        _store.Permissions[permission.Id] = permission;
        return Task.FromResult(permission.Id);
    }
}

public class InMemoryUserSessionRepository : IUserSessionRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryUserSessionRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task<UserSession?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        _store.Sessions.TryGetValue(id, out var s);
        return Task.FromResult(s);
    }

    public Task<long> AddAsync(UserSession session, CancellationToken ct = default)
    {
        session.Id = _store.NextSessionId();
        _store.Sessions[session.Id] = session;
        return Task.FromResult(session.Id);
    }

    public Task RevokeAsync(long id, CancellationToken ct = default)
    {
        if (_store.Sessions.TryGetValue(id, out var session))
        {
            session.Revoke();
        }
        return Task.CompletedTask;
    }

    public Task RevokeAllForUserAsync(long userId, CancellationToken ct = default)
    {
        foreach (var session in _store.Sessions.Values.Where(s => s.UserId == userId && s.IsActive))
        {
            session.Revoke();
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<UserSession>> GetActiveSessionsByUserIdAsync(long userId, CancellationToken ct = default)
    {
        IReadOnlyList<UserSession> list = _store.Sessions.Values.Where(s => s.UserId == userId && s.IsActive).ToList();
        return Task.FromResult(list);
    }
}

public class InMemoryPasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryPasswordResetTokenRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
    {
        var token = _store.ResetTokens.Values.FirstOrDefault(t => t.TokenHash == tokenHash);
        return Task.FromResult(token);
    }

    public Task<long> AddAsync(PasswordResetToken token, CancellationToken ct = default)
    {
        token.Id = _store.NextTokenId();
        _store.ResetTokens[token.Id] = token;
        return Task.FromResult(token.Id);
    }

    public Task MarkAsUsedAsync(long id, CancellationToken ct = default)
    {
        if (_store.ResetTokens.TryGetValue(id, out var token))
        {
            token.MarkAsUsed();
        }
        return Task.CompletedTask;
    }

    public Task InvalidateAllForUserAsync(long userId, CancellationToken ct = default)
    {
        foreach (var token in _store.ResetTokens.Values.Where(t => t.UserId == userId && t.UsedAt == null))
        {
            token.MarkAsUsed();
        }
        return Task.CompletedTask;
    }
}

public class InMemoryAuditEventRepository : IAuditEventRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryAuditEventRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task<long> AddAsync(AuditEvent auditEvent, CancellationToken ct = default)
    {
        auditEvent.Id = _store.NextAuditId();
        _store.AuditEvents[auditEvent.Id] = auditEvent;
        return Task.FromResult(auditEvent.Id);
    }

    public Task<IReadOnlyList<AuditEvent>> GetRecentAsync(int limit = 50, CancellationToken ct = default)
    {
        IReadOnlyList<AuditEvent> list = _store.AuditEvents.Values
            .OrderByDescending(a => a.OccurredAt)
            .Take(limit)
            .ToList();
        return Task.FromResult(list);
    }

    public Task<IReadOnlyList<AuditEvent>> GetByEntityAsync(string entityType, string entityId, CancellationToken ct = default)
    {
        IReadOnlyList<AuditEvent> list = _store.AuditEvents.Values
            .Where(a => a.EntityType.Equals(entityType, StringComparison.OrdinalIgnoreCase) && a.EntityId == entityId)
            .OrderByDescending(a => a.OccurredAt)
            .ToList();
        return Task.FromResult(list);
    }
}

public class InMemoryClientRepository : IClientRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryClientRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task<Client?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        _store.Clients.TryGetValue(id, out var client);
        return Task.FromResult(client);
    }

    public Task<Client?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var client = _store.Clients.Values.FirstOrDefault(c => string.Equals(c.Code, code, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(client);
    }

    public Task<Client?> GetByExternalCrmIdAsync(string externalCrmId, CancellationToken ct = default)
    {
        var client = _store.Clients.Values.FirstOrDefault(c => string.Equals(c.ExternalCrmId, externalCrmId, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(client);
    }

    public Task<IReadOnlyList<Client>> GetAllAsync(CancellationToken ct = default)
    {
        IReadOnlyList<Client> list = _store.Clients.Values.OrderBy(c => c.Name).ToList();
        return Task.FromResult(list);
    }

    public Task<long> AddAsync(Client client, CancellationToken ct = default)
    {
        client.Id = _store.NextClientId();
        _store.Clients[client.Id] = client;
        return Task.FromResult(client.Id);
    }

    public Task UpdateAsync(Client client, CancellationToken ct = default)
    {
        _store.Clients[client.Id] = client;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ClientUnit>> GetUnitsByClientIdAsync(long clientId, CancellationToken ct = default)
    {
        IReadOnlyList<ClientUnit> list = _store.ClientUnits.Values
            .Where(u => u.ClientId == clientId)
            .OrderBy(u => u.Name)
            .ToList();
        return Task.FromResult(list);
    }

    public Task<ClientUnit?> GetUnitByIdAsync(long unitId, CancellationToken ct = default)
    {
        _store.ClientUnits.TryGetValue(unitId, out var unit);
        return Task.FromResult(unit);
    }

    public Task<long> AddUnitAsync(ClientUnit unit, CancellationToken ct = default)
    {
        unit.Id = _store.NextClientUnitId();
        _store.ClientUnits[unit.Id] = unit;
        return Task.FromResult(unit.Id);
    }

    public Task UpdateUnitAsync(ClientUnit unit, CancellationToken ct = default)
    {
        _store.ClientUnits[unit.Id] = unit;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ClientTechnicalContext>> GetTechnicalContextsByClientIdAsync(long clientId, CancellationToken ct = default)
    {
        IReadOnlyList<ClientTechnicalContext> list = _store.ClientTechnicalContexts.Values
            .Where(c => c.ClientId == clientId)
            .OrderByDescending(c => c.EffectiveFrom)
            .ToList();
        return Task.FromResult(list);
    }

    public Task<ClientTechnicalContext?> GetTechnicalContextByIdAsync(long contextId, CancellationToken ct = default)
    {
        _store.ClientTechnicalContexts.TryGetValue(contextId, out var ctx);
        return Task.FromResult(ctx);
    }

    public Task<long> AddTechnicalContextAsync(ClientTechnicalContext context, CancellationToken ct = default)
    {
        context.Id = _store.NextClientTechnicalContextId();
        _store.ClientTechnicalContexts[context.Id] = context;
        return Task.FromResult(context.Id);
    }

    public Task UpdateTechnicalContextAsync(ClientTechnicalContext context, CancellationToken ct = default)
    {
        _store.ClientTechnicalContexts[context.Id] = context;
        return Task.CompletedTask;
    }
}

public class InMemoryCatalogRepository : ICatalogRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryCatalogRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task<IReadOnlyList<Product>> GetAllProductsAsync(CancellationToken ct = default)
    {
        IReadOnlyList<Product> list = _store.Products.Values.OrderBy(p => p.Name).ToList();
        return Task.FromResult(list);
    }

    public Task<IReadOnlyList<ProductVersion>> GetVersionsByProductIdAsync(long productId, CancellationToken ct = default)
    {
        IReadOnlyList<ProductVersion> list = _store.ProductVersions.Values
            .Where(v => v.ProductId == productId)
            .OrderByDescending(v => v.VersionLabel)
            .ToList();
        return Task.FromResult(list);
    }

    public Task<IReadOnlyList<EnvironmentEntity>> GetAllEnvironmentsAsync(CancellationToken ct = default)
    {
        IReadOnlyList<EnvironmentEntity> list = _store.Environments.Values.OrderBy(e => e.Name).ToList();
        return Task.FromResult(list);
    }

    public Task<IReadOnlyList<ComponentEntity>> GetAllComponentsAsync(long? productId = null, CancellationToken ct = default)
    {
        var query = _store.Components.Values.AsEnumerable();
        if (productId.HasValue)
        {
            query = query.Where(c => c.ProductId == productId.Value);
        }
        IReadOnlyList<ComponentEntity> list = query.OrderBy(c => c.Name).ToList();
        return Task.FromResult(list);
    }

    public Task<Product?> GetProductByIdAsync(long id, CancellationToken ct = default)
    {
        _store.Products.TryGetValue(id, out var p);
        return Task.FromResult(p);
    }

    public Task<ProductVersion?> GetProductVersionByIdAsync(long id, CancellationToken ct = default)
    {
        _store.ProductVersions.TryGetValue(id, out var pv);
        return Task.FromResult(pv);
    }

    public Task<EnvironmentEntity?> GetEnvironmentByIdAsync(long id, CancellationToken ct = default)
    {
        _store.Environments.TryGetValue(id, out var env);
        return Task.FromResult(env);
    }

    public Task<ComponentEntity?> GetComponentByIdAsync(long id, CancellationToken ct = default)
    {
        _store.Components.TryGetValue(id, out var comp);
        return Task.FromResult(comp);
    }

    public Task<long> AddProductAsync(Product product, CancellationToken ct = default)
    {
        product.Id = _store.NextProductId();
        _store.Products[product.Id] = product;
        return Task.FromResult(product.Id);
    }

    public Task<long> AddProductVersionAsync(ProductVersion version, CancellationToken ct = default)
    {
        version.Id = _store.NextVersionId();
        _store.ProductVersions[version.Id] = version;
        return Task.FromResult(version.Id);
    }

    public Task<long> AddEnvironmentAsync(EnvironmentEntity environment, CancellationToken ct = default)
    {
        environment.Id = _store.NextEnvId();
        _store.Environments[environment.Id] = environment;
        return Task.FromResult(environment.Id);
    }

    public Task<long> AddComponentAsync(ComponentEntity component, CancellationToken ct = default)
    {
        component.Id = _store.NextCompId();
        _store.Components[component.Id] = component;
        return Task.FromResult(component.Id);
    }

    public Task UpdateProductAsync(Product product, CancellationToken ct = default)
    {
        product.UpdatedAt = DateTime.UtcNow;
        _store.Products[product.Id] = product;
        return Task.CompletedTask;
    }

    public Task UpdateComponentAsync(ComponentEntity component, CancellationToken ct = default)
    {
        component.UpdatedAt = DateTime.UtcNow;
        _store.Components[component.Id] = component;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ComponentDependency>> GetComponentDependenciesAsync(long? componentId = null, CancellationToken ct = default)
    {
        var query = _store.ComponentDependencies.Values.AsEnumerable();
        if (componentId.HasValue)
        {
            query = query.Where(d => d.SourceComponentId == componentId.Value || d.TargetComponentId == componentId.Value);
        }
        var list = query.Select(d =>
        {
            _store.Components.TryGetValue(d.SourceComponentId, out var sc);
            _store.Components.TryGetValue(d.TargetComponentId, out var tc);
            return new ComponentDependency
            {
                Id = d.Id,
                SourceComponentId = d.SourceComponentId,
                TargetComponentId = d.TargetComponentId,
                DependencyType = d.DependencyType,
                Criticality = d.Criticality,
                Description = d.Description,
                ValidFrom = d.ValidFrom,
                ValidTo = d.ValidTo,
                CreatedAt = d.CreatedAt,
                SourceComponentName = sc?.Name ?? $"Component #{d.SourceComponentId}",
                TargetComponentName = tc?.Name ?? $"Component #{d.TargetComponentId}"
            };
        }).OrderBy(d => d.SourceComponentName).ThenBy(d => d.TargetComponentName).ToList();

        return Task.FromResult<IReadOnlyList<ComponentDependency>>(list);
    }

    public Task<ComponentDependency?> GetComponentDependencyByIdAsync(long id, CancellationToken ct = default)
    {
        _store.ComponentDependencies.TryGetValue(id, out var d);
        if (d != null)
        {
            _store.Components.TryGetValue(d.SourceComponentId, out var sc);
            _store.Components.TryGetValue(d.TargetComponentId, out var tc);
            d.SourceComponentName = sc?.Name;
            d.TargetComponentName = tc?.Name;
        }
        return Task.FromResult(d);
    }

    public Task<long> AddComponentDependencyAsync(ComponentDependency dependency, CancellationToken ct = default)
    {
        dependency.Id = _store.NextComponentDependencyId();
        _store.ComponentDependencies[dependency.Id] = dependency;
        return Task.FromResult(dependency.Id);
    }

    public Task<bool> DeleteComponentDependencyAsync(long id, CancellationToken ct = default)
    {
        var removed = _store.ComponentDependencies.TryRemove(id, out _);
        return Task.FromResult(removed);
    }

    public Task<IReadOnlyList<ComponentOwner>> GetComponentOwnersAsync(long? componentId = null, CancellationToken ct = default)
    {
        var query = _store.ComponentOwners.Values.AsEnumerable();
        if (componentId.HasValue)
        {
            query = query.Where(o => o.ComponentId == componentId.Value);
        }
        var list = query.Select(o =>
        {
            _store.Components.TryGetValue(o.ComponentId, out var c);
            _store.Departments.TryGetValue(o.DepartmentId, out var d);
            return new ComponentOwner
            {
                Id = o.Id,
                ComponentId = o.ComponentId,
                DepartmentId = o.DepartmentId,
                OwnershipRole = o.OwnershipRole,
                ValidFrom = o.ValidFrom,
                ValidTo = o.ValidTo,
                CreatedAt = o.CreatedAt,
                ComponentName = c?.Name ?? $"Component #{o.ComponentId}",
                DepartmentName = d?.Name ?? $"Department #{o.DepartmentId}"
            };
        }).OrderBy(o => o.ComponentName).ThenBy(o => o.OwnershipRole).ToList();

        return Task.FromResult<IReadOnlyList<ComponentOwner>>(list);
    }

    public Task<long> AddComponentOwnerAsync(ComponentOwner owner, CancellationToken ct = default)
    {
        owner.Id = _store.NextComponentOwnerId();
        _store.ComponentOwners[owner.Id] = owner;
        return Task.FromResult(owner.Id);
    }

    public Task<bool> DeleteComponentOwnerAsync(long id, CancellationToken ct = default)
    {
        var removed = _store.ComponentOwners.TryRemove(id, out _);
        return Task.FromResult(removed);
    }
}

public class InMemoryAttachmentRepository : IAttachmentRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryAttachmentRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task<long> AddAsync(Attachment attachment, CancellationToken ct = default)
    {
        attachment.Id = _store.NextAttachmentId();
        _store.Attachments[attachment.Id] = attachment;
        return Task.FromResult(attachment.Id);
    }

    public Task<Attachment?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        _store.Attachments.TryGetValue(id, out var att);
        return Task.FromResult(att);
    }

    public Task<IReadOnlyList<Attachment>> GetByEntityAsync(string entityType, long entityId, CancellationToken ct = default)
    {
        IReadOnlyList<Attachment> list = _store.Attachments.Values
            .Where(a => string.Equals(a.EntityType, entityType, StringComparison.OrdinalIgnoreCase) && a.EntityId == entityId)
            .OrderBy(a => a.UploadedAt)
            .ToList();
        return Task.FromResult(list);
    }
}

public class InMemoryCaseRepository : ICaseRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryCaseRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task<ulong> NextCaseNumberAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_store.NextCaseNumber());
    }

    public Task<long> AddAsync(Case @case, CancellationToken ct = default)
    {
        @case.Id = _store.NextCaseId();
        if (@case.CaseNumber == 0)
        {
            @case.CaseNumber = _store.NextCaseNumber();
        }

        // Atribui CaseId e IDs para os sintomas e evidências
        for (int i = 0; i < @case.Symptoms.Count; i++)
        {
            @case.Symptoms[i].CaseId = @case.Id;
            @case.Symptoms[i].Id = i + 1;
        }

        for (int i = 0; i < @case.AffectedComponents.Count; i++)
        {
            @case.AffectedComponents[i].CaseId = @case.Id;
        }

        for (int i = 0; i < @case.Evidences.Count; i++)
        {
            @case.Evidences[i].CaseId = @case.Id;
            @case.Evidences[i].Id = i + 1;
        }

        // Bloco 7.A.3: Persiste iterações do caso
        if (@case.Iterations.Count == 0)
        {
            @case.Iterations.Add(new CaseIteration(@case.Id, 1, @case.CreatedBy ?? 1, "Abertura inicial do caso", @case.OpenedAt, "Open"));
        }

        foreach (var iter in @case.Iterations)
        {
            if (iter.Id == 0)
            {
                iter.Id = _store.NextCaseIterationId();
            }
            iter.CaseId = @case.Id;
            _store.CaseIterations[iter.Id] = iter;
        }

        _store.Cases[@case.Id] = @case;
        return Task.FromResult(@case.Id);
    }

    public Task<Case?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        _store.Cases.TryGetValue(id, out var @case);
        return Task.FromResult(@case);
    }

    public Task<Case?> GetByCaseNumberAsync(ulong caseNumber, CancellationToken ct = default)
    {
        var @case = _store.Cases.Values.FirstOrDefault(c => c.CaseNumber == caseNumber);
        return Task.FromResult(@case);
    }

    public Task<IReadOnlyList<Case>> GetAllAsync(int limit = 50, CancellationToken ct = default)
    {
        IReadOnlyList<Case> list = _store.Cases.Values
            .OrderByDescending(c => c.OpenedAt)
            .Take(limit)
            .ToList();
        return Task.FromResult(list);
    }

    public Task UpdateNormalizedSummaryAsync(long caseId, string? normalizedSummary, long? updatedBy, CancellationToken ct = default)
    {
        if (_store.Cases.TryGetValue(caseId, out var @case))
        {
            @case.UpdateNormalizedSummary(normalizedSummary, updatedBy);
        }
        return Task.CompletedTask;
    }

    public Task UpdateCaseResolutionStatusAsync(long caseId, string status, string rootCauseStatus, DateTime resolvedAt, long resolvedBy, CancellationToken ct = default)
    {
        if (_store.Cases.TryGetValue(caseId, out var @case))
        {
            @case.Resolve(rootCauseStatus, resolvedAt, resolvedBy);
        }
        return Task.CompletedTask;
    }

    public Task UpdateComponentRelationAsync(long caseId, long componentId, string relationType, CancellationToken ct = default)
    {
        if (_store.Cases.TryGetValue(caseId, out var @case))
        {
            @case.MarkComponentAsRootCause(componentId);
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CaseIteration>> GetIterationsByCaseIdAsync(long caseId, CancellationToken ct = default)
    {
        IReadOnlyList<CaseIteration> list = _store.CaseIterations.Values
            .Where(i => i.CaseId == caseId)
            .OrderBy(i => i.SequenceNumber)
            .ToList();
        return Task.FromResult(list);
    }

    public Task<CaseIteration?> GetCurrentIterationAsync(long caseId, CancellationToken ct = default)
    {
        var iter = _store.CaseIterations.Values
            .Where(i => i.CaseId == caseId)
            .OrderByDescending(i => i.SequenceNumber)
            .FirstOrDefault();
        return Task.FromResult(iter);
    }

    public Task<long> AddIterationAsync(CaseIteration iteration, CancellationToken ct = default)
    {
        iteration.Id = _store.NextCaseIterationId();
        _store.CaseIterations[iteration.Id] = iteration;

        if (_store.Cases.TryGetValue(iteration.CaseId, out var @case))
        {
            if (!@case.Iterations.Any(i => i.Id == iteration.Id))
            {
                @case.Iterations.Add(iteration);
            }
        }

        return Task.FromResult(iteration.Id);
    }

    public Task UpdateIterationStatusAsync(long iterationId, string status, DateTime? closedAt, CancellationToken ct = default)
    {
        if (_store.CaseIterations.TryGetValue(iterationId, out var iter))
        {
            iter.Status = status;
            iter.ClosedAt = closedAt;

            if (_store.Cases.TryGetValue(iter.CaseId, out var @case))
            {
                var caseIter = @case.Iterations.FirstOrDefault(i => i.Id == iterationId);
                if (caseIter != null)
                {
                    caseIter.Status = status;
                    caseIter.ClosedAt = closedAt;
                }
            }
        }
        return Task.CompletedTask;
    }

    public Task UpdateCaseReopenStatusAsync(long caseId, string status, long reopenedBy, CancellationToken ct = default)
    {
        if (_store.Cases.TryGetValue(caseId, out var @case))
        {
            @case.Status = status;
            @case.ResolvedAt = null;
            @case.UpdatedAt = DateTime.UtcNow;
            @case.UpdatedBy = reopenedBy;
            @case.RowVersion++;
        }
        return Task.CompletedTask;
    }
}

public class InMemoryDiagnosticRepository : IDiagnosticRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryDiagnosticRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task<DiagnosticSession?> GetOpenSessionByCaseIdAsync(long caseId, CancellationToken ct = default)
    {
        var session = _store.DiagnosticSessions.Values
            .Where(s => s.CaseId == caseId && s.Status == "Open")
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefault();
        return Task.FromResult(session);
    }

    public Task<long> CreateSessionAsync(DiagnosticSession session, CancellationToken ct = default)
    {
        session.Id = _store.NextDiagnosticSessionId();
        _store.DiagnosticSessions[session.Id] = session;
        return Task.FromResult(session.Id);
    }

    public Task<DiagnosticSession?> GetSessionByIdAsync(long sessionId, CancellationToken ct = default)
    {
        _store.DiagnosticSessions.TryGetValue(sessionId, out var session);
        return Task.FromResult(session);
    }

    public Task<long> AddHypothesisAsync(CaseHypothesis hypothesis, CancellationToken ct = default)
    {
        hypothesis.Id = _store.NextCaseHypothesisId();
        _store.CaseHypotheses[hypothesis.Id] = hypothesis;
        return Task.FromResult(hypothesis.Id);
    }

    public Task<CaseHypothesis?> GetHypothesisByIdAsync(long hypothesisId, CancellationToken ct = default)
    {
        _store.CaseHypotheses.TryGetValue(hypothesisId, out var hypothesis);
        return Task.FromResult(hypothesis);
    }

    public Task<IReadOnlyList<CaseHypothesis>> GetHypothesesByCaseIdAsync(long caseId, CancellationToken ct = default)
    {
        IReadOnlyList<CaseHypothesis> list = _store.CaseHypotheses.Values
            .Where(h => h.CaseId == caseId)
            .OrderBy(h => h.CreatedAt)
            .ThenBy(h => h.Id)
            .ToList();
        return Task.FromResult(list);
    }

    public Task UpdateHypothesisAsync(CaseHypothesis hypothesis, CancellationToken ct = default)
    {
        _store.CaseHypotheses[hypothesis.Id] = hypothesis;
        return Task.CompletedTask;
    }

    public Task<int> GetNextStepSequenceNoAsync(long sessionId, CancellationToken ct = default)
    {
        var steps = _store.DiagnosticSteps.Values.Where(s => s.DiagnosticSessionId == sessionId);
        var nextSeq = steps.Any() ? steps.Max(s => s.SequenceNo) + 1 : 1;
        return Task.FromResult(nextSeq);
    }

    public Task<long> AddStepAsync(DiagnosticStep step, CancellationToken ct = default)
    {
        step.Id = _store.NextDiagnosticStepId();
        _store.DiagnosticSteps[step.Id] = step;
        return Task.FromResult(step.Id);
    }

    public Task<DiagnosticStep?> GetStepByIdAsync(long stepId, CancellationToken ct = default)
    {
        _store.DiagnosticSteps.TryGetValue(stepId, out var step);
        return Task.FromResult(step);
    }

    public Task<IReadOnlyList<DiagnosticStep>> GetStepsBySessionIdAsync(long sessionId, CancellationToken ct = default)
    {
        IReadOnlyList<DiagnosticStep> list = _store.DiagnosticSteps.Values
            .Where(s => s.DiagnosticSessionId == sessionId)
            .OrderBy(s => s.SequenceNo)
            .ToList();
        return Task.FromResult(list);
    }

    public Task<IReadOnlyList<DiagnosticStep>> GetStepsByCaseIdAsync(long caseId, CancellationToken ct = default)
    {
        var sessionIds = _store.DiagnosticSessions.Values
            .Where(s => s.CaseId == caseId)
            .Select(s => s.Id)
            .ToHashSet();

        IReadOnlyList<DiagnosticStep> list = _store.DiagnosticSteps.Values
            .Where(s => sessionIds.Contains(s.DiagnosticSessionId))
            .OrderBy(s => s.PerformedAt)
            .ThenBy(s => s.SequenceNo)
            .ToList();
        return Task.FromResult(list);
    }

    public Task<IReadOnlyList<DiagnosticStep>> GetStepsByHypothesisIdAsync(long hypothesisId, CancellationToken ct = default)
    {
        IReadOnlyList<DiagnosticStep> list = _store.DiagnosticSteps.Values
            .Where(s => s.HypothesisId == hypothesisId)
            .OrderBy(s => s.PerformedAt)
            .ThenBy(s => s.SequenceNo)
            .ToList();
        return Task.FromResult(list);
    }

    public Task<long> AddEvidenceAsync(CaseEvidence evidence, CancellationToken ct = default)
    {
        evidence.Id = _store.NextCaseEvidenceId();
        _store.CaseEvidences[evidence.Id] = evidence;
        return Task.FromResult(evidence.Id);
    }

    public Task<CaseEvidence?> GetEvidenceByIdAsync(long evidenceId, CancellationToken ct = default)
    {
        _store.CaseEvidences.TryGetValue(evidenceId, out var ev);
        if (ev != null)
        {
            ev.HypothesisRelations = _store.CaseHypothesisEvidences.Values
                .Where(r => r.EvidenceId == evidenceId)
                .ToList();
        }
        return Task.FromResult(ev);
    }

    public Task<IReadOnlyList<CaseEvidence>> GetEvidencesByCaseIdAsync(long caseId, CancellationToken ct = default)
    {
        var list = _store.CaseEvidences.Values
            .Where(e => e.CaseId == caseId)
            .OrderByDescending(e => e.CreatedAt)
            .ToList();

        foreach (var ev in list)
        {
            ev.HypothesisRelations = _store.CaseHypothesisEvidences.Values
                .Where(r => r.EvidenceId == ev.Id)
                .ToList();
        }

        return Task.FromResult<IReadOnlyList<CaseEvidence>>(list);
    }

    public Task<IReadOnlyList<CaseEvidence>> GetEvidencesByHypothesisIdAsync(long hypothesisId, CancellationToken ct = default)
    {
        var evidenceIds = _store.CaseHypothesisEvidences.Values
            .Where(r => r.HypothesisId == hypothesisId)
            .Select(r => r.EvidenceId)
            .ToHashSet();

        var list = _store.CaseEvidences.Values
            .Where(e => evidenceIds.Contains(e.Id))
            .OrderByDescending(e => e.CreatedAt)
            .ToList();

        foreach (var ev in list)
        {
            ev.HypothesisRelations = _store.CaseHypothesisEvidences.Values
                .Where(r => r.EvidenceId == ev.Id)
                .ToList();
        }

        return Task.FromResult<IReadOnlyList<CaseEvidence>>(list);
    }

    public Task AddHypothesisEvidenceRelationAsync(CaseHypothesisEvidence relation, CancellationToken ct = default)
    {
        var existing = _store.CaseHypothesisEvidences.Values
            .FirstOrDefault(r => r.EvidenceId == relation.EvidenceId && r.HypothesisId == relation.HypothesisId);

        if (existing != null)
        {
            existing.RelationType = relation.RelationType;
            existing.Justification = relation.Justification;
        }
        else
        {
            relation.Id = _store.NextCaseHypothesisEvidenceId();
            _store.CaseHypothesisEvidences[relation.Id] = relation;
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CaseHypothesisEvidence>> GetHypothesisRelationsByEvidenceIdAsync(long evidenceId, CancellationToken ct = default)
    {
        IReadOnlyList<CaseHypothesisEvidence> list = _store.CaseHypothesisEvidences.Values
            .Where(r => r.EvidenceId == evidenceId)
            .ToList();
        return Task.FromResult(list);
    }
}

public class InMemoryCaseResolutionRepository : ICaseResolutionRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryCaseResolutionRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task<long> AddResolutionAsync(CaseResolution resolution, CancellationToken ct = default)
    {
        resolution.Id = _store.NextCaseResolutionId();
        _store.CaseResolutions[resolution.Id] = resolution;
        return Task.FromResult(resolution.Id);
    }

    public Task<CaseResolution?> GetByCaseIdAsync(long caseId, CancellationToken ct = default)
    {
        var res = _store.CaseResolutions.Values
            .Where(r => r.CaseId == caseId)
            .OrderByDescending(r => r.Id)
            .FirstOrDefault();
        return Task.FromResult(res);
    }

    public Task<CaseResolution?> GetByIterationIdAsync(long iterationId, CancellationToken ct = default)
    {
        var res = _store.CaseResolutions.Values.FirstOrDefault(r => r.CaseIterationId == iterationId);
        return Task.FromResult(res);
    }

    public Task<IReadOnlyList<CaseResolution>> GetAllResolutionsByCaseIdAsync(long caseId, CancellationToken ct = default)
    {
        IReadOnlyList<CaseResolution> list = _store.CaseResolutions.Values
            .Where(r => r.CaseId == caseId)
            .OrderBy(r => r.Id)
            .ToList();
        return Task.FromResult(list);
    }

    public Task<CaseResolution?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        _store.CaseResolutions.TryGetValue(id, out var res);
        return Task.FromResult(res);
    }

    public Task<long> AddRootCauseAsync(RootCause rootCause, CancellationToken ct = default)
    {
        rootCause.Id = _store.NextRootCauseId();
        _store.RootCauses[rootCause.Id] = rootCause;
        return Task.FromResult(rootCause.Id);
    }

    public Task<RootCause?> GetRootCauseByIdAsync(long id, CancellationToken ct = default)
    {
        _store.RootCauses.TryGetValue(id, out var rc);
        return Task.FromResult(rc);
    }

    public Task<RootCause?> GetRootCauseByCodeAsync(string code, CancellationToken ct = default)
    {
        var rc = _store.RootCauses.Values.FirstOrDefault(r => string.Equals(r.Code, code, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(rc);
    }

    public Task<IReadOnlyList<RootCause>> GetAllRootCausesAsync(CancellationToken ct = default)
    {
        IReadOnlyList<RootCause> list = _store.RootCauses.Values
            .OrderBy(r => r.Name)
            .ToList();
        return Task.FromResult(list);
    }
}

public class InMemoryKnowledgeRepository : IKnowledgeRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryKnowledgeRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task<KnowledgeItem?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        _store.KnowledgeItems.TryGetValue(id, out var item);
        return Task.FromResult(item);
    }

    public Task<KnowledgeItem?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var item = _store.KnowledgeItems.Values.FirstOrDefault(k => string.Equals(k.KnowledgeCode, code, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(item);
    }

    public Task<long> CreateItemAsync(KnowledgeItem item, CancellationToken ct = default)
    {
        item.Id = _store.NextKnowledgeItemId();
        _store.KnowledgeItems[item.Id] = item;
        return Task.FromResult(item.Id);
    }

    public Task UpdateItemAsync(KnowledgeItem item, CancellationToken ct = default)
    {
        _store.KnowledgeItems[item.Id] = item;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<KnowledgeItem>> SearchAsync(
        string? search = null,
        long? productId = null,
        string? status = null,
        string? provenance = null,
        string? category = null,
        CancellationToken ct = default)
    {
        var query = _store.KnowledgeItems.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(k =>
                k.Title.ToLowerInvariant().Contains(term) ||
                k.Summary.ToLowerInvariant().Contains(term) ||
                k.KnowledgeCode.ToLowerInvariant().Contains(term));
        }

        if (productId.HasValue && productId.Value > 0)
        {
            var applicableItemIds = _store.KnowledgeApplicabilities.Values
                .Where(a => a.ProductId == productId.Value)
                .Select(a => a.KnowledgeItemId)
                .ToHashSet();

            query = query.Where(k => applicableItemIds.Contains(k.Id));
        }

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(k => string.Equals(k.Status, status, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(provenance) && !provenance.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(k => string.Equals(k.ProvenanceType, provenance, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(category) && !category.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(k => string.Equals(k.KnowledgeType, category, StringComparison.OrdinalIgnoreCase));
        }

        IReadOnlyList<KnowledgeItem> list = query.OrderByDescending(k => k.UpdatedAt).ToList();
        return Task.FromResult(list);
    }

    public Task<KnowledgeVersion?> GetVersionByIdAsync(long versionId, CancellationToken ct = default)
    {
        _store.KnowledgeVersions.TryGetValue(versionId, out var ver);
        return Task.FromResult(ver);
    }

    public Task<KnowledgeVersion?> GetVersionByItemAndNumberAsync(long itemId, int versionNo, CancellationToken ct = default)
    {
        var ver = _store.KnowledgeVersions.Values.FirstOrDefault(v => v.KnowledgeItemId == itemId && v.VersionNo == versionNo);
        return Task.FromResult(ver);
    }

    public Task<IReadOnlyList<KnowledgeVersion>> GetVersionsByItemIdAsync(long itemId, CancellationToken ct = default)
    {
        IReadOnlyList<KnowledgeVersion> list = _store.KnowledgeVersions.Values
            .Where(v => v.KnowledgeItemId == itemId)
            .OrderByDescending(v => v.VersionNo)
            .ToList();
        return Task.FromResult(list);
    }

    public Task<long> CreateVersionAsync(KnowledgeVersion version, CancellationToken ct = default)
    {
        version.Id = _store.NextKnowledgeVersionId();
        _store.KnowledgeVersions[version.Id] = version;
        return Task.FromResult(version.Id);
    }

    public Task UpdateVersionAsync(KnowledgeVersion version, CancellationToken ct = default)
    {
        _store.KnowledgeVersions[version.Id] = version;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<KnowledgeApplicability>> GetApplicabilitiesByItemIdAsync(long itemId, CancellationToken ct = default)
    {
        IReadOnlyList<KnowledgeApplicability> list = _store.KnowledgeApplicabilities.Values
            .Where(a => a.KnowledgeItemId == itemId)
            .ToList();
        return Task.FromResult(list);
    }

    public Task<long> AddApplicabilityAsync(KnowledgeApplicability applicability, CancellationToken ct = default)
    {
        applicability.Id = _store.NextKnowledgeApplicabilityId();
        _store.KnowledgeApplicabilities[applicability.Id] = applicability;
        return Task.FromResult(applicability.Id);
    }

    public Task RemoveApplicabilityAsync(long applicabilityId, CancellationToken ct = default)
    {
        _store.KnowledgeApplicabilities.TryRemove(applicabilityId, out _);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<KnowledgeStep>> GetStepsByVersionIdAsync(long versionId, CancellationToken ct = default)
    {
        IReadOnlyList<KnowledgeStep> list = _store.KnowledgeSteps.Values
            .Where(s => s.KnowledgeVersionId == versionId)
            .OrderBy(s => s.SequenceNo)
            .ToList();
        return Task.FromResult(list);
    }

    public Task AddStepsAsync(IEnumerable<KnowledgeStep> steps, CancellationToken ct = default)
    {
        foreach (var step in steps)
        {
            step.Id = _store.NextKnowledgeStepId();
            _store.KnowledgeSteps[step.Id] = step;
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<KnowledgeSymptom>> GetSymptomsByVersionIdAsync(long versionId, CancellationToken ct = default)
    {
        IReadOnlyList<KnowledgeSymptom> list = _store.KnowledgeSymptoms.Values
            .Where(s => s.KnowledgeVersionId == versionId)
            .ToList();
        return Task.FromResult(list);
    }

    public Task AddSymptomsAsync(IEnumerable<KnowledgeSymptom> symptoms, CancellationToken ct = default)
    {
        foreach (var s in symptoms)
        {
            s.Id = _store.NextKnowledgeSymptomId();
            _store.KnowledgeSymptoms[s.Id] = s;
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> GetTechnologiesByItemIdAsync(long itemId, CancellationToken ct = default)
    {
        lock (_store.KnowledgeTechnologies)
        {
            var techIds = _store.KnowledgeTechnologies.Where(kt => kt.KnowledgeItemId == itemId).Select(kt => kt.TechnologyId).ToHashSet();
            IReadOnlyList<string> list = _store.Technologies.Values
                .Where(t => techIds.Contains(t.Id))
                .Select(t => t.Name)
                .OrderBy(n => n)
                .ToList();
            return Task.FromResult(list);
        }
    }

    public Task SetTechnologiesAsync(long itemId, IEnumerable<string> technologyNames, CancellationToken ct = default)
    {
        lock (_store.KnowledgeTechnologies)
        {
            _store.KnowledgeTechnologies.RemoveAll(kt => kt.KnowledgeItemId == itemId);

            foreach (var name in technologyNames.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()))
            {
                var tech = _store.Technologies.Values.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
                if (tech == null)
                {
                    tech = new Technology(name) { Id = _store.NextTechnologyId() };
                    _store.Technologies[tech.Id] = tech;
                }
                _store.KnowledgeTechnologies.Add((itemId, tech.Id));
            }
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> GetTagsByItemIdAsync(long itemId, CancellationToken ct = default)
    {
        lock (_store.KnowledgeTags)
        {
            var tagIds = _store.KnowledgeTags.Where(kt => kt.KnowledgeItemId == itemId).Select(kt => kt.TagId).ToHashSet();
            IReadOnlyList<string> list = _store.Tags.Values
                .Where(t => tagIds.Contains(t.Id))
                .Select(t => t.Name)
                .OrderBy(n => n)
                .ToList();
            return Task.FromResult(list);
        }
    }

    public Task SetTagsAsync(long itemId, IEnumerable<string> tagNames, CancellationToken ct = default)
    {
        lock (_store.KnowledgeTags)
        {
            _store.KnowledgeTags.RemoveAll(kt => kt.KnowledgeItemId == itemId);

            foreach (var name in tagNames.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim().ToLowerInvariant()))
            {
                var tag = _store.Tags.Values.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
                if (tag == null)
                {
                    tag = new Tag(name) { Id = _store.NextTagId() };
                    _store.Tags[tag.Id] = tag;
                }
                _store.KnowledgeTags.Add((itemId, tag.Id));
            }
        }
        return Task.CompletedTask;
    }

    public Task<long> RecordUsageAsync(KnowledgeUsage usage, CancellationToken ct = default)
    {
        usage.Id = _store.NextKnowledgeUsageId();
        _store.KnowledgeUsages[usage.Id] = usage;
        return Task.FromResult(usage.Id);
    }

    public Task<IReadOnlyList<KnowledgeUsage>> GetUsagesByItemIdAsync(long itemId, CancellationToken ct = default)
    {
        IReadOnlyList<KnowledgeUsage> list = _store.KnowledgeUsages.Values
            .Where(u => u.KnowledgeItemId == itemId)
            .OrderByDescending(u => u.UsedAt)
            .ToList();
        return Task.FromResult(list);
    }

    public Task<(int TotalCount, int PublishedCount, int InReviewCount, int StaleCount)> GetDashboardCountsAsync(CancellationToken ct = default)
    {
        var items = _store.KnowledgeItems.Values;
        int total = items.Count;
        int published = items.Count(k => string.Equals(k.Status, "Published", StringComparison.OrdinalIgnoreCase));
        int review = items.Count(k => string.Equals(k.Status, "Review", StringComparison.OrdinalIgnoreCase));
        int stale = items.Count(k => k.ReviewDueAt.HasValue && k.ReviewDueAt.Value < DateTime.UtcNow);

        return Task.FromResult((total, published, review, stale));
    }
}

public class InMemorySearchRepository : ISearchRepository
{
    private readonly InMemoryDataStore _store;

    public InMemorySearchRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task<long> CreateSessionAsync(SearchSession session, CancellationToken ct = default)
    {
        lock (_store.SearchSessions)
        {
            session.Id = _store.NextSearchSessionId();
            _store.SearchSessions.Add(session);
            return Task.FromResult(session.Id);
        }
    }

    public Task<long> RecordQueryAsync(SearchQueryRecord queryRecord, CancellationToken ct = default)
    {
        lock (_store.SearchQueries)
        {
            queryRecord.Id = _store.NextSearchQueryId();
            _store.SearchQueries.Add(queryRecord);
            return Task.FromResult(queryRecord.Id);
        }
    }

    public Task<long> RecordInteractionAsync(SearchResultInteraction interaction, CancellationToken ct = default)
    {
        lock (_store.SearchResultInteractions)
        {
            interaction.Id = _store.NextSearchResultInteractionId();
            _store.SearchResultInteractions.Add(interaction);
            return Task.FromResult(interaction.Id);
        }
    }

    public Task MarkInteractionOpenedAsync(long interactionId, DateTime openedAt, CancellationToken ct = default)
    {
        lock (_store.SearchResultInteractions)
        {
            var item = _store.SearchResultInteractions.FirstOrDefault(i => i.Id == interactionId);
            if (item != null)
            {
                item.MarkOpened(openedAt);
            }
        }
        return Task.CompletedTask;
    }

    public Task RecordFeedbackAsync(long interactionId, bool useful, CancellationToken ct = default)
    {
        lock (_store.SearchResultInteractions)
        {
            var item = _store.SearchResultInteractions.FirstOrDefault(i => i.Id == interactionId);
            if (item != null)
            {
                item.SetFeedback(useful);
            }
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CaseSearchRawResult>> SearchCasesAsync(string query, SearchFilterCriteriaDb filters, int limit = 50, CancellationToken ct = default)
    {
        var term = query?.Trim() ?? string.Empty;
        var cases = _store.Cases.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(term))
        {
            cases = cases.Where(c =>
                c.CaseNumber.ToString().Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (c.NormalizedSummary?.Contains(term, StringComparison.OrdinalIgnoreCase) == true) ||
                c.OriginalReport.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (c.ErrorCode?.Contains(term, StringComparison.OrdinalIgnoreCase) == true) ||
                (c.ErrorMessage?.Contains(term, StringComparison.OrdinalIgnoreCase) == true));
        }

        if (filters.ProductId.HasValue) cases = cases.Where(c => c.ProductId == filters.ProductId.Value);
        if (filters.ProductVersionId.HasValue) cases = cases.Where(c => c.ProductVersionId == filters.ProductVersionId.Value);
        if (filters.ClientId.HasValue) cases = cases.Where(c => c.ClientId == filters.ClientId.Value);
        if (filters.ClientUnitId.HasValue) cases = cases.Where(c => c.ClientUnitId == filters.ClientUnitId.Value);
        if (filters.DepartmentId.HasValue) cases = cases.Where(c => c.CurrentDepartmentId == filters.DepartmentId.Value);
        if (filters.ComponentId.HasValue) cases = cases.Where(c => c.AffectedComponents.Any(ac => ac.ComponentId == filters.ComponentId.Value));
        if (!string.IsNullOrWhiteSpace(filters.ErrorCode)) cases = cases.Where(c => string.Equals(c.ErrorCode, filters.ErrorCode, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(filters.Status)) cases = cases.Where(c => string.Equals(c.Status.ToString(), filters.Status, StringComparison.OrdinalIgnoreCase));
        if (filters.StartDate.HasValue) cases = cases.Where(c => c.OpenedAt >= filters.StartDate.Value);
        if (filters.EndDate.HasValue) cases = cases.Where(c => c.OpenedAt <= filters.EndDate.Value);

        var list = cases
            .Select(c =>
            {
                double score = 50.0;
                if (!string.IsNullOrWhiteSpace(term))
                {
                    if (c.CaseNumber.ToString().Contains(term, StringComparison.OrdinalIgnoreCase)) score = 90.0;
                    else if (c.ErrorCode?.Contains(term, StringComparison.OrdinalIgnoreCase) == true) score = 80.0;
                    else if (c.NormalizedSummary?.Contains(term, StringComparison.OrdinalIgnoreCase) == true) score = 70.0;
                    else score = 60.0;
                }

                _store.Products.TryGetValue(c.ProductId ?? 0, out var prod);
                _store.ProductVersions.TryGetValue(c.ProductVersionId ?? 0, out var ver);
                _store.Clients.TryGetValue(c.ClientId ?? 0, out var client);
                _store.ClientUnits.TryGetValue(c.ClientUnitId ?? 0, out var clientUnit);
                _store.Departments.TryGetValue(c.CurrentDepartmentId ?? 0, out var dept);

                var compId = c.AffectedComponents.FirstOrDefault()?.ComponentId;
                string? compName = null;
                if (compId.HasValue && _store.Components.TryGetValue(compId.Value, out var comp))
                {
                    compName = comp.Name;
                }

                return new CaseSearchRawResult
                {
                    Id = c.Id,
                    CaseNumber = c.CaseNumber.ToString(),
                    NormalizedSummary = c.NormalizedSummary,
                    OriginalReport = c.OriginalReport,
                    Status = c.Status.ToString(),
                    Severity = c.Severity.ToString(),
                    ErrorCode = c.ErrorCode,
                    ErrorMessage = c.ErrorMessage,
                    ProductId = c.ProductId,
                    ProductName = prod?.Name,
                    ProductVersionId = c.ProductVersionId,
                    VersionName = ver?.VersionLabel,
                    ComponentId = compId,
                    ComponentName = compName,
                    ClientId = c.ClientId,
                    ClientName = client?.Name,
                    ClientUnitId = c.ClientUnitId,
                    ClientUnitName = clientUnit?.Name,
                    DepartmentId = c.CurrentDepartmentId,
                    DepartmentName = dept?.Name,
                    OpenedAt = c.OpenedAt,
                    TextScore = score
                };
            })
            .OrderByDescending(c => c.TextScore)
            .ThenByDescending(c => c.OpenedAt)
            .Take(limit)
            .ToList();

        return Task.FromResult<IReadOnlyList<CaseSearchRawResult>>(list);
    }

    public Task<IReadOnlyList<SolutionSearchRawResult>> SearchSolutionsAsync(string query, SearchFilterCriteriaDb filters, bool allowDrafts = false, int limit = 50, CancellationToken ct = default)
    {
        var term = query?.Trim() ?? string.Empty;
        var items = _store.KnowledgeItems.Values.AsEnumerable();

        if (!allowDrafts)
        {
            items = items.Where(k => string.Equals(k.Status, "Published", StringComparison.OrdinalIgnoreCase));
        }
        else if (!string.IsNullOrWhiteSpace(filters.Status))
        {
            items = items.Where(k => string.Equals(k.Status, filters.Status, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            items = items.Where(k => !string.Equals(k.Status, "Deprecated", StringComparison.OrdinalIgnoreCase));
        }

        if (filters.DepartmentId.HasValue)
        {
            items = items.Where(k => k.OwnerDepartmentId == filters.DepartmentId.Value);
        }

        if (filters.TechnologyId.HasValue)
        {
            var matchingItemIds = _store.KnowledgeTechnologies
                .Where(kt => kt.TechnologyId == filters.TechnologyId.Value)
                .Select(kt => kt.KnowledgeItemId)
                .ToHashSet();
            items = items.Where(k => matchingItemIds.Contains(k.Id));
        }

        var results = new List<SolutionSearchRawResult>();

        foreach (var ki in items)
        {
            var kv = _store.KnowledgeVersions.Values.FirstOrDefault(v => v.KnowledgeItemId == ki.Id && v.VersionNo == ki.CurrentVersionNo);

            if (!string.IsNullOrWhiteSpace(term))
            {
                bool match = ki.KnowledgeCode.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                             ki.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                             ki.Summary.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                             (kv?.ProblemDescription?.Contains(term, StringComparison.OrdinalIgnoreCase) == true) ||
                             (kv?.ContentMarkdown?.Contains(term, StringComparison.OrdinalIgnoreCase) == true);
                if (!match) continue;
            }

            double score = 50.0;
            if (!string.IsNullOrWhiteSpace(term))
            {
                if (ki.KnowledgeCode.Contains(term, StringComparison.OrdinalIgnoreCase)) score = 90.0;
                else if (ki.Title.Contains(term, StringComparison.OrdinalIgnoreCase)) score = 75.0;
                else if (ki.Summary.Contains(term, StringComparison.OrdinalIgnoreCase)) score = 65.0;
                else score = 55.0;
            }

            var apps = _store.KnowledgeApplicabilities.Values.Where(a => a.KnowledgeItemId == ki.Id).ToList();
            var appProds = apps.Where(a => a.ProductId.HasValue).Select(a => a.ProductId!.Value).Distinct().ToList();
            var appComps = apps.Where(a => a.ComponentId.HasValue).Select(a => a.ComponentId!.Value).Distinct().ToList();
            var negativeVersions = apps.Where(a => string.Equals(a.ApplicabilityType, "DoesNotApply", StringComparison.OrdinalIgnoreCase) && a.ProductVersionId.HasValue)
                                       .Select(a => a.ProductVersionId!.Value).Distinct().ToList();
            // Bloco 7.A (§11): removido mock fixo ["Produção", "Homologação"] — calcula de
            // knowledge_applicability.environment_id de verdade, igual a appProds/appComps.
            var appEnvironments = apps.Where(a => a.EnvironmentId.HasValue)
                                       .Select(a => a.EnvironmentId!.Value)
                                       .Distinct()
                                       .Select(id => _store.Environments.TryGetValue(id, out var env) ? env.Name : null)
                                       .Where(name => name != null)
                                       .Select(name => name!)
                                       .ToList();

            var usages = _store.KnowledgeUsages.Values.Where(u => u.KnowledgeItemId == ki.Id).ToList();
            int totalUsages = usages.Count;
            int successfulUsages = usages.Count(u => string.Equals(u.Outcome, "Worked", StringComparison.OrdinalIgnoreCase) || string.Equals(u.Outcome, "PartiallyWorked", StringComparison.OrdinalIgnoreCase));

            results.Add(new SolutionSearchRawResult
            {
                Id = ki.Id,
                KnowledgeCode = ki.KnowledgeCode,
                Title = ki.Title,
                Summary = ki.Summary,
                Status = ki.Status,
                Version = $"v{ki.CurrentVersionNo}.0",
                ProblemDescription = kv?.ProblemDescription,
                ValidationMethod = kv?.ValidationMethod,
                RiskWarning = kv?.RiskWarning,
                RollbackPlan = kv?.RollbackPlan,
                ContentMarkdown = kv?.ContentMarkdown,
                OwnerDepartmentId = ki.OwnerDepartmentId,
                CreatedAt = ki.CreatedAt,
                PublishedAt = ki.PublishedAt,
                SourceCaseId = ki.ProvenanceCaseId,
                TotalUsages = totalUsages,
                SuccessfulUsages = successfulUsages,
                TextScore = score,
                ApplicableEnvironments = appEnvironments,
                ApplicableProductIds = appProds,
                ApplicableComponentIds = appComps,
                NegativeProductVersionIds = negativeVersions
            });
        }

        var list = results.OrderByDescending(r => r.TextScore).Take(limit).ToList();
        return Task.FromResult<IReadOnlyList<SolutionSearchRawResult>>(list);
    }

    public Task<IReadOnlyList<ProductSearchRawResult>> SearchProductsAsync(string query, int limit = 20, CancellationToken ct = default)
    {
        var term = query?.Trim() ?? string.Empty;
        var prods = _store.Products.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(term))
        {
            prods = prods.Where(p => p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) || (p.Code?.Contains(term, StringComparison.OrdinalIgnoreCase) == true));
        }

        var list = prods
            .Select(p =>
            {
                double score = 50.0;
                if (!string.IsNullOrWhiteSpace(term))
                {
                    if (string.Equals(p.Code, term, StringComparison.OrdinalIgnoreCase)) score = 100.0;
                    else if (string.Equals(p.Name, term, StringComparison.OrdinalIgnoreCase)) score = 95.0;
                    else if (p.Code?.Contains(term, StringComparison.OrdinalIgnoreCase) == true) score = 80.0;
                    else if (p.Name.Contains(term, StringComparison.OrdinalIgnoreCase)) score = 70.0;
                }
                return new ProductSearchRawResult { Id = p.Id, Code = p.Code ?? $"PRD-{p.Id}", Name = p.Name, Description = p.Description, TextScore = score };
            })
            .OrderByDescending(p => p.TextScore)
            .ThenBy(p => p.Name)
            .Take(limit)
            .ToList();

        return Task.FromResult<IReadOnlyList<ProductSearchRawResult>>(list);
    }

    public Task<IReadOnlyList<ComponentSearchRawResult>> SearchComponentsAsync(string query, long? productId = null, int limit = 20, CancellationToken ct = default)
    {
        var term = query?.Trim() ?? string.Empty;
        var comps = _store.Components.Values.AsEnumerable();

        if (productId.HasValue)
        {
            comps = comps.Where(c => c.ProductId == productId.Value);
        }

        if (!string.IsNullOrWhiteSpace(term))
        {
            comps = comps.Where(c => c.Name.Contains(term, StringComparison.OrdinalIgnoreCase) || (c.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) == true));
        }

        var list = comps
            .Select(c =>
            {
                _store.Products.TryGetValue(c.ProductId ?? 0, out var prod);
                double score = 50.0;
                if (!string.IsNullOrWhiteSpace(term))
                {
                    if (string.Equals(c.Name, term, StringComparison.OrdinalIgnoreCase)) score = 95.0;
                    else if (c.Name.Contains(term, StringComparison.OrdinalIgnoreCase)) score = 80.0;
                }
                return new ComponentSearchRawResult { Id = c.Id, ProductId = c.ProductId ?? 0, ProductName = prod?.Name ?? "Sistema", Name = c.Name, Description = c.Description, Technology = c.ComponentType, TextScore = score };
            })
            .OrderByDescending(c => c.TextScore)
            .ThenBy(c => c.Name)
            .Take(limit)
            .ToList();

        return Task.FromResult<IReadOnlyList<ComponentSearchRawResult>>(list);
    }

    public Task<IReadOnlyList<RootCauseSearchRawResult>> SearchRootCausesAsync(string query, int limit = 20, CancellationToken ct = default)
    {
        var term = query?.Trim() ?? string.Empty;
        var rcs = _store.RootCauses.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(term))
        {
            rcs = rcs.Where(r => r.Name.Contains(term, StringComparison.OrdinalIgnoreCase) || (r.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) == true));
        }

        var list = rcs
            .Select(r =>
            {
                double score = 50.0;
                if (!string.IsNullOrWhiteSpace(term))
                {
                    if (string.Equals(r.Name, term, StringComparison.OrdinalIgnoreCase)) score = 95.0;
                    else if (r.Name.Contains(term, StringComparison.OrdinalIgnoreCase)) score = 80.0;
                }
                return new RootCauseSearchRawResult { Id = r.Id, Name = r.Name, Category = r.Category ?? "Geral", Description = r.Description, TextScore = score };
            })
            .OrderByDescending(r => r.TextScore)
            .ThenBy(r => r.Name)
            .Take(limit)
            .ToList();

        return Task.FromResult<IReadOnlyList<RootCauseSearchRawResult>>(list);
    }
}

public class InMemoryCaseRelationRepository : ICaseRelationRepository
{
    private readonly InMemoryDataStore _store;

    public InMemoryCaseRelationRepository(InMemoryDataStore store)
    {
        _store = store;
    }

    public Task<IReadOnlyList<CaseRelation>> GetRelationsByCaseIdAsync(long caseId, CancellationToken ct = default)
    {
        lock (_store.CaseRelations)
        {
            var list = _store.CaseRelations.Values
                .Where(r => r.SourceCaseId == caseId || r.TargetCaseId == caseId)
                .OrderByDescending(r => r.SimilarityScore ?? 0)
                .ThenByDescending(r => r.CreatedAt)
                .ToList();

            return Task.FromResult<IReadOnlyList<CaseRelation>>(list);
        }
    }

    public Task SaveSimilarRelationsAsync(long sourceCaseId, IEnumerable<CaseRelation> relations, CancellationToken ct = default)
    {
        lock (_store.CaseRelations)
        {
            var toRemove = _store.CaseRelations.Values
                .Where(r => r.SourceCaseId == sourceCaseId && r.RelationType == CaseRelationType.Similar.ToString())
                .Select(r => r.Id)
                .ToList();

            foreach (var id in toRemove)
            {
                _store.CaseRelations.TryRemove(id, out _);
            }

            foreach (var r in relations)
            {
                r.Id = _store.NextCaseRelationId();
                _store.CaseRelations[r.Id] = r;
            }
        }

        return Task.CompletedTask;
    }

    public Task<long> AddManualRelationAsync(CaseRelation relation, CancellationToken ct = default)
    {
        lock (_store.CaseRelations)
        {
            relation.Id = _store.NextCaseRelationId();
            _store.CaseRelations[relation.Id] = relation;
            return Task.FromResult(relation.Id);
        }
    }

    public Task<IReadOnlyList<Case>> GetPotentialSimilarCandidatesAsync(long excludeCaseId, long? productId, string? errorCode, int limit = 50, CancellationToken ct = default)
    {
        var cases = _store.Cases.Values
            .Where(c => c.Id != excludeCaseId)
            .OrderByDescending(c => c.OpenedAt)
            .Take(limit)
            .ToList();

        return Task.FromResult<IReadOnlyList<Case>>(cases);
    }

    public Task<IReadOnlyList<DiagnosticStep>> GetSuccessfulDiagnosticStepsForCasesAsync(IEnumerable<long> caseIds, CancellationToken ct = default)
    {
        var idSet = caseIds.ToHashSet();
        var sessions = _store.DiagnosticSessions.Values.Where(s => idSet.Contains(s.CaseId)).Select(s => s.Id).ToHashSet();
        var steps = _store.DiagnosticSteps.Values
            .Where(ds => sessions.Contains(ds.DiagnosticSessionId) && string.Equals(ds.Outcome, "Worked", StringComparison.OrdinalIgnoreCase))
            .OrderBy(ds => ds.PerformedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<DiagnosticStep>>(steps);
    }

    public Task<IReadOnlyList<CaseResolution>> GetResolutionsForCasesAsync(IEnumerable<long> caseIds, CancellationToken ct = default)
    {
        var idSet = caseIds.ToHashSet();
        var resolutions = _store.CaseResolutions.Values
            .Where(cr => idSet.Contains(cr.CaseId))
            .ToList();

        return Task.FromResult<IReadOnlyList<CaseResolution>>(resolutions);
    }
}



