using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using TraceCore.Application.DTOs;
using TraceCore.Application.Services;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;
using TraceCore.Domain.Services;
using Xunit;

namespace TraceCore.IntegrationTests;

/// <summary>
/// Prompt 3 — critério de aceite principal (§66/§67/§98): o Copiloto investigativo
/// precisa encontrar histórico interno relevante e responder SEM que nenhum provedor
/// de embedding esteja configurado. Como não há um provedor de LLM real disponível em
/// ambiente de teste, o provedor de GERAÇÃO é substituído por um roteiro determinístico
/// (fake) só para estes testes — toda a orquestração (interpretação, resolução de
/// entidades, loop de tool-calling, execução de ferramentas, persistência) é real,
/// rodando contra os repositórios InMemory da própria aplicação.
/// </summary>
public class InvestigationCopilotIntegrationTests : IClassFixture<TraceCoreTestApplicationFactory>
{
    private readonly TraceCoreTestApplicationFactory _factory;

    public InvestigationCopilotIntegrationTests(TraceCoreTestApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    private sealed class ScriptedFakeLlmProvider : ILlmProvider
    {
        private readonly Func<LlmGenerationRequest, LlmGenerationResult> _script;
        public ScriptedFakeLlmProvider(Func<LlmGenerationRequest, LlmGenerationResult> script) => _script = script;
        public string ProviderCode => "FakeTest";
        public string ModelName => "fake-model";
        public Task<LlmGenerationResult> GenerateAsync(LlmGenerationRequest request, CancellationToken ct = default)
            => Task.FromResult(_script(request));
    }

    private sealed class FakeLlmProviderResolver : ILlmProviderResolver
    {
        private readonly ILlmProvider _provider;
        public FakeLlmProviderResolver(ILlmProvider provider) => _provider = provider;

        public Task<ILlmProvider> ResolveGenerationProviderAsync(CancellationToken ct = default) => Task.FromResult(_provider);

        public Task<IEmbeddingProvider> ResolveEmbeddingProviderAsync(CancellationToken ct = default) =>
            throw new InvalidOperationException("O Copiloto investigativo (Prompt 3) NUNCA deve resolver um provedor de embedding.");

        public bool IsCredentialConfigured(string providerCode) => true;

        public Task<ConnectionTestResult> TestModelConfigConnectionAsync(long modelConfigId, CancellationToken ct = default) =>
            Task.FromResult(new ConnectionTestResult(true, null));

        public Task<ConnectionTestResult> TestLegacyConnectionAsync(string providerCode, string purpose, CancellationToken ct = default) =>
            Task.FromResult(new ConnectionTestResult(true, null));
    }

    [Fact]
    public async Task AskAsync_WithoutAnyEmbeddingProviderConfigured_FindsResolvedHistoryAndAnswers()
    {
        long clientId = 0, productId = 0, oldCaseId = 0;

        var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                int callIndex = 0;

                LlmGenerationResult Script(LlmGenerationRequest request)
                {
                    callIndex++;

                    // Chamada 1: interpretação (sem Tools) — extrai cliente/produto/sintomas do relato.
                    if (callIndex == 1)
                    {
                        request.Tools.Should().BeNull("a interpretação não usa ferramentas");
                        var json = JsonSerializer.Serialize(new
                        {
                            clientText = "Cliente Alfa",
                            productText = "Sistema X",
                            symptoms = new[] { "falha intermitente", "timeout" },
                            errorCodes = Array.Empty<string>(),
                            componentHint = (string?)null,
                            versionHint = (string?)null,
                            environmentHint = (string?)null
                        });
                        return new LlmGenerationResult(json);
                    }

                    // Chamada 2: primeira rodada do loop de ferramentas — busca por cliente+produto (Camada 1).
                    if (callIndex == 2)
                    {
                        var argsJson = JsonSerializer.Serialize(new { clientId, productId, pageSize = 20 });
                        return new LlmGenerationResult(
                            Text: string.Empty,
                            ToolCalls: new List<LlmToolCall> { new LlmToolCall("SearchCases", argsJson, "call_1") });
                    }

                    // Chamada 3: modelo já viu o resultado da busca (via PriorTurns) e pede detalhes do caso.
                    if (callIndex == 3)
                    {
                        request.PriorTurns.Should().NotBeNull();
                        request.PriorTurns!.Should().Contain(t => t.Role == "tool" && t.ToolResultJson!.Contains("CAS-"));

                        var argsJson = JsonSerializer.Serialize(new { caseId = oldCaseId });
                        return new LlmGenerationResult(
                            Text: string.Empty,
                            ToolCalls: new List<LlmToolCall> { new LlmToolCall("GetCaseDetails", argsJson, "call_2") });
                    }

                    // Chamada 4: síntese final — sem mais tool calls.
                    return new LlmGenerationResult(
                        "HISTÓRICO INTERNO: encontrei um caso semelhante já resolvido para este cliente e produto.");
                }

                services.AddScoped<ILlmProviderResolver>(_ => new FakeLlmProviderResolver(new ScriptedFakeLlmProvider(Script)));
            });
        });

        using var scope = customFactory.Services.CreateScope();
        var clientRepository = scope.ServiceProvider.GetRequiredService<IClientRepository>();
        var catalogRepository = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var caseService = scope.ServiceProvider.GetRequiredService<ICaseService>();
        var copilot = scope.ServiceProvider.GetRequiredService<IInvestigationCopilotService>();

        clientId = await clientRepository.AddAsync(new Client("Cliente Alfa"));
        productId = await catalogRepository.AddProductAsync(new Product("Sistema X"));

        var oldCase = await caseService.OpenCaseAsync(new OpenCaseCommand(
            OriginalReport: "Falha intermitente com timeout ocorrida anteriormente, já resolvida.",
            ClientId: clientId,
            ProductId: productId
        ), currentUserId: 1L);
        oldCaseId = oldCase.Id;

        // Act
        var answer = await copilot.AskAsync("O cliente Alfa está relatando falha intermitente no Sistema X.", userId: 1L);

        // Assert — critério de aceite principal do Prompt 3: funciona sem embedding,
        // encontra o histórico e não estoura o limite de tool calls.
        answer.Should().NotBeNull();
        answer.Answer.Should().Contain("HISTÓRICO INTERNO");
        answer.RelatedCases.Should().ContainSingle(c => c.CaseId == oldCaseId);
        answer.RetrievalStrategies.Should().Contain(s => s.StartsWith("StructuredSearch"));
        answer.ToolCallCount.Should().Be(2);
        answer.InteractionId.Should().NotBeNull();
        answer.Warnings.Should().NotContain(w => w.Contains("Limite de chamadas"));
    }

    [Fact]
    public async Task AskAsync_WhenNoHistoryFound_ReturnsWarningAndStillAnswers()
    {
        var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                int callIndex = 0;
                LlmGenerationResult Script(LlmGenerationRequest request)
                {
                    callIndex++;
                    if (callIndex == 1)
                    {
                        var json = JsonSerializer.Serialize(new
                        {
                            clientText = (string?)null,
                            productText = (string?)null,
                            symptoms = new[] { "erro genérico" },
                            errorCodes = Array.Empty<string>(),
                            componentHint = (string?)null,
                            versionHint = (string?)null,
                            environmentHint = (string?)null
                        });
                        return new LlmGenerationResult(json);
                    }

                    // Modelo desiste de usar ferramentas de imediato (nenhum sinal suficiente).
                    return new LlmGenerationResult(
                        "Não encontrei ocorrência anterior suficientemente semelhante no histórico do TraceCore. HIPÓTESE DA IA: verificar logs recentes.");
                }

                services.AddScoped<ILlmProviderResolver>(_ => new FakeLlmProviderResolver(new ScriptedFakeLlmProvider(Script)));
            });
        });

        using var scope = customFactory.Services.CreateScope();
        var copilot = scope.ServiceProvider.GetRequiredService<IInvestigationCopilotService>();

        var answer = await copilot.AskAsync("Sistema apresentando erro genérico não identificado.", userId: 1L);

        answer.RelatedCases.Should().BeEmpty();
        answer.Warnings.Should().Contain(w => w.Contains("Não foi encontrado histórico"));
        answer.Answer.Should().Contain("HIPÓTESE DA IA");
    }

    private sealed class QueryEchoingExternalProvider : IExternalResearchProvider
    {
        public string ProviderCode => "FakeSearch";
        public bool IsConfigured => true;
        public Task<IReadOnlyList<TraceCore.Domain.Services.ExternalSearchResult>> SearchAsync(string sanitizedQuery, int limit, CancellationToken ct = default)
        {
            IReadOnlyList<TraceCore.Domain.Services.ExternalSearchResult> results = new[]
            {
                new TraceCore.Domain.Services.ExternalSearchResult(
                    Title: $"Doc sobre {sanitizedQuery}",
                    Url: $"https://learn.microsoft.com/{sanitizedQuery.Replace(" ", "-")}",
                    Domain: "learn.microsoft.com",
                    Snippet: "snippet",
                    PublishedAt: null,
                    RetrievedAt: DateTime.UtcNow)
            };
            return Task.FromResult(results);
        }
    }

    private sealed class FakeExternalResearchProviderFactory : IExternalResearchProviderFactory
    {
        private readonly IExternalResearchProvider _provider;
        public FakeExternalResearchProviderFactory(IExternalResearchProvider provider) => _provider = provider;
        public Task<IExternalResearchProvider> GetProviderAsync(CancellationToken ct = default) => Task.FromResult(_provider);
    }

    [Fact]
    public async Task AskAsync_ExternalResearchSubLimit_NeverExceedsThreeCallsEvenWhenModelKeepsAsking()
    {
        long productId = 0;

        var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                int callIndex = 0;
                LlmGenerationResult Script(LlmGenerationRequest request)
                {
                    callIndex++;
                    if (callIndex == 1)
                    {
                        var json = JsonSerializer.Serialize(new
                        {
                            clientText = (string?)null,
                            productText = (string?)null,
                            symptoms = Array.Empty<string>(),
                            errorCodes = Array.Empty<string>(),
                            componentHint = (string?)null,
                            versionHint = (string?)null,
                            environmentHint = (string?)null
                        });
                        return new LlmGenerationResult(json);
                    }

                    // Modelo insiste em pesquisar externamente 4 vezes seguidas (topic1..topic4).
                    if (callIndex <= 5)
                    {
                        var topic = $"topic{callIndex - 1}";
                        var argsJson = JsonSerializer.Serialize(new { productId, query = topic, maxResults = 5 });
                        return new LlmGenerationResult(
                            Text: string.Empty,
                            ToolCalls: new List<LlmToolCall> { new LlmToolCall("SearchExternalSources", argsJson, $"call_{callIndex}") });
                    }

                    return new LlmGenerationResult("DOCUMENTAÇÃO EXTERNA consultada dentro do limite permitido.");
                }

                services.AddScoped<ILlmProviderResolver>(_ => new FakeLlmProviderResolver(new ScriptedFakeLlmProvider(Script)));
                services.AddScoped<IExternalResearchProviderFactory>(_ => new FakeExternalResearchProviderFactory(new QueryEchoingExternalProvider()));
            });
        });

        using var scope = customFactory.Services.CreateScope();
        var catalogRepository = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        var contextService = scope.ServiceProvider.GetRequiredService<IProductTechnicalContextService>();
        var copilot = scope.ServiceProvider.GetRequiredService<IInvestigationCopilotService>();

        productId = await catalogRepository.AddProductAsync(new Product("Sistema Pesquisa Externa"));
        await contextService.UpsertTechnicalProfileAsync(new UpsertProductTechnicalProfileCommand(
            productId, null, null, null, null, null, null, null, null, null, null, null, null, null, null,
            ExternalResearchPolicy: "OpenWeb"), null);

        var answer = await copilot.AskAsync("Erro externo desconhecido, preciso pesquisar documentação.", userId: 1L);

        // O modelo pediu 4 pesquisas externas, mas só as 3 primeiras devem ter sido aceitas.
        answer.ExternalSources.Should().HaveCount(3);
        answer.ExternalSources.Should().NotContain(s => s.Url.Contains("topic4"));
        answer.RetrievalStrategies.Should().Contain("ExternalResearch");
    }
}
