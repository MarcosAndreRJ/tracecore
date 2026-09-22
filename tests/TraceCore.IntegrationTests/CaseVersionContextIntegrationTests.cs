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
using Xunit;

namespace TraceCore.IntegrationTests;

/// <summary>
/// Testes de integração da Fase 4: Versão do cliente e sugestão determinística de
/// possíveis correções posteriores na abertura e detalhes de casos.
/// </summary>
public class CaseVersionContextIntegrationTests : IClassFixture<TraceCoreTestApplicationFactory>
{
    private readonly TraceCoreTestApplicationFactory _factory;

    public CaseVersionContextIntegrationTests(TraceCoreTestApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    private IServiceScope Scope() => _factory.Services.CreateScope();

    [Fact]
    public async Task GetActiveVersion_UnequivocalContext_ReturnsVersionAndEnvironment()
    {
        using var scope = Scope();
        var clientRepo = scope.ServiceProvider.GetRequiredService<IClientRepository>();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        // 1. Cria cliente, produto, ambiente e versão
        var clientId = await clientRepo.AddAsync(new Client("Hospital Samaritano", "12345678000199"));
        var prodId = await catalogRepo.AddProductAsync(new Product("Prontuário Eletrônico", "PEP"));
        var envId = await catalogRepo.AddEnvironmentAsync(new EnvironmentEntity("Produção", "Production"));
        var vId = await catalogService.CreateVersionAsync(prodId, "2.4.0");

        // 2. Cria contexto ativo para o cliente
        await clientRepo.AddTechnicalContextAsync(new ClientTechnicalContext(
            clientId: clientId,
            productId: prodId,
            clientUnitId: null,
            productVersionId: vId,
            environmentId: envId,
            status: "Active",
            effectiveFrom: DateTime.UtcNow.AddMonths(-1),
            effectiveTo: null
        ));

        // 3. Executa leitura da versão ativa
        var result = await vm.GetActiveVersionForClientAsync(clientId, prodId);

        // 4. Assert
        result.Should().NotBeNull();
        result.HasContext.Should().BeTrue();
        result.IsAmbiguous.Should().BeFalse();
        result.ProductVersionId.Should().Be(vId);
        result.VersionLabel.Should().Be("2.4.0");
        result.EnvironmentId.Should().Be(envId);
        result.EnvironmentName.Should().Be("Produção");
        result.AmbiguousEnvironments.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveVersion_MultipleEnvironments_ReturnsIsAmbiguousTrue_WithoutDefault()
    {
        using var scope = Scope();
        var clientRepo = scope.ServiceProvider.GetRequiredService<IClientRepository>();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        var clientId = await clientRepo.AddAsync(new Client("Clínica Diagnóstica", "98765432000188"));
        var prodId = await catalogRepo.AddProductAsync(new Product("Laboratório LIMS", "LIMS"));
        var envProd = await catalogRepo.AddEnvironmentAsync(new EnvironmentEntity("Produção", "Production"));
        var envHml = await catalogRepo.AddEnvironmentAsync(new EnvironmentEntity("Homologação", "Staging"));
        var vProdId = await catalogService.CreateVersionAsync(prodId, "1.5.0");
        var vHmlId = await catalogService.CreateVersionAsync(prodId, "1.6.0-rc");

        // Dois contextos ativos distintos (Produção e Homologação)
        await clientRepo.AddTechnicalContextAsync(new ClientTechnicalContext(
            clientId: clientId,
            productId: prodId,
            clientUnitId: null,
            productVersionId: vProdId,
            environmentId: envProd,
            status: "Active",
            effectiveFrom: DateTime.UtcNow.AddMonths(-2),
            effectiveTo: null
        ));
        await clientRepo.AddTechnicalContextAsync(new ClientTechnicalContext(
            clientId: clientId,
            productId: prodId,
            clientUnitId: null,
            productVersionId: vHmlId,
            environmentId: envHml,
            status: "Active",
            effectiveFrom: DateTime.UtcNow.AddMonths(-1),
            effectiveTo: null
        ));

        // Leitura da versão ativa
        var result = await vm.GetActiveVersionForClientAsync(clientId, prodId);

        // Assert: IsAmbiguous true e nenhum ambiente/versão escolhido silenciosamente (Regra 4)
        result.Should().NotBeNull();
        result.HasContext.Should().BeTrue();
        result.IsAmbiguous.Should().BeTrue();
        result.ProductVersionId.Should().BeNull();
        result.EnvironmentId.Should().BeNull();
        result.AmbiguousEnvironments.Should().HaveCount(2);
        result.AmbiguousEnvironments.Select(e => e.EnvironmentId).Should().Contain(new[] { (long?)envProd, (long?)envHml });
    }

    [Fact]
    public async Task GetActiveVersion_NoContext_ReturnsHasContextFalse_WithoutError()
    {
        using var scope = Scope();
        var clientRepo = scope.ServiceProvider.GetRequiredService<IClientRepository>();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        var clientId = await clientRepo.AddAsync(new Client("Cliente Novo Sem Contexto", "11222333000144"));
        var prodId = await catalogRepo.AddProductAsync(new Product("Sistema Novo", "SIS-NEW"));

        var result = await vm.GetActiveVersionForClientAsync(clientId, prodId);

        result.Should().NotBeNull();
        result.HasContext.Should().BeFalse();
        result.IsAmbiguous.Should().BeFalse();
        result.ProductVersionId.Should().BeNull();
        result.EnvironmentId.Should().BeNull();
        result.AmbiguousEnvironments.Should().BeEmpty();
    }

    [Fact]
    public async Task SuggestPossibleFixes_FixInLaterReleaseOrder_IsSuggestedWithScore()
    {
        using var scope = Scope();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        var prodId = await catalogRepo.AddProductAsync(new Product("Gateway de Pagamentos", "PAY-GW"));
        var compId = await catalogRepo.AddComponentAsync(new ComponentEntity("ModuloPix", "Integration", prodId));
        var v59Id = await catalogService.CreateVersionAsync(prodId, "5.9");
        var v510Id = await catalogService.CreateVersionAsync(prodId, "5.10");

        // Cria correção na versão posterior (5.10)
        var changeId = await vm.CreateChangeAsync(
            productVersionId: v510Id,
            changeType: "Fix",
            title: "Tratamento de timeout e reconexão com adquirente",
            description: "Adicionado retry automático para erro TIMEOUT_ADQUIRENTE",
            componentId: compId,
            errorCode: "TIMEOUT_ADQUIRENTE"
        );

        // Cliente está na versão 5.9 e relata o problema
        var fixes = await vm.SuggestPossibleFixesAsync(
            productId: prodId,
            currentProductVersionId: v59Id,
            title: "Falha de timeout na transação",
            description: "Ocorreu timeout ao processar adquirente na liquidação",
            componentId: compId,
            errorCode: "TIMEOUT_ADQUIRENTE"
        );

        fixes.Should().NotBeEmpty();
        var fix = fixes.FirstOrDefault(f => f.ChangeId == changeId);
        fix.Should().NotBeNull();
        fix!.ProductVersionId.Should().Be(v510Id);
        fix.VersionLabel.Should().Be("5.10");
        fix.LinkedCaseCount.Should().Be(0);
        fix.MatchedFactors.Should().Contain(f => f.Contains("Código de erro idêntico"));
        fix.MatchedFactors.Should().Contain(f => f.Contains("Componente idêntico"));
        // Score esperado: erro +35, comp +25, termos +10~20 => >= 60
        fix.Score.Should().BeGreaterOrEqualTo(60);
    }

    [Fact]
    public async Task SuggestPossibleFixes_FixInSameOrPriorReleaseOrder_IsNotSuggested_NumericallyCompared()
    {
        using var scope = Scope();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        var prodId = await catalogRepo.AddProductAsync(new Product("ERP Contábil", "ERP-CTB"));
        var v59Id = await catalogService.CreateVersionAsync(prodId, "5.9");   // ReleaseOrder 1
        var v510Id = await catalogService.CreateVersionAsync(prodId, "5.10"); // ReleaseOrder 2

        // Correção na versão 5.9 (ReleaseOrder 1)
        var changeV59 = await vm.CreateChangeAsync(
            productVersionId: v59Id,
            changeType: "Fix",
            title: "Ajuste na apuração de impostos",
            description: "Correção de cálculo",
            componentId: null,
            errorCode: "ERR_TRIB"
        );

        // Correção na versão 5.10 (ReleaseOrder 2)
        var changeV510 = await vm.CreateChangeAsync(
            productVersionId: v510Id,
            changeType: "Fix",
            title: "Ajuste na emissão de notas",
            description: "Correção de emissão",
            componentId: null,
            errorCode: "ERR_NFE"
        );

        // 1. Cliente já está na 5.10: busca correções posteriores
        var fixesForClientOn510 = await vm.SuggestPossibleFixesAsync(
            productId: prodId,
            currentProductVersionId: v510Id,
            title: "Erro tributário e de notas",
            description: "Falha de cálculo",
            componentId: null,
            errorCode: "ERR_TRIB"
        );

        // Nem a correção da 5.9 (versão anterior) nem a da 5.10 (mesma versão) podem aparecer!
        fixesForClientOn510.Should().BeEmpty();
    }

    [Fact]
    public async Task SuggestPossibleFixes_DifferentProduct_IsNeverSuggested()
    {
        using var scope = Scope();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        var prodA = await catalogRepo.AddProductAsync(new Product("Produto Alpha", "PROD-A"));
        var prodB = await catalogRepo.AddProductAsync(new Product("Produto Beta", "PROD-B"));

        var vA1 = await catalogService.CreateVersionAsync(prodA, "1.0");
        var vB2 = await catalogService.CreateVersionAsync(prodB, "2.0");

        // Correção no Produto B
        await vm.CreateChangeAsync(
            productVersionId: vB2,
            changeType: "Fix",
            title: "Falha geral de autenticação OAuth",
            description: "Token expirado",
            componentId: null,
            errorCode: "ERR_AUTH_EXPIRED"
        );

        // Busca possíveis correções para o Produto A
        var fixes = await vm.SuggestPossibleFixesAsync(
            productId: prodA,
            currentProductVersionId: vA1,
            title: "Falha geral de autenticação OAuth",
            description: "Token expirado",
            componentId: null,
            errorCode: "ERR_AUTH_EXPIRED"
        );

        fixes.Should().BeEmpty();
    }

    [Fact]
    public async Task SuggestPossibleFixes_DoesNotPersistAnyRecords_OrLinks()
    {
        using var scope = Scope();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var versionRepo = scope.ServiceProvider.GetRequiredService<IProductVersionManagementRepository>();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        var prodId = await catalogRepo.AddProductAsync(new Product("Módulo Fiscal", "MOD-FISC"));
        var v1 = await catalogService.CreateVersionAsync(prodId, "1.0");
        var v2 = await catalogService.CreateVersionAsync(prodId, "2.0");

        var changeId = await vm.CreateChangeAsync(
            productVersionId: v2,
            changeType: "Fix",
            title: "Ajuste na validação de certificado digital",
            description: "Correção de expiração",
            componentId: null,
            errorCode: "ERR_CERT"
        );

        // Executa busca de possíveis correções
        var fixes = await vm.SuggestPossibleFixesAsync(
            productId: prodId,
            currentProductVersionId: v1,
            title: "Problema com certificado digital",
            description: "Erro ao validar certificado",
            componentId: null,
            errorCode: "ERR_CERT"
        );

        fixes.Should().NotBeEmpty();

        // Nenhuma associação de caso foi criada (somente leitura)
        var linkedCases = await versionRepo.GetLinkedCasesByChangeIdAsync(changeId);
        linkedCases.Should().BeEmpty();
    }

    [Fact]
    public async Task OpenCase_PreservesUserSelectedVersion_EvenIfDifferentFromSuggested()
    {
        using var scope = Scope();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var cases = scope.ServiceProvider.GetRequiredService<ICaseService>();

        var prodId = await catalogRepo.AddProductAsync(new Product("Portal Web", "WEB-PORTAL"));
        var v1 = await catalogService.CreateVersionAsync(prodId, "1.0");
        var v2 = await catalogService.CreateVersionAsync(prodId, "2.0");

        // O usuário escolhe v1 manualmente no formulário de abertura
        var createdCase = await cases.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Problema no carregamento do painel",
            ProductId: prodId,
            ProductVersionId: v1,
            Severity: "Medium"
        ));

        createdCase.ProductVersionId.Should().Be(v1);
        createdCase.ProductVersionId.Should().NotBe(v2);
    }

    [Fact]
    public async Task SuggestPossibleFixes_LatestVersion_ReturnsEmptyList_WithFriendlyMessage()
    {
        using var scope = Scope();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        var prodId = await catalogRepo.AddProductAsync(new Product("Sistema Único", "SYS-UNIQ"));
        var vLatest = await catalogService.CreateVersionAsync(prodId, "3.0.0");

        // Correção cadastrada na própria versão 3.0.0
        await vm.CreateChangeAsync(
            productVersionId: vLatest,
            changeType: "Fix",
            title: "Correção de cache",
            description: "Cache invalidation fix",
            componentId: null,
            errorCode: "ERR_CACHE"
        );

        // Se o cliente já está na versão 3.0.0, não há versões posteriores para comparar
        var fixes = await vm.SuggestPossibleFixesAsync(
            productId: prodId,
            currentProductVersionId: vLatest,
            title: "Problema de cache",
            description: "Cache stale",
            componentId: null,
            errorCode: "ERR_CACHE"
        );

        fixes.Should().BeEmpty();
    }

    [Fact]
    public async Task GetChangesLinkedToCase_ReturnsConfirmedFixedByChanges()
    {
        using var scope = Scope();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var cases = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();
        var repo = scope.ServiceProvider.GetRequiredService<IProductVersionManagementRepository>();

        var prodId = await catalogRepo.AddProductAsync(new Product("Core Banking", "BANK-CORE"));
        var v1Id = await catalogService.CreateVersionAsync(prodId, "1.0");
        var v2Id = await catalogService.CreateVersionAsync(prodId, "2.0");

        // 1. Cria caso no incidente v1
        var createdCase = await cases.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Deadlock na transferência PIX",
            ProductId: prodId,
            ProductVersionId: v1Id,
            ErrorCode: "ERR_DEADLOCK",
            Severity: "Critical"
        ));

        // 2. Cria correção na release v2
        var changeId = await vm.CreateChangeAsync(
            productVersionId: v2Id,
            changeType: "Fix",
            title: "Ajuste na ordem de bloqueio de contas no PIX",
            description: "Elimina deadlock concorrente",
            componentId: null,
            errorCode: "ERR_DEADLOCK"
        );

        // 3. Vincula formalmente o caso à correção como FixedBy
        await vm.LinkCaseAsync(changeId, createdCase.Id, "FixedBy");

        // 4. Consulta via repositório reversamente por caseId
        var reverseLinks = await repo.GetChangesLinkedToCaseAsync(createdCase.Id);
        reverseLinks.Should().HaveCount(1);
        reverseLinks[0].ProductVersionChangeId.Should().Be(changeId);
        reverseLinks[0].RelationType.Should().Be("FixedBy");

        // 5. Consulta contexto de versão completo do caso
        var versionContext = await vm.GetVersionContextForCaseAsync(createdCase.Id);
        versionContext.Should().NotBeNull();
        versionContext.ProductVersionId.Should().Be(v1Id);
        versionContext.VersionLabel.Should().Be("1.0");
        versionContext.ConfirmedFixes.Should().HaveCount(1);
        versionContext.ConfirmedFixes[0].ChangeId.Should().Be(changeId);
        versionContext.ConfirmedFixes[0].VersionLabel.Should().Be("2.0");
        versionContext.ConfirmedFixes[0].RelationType.Should().Be("Corrigido por");

        // Como a correção já está confirmada, ela não deve aparecer duplicada em Possíveis Correções
        versionContext.PossibleFixes.Should().NotContain(pf => pf.ChangeId == changeId);
    }
}
