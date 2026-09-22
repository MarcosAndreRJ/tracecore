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
/// Testes de integração da Fase 3: Ranking e Sugestão determinística de casos
/// para correções de versão (FixedBy) sem LLM ou algoritmos probabilísticos.
/// </summary>
public class VersionChangeCaseSuggestionTests : IClassFixture<TraceCoreTestApplicationFactory>
{
    private readonly TraceCoreTestApplicationFactory _factory;

    public VersionChangeCaseSuggestionTests(TraceCoreTestApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    private IServiceScope Scope() => _factory.Services.CreateScope();

    [Fact]
    public async Task SuggestCases_FindsPreviousVersionCase_UsingReleaseOrderNumericComparison()
    {
        using var scope = Scope();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var cases = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        // 1. Cria produto com versões tipo "5.9" (ReleaseOrder 1) e "5.10" (ReleaseOrder 2)
        // Se fosse comparação por string, "5.10" < "5.9", o que estaria errado.
        var prodId = await catalogRepo.AddProductAsync(new Product("Sistema de Faturamento", "FAT-CORE"));
        var v59Id = await catalogService.CreateVersionAsync(prodId, "5.9");
        var v510Id = await catalogService.CreateVersionAsync(prodId, "5.10");

        // 2. Cria caso reportado na versão anterior (5.9)
        var caseInOlderVersion = await cases.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Falha ao calcular alíquota de ICMS interestadual",
            ProductId: prodId,
            ProductVersionId: v59Id,
            ErrorCode: "ERR_ICMS_CALC",
            Severity: "High"
        ));

        // 3. Busca sugestões ao registrar correção na versão 5.10
        var suggestions = await vm.SuggestCasesForChangeAsync(new VersionChangeCaseSuggestionInput(
            ProductVersionChangeId: null,
            ProductVersionId: v510Id,
            ProductId: prodId,
            ComponentId: null,
            ErrorCode: "ERR_ICMS_CALC",
            Title: "Ajuste no cálculo de alíquota de ICMS",
            Description: "Correção na regra tributária interestadual"
        ));

        // 4. Assert: o caso foi encontrado com bônus de versão anterior
        suggestions.Should().NotBeEmpty();
        var sug = suggestions.FirstOrDefault(s => s.CaseId == caseInOlderVersion.Id);
        sug.Should().NotBeNull();
        sug!.ProductVersionLabel.Should().Be("5.9");
        sug.MatchedFactors.Should().Contain(f => f.StartsWith("Versão anterior"));
        // Score esperado: produto +30, erro +35, versão anterior +15, termos relato/sintomas +10~20 => >= 80
        sug.Score.Should().BeGreaterOrEqualTo(80);
    }

    [Fact]
    public async Task SuggestCases_CrossProductCandidate_IsStrictlyFilteredOut()
    {
        using var scope = Scope();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var cases = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        var prod1 = await catalogRepo.AddProductAsync(new Product("ERP Financeiro", "ERP-FIN"));
        var v1 = await catalogService.CreateVersionAsync(prod1, "1.0.0");

        var prod2 = await catalogRepo.AddProductAsync(new Product("CRM Vendas", "CRM-VEN"));
        var v2 = await catalogService.CreateVersionAsync(prod2, "1.0.0");

        // Caso no CRM com erro ERR_TIMEOUT_SQL
        var crmCase = await cases.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Timeout de conexão com banco de dados",
            ProductId: prod2,
            ProductVersionId: v2,
            ErrorCode: "ERR_TIMEOUT_SQL"
        ));

        // Sugestões para correção no ERP Financeiro com o MESMO código de erro
        var suggestions = await vm.SuggestCasesForChangeAsync(new VersionChangeCaseSuggestionInput(
            ProductVersionChangeId: null,
            ProductVersionId: v1,
            ProductId: prod1,
            ComponentId: null,
            ErrorCode: "ERR_TIMEOUT_SQL",
            Title: "Correção de timeout no banco",
            Description: "Ajuste do pool de conexões"
        ));

        // Assert estrito: candidato cross-produto NUNCA deve aparecer
        suggestions.Should().NotContain(s => s.CaseId == crmCase.Id);
    }

    [Fact]
    public async Task SuggestCases_MatchingComponent_IncreasesScore()
    {
        using var scope = Scope();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var cases = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        var prodId = await catalogRepo.AddProductAsync(new Product("Logística WMS", "WMS-LOG"));
        var v1 = await catalogService.CreateVersionAsync(prodId, "1.0");
        var v2 = await catalogService.CreateVersionAsync(prodId, "2.0");
        var compId = await catalogRepo.AddComponentAsync(new ComponentEntity("Expedição", "Module", productId: prodId, code: "EXP"));

        var caseWithComp = await cases.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Etiqueta de despacho não é gerada no leitor",
            ProductId: prodId,
            ProductVersionId: v1,
            ComponentIds: new List<long> { compId }
        ));

        // Sem informar componente
        var withoutComp = await vm.SuggestCasesForChangeAsync(new VersionChangeCaseSuggestionInput(
            ProductVersionChangeId: null,
            ProductVersionId: v2,
            ProductId: prodId,
            ComponentId: null,
            ErrorCode: null,
            Title: "Correção na geração de etiquetas",
            Description: null
        ));

        // Informando componente
        var withComp = await vm.SuggestCasesForChangeAsync(new VersionChangeCaseSuggestionInput(
            ProductVersionChangeId: null,
            ProductVersionId: v2,
            ProductId: prodId,
            ComponentId: compId,
            ErrorCode: null,
            Title: "Correção na geração de etiquetas",
            Description: null
        ));

        var sugWithout = withoutComp.First(s => s.CaseId == caseWithComp.Id);
        var sugWith = withComp.First(s => s.CaseId == caseWithComp.Id);

        // Componente confere +25 pontos
        sugWith.Score.Should().Be(sugWithout.Score + 25);
        sugWith.MatchedFactors.Should().Contain("Mesmo componente");
        sugWithout.MatchedFactors.Should().NotContain("Mesmo componente");
    }

    [Fact]
    public async Task SuggestCases_MatchingErrorCode_IncreasesScore()
    {
        using var scope = Scope();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var cases = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        var prodId = await catalogRepo.AddProductAsync(new Product("PDV Caixa", "PDV-CX"));
        var v1 = await catalogService.CreateVersionAsync(prodId, "1.0");
        var v2 = await catalogService.CreateVersionAsync(prodId, "2.0");

        var caseErr = await cases.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Erro de comunicação com impressora fiscal",
            ProductId: prodId,
            ProductVersionId: v1,
            ErrorCode: "ERR_PRINTER_OFFLINE"
        ));

        var sug = await vm.SuggestCasesForChangeAsync(new VersionChangeCaseSuggestionInput(
            ProductVersionChangeId: null,
            ProductVersionId: v2,
            ProductId: prodId,
            ComponentId: null,
            ErrorCode: "ERR_PRINTER_OFFLINE",
            Title: "Correção impressora",
            Description: null
        ));

        var item = sug.First(s => s.CaseId == caseErr.Id);
        item.MatchedFactors.Should().Contain("Mesmo erro (ERR_PRINTER_OFFLINE)");
    }

    [Fact]
    public async Task SuggestCases_TextOverlapInTitleAndDescription_IncreasesScore()
    {
        using var scope = Scope();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var cases = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        var prodId = await catalogRepo.AddProductAsync(new Product("Plataforma BI", "BI-PLAT"));
        var v1 = await catalogService.CreateVersionAsync(prodId, "1.0");
        var v2 = await catalogService.CreateVersionAsync(prodId, "2.0");

        var c1 = await cases.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Falha de processamento em lote no relatorio de consolidacao orcamentaria com timeout",
            ProductId: prodId,
            ProductVersionId: v1
        ));

        var sug = await vm.SuggestCasesForChangeAsync(new VersionChangeCaseSuggestionInput(
            ProductVersionChangeId: null,
            ProductVersionId: v2,
            ProductId: prodId,
            ComponentId: null,
            ErrorCode: null,
            Title: "Ajuste de relatorio com consolidacao orcamentaria",
            Description: "Otimizacao para evitar timeout no lote"
        ));

        var item = sug.First(s => s.CaseId == c1.Id);
        item.MatchedFactors.Should().Contain("Termos semelhantes no relato/sintomas");
    }

    [Fact]
    public async Task SuggestCases_CaseWithoutProductVersion_AppearsWithoutVersionBonus()
    {
        using var scope = Scope();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var cases = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        var prodId = await catalogRepo.AddProductAsync(new Product("App Mobile", "MOB-APP"));
        var v2 = await catalogService.CreateVersionAsync(prodId, "2.0");

        // Caso sem ProductVersionId informado (comum na abertura rápida)
        var caseNoVer = await cases.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Falha de autenticação biométrica após suspensão",
            ProductId: prodId,
            ProductVersionId: null,
            ErrorCode: "ERR_AUTH_BIO"
        ));

        var suggestions = await vm.SuggestCasesForChangeAsync(new VersionChangeCaseSuggestionInput(
            ProductVersionChangeId: null,
            ProductVersionId: v2,
            ProductId: prodId,
            ComponentId: null,
            ErrorCode: "ERR_AUTH_BIO",
            Title: "Correção na autenticação biométrica",
            Description: null
        ));

        // Aparece por produto + erro + termos, mas sem bônus de versão anterior
        suggestions.Should().Contain(s => s.CaseId == caseNoVer.Id);
        var item = suggestions.First(s => s.CaseId == caseNoVer.Id);
        item.ProductVersionLabel.Should().BeNull();
        item.MatchedFactors.Should().NotContain(f => f.Contains("Versão"));
    }

    [Fact]
    public async Task SuggestCases_SameVersion_DoesNotReceiveVersionBonus()
    {
        using var scope = Scope();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var cases = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        var prodId = await catalogRepo.AddProductAsync(new Product("Portal Web", "PORT-WEB"));
        var v1 = await catalogService.CreateVersionAsync(prodId, "1.0");

        // Caso aberto na mesma versão v1
        var caseSameVer = await cases.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Upload de arquivo trava em 99 por cento",
            ProductId: prodId,
            ProductVersionId: v1,
            ErrorCode: "ERR_UPLOAD_HANG"
        ));

        // Correção cadastrada para a mesma versão v1
        var suggestions = await vm.SuggestCasesForChangeAsync(new VersionChangeCaseSuggestionInput(
            ProductVersionChangeId: null,
            ProductVersionId: v1,
            ProductId: prodId,
            ComponentId: null,
            ErrorCode: "ERR_UPLOAD_HANG",
            Title: "Correção no upload",
            Description: null
        ));

        var item = suggestions.First(s => s.CaseId == caseSameVer.Id);
        // Na Fase 3 o bônus "Mesma versão" é desligado, e como não é versão anterior, não tem bônus de versão
        item.MatchedFactors.Should().NotContain("Mesma versão");
        item.MatchedFactors.Should().NotContain(f => f.Contains("Versão anterior"));
    }

    [Fact]
    public async Task ComputeSimilarCases_RegressionCheck_ProducesIdenticalResults()
    {
        using var scope = Scope();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var relationService = scope.ServiceProvider.GetRequiredService<ICaseRelationService>();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();

        long prodId = await catalogRepo.AddProductAsync(new Product("Gateway Regressão", "GW-REG"));
        long verId = await catalogRepo.AddProductVersionAsync(new ProductVersion(prodId, "v1.0.0"));

        var caseA = await caseService.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Instabilidade com timeout na conexão HTTP com o adquirente",
            ProductId: prodId,
            ProductVersionId: verId,
            ErrorCode: "ERR_TIMEOUT_GATEWAY"
        ), currentUserId: 1L);

        var caseB = await caseService.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Timeout intermitente na conexão HTTP com adquirente durante checkout",
            ProductId: prodId,
            ProductVersionId: verId,
            ErrorCode: "ERR_TIMEOUT_GATEWAY"
        ), currentUserId: 1L);

        // Act: Computar similaridades
        var similarCases = await relationService.ComputeSimilarCasesAsync(caseB.Id);

        // Assert: o motor extraído continua calculando com "Mesma versão", "Mesmo produto" e "Mesmo erro"
        similarCases.Should().NotBeEmpty();
        var matched = similarCases.FirstOrDefault(c => c.TargetCaseId == caseA.Id);
        matched.Should().NotBeNull();
        matched!.MatchedFactors.Should().Contain("Mesma versão");
        matched.MatchedFactors.Should().Contain("Mesmo produto");
        matched.MatchedFactors.Should().Contain("Mesmo erro (ERR_TIMEOUT_GATEWAY)");
    }

    [Fact]
    public async Task SuggestCases_DoesNotPersistAnyRelation_IsPureQuery()
    {
        using var scope = Scope();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var cases = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();
        var repo = scope.ServiceProvider.GetRequiredService<IProductVersionManagementRepository>();

        var prodId = await catalogRepo.AddProductAsync(new Product("Sistema Query Only", "SYS-QO"));
        var v1 = await catalogService.CreateVersionAsync(prodId, "1.0");
        var v2 = await catalogService.CreateVersionAsync(prodId, "2.0");

        var c = await cases.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Erro de autenticação",
            ProductId: prodId,
            ProductVersionId: v1,
            ErrorCode: "ERR_AUTH"
        ));

        var changeId = await vm.CreateChangeAsync(v2, "Fix", "Corrige autenticação", null, null, "ERR_AUTH");

        // Executa sugestão
        var suggestions = await vm.SuggestCasesForChangeAsync(new VersionChangeCaseSuggestionInput(
            ProductVersionChangeId: changeId,
            ProductVersionId: v2,
            ProductId: prodId,
            ComponentId: null,
            ErrorCode: "ERR_AUTH",
            Title: "Corrige autenticação",
            Description: null
        ));

        suggestions.Should().NotBeEmpty();

        // Assert: nenhum vínculo gravado na tabela
        var links = await repo.GetLinkedCasesByChangeIdAsync(changeId);
        links.Should().BeEmpty();
    }

    [Fact]
    public async Task LinkCase_CreatesFixedBy_AndPreservesCaseStatus()
    {
        using var scope = Scope();
        var catalogRepo = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var catalogService = scope.ServiceProvider.GetRequiredService<ICatalogService>();
        var cases = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var vm = scope.ServiceProvider.GetRequiredService<IVersionManagementService>();

        var prodId = await catalogRepo.AddProductAsync(new Product("ERP Status Check", "ERP-ST"));
        var v1 = await catalogService.CreateVersionAsync(prodId, "1.0");
        var v2 = await catalogService.CreateVersionAsync(prodId, "2.0");

        var openedCase = await cases.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Falha de concorrência em transação bancária",
            ProductId: prodId,
            ProductVersionId: v1
        ));

        openedCase.Status.Should().Be("Open");

        var changeId = await vm.CreateChangeAsync(v2, "Fix", "Correção de concorrência");

        // Vincula como FixedBy
        await vm.LinkCaseAsync(changeId, openedCase.Id, "FixedBy", matchScore: 92.5m, currentUserId: 1);

        // Verifica vínculo criado
        var linked = await vm.GetLinkedCasesAsync(changeId);
        linked.Should().ContainSingle();
        var link = linked.Single();
        link.CaseId.Should().Be(openedCase.Id);
        link.RelationType.Should().Be("FixedBy");
        link.CaseStatus.Should().Be("Open");
        link.MatchScore.Should().Be(92.5m);

        // Assert crucial (Regra 14): status do caso permanece rigorosamente inalterado
        var afterCase = await cases.GetCaseByIdAsync(openedCase.Id);
        afterCase!.Status.Should().Be("Open");
    }
}
