using System;
using FluentMigrator;

namespace TraceCore.Infrastructure.Migrations;

[Migration(2026091705, "Seed initial technical catalog and clients")]
public class M20260917_05_SeedTechnicalCatalogData : Migration
{
    public override void Up()
    {
        // 1. Clients
        var clients = new[]
        {
            new { Code = "CLI-001", Name = "Acme Corporação", Status = "Active" },
            new { Code = "CLI-002", Name = "Tech Solutions Brasil", Status = "Active" },
            new { Code = "CLI-003", Name = "Logística Global S.A.", Status = "Active" }
        };

        foreach (var c in clients)
        {
            Insert.IntoTable("clients")
                .Row(new { code = c.Code, name = c.Name, status = c.Status, created_at = DateTime.UtcNow });
        }

        // 2. Environments
        var environments = new[]
        {
            new { Name = "Produção", Type = "Production" },
            new { Name = "Homologação", Type = "Staging" },
            new { Name = "Desenvolvimento", Type = "Development" }
        };

        foreach (var env in environments)
        {
            Insert.IntoTable("environments")
                .Row(new { name = env.Name, environment_type = env.Type });
        }

        // 3. Products
        var products = new[]
        {
            new { Code = "PRD-ERP", Name = "ERP Desktop", Desc = "Sistema ERP Desktop corporativo legado e moderno", Status = "Active" },
            new { Code = "PRD-WEB", Name = "Portal Web", Desc = "Portal de atendimento e autosserviço", Status = "Active" },
            new { Code = "PRD-API", Name = "API Comercial", Desc = "Gateway e serviços de integração comercial", Status = "Active" },
            new { Code = "PRD-MOB", Name = "Aplicativo Mobile", Desc = "App de campo e força de vendas", Status = "Active" }
        };

        foreach (var p in products)
        {
            Insert.IntoTable("products")
                .Row(new { code = p.Code, name = p.Name, description = p.Desc, status = p.Status, created_at = DateTime.UtcNow });
        }

        // 4. Product Versions & Components via SQL
        Execute.Sql(@"
            INSERT INTO product_versions (product_id, version_label, status)
            SELECT id, 'v1.0.0', 'Active' FROM products WHERE code = 'PRD-ERP';

            INSERT INTO product_versions (product_id, version_label, status)
            SELECT id, 'v2.4.1', 'Active' FROM products WHERE code = 'PRD-ERP';

            INSERT INTO product_versions (product_id, version_label, status)
            SELECT id, 'v3.0.0', 'Active' FROM products WHERE code = 'PRD-WEB';

            INSERT INTO product_versions (product_id, version_label, status)
            SELECT id, 'v1.5.0', 'Active' FROM products WHERE code = 'PRD-API';

            INSERT INTO components (product_id, code, name, component_type, status, created_at)
            SELECT id, 'MOD-FIN', 'Módulo Financeiro', 'Desktop', 'Active', NOW() FROM products WHERE code = 'PRD-ERP';

            INSERT INTO components (product_id, code, name, component_type, status, created_at)
            SELECT id, 'MOD-FAT', 'Faturamento', 'Desktop', 'Active', NOW() FROM products WHERE code = 'PRD-ERP';

            INSERT INTO components (product_id, code, name, component_type, status, created_at)
            SELECT id, 'MOD-AUTH-WEB', 'Autenticação Web', 'Web', 'Active', NOW() FROM products WHERE code = 'PRD-WEB';

            INSERT INTO components (product_id, code, name, component_type, status, created_at)
            SELECT id, 'API-GATEWAY', 'Gateway de Vendas', 'API', 'Active', NOW() FROM products WHERE code = 'PRD-API';

            INSERT INTO components (product_id, code, name, component_type, status, created_at)
            SELECT id, 'SRV-SYNC', 'Sincronizador Mobile', 'Serviço', 'Active', NOW() FROM products WHERE code = 'PRD-MOB';
        ");
    }

    public override void Down()
    {
        Execute.Sql("DELETE FROM components;");
        Execute.Sql("DELETE FROM product_versions;");
        Execute.Sql("DELETE FROM products;");
        Execute.Sql("DELETE FROM environments;");
        Execute.Sql("DELETE FROM clients;");
    }
}
