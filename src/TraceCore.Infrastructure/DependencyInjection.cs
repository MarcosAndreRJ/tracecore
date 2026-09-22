using System.Reflection;
using FluentMigrator.Runner;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TraceCore.Application.Services;
using TraceCore.Domain.Repositories;
using TraceCore.Domain.Services;
using TraceCore.Infrastructure.Migrations;
using TraceCore.Infrastructure.Persistence;
using TraceCore.Infrastructure.Persistence.InMemory;
using TraceCore.Infrastructure.Persistence.Repositories;
using TraceCore.Infrastructure.Services;
using TraceCore.Infrastructure.Services.ExternalResearch;
using TraceCore.Infrastructure.Services.Llm;

namespace TraceCore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        // HttpClient compartilhado pelos provedores de IA (sem headers globais — cada
        // chamada define a própria autenticação; Fase 13 / DEV-AI-003).
        services.AddHttpClient();

        // Prompt 4 (Copiloto — pesquisa externa controlada): client nomeado e isolado,
        // com timeout curto (§31) e redirecionamento automático DESLIGADO (§58 — SSRF:
        // preferimos falhar a seguir silenciosamente um redirect para um alvo não
        // validado; a mesma validação de URL nunca é herdada de IntegrationHealthCheckService,
        // que não tem proteção equivalente).
        services.AddHttpClient("ExternalResearch", client =>
        {
            client.Timeout = System.TimeSpan.FromSeconds(10);
        }).ConfigurePrimaryHttpMessageHandler(() => new System.Net.Http.HttpClientHandler
        {
            AllowAutoRedirect = false
        });
        services.AddSingleton<IExternalUrlSafetyValidator, ExternalUrlSafetyValidator>();
        services.AddScoped<IExternalResearchProviderFactory, ExternalResearchProviderFactory>();

        // Fase 15 (M10): Serviço de verificação automática de saúde de integrações
        services.AddScoped<IIntegrationHealthCheckService, IntegrationHealthCheckService>();

        // Fase 17 (Configurações): Catálogo de modelos (singleton — lista estática em memória)
        services.AddSingleton<ILlmModelCatalog, LlmModelCatalog>();

        // Fase 17 (Configurações): Secret Store usando ASP.NET Core Data Protection.
        // App_Data/Secrets/ — fora de wwwroot e fora do controle de versão.
        // IDataProtectionProvider é registrado pelo AddDataProtection() abaixo — chamado
        // antes do bloco de provider para garantir disponibilidade em ambos (MySql e InMemory).
        var storagePath = configuration["Storage:BasePath"] ?? "App_Data/Storage";
        var appDataRoot = System.IO.Path.GetDirectoryName(storagePath) ?? "App_Data";
        services.AddDataProtection()
            .PersistKeysToFileSystem(new System.IO.DirectoryInfo(
                System.IO.Path.Combine(appDataRoot, "DataProtection-Keys")));
        services.AddSingleton<ISecretStore, ProtectedFileSecretStore>();

        // Bloco 7.A.0 (ADR — Persistência): MySQL é o provider operacional padrão.
        // InMemory NUNCA é selecionado silenciosamente — só quando explicitamente
        // configurado (ex.: TraceCoreTestApplicationFactory define "InMemory" de propósito
        // para testes isolados/rápidos). Provider ausente/inválido falha explicitamente
        // no startup em vez de mascarar o problema caindo para InMemory.
        var provider = configuration["Persistence:Provider"];
        var connectionString = configuration.GetConnectionString("TraceCoreDb");

        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new InvalidOperationException(
                "Configuração ausente: 'Persistence:Provider' não foi definido. " +
                "Defina explicitamente 'MySql' (operacional, requer ConnectionStrings:TraceCoreDb) " +
                "ou 'InMemory' (somente para testes/cenários isolados explicitamente configurados). " +
                "A aplicação não usa mais InMemory como fallback silencioso (ADR — Persistência, Fase 7.A).");
        }

        if (provider.Equals("MySql", System.StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Configuração inválida: 'Persistence:Provider' está definido como 'MySql', mas " +
                    "'ConnectionStrings:TraceCoreDb' não foi fornecida (appsettings, User Secrets ou " +
                    "variável de ambiente). A aplicação não pode iniciar sem uma connection string válida.");
            }

            services.AddSingleton<IDbConnectionFactory, MySqlDbConnectionFactory>();

            services.AddScoped<IUserRepository, MySqlUserRepository>();
            services.AddScoped<IDepartmentRepository, MySqlDepartmentRepository>();
            services.AddScoped<IRoleRepository, MySqlRoleRepository>();
            services.AddScoped<IPermissionRepository, MySqlPermissionRepository>();
            services.AddScoped<IUserSessionRepository, MySqlUserSessionRepository>();
            services.AddScoped<IPasswordResetTokenRepository, MySqlPasswordResetTokenRepository>();
            services.AddScoped<IAuditEventRepository, MySqlAuditEventRepository>();
            services.AddScoped<IClientRepository, MySqlClientRepository>();
            services.AddScoped<ICatalogRepository, MySqlCatalogRepository>();
            services.AddScoped<IAttachmentRepository, MySqlAttachmentRepository>();
            services.AddScoped<ICaseRepository, MySqlCaseRepository>();
            services.AddScoped<IDiagnosticRepository, MySqlDiagnosticRepository>();
            services.AddScoped<ICaseResolutionRepository, MySqlCaseResolutionRepository>();
            services.AddScoped<IKnowledgeRepository, MySqlKnowledgeRepository>();
            services.AddScoped<ISearchRepository, MySqlSearchRepository>();
            services.AddScoped<ICaseRelationRepository, MySqlCaseRelationRepository>();
            services.AddScoped<IProductTechnicalContextRepository, MySqlProductTechnicalContextRepository>();
            services.AddScoped<IProductVersionManagementRepository, MySqlProductVersionManagementRepository>();
            services.AddScoped<IDiagnosticFlowRepository, MySqlDiagnosticFlowRepository>();
            services.AddScoped<IIntegrationRepository, MySqlIntegrationRepository>();
            services.AddScoped<IManagementAnalyticsRepository, MySqlManagementAnalyticsRepository>();
            services.AddScoped<ISearchableContentRepository, MySqlSearchableContentRepository>();
            services.AddScoped<ILlmProviderConfigRepository, MySqlLlmProviderConfigRepository>();
            services.AddScoped<ILlmProviderRepository, MySqlLlmProviderRepository>();
            services.AddScoped<ILlmModelConfigRepository, MySqlLlmModelConfigRepository>();
            services.AddScoped<IAiInteractionRepository, MySqlAiInteractionRepository>();
            services.AddScoped<ILlmProviderResolver, LlmProviderResolver>();

            // FluentMigrator setup
            services.AddFluentMigratorCore()
                .ConfigureRunner(rb => rb
                    .AddMySql5() // MySql connector runner
                    .WithGlobalConnectionString(connectionString)
                    .ScanIn(Assembly.GetExecutingAssembly()).For.Migrations());

            services.AddScoped<DatabaseMigrationRunner>();
        }
        else if (provider.Equals("InMemory", System.StringComparison.OrdinalIgnoreCase))
        {
            // Permitido somente quando explicitamente configurado (testes de integração
            // isolados, TraceCoreTestApplicationFactory) — nunca como default silencioso.
            services.AddSingleton<InMemoryDataStore>();
            services.AddScoped<IUserRepository, InMemoryUserRepository>();
            services.AddScoped<IDepartmentRepository, InMemoryDepartmentRepository>();
            services.AddScoped<IRoleRepository, InMemoryRoleRepository>();
            services.AddScoped<IPermissionRepository, InMemoryPermissionRepository>();
            services.AddScoped<IUserSessionRepository, InMemoryUserSessionRepository>();
            services.AddScoped<IPasswordResetTokenRepository, InMemoryPasswordResetTokenRepository>();
            services.AddScoped<IAuditEventRepository, InMemoryAuditEventRepository>();
            services.AddScoped<IClientRepository, InMemoryClientRepository>();
            services.AddScoped<ICatalogRepository, InMemoryCatalogRepository>();
            services.AddScoped<IAttachmentRepository, InMemoryAttachmentRepository>();
            services.AddScoped<ICaseRepository, InMemoryCaseRepository>();
            services.AddScoped<IDiagnosticRepository, InMemoryDiagnosticRepository>();
            services.AddScoped<ICaseResolutionRepository, InMemoryCaseResolutionRepository>();
            services.AddScoped<IKnowledgeRepository, InMemoryKnowledgeRepository>();
            services.AddScoped<ISearchRepository, InMemorySearchRepository>();
            services.AddScoped<ICaseRelationRepository, InMemoryCaseRelationRepository>();
            services.AddScoped<IProductTechnicalContextRepository, InMemoryProductTechnicalContextRepository>();
            services.AddScoped<IProductVersionManagementRepository, InMemoryProductVersionManagementRepository>();
            services.AddScoped<IDiagnosticFlowRepository, InMemoryDiagnosticFlowRepository>();
            services.AddScoped<IManagementAnalyticsRepository, InMemoryManagementAnalyticsRepository>();
            services.AddScoped<IIntegrationRepository, InMemoryIntegrationRepository>();
            services.AddScoped<ISearchableContentRepository, InMemorySearchableContentRepository>();
            services.AddScoped<ILlmProviderConfigRepository, InMemoryLlmProviderConfigRepository>();
            services.AddScoped<ILlmProviderRepository, InMemoryLlmProviderRepository>();
            services.AddScoped<ILlmModelConfigRepository, InMemoryLlmModelConfigRepository>();
            services.AddScoped<IAiInteractionRepository, InMemoryAiInteractionRepository>();
            services.AddSingleton<ISecretStore, InMemorySecretStore>();
            services.AddScoped<ILlmProviderResolver, LlmProviderResolver>();
        }
        else
        {
            throw new InvalidOperationException(
                $"Configuração inválida: 'Persistence:Provider' = '{provider}' não é reconhecido. " +
                "Valores aceitos: 'MySql' ou 'InMemory'.");
        }

        return services;
    }
}
