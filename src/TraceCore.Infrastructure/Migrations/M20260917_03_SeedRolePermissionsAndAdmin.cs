using FluentMigrator;

namespace TraceCore.Infrastructure.Migrations;

[Migration(2026091703, "Seed role permissions and initial admin user")]
public class M20260917_03_SeedRolePermissionsAndAdmin : Migration
{
    public override void Up()
    {
        // 1. Role Permissions mapping (25_MATRIZ_PERFIS_PERMISSOES.md)
        // Insere associações via subselects portáveis
        Execute.Sql(@"
            INSERT INTO role_permissions (role_id, permission_id)
            SELECT r.id, p.id FROM roles r, permissions p
            WHERE r.name = 'Usuário Técnico' AND p.code IN ('caso.visualizar', 'caso.criar', 'caso.editar', 'solucao.criar');

            INSERT INTO role_permissions (role_id, permission_id)
            SELECT r.id, p.id FROM roles r, permissions p
            WHERE r.name = 'Especialista' AND p.code IN ('caso.visualizar', 'caso.criar', 'caso.editar', 'solucao.criar', 'solucao.validar');

            INSERT INTO role_permissions (role_id, permission_id)
            SELECT r.id, p.id FROM roles r, permissions p
            WHERE r.name = 'Revisor' AND p.code IN ('caso.visualizar', 'caso.criar', 'caso.editar', 'solucao.criar', 'solucao.validar', 'solucao.publicar');

            INSERT INTO role_permissions (role_id, permission_id)
            SELECT r.id, p.id FROM roles r, permissions p
            WHERE r.name = 'Gestor' AND p.code IN ('caso.visualizar', 'caso.criar', 'caso.editar', 'solucao.criar', 'solucao.validar', 'solucao.publicar', 'analytics.visualizar', 'analytics.departamento', 'auditoria.visualizar');

            INSERT INTO role_permissions (role_id, permission_id)
            SELECT r.id, p.id FROM roles r, permissions p
            WHERE r.name = 'Admin Funcional' AND p.code IN ('caso.visualizar', 'caso.criar', 'caso.editar', 'caso.encerrar', 'solucao.criar', 'solucao.validar', 'solucao.publicar', 'analytics.visualizar', 'analytics.departamento', 'usuario.gerenciar', 'auditoria.visualizar');

            INSERT INTO role_permissions (role_id, permission_id)
            SELECT r.id, p.id FROM roles r, permissions p
            WHERE r.name = 'Admin Segurança' AND p.code IN ('usuario.gerenciar', 'permissao.gerenciar', 'auditoria.visualizar', 'analytics.visualizar', 'caso.visualizar');
        ");

        // 2. Initial Admin User (Hash for 'Password123!')
        var adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!", workFactor: 11);

        Insert.IntoTable("users")
            .Row(new
            {
                name = "Administrador de Segurança",
                email = "admin@tracecore.local",
                password_hash = adminPasswordHash,
                status = "Active",
                created_at = System.DateTime.UtcNow,
                row_version = 1
            });

        // 3. Vincular Admin ao departamento 'Gestão' e papel 'Admin Segurança'
        Execute.Sql(@"
            INSERT INTO user_departments (user_id, department_id)
            SELECT u.id, d.id FROM users u, departments d
            WHERE u.email = 'admin@tracecore.local' AND d.name = 'Gestão';

            INSERT INTO user_roles (user_id, role_id)
            SELECT u.id, r.id FROM users u, roles r
            WHERE u.email = 'admin@tracecore.local' AND r.name = 'Admin Segurança';
        ");
    }

    public override void Down()
    {
        Execute.Sql("DELETE FROM user_roles WHERE user_id IN (SELECT id FROM users WHERE email = 'admin@tracecore.local');");
        Execute.Sql("DELETE FROM user_departments WHERE user_id IN (SELECT id FROM users WHERE email = 'admin@tracecore.local');");
        Execute.Sql("DELETE FROM users WHERE email = 'admin@tracecore.local';");
        Delete.FromTable("role_permissions").AllRows();
    }
}
