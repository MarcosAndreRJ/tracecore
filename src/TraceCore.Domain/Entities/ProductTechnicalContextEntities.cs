using System;

namespace TraceCore.Domain.Entities;

/// <summary>
/// Perfil técnico complementar de um Product (Fase 18 / Prompt 2 — Contexto Técnico
/// de Investigação). Relação 1:0..1 com products — um produto sem perfil continua
/// funcionando normalmente em todo o restante do sistema.
/// Todos os campos são opcionais, exceto ProductId.
/// </summary>
public class ProductTechnicalProfile
{
    public long Id { get; set; }
    public long ProductId { get; set; }

    public string? BusinessPurpose { get; set; }
    public string? ArchitectureSummary { get; set; }

    // String controlada simples (não catálogo): Desktop, Web, Mobile, Api, Servico, SaaS, Outro.
    // Valores validados em Application (case-insensitive), sem travar o banco com enum fechado.
    public string? SystemType { get; set; }

    public string? FrontendStack { get; set; }
    public string? BackendStack { get; set; }
    public string? PrimaryDatabase { get; set; }
    public string? RuntimePlatform { get; set; }
    public string? HostingModel { get; set; }
    public string? AuthenticationModel { get; set; }
    public string? ObservabilityStack { get; set; }
    public string? DeploymentModel { get; set; }
    public string? Vendor { get; set; }
    public string? SupportNotes { get; set; }
    public string? KnownConstraints { get; set; }
    public string? InvestigationNotes { get; set; }

    // Catálogo aberto (string, não enum fechado): Disabled, OfficialOnly, AllowListed, OpenWeb.
    // Somente armazenado nesta etapa — nenhuma pesquisa externa é disparada por este campo.
    public string ExternalResearchPolicy { get; set; } = "Disabled";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public long? UpdatedBy { get; set; }

    public ProductTechnicalProfile() { }

    public ProductTechnicalProfile(long productId)
    {
        if (productId <= 0)
            throw new ArgumentException("ProductId inválido.", nameof(productId));

        ProductId = productId;
        ExternalResearchPolicy = "Disabled";
        CreatedAt = DateTime.UtcNow;
    }

    public static readonly string[] ValidExternalResearchPolicies = { "Disabled", "OfficialOnly", "AllowListed", "OpenWeb" };

    // Fase 04 — Ajuste do Ecossistema: tipo do sistema (Desktop, Web, Mobile, Api, Servico, SaaS, Outro).
    public static readonly string[] ValidSystemTypes = { "Desktop", "Web", "Mobile", "Api", "Servico", "SaaS", "Outro" };
}

/// <summary>
/// Fonte técnica relacionada a uma aplicação (documentação oficial/interna, wiki,
/// runbook, swagger, repositório, KB de fornecedor, status page, etc.).
/// Armazena só metadados — nenhum crawling/validação de disponibilidade nesta etapa.
/// </summary>
public class ProductTechnicalSource
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string Name { get; set; } = string.Empty;

    // Catálogo aberto: OfficialDocumentation, InternalDocumentation, Wiki, Runbook,
    // Swagger, Repository, VendorKnowledgeBase, StatusPage, Other.
    public string SourceType { get; set; } = "Other";

    public string? Url { get; set; }
    public string? Description { get; set; }

    // Catálogo aberto: Official, Internal, Trusted, Reference. Classificação simples,
    // sem score numérico artificial.
    public string TrustLevel { get; set; } = "Reference";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public long? UpdatedBy { get; set; }

    public ProductTechnicalSource() { }

    public ProductTechnicalSource(long productId, string name, string sourceType, string? url = null, string? description = null, string trustLevel = "Reference")
    {
        if (productId <= 0)
            throw new ArgumentException("ProductId inválido.", nameof(productId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome da fonte técnica é obrigatório.", nameof(name));
        if (string.IsNullOrWhiteSpace(sourceType))
            throw new ArgumentException("Tipo da fonte técnica é obrigatório.", nameof(sourceType));

        ProductId = productId;
        Name = name.Trim();
        SourceType = sourceType.Trim();
        Url = string.IsNullOrWhiteSpace(url) ? null : url.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        TrustLevel = string.IsNullOrWhiteSpace(trustLevel) ? "Reference" : trustLevel.Trim();
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Domínio autorizado para pesquisa externa quando ExternalResearchPolicy = AllowListed.
/// Só armazena/normaliza o domínio nesta etapa — nenhuma chamada externa é feita.
/// </summary>
public class ProductExternalResearchDomain
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public string Domain { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public long? CreatedBy { get; set; }

    public ProductExternalResearchDomain() { }

    public ProductExternalResearchDomain(long productId, string domain, string? description = null)
    {
        if (productId <= 0)
            throw new ArgumentException("ProductId inválido.", nameof(productId));
        if (string.IsNullOrWhiteSpace(domain))
            throw new ArgumentException("Domínio é obrigatório.", nameof(domain));

        ProductId = productId;
        Domain = domain.Trim().ToLowerInvariant();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }
}
