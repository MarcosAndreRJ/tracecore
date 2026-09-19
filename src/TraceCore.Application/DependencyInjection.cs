using Microsoft.Extensions.DependencyInjection;
using TraceCore.Application.Services;

namespace TraceCore.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<ICaseService, CaseService>();
        services.AddScoped<ICaseInvestigationService, CaseInvestigationService>();
        services.AddScoped<ICaseResolutionService, CaseResolutionService>();
        services.AddScoped<IKnowledgeService, KnowledgeService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<ICaseRelationService, CaseRelationService>();
        services.AddScoped<IDiagnosticEngineService, DiagnosticEngineService>();
        services.AddScoped<IIntegrationService, IntegrationService>();
        services.AddScoped<IManagementAnalyticsService, ManagementAnalyticsService>();
        services.AddScoped<IContentPreparationService, ContentPreparationService>();
        services.AddScoped<IRagService, RagService>();
        services.AddScoped<IEmbeddingIndexingService, EmbeddingIndexingService>();
        services.AddScoped<ILlmConfigurationService, LlmConfigurationService>();

        return services;
    }
}
