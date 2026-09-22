using System;
using FluentMigrator;

namespace TraceCore.Infrastructure.Migrations;

[Migration(2026092231, "Versionamento Inteligente - Fase 5: Dev Seed de massa de testes (Atlas Transportes, TMS 5.17.9/5.18.2/5.18.4, correcao CT-e e rollouts)")]
public class M20260922_31_SeedVersionManagementDevData : Migration
{
    public override void Up()
    {
        // 1. Inserir Produto PRD-TMS caso não exista
        Execute.Sql(@"
            INSERT INTO products (code, name, description, status, created_at)
            SELECT 'PRD-TMS', 'TMS Transportes', 'Sistema de Gestão de Transportes e Logística', 'Active', UTC_TIMESTAMP()
            WHERE NOT EXISTS (SELECT 1 FROM products WHERE code = 'PRD-TMS');
        ");

        // 2. Inserir Clientes Atlas Transportes e Rápido Expresso
        Execute.Sql(@"
            INSERT INTO clients (code, name, status, created_at)
            SELECT 'CLI-ATLAS', 'Atlas Transportes', 'Active', UTC_TIMESTAMP()
            WHERE NOT EXISTS (SELECT 1 FROM clients WHERE code = 'CLI-ATLAS');

            INSERT INTO clients (code, name, status, created_at)
            SELECT 'CLI-RAPIDO', 'Rápido Expresso Logística', 'Active', UTC_TIMESTAMP()
            WHERE NOT EXISTS (SELECT 1 FROM clients WHERE code = 'CLI-RAPIDO');
        ");

        // 3. Inserir Componente Emissão de CT-e no TMS
        Execute.Sql(@"
            INSERT INTO components (product_id, name, code, component_type, status, created_at)
            SELECT p.id, 'Emissão de CT-e', 'CTE-EMISSOR', 'Module', 'Active', UTC_TIMESTAMP()
            FROM products p
            WHERE p.code = 'PRD-TMS'
              AND NOT EXISTS (
                  SELECT 1 FROM components c WHERE c.product_id = p.id AND c.code = 'CTE-EMISSOR'
              );
        ");

        // 4. Inserir Versões do TMS: 5.17.9, 5.18.2, 5.18.4
        Execute.Sql(@"
            -- 5.17.9
            INSERT INTO product_versions (product_id, version_label, status, release_order, released_at)
            SELECT p.id, '5.17.9', 'Active',
                   COALESCE((SELECT MAX(pv2.release_order) FROM product_versions pv2 WHERE pv2.product_id = p.id), 0) + 1,
                   '2026-01-10 10:00:00'
            FROM products p
            WHERE p.code = 'PRD-TMS'
              AND NOT EXISTS (SELECT 1 FROM product_versions pv WHERE pv.product_id = p.id AND pv.version_label = '5.17.9');

            -- 5.18.2
            INSERT INTO product_versions (product_id, version_label, status, release_order, released_at)
            SELECT p.id, '5.18.2', 'Active',
                   COALESCE((SELECT MAX(pv2.release_order) FROM product_versions pv2 WHERE pv2.product_id = p.id), 0) + 1,
                   '2026-02-15 10:00:00'
            FROM products p
            WHERE p.code = 'PRD-TMS'
              AND NOT EXISTS (SELECT 1 FROM product_versions pv WHERE pv.product_id = p.id AND pv.version_label = '5.18.2');

            -- 5.18.4
            INSERT INTO product_versions (product_id, version_label, status, release_order, released_at)
            SELECT p.id, '5.18.4', 'Active',
                   COALESCE((SELECT MAX(pv2.release_order) FROM product_versions pv2 WHERE pv2.product_id = p.id), 0) + 1,
                   '2026-03-20 10:00:00'
            FROM products p
            WHERE p.code = 'PRD-TMS'
              AND NOT EXISTS (SELECT 1 FROM product_versions pv WHERE pv.product_id = p.id AND pv.version_label = '5.18.4');
        ");

        // 5. Histórico de Contexto Técnico para o Atlas (passou pela 5.17.9, atualmente na 5.18.2)
        Execute.Sql(@"
            -- Contexto histórico encerrado em 5.17.9
            INSERT INTO client_technical_contexts (client_id, product_id, product_version_id, effective_from, effective_to, status, created_at)
            SELECT c.id, p.id, v.id, '2026-01-10 10:00:00', '2026-02-15 10:00:00', 'Active', UTC_TIMESTAMP()
            FROM clients c
            CROSS JOIN products p
            CROSS JOIN product_versions v
            WHERE c.code = 'CLI-ATLAS' AND p.code = 'PRD-TMS' AND v.version_label = '5.17.9' AND v.product_id = p.id
              AND NOT EXISTS (
                  SELECT 1 FROM client_technical_contexts ctc 
                  WHERE ctc.client_id = c.id AND ctc.product_id = p.id AND ctc.product_version_id = v.id
              );

            -- Contexto vigente ativo em 5.18.2
            INSERT INTO client_technical_contexts (client_id, product_id, product_version_id, effective_from, effective_to, status, created_at)
            SELECT c.id, p.id, v.id, '2026-02-15 10:00:00', NULL, 'Active', UTC_TIMESTAMP()
            FROM clients c
            CROSS JOIN products p
            CROSS JOIN product_versions v
            WHERE c.code = 'CLI-ATLAS' AND p.code = 'PRD-TMS' AND v.version_label = '5.18.2' AND v.product_id = p.id
              AND NOT EXISTS (
                  SELECT 1 FROM client_technical_contexts ctc 
                  WHERE ctc.client_id = c.id AND ctc.product_id = p.id AND ctc.effective_to IS NULL
              );

            -- Contexto vigente ativo para Rápido Expresso em 5.18.4
            INSERT INTO client_technical_contexts (client_id, product_id, product_version_id, effective_from, effective_to, status, created_at)
            SELECT c.id, p.id, v.id, '2026-03-22 10:00:00', NULL, 'Active', UTC_TIMESTAMP()
            FROM clients c
            CROSS JOIN products p
            CROSS JOIN product_versions v
            WHERE c.code = 'CLI-RAPIDO' AND p.code = 'PRD-TMS' AND v.version_label = '5.18.4' AND v.product_id = p.id
              AND NOT EXISTS (
                  SELECT 1 FROM client_technical_contexts ctc 
                  WHERE ctc.client_id = c.id AND ctc.product_id = p.id AND ctc.effective_to IS NULL
              );
        ");

        // 6. Correção na versão 5.18.4 (Access Violation na emissão de CT-e)
        Execute.Sql(@"
            INSERT INTO product_version_changes (product_version_id, change_type, title, description, component_id, error_code, created_at)
            SELECT v.id, 'Fix', 'Corrigido Access Violation durante emissão de CT-e',
                   'Correção no gerenciamento de memória do emissor de CT-e que causava Access Violation em lotes com mais de 50 documentos.',
                   comp.id, 'Access Violation', '2026-03-19 15:00:00'
            FROM product_versions v
            JOIN products p ON p.id = v.product_id AND p.code = 'PRD-TMS'
            LEFT JOIN components comp ON comp.product_id = p.id AND comp.code = 'CTE-EMISSOR'
            WHERE v.version_label = '5.18.4'
              AND NOT EXISTS (
                  SELECT 1 FROM product_version_changes pvc 
                  WHERE pvc.product_version_id = v.id AND pvc.title = 'Corrigido Access Violation durante emissão de CT-e'
              );
        ");

        // 7. Casos de Suporte do Atlas (um na 5.17.9 e um na 5.18.2)
        Execute.Sql(@"
            -- Caso 1 na 5.17.9
            INSERT INTO cases (case_number, external_reference, source_type, client_id, product_id, product_version_id, normalized_summary, original_report, status, severity, error_code, opened_at, created_at, updated_at, row_version)
            SELECT 9101, 'CAS-ATL-01', 'Manual', c.id, p.id, v.id, 'Falha ao autorizar CT-e em lote',
                   'Cliente relata que ao autorizar lote de CT-e ocorre travamento com código Access Violation.',
                   'Closed', 'High', 'Access Violation', '2026-01-20 14:00:00', '2026-01-20 14:00:00', '2026-01-20 14:00:00', 1
            FROM clients c
            CROSS JOIN products p
            CROSS JOIN product_versions v
            WHERE c.code = 'CLI-ATLAS' AND p.code = 'PRD-TMS' AND v.version_label = '5.17.9' AND v.product_id = p.id
              AND NOT EXISTS (SELECT 1 FROM cases WHERE case_number = 9101);

            -- Caso 2 na 5.18.2
            INSERT INTO cases (case_number, external_reference, source_type, client_id, product_id, product_version_id, normalized_summary, original_report, status, severity, error_code, opened_at, created_at, updated_at, row_version)
            SELECT 9102, 'CAS-ATL-02', 'Manual', c.id, p.id, v.id, 'Access Violation intermitente ao emitir CT-e',
                   'Access Violation persiste ao emitir múltiplos CT-e simultâneos no módulo de transporte.',
                   'InInvestigation', 'High', 'Access Violation', '2026-02-28 09:30:00', '2026-02-28 09:30:00', '2026-02-28 09:30:00', 1
            FROM clients c
            CROSS JOIN products p
            CROSS JOIN product_versions v
            WHERE c.code = 'CLI-ATLAS' AND p.code = 'PRD-TMS' AND v.version_label = '5.18.2' AND v.product_id = p.id
              AND NOT EXISTS (SELECT 1 FROM cases WHERE case_number = 9102);
        ");

        // 8. Vínculo entre a Correção da 5.18.4 e o Caso CAS-ATL-02
        Execute.Sql(@"
            INSERT INTO product_version_change_cases (product_version_change_id, case_id, relation_type, linked_at)
            SELECT pvc.id, cs.id, 'FixedBy', UTC_TIMESTAMP()
            FROM product_version_changes pvc
            CROSS JOIN cases cs
            WHERE pvc.title = 'Corrigido Access Violation durante emissão de CT-e'
              AND cs.case_number = 9102
              AND NOT EXISTS (
                  SELECT 1 FROM product_version_change_cases pvcc 
                  WHERE pvcc.product_version_change_id = pvc.id AND pvcc.case_id = cs.id
              );
        ");

        // 9. Destinações (Rollout) na versão 5.18.4
        Execute.Sql(@"
            -- Atlas Planejada (Planned)
            INSERT INTO product_version_assignments (product_version_id, client_id, status, planned_at, notes, created_at)
            SELECT v.id, c.id, 'Planned', UTC_TIMESTAMP(), 'Rollout agendado para o fim do mês', UTC_TIMESTAMP()
            FROM product_versions v
            CROSS JOIN clients c
            JOIN products p ON p.id = v.product_id AND p.code = 'PRD-TMS'
            WHERE v.version_label = '5.18.4' AND c.code = 'CLI-ATLAS'
              AND NOT EXISTS (
                  SELECT 1 FROM product_version_assignments pva 
                  WHERE pva.product_version_id = v.id AND pva.client_id = c.id
              );

            -- Rápido Expresso Implantada (Deployed)
            INSERT INTO product_version_assignments (product_version_id, client_id, status, planned_at, deployed_at, notes, created_at)
            SELECT v.id, c.id, 'Deployed', '2026-03-22 10:00:00', '2026-03-22 10:00:00', 'Implantado com sucesso em piloto', UTC_TIMESTAMP()
            FROM product_versions v
            CROSS JOIN clients c
            JOIN products p ON p.id = v.product_id AND p.code = 'PRD-TMS'
            WHERE v.version_label = '5.18.4' AND c.code = 'CLI-RAPIDO'
              AND NOT EXISTS (
                  SELECT 1 FROM product_version_assignments pva 
                  WHERE pva.product_version_id = v.id AND pva.client_id = c.id
              );
        ");
    }

    public override void Down()
    {
        // Operação aditiva de dev seed - down mantido limpo e seguro
    }
}
