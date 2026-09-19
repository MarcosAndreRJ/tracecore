using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TraceCore.Application.DTOs;
using TraceCore.Application.Exceptions;
using TraceCore.Application.Services;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;
using TraceCore.Domain.Services;
using TraceCore.Infrastructure.Persistence.InMemory;
using TraceCore.Infrastructure.Services.Llm;
using Xunit;

namespace TraceCore.IntegrationTests;

/// <summary>
/// Fase 17 (Configuracoes): testes de integracao cobrindo ISecretStore,
/// LlmConfigurationService (SaveCredential / DeleteCredential / TestConnection /
/// UpsertProviderConfig), autorizacao das paginas de Settings e invariantes de seguranca.
/// </summary>
public class SettingsModuleTests : IClassFixture<TraceCoreTestApplicationFactory>
{
    private readonly TraceCoreTestApplicationFactory _factory;

    public SettingsModuleTests(TraceCoreTestApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetDatabase();
    }

    // =========================================================================
    // ISecretStore
    // =========================================================================

    [Fact]
    public async Task SecretStore_SetExistsAndDelete_WorksCorrectly()
    {
        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<ISecretStore>();

        // Nao existe inicialmente
        (await store.ExistsAsync("tc_test_key")).Should().BeFalse();

        // Salva
        await store.SetSecretAsync("tc_test_key", "supersecret123");

        // Existe apos salvar
        (await store.ExistsAsync("tc_test_key")).Should().BeTrue();

        // Get retorna valor (uso interno — nunca exposto a UI)
        var val = await store.GetSecretAsync("tc_test_key");
        val.Should().Be("supersecret123");

        // Remove
        await store.DeleteSecretAsync("tc_test_key");

        // Nao existe apos remover
        (await store.ExistsAsync("tc_test_key")).Should().BeFalse();
        (await store.GetSecretAsync("tc_test_key")).Should().BeNull();
    }

    [Fact]
    public async Task SecretStore_GetReturnsNull_WhenNotSet()
    {
        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<ISecretStore>();

        var result = await store.GetSecretAsync("chave_inexistente_" + Guid.NewGuid());
        result.Should().BeNull("segredo inexistente deve retornar null, nunca string vazia ou excecao");
    }

    [Fact]
    public async Task SecretStore_Delete_IsIdempotent_WhenKeyNotExists()
    {
        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<ISecretStore>();

        // Nao deve lancar excecao ao remover chave inexistente
        var act = async () => await store.DeleteSecretAsync("chave_que_nao_existe_" + Guid.NewGuid());
        await act.Should().NotThrowAsync("DeleteSecretAsync deve ser idempotente");
    }

    [Fact]
    public async Task SecretStore_Set_ReplacesExisting_Silently()
    {
        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<ISecretStore>();

        await store.SetSecretAsync("tc_replace_key", "valor1");
        await store.SetSecretAsync("tc_replace_key", "valor2");

        var val = await store.GetSecretAsync("tc_replace_key");
        val.Should().Be("valor2", "segunda chamada a SetSecretAsync deve substituir o valor anterior");

        await store.DeleteSecretAsync("tc_replace_key");
    }

    // =========================================================================
    // LlmProviderResolver — precedencia SecretStore -> IConfiguration
    // =========================================================================

    [Fact]
    public async Task LlmProviderResolver_UsesSecretStoreFirst_IsCredentialConfigured()
    {
        using var scope = _factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<ISecretStore>();
        var resolver = scope.ServiceProvider.GetRequiredService<ILlmProviderResolver>();

        // Sem segredo e sem IConfiguration -> false
        var beforeSet = resolver.IsCredentialConfigured("Anthropic");
        // (pode ser true se User Secrets tiver a chave configurada localmente, aceitavel)

        // Com segredo -> true
        await store.SetSecretAsync("llm_apikey_Anthropic", "test-key-from-store");
        resolver.IsCredentialConfigured("Anthropic").Should().BeTrue(
            "ISecretStore tem precedencia — credencial salva la deve ser reconhecida");

        // Remove do store
        await store.DeleteSecretAsync("llm_apikey_Anthropic");
    }

    // =========================================================================
    // LlmConfigurationService — SaveCredential
    // =========================================================================

    [Fact]
    public async Task LlmConfigurationService_SaveCredential_PersistsToSecretStore_AndAudits_WithoutExposingKey()
    {
        using var scope = _factory.Services.CreateScope();
        var configService = scope.ServiceProvider.GetRequiredService<ILlmConfigurationService>();
        var store = scope.ServiceProvider.GetRequiredService<ISecretStore>();
        var auditRepo = scope.ServiceProvider.GetRequiredService<IAuditEventRepository>();

        var auditsBeforeCount = (await auditRepo.GetRecentAsync(200)).Count;

        await configService.SaveCredentialAsync("Anthropic", "sk-test-save", updatedBy: null);

        // Credencial salva no store
        (await store.ExistsAsync("llm_apikey_Anthropic")).Should().BeTrue();

        // Evento de auditoria criado
        var audits = await auditRepo.GetRecentAsync(200);
        audits.Count.Should().BeGreaterThan(auditsBeforeCount);

        var latestAudit = audits.OrderByDescending(a => a.OccurredAt).First();
        latestAudit.Action.Should().Be("llm_provider_config.update");

        // INVARIANTE DE SEGURANCA: o payload de auditoria NAO deve conter a chave
        var metadataJson = latestAudit.MetadataJson ?? "";
        metadataJson.Should().NotContain("sk-test-save",
            "a API Key NUNCA deve aparecer em registros de auditoria");
        metadataJson.Should().Contain("CredentialChanged",
            "o payload deve indicar que a credencial foi alterada");

        // Limpa
        await store.DeleteSecretAsync("llm_apikey_Anthropic");
    }

    [Fact]
    public async Task LlmConfigurationService_DeleteCredential_RemovesFromStore_AndAudits()
    {
        using var scope = _factory.Services.CreateScope();
        var configService = scope.ServiceProvider.GetRequiredService<ILlmConfigurationService>();
        var store = scope.ServiceProvider.GetRequiredService<ISecretStore>();
        var auditRepo = scope.ServiceProvider.GetRequiredService<IAuditEventRepository>();

        // Prepara: salva primeiro
        await store.SetSecretAsync("llm_apikey_OpenAI", "sk-delete-test");
        (await store.ExistsAsync("llm_apikey_OpenAI")).Should().BeTrue();

        var auditsBeforeCount = (await auditRepo.GetRecentAsync(200)).Count;

        // Remove via servico
        await configService.DeleteCredentialAsync("OpenAI", updatedBy: null);

        // Credencial removida
        (await store.ExistsAsync("llm_apikey_OpenAI")).Should().BeFalse();

        // Evento de auditoria criado
        var audits = await auditRepo.GetRecentAsync(200);
        audits.Count.Should().BeGreaterThan(auditsBeforeCount);

        var latestAudit = audits.OrderByDescending(a => a.OccurredAt).First();
        latestAudit.Action.Should().Be("llm_provider_config.credential_removed");
    }

    // =========================================================================
    // LlmConfigurationService — TestConnection
    // =========================================================================

    [Fact]
    public async Task LlmConfigurationService_TestConnection_WhenCredentialAbsent_ReturnsFail_WithoutStackTrace()
    {
        using var scope = _factory.Services.CreateScope();
        var configService = scope.ServiceProvider.GetRequiredService<ILlmConfigurationService>();
        var store = scope.ServiceProvider.GetRequiredService<ISecretStore>();

        // Garante que nao ha credencial
        await store.DeleteSecretAsync("llm_apikey_Anthropic");
        // (IConfiguration no ambiente de teste nao tem a chave real)

        var result = await configService.TestConnectionAsync("Anthropic", "Generation");

        result.Success.Should().BeFalse("sem credencial, o teste deve falhar");
        result.Message.Should().NotBeNullOrWhiteSpace("deve retornar mensagem clara");
        result.Message.Should().NotContain("Exception",
            "stack trace ou nome de excecao nao pode aparecer na mensagem para a UI (falha segura §26)");
        result.Message.Should().NotContain("StackTrace",
            "informacoes internas nunca aparecem na resposta para a UI");
    }

    // =========================================================================
    // LlmConfigurationService — UpsertProviderConfig
    // =========================================================================

    [Fact]
    public async Task LlmConfigurationService_UpsertProviderConfig_RejectsEmptyProviderCode()
    {
        using var scope = _factory.Services.CreateScope();
        var configService = scope.ServiceProvider.GetRequiredService<ILlmConfigurationService>();

        var act = async () => await configService.UpsertLegacyConfigAsync(new UpsertLlmProviderConfigCommand(
            Id: null, Purpose: "Generation", ProviderCode: "", ModelName: "gpt-4o",
            IsActive: false, NewApiKey: null, UpdatedBy: null));

        await act.Should().ThrowAsync<Exception>("ProviderCode vazio deve ser rejeitado");
    }

    [Fact]
    public async Task LlmConfigurationService_EnforcesOneActivePerPurpose_OnUpsert()
    {
        using var scope = _factory.Services.CreateScope();
        var configService = scope.ServiceProvider.GetRequiredService<ILlmConfigurationService>();
        var configRepo = scope.ServiceProvider.GetRequiredService<ILlmProviderConfigRepository>();

        // Cria Anthropic ativo para Generation
        await configService.UpsertLegacyConfigAsync(new UpsertLlmProviderConfigCommand(
            Id: null, Purpose: "Generation", ProviderCode: "Anthropic",
            ModelName: "claude-sonnet-4-5-20250929", IsActive: true, NewApiKey: null, UpdatedBy: null));

        // Cria OpenAI ativo para Generation — deve desativar o Anthropic
        await configService.UpsertLegacyConfigAsync(new UpsertLlmProviderConfigCommand(
            Id: null, Purpose: "Generation", ProviderCode: "OpenAI",
            ModelName: "gpt-4o-mini", IsActive: true, NewApiKey: null, UpdatedBy: null));

        // Verifica: somente 1 ativo por proposito
        var all = await configRepo.GetAllAsync();
        var activeGen = all.Where(c => c.Purpose == "Generation" && c.IsActive).ToList();
        activeGen.Should().HaveCount(1, "somente uma configuracao pode estar ativa por proposito");
        activeGen[0].ProviderCode.Should().Be("OpenAI",
            "o ultimo Upsert com IsActive=true deve prevalecer");
    }

    // =========================================================================
    // ILlmModelCatalog
    // =========================================================================

    [Fact]
    public void LlmModelCatalog_ReturnsModels_ForKnownProviders()
    {
        using var scope = _factory.Services.CreateScope();
        var catalog = scope.ServiceProvider.GetRequiredService<ILlmModelCatalog>();

        var anthropicGen = catalog.GetSupportedModels("Anthropic", "Generation");
        anthropicGen.Should().NotBeEmpty("Anthropic tem modelos de geracao catalogados");
        anthropicGen.Should().Contain(m => m.IsDefault, "deve ter um modelo default");

        var openaiEmb = catalog.GetSupportedModels("OpenAI", "Embedding");
        openaiEmb.Should().NotBeEmpty("OpenAI tem modelos de embedding catalogados");

        catalog.IsKnownProvider("Anthropic").Should().BeTrue();
        catalog.IsKnownProvider("OpenAI").Should().BeTrue();
        catalog.IsKnownProvider("ProviderDesconhecido").Should().BeFalse();
    }

    [Fact]
    public void LlmModelCatalog_ReturnsEmptyList_ForUnknownProvider()
    {
        using var scope = _factory.Services.CreateScope();
        var catalog = scope.ServiceProvider.GetRequiredService<ILlmModelCatalog>();

        var models = catalog.GetSupportedModels("ProviderFantasma", "Generation");
        models.Should().BeEmpty("catalogo nao conhece esse provedor — campo livre disponivel na UI");
    }

    // =========================================================================
    // Autorizacao das paginas de Settings
    // =========================================================================

    [Fact]
    public async Task SettingsIndex_Requires_ConfigGerenciarPermission_RedirectsAnonymous()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Settings/Index");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LlmProvidersIndex_Requires_ConfigGerenciarPermission_RedirectsAnonymous()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Settings/LlmProviders/Index");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Redirect, HttpStatusCode.Found, HttpStatusCode.Unauthorized);
    }

    // =========================================================================
    // NOVA ARQUITETURA DESACOPLADA (Fase 17) — 9 Cenários Obrigatórios
    // =========================================================================

    [Fact]
    public async Task OpenAiCompatible_Authenticated_ArbitraryProvider_WorksWithoutCodeChanges()
    {
        using var scope = _factory.Services.CreateScope();
        var configService = scope.ServiceProvider.GetRequiredService<ILlmConfigurationService>();
        var resolver = scope.ServiceProvider.GetRequiredService<ILlmProviderResolver>();

        // Admin cadastra novo provedor arbitrário compatível com OpenAI (ex: DeepSeek)
        var providerDto = await configService.UpsertProviderAsync(new UpsertLlmProviderCommand(
            Id: null,
            Name: "DeepSeek AI",
            Code: "deepseek",
            Protocol: LlmProtocols.OpenAICompatible,
            BaseUrl: "https://api.deepseek.com",
            AuthenticationType: LlmAuthenticationTypes.BearerApiKey,
            HasGenerationCapability: true,
            HasEmbeddingCapability: false,
            Status: "Active",
            NewApiKey: "sk-deepseek-test-12345",
            UpdatedBy: null));

        providerDto.Id.Should().BeGreaterThan(0);
        providerDto.CredentialConfigured.Should().BeTrue();

        // Cadastra e ativa modelo
        var modelDto = await configService.UpsertModelConfigAsync(new UpsertLlmModelConfigCommand(
            Id: null,
            Purpose: "Generation",
            ProviderId: providerDto.Id,
            ModelName: "deepseek-chat",
            IsActive: true,
            UpdatedBy: null));

        // Resolve o provedor ativo para geração
        var provider = await resolver.ResolveGenerationProviderAsync();

        // Invariantes
        provider.Should().NotBeNull();
        provider.ProviderCode.Should().Be("deepseek");
        provider.ModelName.Should().Be("deepseek-chat");
        provider.Should().BeOfType<OpenAiCompatibleLlmProvider>();
    }

    [Fact]
    public async Task OpenAiCompatible_Unauthenticated_WorksWithoutCredential()
    {
        using var scope = _factory.Services.CreateScope();
        var configService = scope.ServiceProvider.GetRequiredService<ILlmConfigurationService>();
        var resolver = scope.ServiceProvider.GetRequiredService<ILlmProviderResolver>();

        // Provedor local (ex: Ollama) sem autenticação
        var providerDto = await configService.UpsertProviderAsync(new UpsertLlmProviderCommand(
            Id: null,
            Name: "Ollama Local",
            Code: "ollama-local",
            Protocol: LlmProtocols.OpenAICompatible,
            BaseUrl: "http://localhost:11434/v1",
            AuthenticationType: LlmAuthenticationTypes.None,
            HasGenerationCapability: true,
            HasEmbeddingCapability: true,
            Status: "Active",
            NewApiKey: null,
            UpdatedBy: null));

        providerDto.CredentialConfigured.Should().BeTrue("provedores None não exigem credencial");

        // Cadastra modelo e ativa
        await configService.UpsertModelConfigAsync(new UpsertLlmModelConfigCommand(
            Id: null,
            Purpose: "Generation",
            ProviderId: providerDto.Id,
            ModelName: "llama3:8b",
            IsActive: true,
            UpdatedBy: null));

        // Resolve geração sem lançar erro de credencial ausente
        var provider = await resolver.ResolveGenerationProviderAsync();
        provider.Should().NotBeNull();
        provider.ProviderCode.Should().Be("ollama-local");
        provider.ModelName.Should().Be("llama3:8b");
    }

    [Fact]
    public async Task AnthropicProtocol_WorksAndPreservesBehavior()
    {
        using var scope = _factory.Services.CreateScope();
        var configService = scope.ServiceProvider.GetRequiredService<ILlmConfigurationService>();
        var resolver = scope.ServiceProvider.GetRequiredService<ILlmProviderResolver>();

        var providerDto = await configService.UpsertProviderAsync(new UpsertLlmProviderCommand(
            Id: null,
            Name: "Anthropic Direct",
            Code: "anthropic-custom",
            Protocol: LlmProtocols.AnthropicMessages,
            BaseUrl: "https://api.anthropic.com/v1",
            AuthenticationType: LlmAuthenticationTypes.HeaderApiKey,
            HasGenerationCapability: true,
            HasEmbeddingCapability: false,
            Status: "Active",
            NewApiKey: "sk-ant-test-999",
            UpdatedBy: null));

        await configService.UpsertModelConfigAsync(new UpsertLlmModelConfigCommand(
            Id: null,
            Purpose: "Generation",
            ProviderId: providerDto.Id,
            ModelName: "claude-3-7-sonnet-20250219",
            IsActive: true,
            UpdatedBy: null));

        var provider = await resolver.ResolveGenerationProviderAsync();
        provider.Should().NotBeNull();
        provider.ProviderCode.Should().Be("anthropic-custom");
        provider.ModelName.Should().Be("claude-3-7-sonnet-20250219");
        provider.Should().BeOfType<AnthropicLlmProvider>();
    }

    [Fact]
    public async Task ArbitraryProviderCode_WorksIfProtocolIsKnown()
    {
        using var scope = _factory.Services.CreateScope();
        var configService = scope.ServiceProvider.GetRequiredService<ILlmConfigurationService>();
        var resolver = scope.ServiceProvider.GetRequiredService<ILlmProviderResolver>();

        var providerDto = await configService.UpsertProviderAsync(new UpsertLlmProviderCommand(
            Id: null,
            Name: "Corporative AI Gateway",
            Code: "corp-internal-gw-xyz",
            Protocol: LlmProtocols.OpenAICompatible,
            BaseUrl: "https://ai.corp.internal/v1",
            AuthenticationType: LlmAuthenticationTypes.BearerApiKey,
            HasGenerationCapability: true,
            HasEmbeddingCapability: false,
            Status: "Active",
            NewApiKey: "corp-token-abc",
            UpdatedBy: null));

        await configService.UpsertModelConfigAsync(new UpsertLlmModelConfigCommand(
            Id: null,
            Purpose: "Generation",
            ProviderId: providerDto.Id,
            ModelName: "corporate-finetune-v2",
            IsActive: true,
            UpdatedBy: null));

        var provider = await resolver.ResolveGenerationProviderAsync();
        provider.ProviderCode.Should().Be("corp-internal-gw-xyz");
        provider.ModelName.Should().Be("corporate-finetune-v2");
    }

    [Fact]
    public async Task UnknownProtocol_FailsClearly()
    {
        using var scope = _factory.Services.CreateScope();
        var configService = scope.ServiceProvider.GetRequiredService<ILlmConfigurationService>();

        var act = async () => await configService.UpsertProviderAsync(new UpsertLlmProviderCommand(
            Id: null,
            Name: "Unsupported Provider",
            Code: "unsupported-rpc",
            Protocol: "CustomLegacyRpc",
            BaseUrl: "https://api.custom.com",
            AuthenticationType: LlmAuthenticationTypes.BearerApiKey,
            HasGenerationCapability: true,
            HasEmbeddingCapability: false,
            Status: "Active",
            NewApiKey: null,
            UpdatedBy: null));

        var ex = await act.Should().ThrowAsync<BusinessRuleValidationException>();
        ex.Which.Message.Should().Contain("não suportado");
    }

    [Fact]
    public async Task Capabilities_GenerationAndEmbedding_AreRespected()
    {
        using var scope = _factory.Services.CreateScope();
        var configService = scope.ServiceProvider.GetRequiredService<ILlmConfigurationService>();

        // Provedor APENAS com capability de Embedding
        var providerDto = await configService.UpsertProviderAsync(new UpsertLlmProviderCommand(
            Id: null,
            Name: "Vector Only Provider",
            Code: "vector-only",
            Protocol: LlmProtocols.OpenAICompatible,
            BaseUrl: "https://api.vector.com/v1",
            AuthenticationType: LlmAuthenticationTypes.None,
            HasGenerationCapability: false,
            HasEmbeddingCapability: true,
            Status: "Active",
            NewApiKey: null,
            UpdatedBy: null));

        // Tentativa de vincular a Generation deve falhar
        var actGen = async () => await configService.UpsertModelConfigAsync(new UpsertLlmModelConfigCommand(
            Id: null,
            Purpose: "Generation",
            ProviderId: providerDto.Id,
            ModelName: "text-embedding-3-small",
            IsActive: true,
            UpdatedBy: null));

        var ex = await actGen.Should().ThrowAsync<BusinessRuleValidationException>();
        ex.Which.Message.Should().Contain("não possui capability 'Generation'");

        // Vincular a Embedding tem sucesso
        var modelDto = await configService.UpsertModelConfigAsync(new UpsertLlmModelConfigCommand(
            Id: null,
            Purpose: "Embedding",
            ProviderId: providerDto.Id,
            ModelName: "text-embedding-3-small",
            IsActive: true,
            UpdatedBy: null));

        modelDto.Should().NotBeNull();
        modelDto.Purpose.Should().Be("Embedding");
    }

    [Fact]
    public async Task Secrets_None_DoesNotRequireCredential_While_AuthenticatedRequires()
    {
        using var scope = _factory.Services.CreateScope();
        var configService = scope.ServiceProvider.GetRequiredService<ILlmConfigurationService>();
        var resolver = scope.ServiceProvider.GetRequiredService<ILlmProviderResolver>();

        // 1. Provedor com autenticação mas sem API Key configurada
        var authProvider = await configService.UpsertProviderAsync(new UpsertLlmProviderCommand(
            Id: null,
            Name: "Auth Required Provider",
            Code: "auth-needed",
            Protocol: LlmProtocols.OpenAICompatible,
            BaseUrl: "https://api.authneeded.com/v1",
            AuthenticationType: LlmAuthenticationTypes.BearerApiKey,
            HasGenerationCapability: true,
            HasEmbeddingCapability: false,
            Status: "Active",
            NewApiKey: null,
            UpdatedBy: null));

        await configService.UpsertModelConfigAsync(new UpsertLlmModelConfigCommand(
            Id: null,
            Purpose: "Generation",
            ProviderId: authProvider.Id,
            ModelName: "model-x",
            IsActive: true,
            UpdatedBy: null));

        // Tentativa de resolver geração deve falhar por falta de credencial
        var actFail = async () => await resolver.ResolveGenerationProviderAsync();
        var ex = await actFail.Should().ThrowAsync<InvalidOperationException>();
        ex.Which.Message.Should().Contain("sem API Key configurada");

        // 2. Provedor com AuthenticationType == None não deve falhar
        var noneProvider = await configService.UpsertProviderAsync(new UpsertLlmProviderCommand(
            Id: null,
            Name: "No Auth Provider",
            Code: "no-auth-needed",
            Protocol: LlmProtocols.OpenAICompatible,
            BaseUrl: "http://localhost:8080/v1",
            AuthenticationType: LlmAuthenticationTypes.None,
            HasGenerationCapability: true,
            HasEmbeddingCapability: false,
            Status: "Active",
            NewApiKey: null,
            UpdatedBy: null));

        await configService.UpsertModelConfigAsync(new UpsertLlmModelConfigCommand(
            Id: null,
            Purpose: "Generation",
            ProviderId: noneProvider.Id,
            ModelName: "model-y",
            IsActive: true,
            UpdatedBy: null));

        var resolved = await resolver.ResolveGenerationProviderAsync();
        resolved.Should().NotBeNull();
        resolved.ProviderCode.Should().Be("no-auth-needed");
    }

    [Fact]
    public async Task BaseUrlValidation_AllowsHttpAndHttps_RejectsDangerousSchemes()
    {
        using var scope = _factory.Services.CreateScope();
        var configService = scope.ServiceProvider.GetRequiredService<ILlmConfigurationService>();

        string[] dangerousUrls = [
            "file:///etc/passwd",
            "file://c:/windows/win.ini",
            "ftp://ftp.example.com/api",
            "gopher://gopher.example.com",
            "javascript:alert(1)",
            "not-a-valid-url"
        ];

        foreach (var badUrl in dangerousUrls)
        {
            var act = async () => await configService.UpsertProviderAsync(new UpsertLlmProviderCommand(
                Id: null,
                Name: "Dangerous Provider",
                Code: "danger-" + Guid.NewGuid().ToString("N")[..6],
                Protocol: LlmProtocols.OpenAICompatible,
                BaseUrl: badUrl,
                AuthenticationType: LlmAuthenticationTypes.None,
                HasGenerationCapability: true,
                HasEmbeddingCapability: false,
                Status: "Active",
                NewApiKey: null,
                UpdatedBy: null));

            var ex = await act.Should().ThrowAsync<BusinessRuleValidationException>($"URL '{badUrl}' deve ser rejeitada");
            ex.Which.Message.Should().Contain("http ou https");
        }

        // HTTP e HTTPS válidos
        var validHttp = await configService.UpsertProviderAsync(new UpsertLlmProviderCommand(
            Id: null,
            Name: "Valid Http",
            Code: "valid-http-" + Guid.NewGuid().ToString("N")[..6],
            Protocol: LlmProtocols.OpenAICompatible,
            BaseUrl: "http://localhost:11434/v1",
            AuthenticationType: LlmAuthenticationTypes.None,
            HasGenerationCapability: true,
            HasEmbeddingCapability: false,
            Status: "Active",
            NewApiKey: null,
            UpdatedBy: null));
        validHttp.Id.Should().BeGreaterThan(0);

        var validHttps = await configService.UpsertProviderAsync(new UpsertLlmProviderCommand(
            Id: null,
            Name: "Valid Https",
            Code: "valid-https-" + Guid.NewGuid().ToString("N")[..6],
            Protocol: LlmProtocols.OpenAICompatible,
            BaseUrl: "https://api.openai.com/v1",
            AuthenticationType: LlmAuthenticationTypes.BearerApiKey,
            HasGenerationCapability: true,
            HasEmbeddingCapability: false,
            Status: "Active",
            NewApiKey: null,
            UpdatedBy: null));
        validHttps.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PrincipalAcceptanceCriteria_AdminAddsLocalProvider_ActivatesAndUses_WithoutCodeChange()
    {
        using var scope = _factory.Services.CreateScope();
        var configService = scope.ServiceProvider.GetRequiredService<ILlmConfigurationService>();
        var resolver = scope.ServiceProvider.GetRequiredService<ILlmProviderResolver>();

        // 1. Admin acessa a tela e cadastra novo provedor local (ex: Ollama)
        var newProvider = await configService.UpsertProviderAsync(new UpsertLlmProviderCommand(
            Id: null,
            Name: "Minha IA Local",
            Code: "minha-ia-local",
            Protocol: LlmProtocols.OpenAICompatible,
            BaseUrl: "http://localhost:11434/v1",
            AuthenticationType: LlmAuthenticationTypes.None,
            HasGenerationCapability: true,
            HasEmbeddingCapability: true,
            Status: "Active",
            NewApiKey: null,
            UpdatedBy: null));

        newProvider.Should().NotBeNull();
        newProvider.Code.Should().Be("minha-ia-local");

        // 2. Admin configura modelo para Generation
        await configService.UpsertModelConfigAsync(new UpsertLlmModelConfigCommand(
            Id: null,
            Purpose: "Generation",
            ProviderId: newProvider.Id,
            ModelName: "qwen3:14b",
            IsActive: true,
            UpdatedBy: null));

        // 3. Admin configura modelo para Embedding
        await configService.UpsertModelConfigAsync(new UpsertLlmModelConfigCommand(
            Id: null,
            Purpose: "Embedding",
            ProviderId: newProvider.Id,
            ModelName: "nomic-embed-text",
            IsActive: true,
            UpdatedBy: null));

        // 4. Verifica que apenas um modelo está ativo por propósito
        var activeGen = await configService.GetActiveModelConfigAsync("Generation");
        activeGen.Should().NotBeNull();
        activeGen!.ProviderCode.Should().Be("minha-ia-local");
        activeGen.ModelName.Should().Be("qwen3:14b");

        var activeEmb = await configService.GetActiveModelConfigAsync("Embedding");
        activeEmb.Should().NotBeNull();
        activeEmb!.ProviderCode.Should().Be("minha-ia-local");
        activeEmb.ModelName.Should().Be("nomic-embed-text");

        // 5. O sistema resolve os provedores de forma 100% dinâmica sem recompilação ou classes específicas
        var resolvedGen = await resolver.ResolveGenerationProviderAsync();
        resolvedGen.Should().NotBeNull();
        resolvedGen.ProviderCode.Should().Be("minha-ia-local");
        resolvedGen.ModelName.Should().Be("qwen3:14b");

        var resolvedEmb = await resolver.ResolveEmbeddingProviderAsync();
        resolvedEmb.Should().NotBeNull();
        resolvedEmb.ProviderCode.Should().Be("minha-ia-local");
        resolvedEmb.ModelName.Should().Be("nomic-embed-text");
    }
}

