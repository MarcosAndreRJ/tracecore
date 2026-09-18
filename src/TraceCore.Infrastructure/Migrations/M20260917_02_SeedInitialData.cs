using FluentMigrator;

namespace TraceCore.Infrastructure.Migrations;

[Migration(2026091702, "Seed initial permissions, roles and departments")]
public class M20260917_02_SeedInitialData : Migration
{
    public override void Up()
    {
        // 1. Seed Permissions
        // Convenção: português, 'dominio.acao' em lowercase
        var permissions = new[]
        {
            new { Code = "caso.visualizar", Desc = "Visualizar casos e incidentes" },
            new { Code = "caso.criar", Desc = "Criar novos casos" },
            new { Code = "caso.editar", Desc = "Editar casos existentes" },
            new { Code = "caso.encerrar", Desc = "Encerrar casos" },
            new { Code = "solucao.criar", Desc = "Criar soluções de conhecimento" },
            new { Code = "solucao.validar", Desc = "Validar e revisar soluções técnicas" },
            new { Code = "solucao.publicar", Desc = "Publicar soluções oficiais de conhecimento" },
            new { Code = "analytics.visualizar", Desc = "Visualizar relatórios e painéis analíticos" },
            new { Code = "analytics.departamento", Desc = "Visualizar analytics departamentais" },
            new { Code = "usuario.gerenciar", Desc = "Gerenciar usuários, status e vínculos" },
            new { Code = "permissao.gerenciar", Desc = "Gerenciar papéis e permissões" },
            new { Code = "auditoria.visualizar", Desc = "Visualizar trilhas de auditoria" }
        };

        foreach (var p in permissions)
        {
            Insert.IntoTable("permissions")
                .Row(new { code = p.Code, description = p.Desc });
        }

        // 2. Seed Departments
        var departments = new[]
        {
            new { Name = "Desenvolvimento Web", Desc = "Equipes focadas em soluções web e front-end", Status = "Active" },
            new { Name = "Desenvolvimento Desktop", Desc = "Equipes focadas em aplicações desktop legadas e modernas", Status = "Active" },
            new { Name = "Mobile", Desc = "Aplicações corporativas móveis", Status = "Active" },
            new { Name = "Infraestrutura", Desc = "Servidores, redes, nuvem e sustentação de TI", Status = "Active" },
            new { Name = "Banco de Dados", Desc = "Administração, performance e sustentação de dados", Status = "Active" },
            new { Name = "Suporte", Desc = "Atendimento ao cliente e suporte operacional de 1º/2º nível", Status = "Active" },
            new { Name = "Integrações", Desc = "Conectores e integrações corporativas (SAP, mensageria)", Status = "Active" },
            new { Name = "Gestão", Desc = "Liderança de projetos, produtos e governança", Status = "Active" }
        };

        foreach (var d in departments)
        {
            Insert.IntoTable("departments")
                .Row(new { name = d.Name, description = d.Desc, status = d.Status });
        }

        // 3. Seed Roles (25_MATRIZ_PERFIS_PERMISSOES.md)
        var roles = new[]
        {
            new { Name = "Usuário Técnico", Desc = "Pesquisa conhecimento, registra casos e rascunhos de soluções" },
            new { Name = "Especialista", Desc = "Atua no diagnóstico aprofundado e validação de soluções" },
            new { Name = "Revisor", Desc = "Revisa e publica artigos e soluções na base de conhecimento" },
            new { Name = "Gestor", Desc = "Acompanha indicadores, métricas da equipe e auditoria do escopo" },
            new { Name = "Admin Funcional", Desc = "Administra catálogo, usuários funcionais e configurações do sistema" },
            new { Name = "Admin Segurança", Desc = "Gestão de acessos, papéis, permissões de segurança e auditoria completa" }
        };

        foreach (var r in roles)
        {
            Insert.IntoTable("roles")
                .Row(new { name = r.Name, description = r.Desc });
        }
    }

    public override void Down()
    {
        Delete.FromTable("permissions").AllRows();
        Delete.FromTable("roles").AllRows();
        Delete.FromTable("departments").AllRows();
    }
}
