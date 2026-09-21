-- ============================================================
-- TRACECORE - MASSA DE TESTES REALISTA (UNIVERSO LOGISTICA)
-- SOMENTE DESENVOLVIMENTO / NAO EXECUTAR EM PRODUCAO
-- ============================================================
-- Empresa ficticia: fornecedora de software para transportes e
-- logistica. Portfolio:
--   * TMS Desktop (Delphi + Firebird 5)           -> PRD-TMS
--   * Servicos Delphi (filas, CT-e/MDF-e, sync)   -> PRD-SRV
--   * Portal Web (Angular + Laravel)              -> PRD-WEB
--   * App Mobile (Flutter)                        -> PRD-MOB
--   * API Comercial                               -> PRD-API
--   * Conectores / Integracoes (SAP, TOTVS, EDI)  -> PRD-INT
-- Carga deterministica: 1.280 casos reais por template,
-- 55 familias causais, 12 clientes, 200 itens de conhecimento,
-- ~560 relacoes, busca/AI/auditoria populados.
-- ============================================================

SET NAMES utf8mb4;
SET CHARACTER SET utf8mb4;

-- ============================================================
-- SECAO 1: CLIENTES (12 - empresas de transporte/logistica)
-- ============================================================
INSERT INTO clients (code, name, status, created_at, updated_at, notes) VALUES
  ('CLI-001','Translog Rapido Transportes','Active',NOW(),NOW(),'Transporte rodoviario de cargas gerais. Customizacoes: emissao CT-e dedicada, tabela de frete por UF, impressao DANFE propria.'),
  ('CLI-002','CargaVix Logistica','Active',NOW(),NOW(),'Operacao portuaria e distribuicao regional. Customizacoes: integracao SAP de notas, numeracao propria, relatorio de conferencia.'),
  ('CLI-003','Rodovix Cargas e Encomendas','Active',NOW(),NOW(),'Cargas fracionadas e encomendas. Customizacoes: roteirizacao por CEP, app de coleta, conciliacao bancaria.'),
  ('CLI-004','ArmazemPlus Estoque Avancado','Active',NOW(),NOW(),'Operacao de armazenagem (3PL). Customizacoes: WMS leve, etiquetas de enderecamento, integracao EDI com varejo.'),
  ('CLI-005','SulFretes Transportes','Active',NOW(),NOW(),'Fretes pesados para o Sul. Customizacoes: calculo por eixo/peso, MDF-e vinculado, relatorio de pedagio.'),
  ('CLI-006','DistribuNordeste Alimentos','Active',NOW(),NOW(),'Distribuicao de alimentos. Customizacoes: validacao por lote, temperatura em coleta, integracao com nota de consumidor.'),
  ('CLI-007','CentroOeste Cargas','Active',NOW(),NOW(),'Corredor GT c/ MT. Customizacoes: tabela frete spot, sincronizacao Firebird em filiais, agendamento de coleta.'),
  ('CLI-008','FrioEx Logistica Refrigerada','Active',NOW(),NOW(),'Transporte refrigerado. Customizacoes: sensores de temperatura, alarme de rota, integracao com IoT.'),
  ('CLI-009','PortoSul Operacoes Portuarias','Active',NOW(),NOW(),'Conteineres e operacao no porto. Customizacoes: integracao TOTVS, modais via suite, relatorios alfandegarios.'),
  ('CLI-010','AgroTrans Commodities','Active',NOW(),NOW(),'Escoamento de graos. Customizacoes: MDF-e por talhao, RO/OT entregas, integracao com balanca.'),
  ('CLI-011','UrbanoExpress Last Mile','Active',NOW(),NOW(),'Entregas urbanas mesmas horas. Customizacoes: app entregador, fotos de comprovante, ranking de rotas.'),
  ('CLI-012','SaudeLog Medicamentos','Active',NOW(),NOW(),'Farmacos e insumos. Customizacoes: rastreabilidade por lote, requisitos ANVISA, controle de temperatura.');

-- Unidades: 2 por cliente (24)
INSERT INTO client_units (client_id, code, name, status, created_at, updated_at)
SELECT c.id, CONCAT(c.code,'-MAT'), CONCAT(c.name,' - Matriz'), 'Active', NOW(), NOW()
FROM clients c;
INSERT INTO client_units (client_id, code, name, status, created_at, updated_at)
SELECT c.id, CONCAT(c.code,'-FIL'), CONCAT(c.name,' - Filial Operacional'), 'Active', NOW(), NOW()
FROM clients c;

-- Contextos tecnicos: produtos licenciados por cliente
INSERT INTO client_technical_contexts (client_id, client_unit_id, product_id, environment_id, status, effective_from, created_at, updated_at, created_by, updated_by)
SELECT c.id, cu.id, p.id, e.id, 'Active', DATE_SUB(NOW(), INTERVAL 400 DAY), NOW(), NOW(), 1, 1
FROM clients c
JOIN client_units cu ON cu.client_id = c.id AND cu.code LIKE '%-MAT'
JOIN products p ON p.code IN ('PRD-TMS','PRD-WEB','PRD-MOB')
JOIN environments e ON e.name = 'Produção';

INSERT INTO client_technical_contexts (client_id, client_unit_id, product_id, status, effective_from, created_at, updated_at, created_by, updated_by)
SELECT c.id, cu.id, p.id, 'Active', DATE_SUB(NOW(), INTERVAL 300 DAY), NOW(), NOW(), 1, 1
FROM clients c
JOIN client_units cu ON cu.client_id = c.id AND cu.code LIKE '%-MAT'
JOIN products p ON p.code IN ('PRD-SRV','PRD-API')
WHERE c.code IN ('CLI-002','CLI-004','CLI-005','CLI-007','CLI-009','CLI-010');

INSERT INTO client_technical_contexts (client_id, client_unit_id, product_id, status, effective_from, created_at, updated_at, created_by, updated_by)
SELECT c.id, cu.id, p.id, 'Active', DATE_SUB(NOW(), INTERVAL 200 DAY), NOW(), NOW(), 1, 1
FROM clients c
JOIN client_units cu ON cu.client_id = c.id AND cu.code LIKE '%-MAT'
JOIN products p ON p.code = 'PRD-INT'
WHERE c.code IN ('CLI-002','CLI-004','CLI-008','CLI-009');

-- ============================================================
-- SECAO 2: AMBIENTES
-- ============================================================
INSERT INTO environments (name, environment_type) VALUES
  ('Produção','Production'),
  ('Homologação','Staging'),
  ('Desenvolvimento','Development');

-- ============================================================
-- SECAO 3: PRODUTOS E VERSOES
-- ============================================================
INSERT INTO products (code, name, description, status, created_at, updated_at, is_external) VALUES
  ('PRD-TMS','TMS Desktop','Sistema de gestao de transportes desktop (Delphi). Emissao CT-e/NF-e/MDF-e, frota, roteirizacao e financeiro de fretes. Banco Firebird 5 por filial.','Active',NOW(),NOW(),0),
  ('PRD-SRV','Servicos de Integracao (Delphi)','Servicos Windows (Delphi) responsaveis por filas, emissao em lote de CT-e/MDF-e, sincronizacao local-nuvem, backup e watchdog.','Active',NOW(),NOW(),0),
  ('PRD-WEB','Portal Web TMS','Portal da transportadora (Angular + Laravel) com dashboard, geocoding, relatorios e autosservico do embarcador.','Active',NOW(),NOW(),0),
  ('PRD-MOB','TMS Mobile','Aplicativo do motorista/entregador (Flutter) com roteiro, coletas, comprovante digital e rastreamento GPS offline.','Active',NOW(),NOW(),0),
  ('PRD-API','API Comercial','Gateway de APIs REST para autosservico e integracao com sistemas dos embarcadores.','Active',NOW(),NOW(),0),
  ('PRD-INT','Conectores e Integracoes','Adaptadores de integracao com SAP ERP, TOTVS Protheus, EDI de parceiros, SEFAZ, bancos e provedores de rastreio.','Active',NOW(),NOW(),0);

-- Versoes
INSERT INTO product_versions (product_id, version_label, released_at, end_of_support_at, status)
SELECT p.id, v.version_label, v.released_at, v.end_support, 'Active'
FROM products p
JOIN (SELECT 'PRD-TMS' code, '3.6.0' version_label, '2026-02-10 00:00:00' released_at, NULL end_support
      UNION ALL SELECT 'PRD-TMS','3.5.1','2025-11-03 00:00:00', NULL
      UNION ALL SELECT 'PRD-TMS','3.5.0','2025-06-16 00:00:00', NULL
      UNION ALL SELECT 'PRD-TMS','3.4.0','2025-02-24 00:00:00', NULL
      UNION ALL SELECT 'PRD-TMS','3.3.0','2024-11-11 00:00:00', NULL
      UNION ALL SELECT 'PRD-SRV','2.2.0','2026-01-19 00:00:00', NULL
      UNION ALL SELECT 'PRD-SRV','2.1.0','2025-08-04 00:00:00', NULL
      UNION ALL SELECT 'PRD-SRV','2.0.0','2025-03-10 00:00:00', NULL
      UNION ALL SELECT 'PRD-WEB','5.2.0','2026-04-27 00:00:00', NULL
      UNION ALL SELECT 'PRD-WEB','5.1.0','2025-12-08 00:00:00', NULL
      UNION ALL SELECT 'PRD-WEB','5.0.0','2025-07-21 00:00:00', NULL
      UNION ALL SELECT 'PRD-MOB','1.6.0','2026-03-23 00:00:00', NULL
      UNION ALL SELECT 'PRD-MOB','1.5.0','2025-10-13 00:00:00', NULL
      UNION ALL SELECT 'PRD-MOB','1.4.0','2025-05-12 00:00:00', NULL
      UNION ALL SELECT 'PRD-API','4.2.0','2026-05-11 00:00:00', NULL
      UNION ALL SELECT 'PRD-API','4.1.0','2025-09-15 00:00:00', NULL
      UNION ALL SELECT 'PRD-API','4.0.0','2025-04-07 00:00:00', NULL
      UNION ALL SELECT 'PRD-INT','3.1.0','2026-02-02 00:00:00', NULL
      UNION ALL SELECT 'PRD-INT','3.0.0','2025-06-30 00:00:00', NULL
     ) v ON v.code = p.code;

-- Perfis tecnicos por produto
INSERT INTO product_technical_profiles (product_id, business_purpose, architecture_summary, frontend_stack, backend_stack, primary_database, runtime_platform, hosting_model, authentication_model, observability_stack, deployment_model, vendor, support_notes, known_constraints, investigation_notes, external_research_policy, created_at, created_by, updated_at, updated_by)
SELECT p.id,
  CASE p.code
    WHEN 'PRD-TMS' THEN 'Gestao operacional do transporte rodoviario.'
    WHEN 'PRD-SRV' THEN 'Automatizacao de fluxos fiscais e sincronizacao.'
    WHEN 'PRD-WEB' THEN 'Portal e autosservico de embarcadores.'
    WHEN 'PRD-MOB' THEN 'Execucao de roteiros em campo com offline.'
    WHEN 'PRD-API' THEN 'Exposicao controlada de dados via REST.'
    ELSE 'Integracao com sistemas legados dos clientes.' END,
  CASE p.code
    WHEN 'PRD-TMS' THEN 'Cliente-servidor com banco Firebird 5 por filial.'
    WHEN 'PRD-SRV' THEN 'Servicos Windows executados em cada filial.'
    WHEN 'PRD-WEB' THEN 'SPA Angular consumindo API Laravel.'
    WHEN 'PRD-MOB' THEN 'Aplicativo Flutter com sincronizacao offline.'
    WHEN 'PRD-API' THEN 'API REST com gateway e quotas.'
    ELSE 'Adaptadores REST/WS para sistemas parceiros.' END,
  CASE p.code WHEN 'PRD-WEB' THEN 'Angular 18' WHEN 'PRD-MOB' THEN 'Flutter 3' ELSE 'VCL (Delphi)' END,
  CASE p.code WHEN 'PRD-WEB' THEN 'Laravel 11 (PHP)' WHEN 'PRD-API' THEN '.NET 8' WHEN 'PRD-MOB' THEN NULL ELSE 'Delphi 11' END,
  CASE p.code WHEN 'PRD-TMS' THEN 'Firebird 5 (por filial)' WHEN 'PRD-WEB' THEN 'PostgreSQL' WHEN 'PRD-API' THEN 'PostgreSQL' ELSE NULL END,
  CASE p.code WHEN 'PRD-TMS' THEN 'Windows 10/11' WHEN 'PRD-SRV' THEN 'Windows Server' WHEN 'PRD-MOB' THEN 'Android 8+ / iOS' ELSE 'Linux (Docker)' END,
  'On-premise por filial',
  CASE p.code WHEN 'PRD-WEB' THEN 'SSO via Laravel' WHEN 'PRD-MOB' THEN 'OAuth2 + PUSH' ELSE 'Login local + AD' END,
  'Logs de aplicacao + monitoramento dos servicos',
  'Pacote de instalacao por filial',
  NULL,
  'Historico: falhas de emissao concentram-se em filiais com Firebird desatualizado.',
  'Nao depende de chamadas externas para opera; emissao fiscal exige conexao com SEFAZ.',
  'Investigacao prioritaria nas familias de CT-e/MDF-e e sincronizacao.',
  'OfficialOnly', NOW(), 1, NOW(), 1
FROM products p;

-- Tecnologias
INSERT INTO technologies (name) VALUES
  ('Delphi'),('Firebird'),('Laravel'),('Angular'),('Flutter'),('PostgreSQL'),
  ('Redis'),('RabbitMQ'),('Docker'),('Kubernetes'),('REST'),('SOAP'),
  ('SAP RFC'),('TOTVS Protheus'),('NF-e/CT-e (SEFAZ)'),('GPS/Telemetria');

-- Relacao produto x tecnologia
INSERT INTO product_technologies (product_id, technology_id, created_at)
SELECT p.id, t.id, NOW()
FROM products p
JOIN technologies t ON (
  (p.code='PRD-TMS' AND t.name IN ('Delphi','Firebird','NF-e/CT-e (SEFAZ)','GPS/Telemetria'))
  OR (p.code='PRD-SRV' AND t.name IN ('Delphi','Firebird','RabbitMQ','NF-e/CT-e (SEFAZ)'))
  OR (p.code='PRD-WEB' AND t.name IN ('Angular','Laravel','PostgreSQL','Redis'))
  OR (p.code='PRD-MOB' AND t.name IN ('Flutter','REST','GPS/Telemetria'))
  OR (p.code='PRD-API' AND t.name IN ('REST','Redis','Docker','Kubernetes'))
  OR (p.code='PRD-INT' AND t.name IN ('SAP RFC','TOTVS Protheus','SOAP','REST','NF-e/CT-e (SEFAZ)'))
);

-- Fontes tecnicas por produto
INSERT INTO product_technical_sources (product_id, name, source_type, url, description, trust_level, is_active, created_at, created_by, updated_at)
SELECT p.id, s.name, s.source_type, s.url, s.description, s.trust_level, 1, NOW(), 1, NOW()
FROM products p
JOIN (SELECT 'PRD-TMS' code, 'Base de conhecimento interna - Desktop' name, 'InternalDocumentation' source_type, NULL url, 'Runbooks de instalacao e parametrizacao TMS.' description, 'Internal' trust_level
      UNION ALL SELECT 'PRD-TMS','Manual de parametrizacao SEFAZ','OfficialDocumentation','https://www.nfe.fazenda.gov.br/','Documentacao oficial do leiaute CT-e/NF-e.','Official'
      UNION ALL SELECT 'PRD-WEB','Wiki tecnica do Portal','Wiki','https://wiki.tracecore.local/portal','Documentacao da arquitetura Angular/Laravel.','Internal'
      UNION ALL SELECT 'PRD-MOB','Runbook de sincronizacao offline','Runbook',NULL,'Procedimentos para diagnostico de sync.','Internal'
      UNION ALL SELECT 'PRD-INT','SAP Developer Portal','VendorKnowledgeBase','https://api.sap.com/','Documentacao das RFCs utilizadas.','Trusted'
      UNION ALL SELECT 'PRD-SRV','Swagger dos servicos internos','Swagger','https://svc.tracecore.local/swagger','Especificacao dos endpoints dos servicos.','Internal'
     ) s ON s.code = p.code;

-- Dominios de pesquisa externa
INSERT INTO product_external_research_domains (product_id, domain, description, is_active, created_at, created_by)
SELECT p.id, d.domain, d.description, 1, NOW(), 1
FROM products p
JOIN (SELECT 'PRD-TMS' code, 'nfe.fazenda.gov.br' domain, 'Documentacao CT-e/NF-e/MDF-e.' description
      UNION ALL SELECT 'PRD-INT','api.sap.com','API Hub SAP.'
      UNION ALL SELECT 'PRD-INT','dev.frameworks.cors.apps','Documentacao oficiais de provedores GPS.'
     ) d ON d.code = p.code;

-- ============================================================
-- SECAO 4: COMPONENTES (43)
-- ============================================================
INSERT INTO components (product_id, code, name, component_type, description, owner_department_id, status, created_at, updated_at)
SELECT p.id, c.code, c.name, c.component_type, c.description,
       (SELECT id FROM departments d WHERE d.name = c.dept), 'Active', NOW(), NOW()
FROM products p
JOIN (SELECT 'PRD-TMS' pcode, 'FAT-CTE' code, 'Emissao de CT-e' name, 'Module' component_type, 'Emissao e cancelamento de Conhecimento de Transporte Eletronico.' description, 'Desenvolvimento Desktop' dept
      UNION ALL SELECT 'PRD-TMS','FAT-NFE','Emissao de NF-e','Module','Emissao de Nota Fiscal Eletronica de servico de transporte.', 'Desenvolvimento Desktop'
      UNION ALL SELECT 'PRD-TMS','FAT-MDFE','Manifesto Eletronico (MDF-e)','Module','Vinculacao e emissao do Manifesto Eletronico de Documentos Fiscais.','Desenvolvimento Desktop'
      UNION ALL SELECT 'PRD-TMS','MOD-FROTA','Gestao de Frota','Module','Cadastro de veiculos, motoristas, vencimentos e manutencao.','Desenvolvimento Desktop'
      UNION ALL SELECT 'PRD-TMS','MOD-ROTA','Roteirizacao','Module','Planejamento de rotas, sequenciamento e estimativa.','Desenvolvimento Desktop'
      UNION ALL SELECT 'PRD-TMS','MOD-CARGA','Gestao de Cargas','Module','Agrupamento de pedidos em cargas e rateio.','Desenvolvimento Desktop'
      UNION ALL SELECT 'PRD-TMS','MOD-OC','Ordens de Coleta','Module','Abertura e acompanhamento de ordens de coleta.','Suporte'
      UNION ALL SELECT 'PRD-TMS','MOD-OS','Ordens de Servico','Module','Ordens de servico manutencao/assistencia.','Suporte'
      UNION ALL SELECT 'PRD-TMS','MOD-FIN','Financeiro de Fretes','Module','Contas a pagar/receber de fretes, duplicatas.','Desenvolvimento Desktop'
      UNION ALL SELECT 'PRD-TMS','CORE-CALC','Calculadora de Fretes','Core','Calculo por tabela, eixo, peso e UF.','Desenvolvimento Desktop'
      UNION ALL SELECT 'PRD-TMS','CORE-ACESSO','Autenticacao Desktop','Core','Login local, perfil e permissoes por usuario.','Seguranca'
      UNION ALL SELECT 'PRD-TMS','DB-FIREBIRD','Camada de Dados Firebird','Infraestrutura','Acesso ao banco Firebird 5 por filial.','Banco de Dados'
      UNION ALL SELECT 'PRD-SRV','SRV-SYNC','Sincronizador Local-Nuvem','Servico','Sincronizacao de dados locais para o datacenter.','Banco de Dados'
      UNION ALL SELECT 'PRD-SRV','SRV-INTEGRADOR','Orquestrador de Servicos','Servico','Orquestracao dos fluxos entre servicos e API.','Integracoes'
      UNION ALL SELECT 'PRD-SRV','SRV-CTE-EMIS','Emissor CT-e em Lote','Servico','Emissao em lote de CT-e com retry e agenda.','Desenvolvimento Desktop'
      UNION ALL SELECT 'PRD-SRV','SRV-MDFE-MON','Monitor de MDF-e','Servico','Acompanhamento do ciclo de vida do MDF-e.','Desenvolvimento Desktop'
      UNION ALL SELECT 'PRD-SRV','SRV-BACKUP','Backup e Rotacionamento','Servico','Backup dos bancos Firebird e envio para nuvem.','Infraestrutura'
      UNION ALL SELECT 'PRD-SRV','SRV-WATCHDOG','Watchdog de Servicos','Servico','Supervisao e reinicio automatico de servicos.','Infraestrutura'
      UNION ALL SELECT 'PRD-WEB','WEB-FRONT','Frontend Angular','Web','Interface com o usuario do portal (Angular).','Desenvolvimento Web'
      UNION ALL SELECT 'PRD-WEB','WEB-API','API Laravel','Web','Backend do portal (Laravel/PHP).','Desenvolvimento Web'
      UNION ALL SELECT 'PRD-WEB','WEB-AUTH','SSO e Autenticacao','Seguranca','Autenticacao SSO e gestao de sessoes do portal.','Seguranca'
      UNION ALL SELECT 'PRD-WEB','WEB-REPORT','Relatorios','Web','Geracao de relatorios operacionais e fiscais.','Desenvolvimento Web'
      UNION ALL SELECT 'PRD-WEB','WEB-DASH','Dashboard Operacional','Web','Dashboards de volume, custo e SLA do cliente.','Desenvolvimento Web'
      UNION ALL SELECT 'PRD-WEB','WEB-GEO','Geocoding e Mapas','Web','Geocodificacao de enderecos e exibicao de rotas.','Desenvolvimento Web'
      UNION ALL SELECT 'PRD-MOB','MOB-ROTA','Roteiro do Motorista','Mobile','Visualizacao e execucao do roteiro de entregas.','Mobile'
      UNION ALL SELECT 'PRD-MOB','MOB-COLETA','Coletas e Comprovante','Mobile','Registro de coleta/entrega, foto e assinatura.','Mobile'
      UNION ALL SELECT 'PRD-MOB','MOB-SYNC','Sincronizacao Offline','Mobile','Sincronizacao de dados offline com a nuvem.','Mobile'
      UNION ALL SELECT 'PRD-MOB','MOB-RASTRO','Rastreamento GPS','Mobile','Captura de posicao GPS e envio periodico.','Mobile'
      UNION ALL SELECT 'PRD-API','API-GW','Gateway de API','API','Proxy, autenticacao de chave e agregacao REST.','Desenvolvimento Web'
      UNION ALL SELECT 'PRD-API','API-RATE','Quotas e Rate Limit','API','Controle de quota, rate limiting e cache.','Infraestrutura'
      UNION ALL SELECT 'PRD-API','API-AUTH','Tokens OAuth2','API','Emissao e validacao de tokens de integracao.','Seguranca'
      UNION ALL SELECT 'PRD-INT','INT-SAP','Conector SAP ERP','Integracao','Integracao de notas e numeracao via RFC SAP.','Integracoes'
      UNION ALL SELECT 'PRD-INT','INT-TOTVS','Conector TOTVS Protheus','Integracao','API de integracao com o Protheus.','Integracoes'
      UNION ALL SELECT 'PRD-INT','INT-EDI','EDI de Parceiros','Integracao','Troca de arquivos EDI com embarcadores.','Integracoes'
      UNION ALL SELECT 'PRD-INT','INT-BANK','Conciliacao Bancaria','Integracao','Upload de boletos e conciliacao de pagamentos.','Integracoes'
      UNION ALL SELECT 'PRD-INT','INT-CTE-GOV','Integracao SEFAZ','Integracao','Comunicacao com SEFAZ via WS (CT-e/NF-e/MDF-e).','Integracoes'
      UNION ALL SELECT 'PRD-INT','INT-RASTREO','Provedores de Rastreio','Integracao','Adaptadores para provedores de GPS.','Integracoes'
      UNION ALL SELECT 'PRD-INT','INT-WHATS','Notificacoes','Integracao','Disparo de notificacoes por e-mail/WhatsApp.','Integracoes'
      UNION ALL SELECT 'PRD-INT','INT-CORREIOS','Correios e Log','Integracao','Consulta de rastreio e etiquetas Correios.','Integracoes'
      UNION ALL SELECT NULL,'BASE-MONITORIA','Monitoria Central','Infraestrutura','Painel central de monitoria do fornecedor.','Infraestrutura'
      UNION ALL SELECT NULL,'BASE-TICKET','Sistema de Chamados','Infraestrutura','Sistema de atendimento/helpdesk do fornecedor.','Suporte'
     ) c ON c.pcode = p.code;

-- Dependencias entre componentes (14 pares, chave unica (src,tgt,type))
INSERT INTO component_dependencies (source_component_id, target_component_id, dependency_type, criticality, description, valid_from, created_at)
SELECT sc.id, tc.id, d.dependency_type, d.criticality, d.description, NOW(), NOW()
FROM (SELECT 'FAT-CTE' sc, 'DB-FIREBIRD' tc, 'Runtime' dependency_type, 'High' criticality, 'Emissao depende da camada de dados Firebird.' description
      UNION ALL SELECT 'FAT-CTE','INT-CTE-GOV','Runtime','Critical','Emissao depende do conector SEFAZ.'
      UNION ALL SELECT 'FAT-NFE','INT-CTE-GOV','Runtime','Critical','NF-e depende do conector SEFAZ.'
      UNION ALL SELECT 'FAT-MDFE','INT-CTE-GOV','Runtime','High','Eventos do MDF-e via conector.'
      UNION ALL SELECT 'MOD-FIN','INT-BANK','Runtime','Medium','Financeiro usa o conector bancario.'
      UNION ALL SELECT 'SRV-SYNC','API-GW','Runtime','High','Sincronizacao consome o gateway.'
      UNION ALL SELECT 'SRV-INTEGRADOR','INT-SAP','Runtime','High','Orquestra RFC SAP.'
      UNION ALL SELECT 'SRV-INTEGRADOR','INT-EDI','Runtime','Medium','Troca de arquivos EDI.'
      UNION ALL SELECT 'WEB-FRONT','WEB-API','Runtime','High','SPA consome API Laravel.'
      UNION ALL SELECT 'WEB-API','DB-FIREBIRD','Runtime','High','API le dados locais para dashboards.'
      UNION ALL SELECT 'MOB-SYNC','API-GW','Runtime','High','Sync do app usa o gateway.'
      UNION ALL SELECT 'API-GW','API-RATE','Runtime','Medium','Quotas aplicadas no gateway.'
      UNION ALL SELECT 'API-GW','API-AUTH','Runtime','Critical','Validacao de tokens no gateway.'
      UNION ALL SELECT 'SRV-WATCHDOG','SRV-INTEGRADOR','Runtime','Medium','Watchdog supervisiona o integrador.'
     ) d
JOIN components sc ON sc.code = d.sc
JOIN components tc ON tc.code = d.tc;

-- Owners de componente
INSERT INTO component_owners (component_id, department_id, ownership_role, valid_from, created_at)
SELECT co.id, de.id, o.ownership_role, NOW(), NOW()
FROM components co
JOIN (SELECT 'Desenvolvimento Desktop' dept, 'Primary' ownership_role
      UNION ALL SELECT 'Desenvolvimento Web','Primary'
      UNION ALL SELECT 'Mobile','Primary'
      UNION ALL SELECT 'Infraestrutura','Primary'
      UNION ALL SELECT 'Banco de Dados','Primary'
      UNION ALL SELECT 'Suporte','Primary'
      UNION ALL SELECT 'Integracoes','Primary'
      UNION ALL SELECT 'Seguranca','Primary'
     ) o ON TRUE
JOIN departments de ON de.name = o.dept
WHERE co.owner_department_id = de.id;

-- ============================================================
-- SECAO 5: USUARIOS OPERACIONAIS (10) + VINCULOS
-- ============================================================
INSERT INTO users (name, email, password_hash, status, created_at, row_version) VALUES
  ('Carlos Mendes','carlos.mendes@tracecore.local','','Active',NOW(),1),
  ('Juliana Prado','juliana.prado@tracecore.local','','Active',NOW(),1),
  ('Rafael Nogueira','rafael.nogueira@tracecore.local','','Active',NOW(),1),
  ('Marina Lopes','marina.lopes@tracecore.local','','Active',NOW(),1),
  ('Diego Martins','diego.martins@tracecore.local','','Active',NOW(),1),
  ('Fernanda Rocha','fernanda.rocha@tracecore.local','','Active',NOW(),1),
  ('Paulo Cesar Andrade','paulo.andrade@tracecore.local','','Active',NOW(),1),
  ('Ana Beatriz Souza','ana.souza@tracecore.local','','Active',NOW(),1),
  ('Rodrigo Alves','rodrigo.alves@tracecore.local','','Active',NOW(),1),
  ('Talita Freitas','talita.freitas@tracecore.local','','Active',NOW(),1);

INSERT INTO user_departments (user_id, department_id)
SELECT u.id, d.id
FROM users u
JOIN (SELECT 'carlos.mendes@tracecore.local' email, 'Suporte' dept
      UNION ALL SELECT 'juliana.prado@tracecore.local','Desenvolvimento Desktop'
      UNION ALL SELECT 'rafael.nogueira@tracecore.local','Integracoes'
      UNION ALL SELECT 'marina.lopes@tracecore.local','Desenvolvimento Web'
      UNION ALL SELECT 'diego.martins@tracecore.local','Mobile'
      UNION ALL SELECT 'fernanda.rocha@tracecore.local','Banco de Dados'
      UNION ALL SELECT 'paulo.andrade@tracecore.local','Infraestrutura'
      UNION ALL SELECT 'ana.souza@tracecore.local','Suporte'
      UNION ALL SELECT 'rodrigo.alves@tracecore.local','Banco de Dados'
      UNION ALL SELECT 'talita.freitas@tracecore.local','Gestao'
     ) m ON m.email = u.email
JOIN departments d ON d.name = m.dept;

INSERT INTO user_roles (user_id, role_id)
SELECT u.id, r.id
FROM users u
JOIN roles r
WHERE r.name = 'Usuário Técnico'
  AND u.email IN ('carlos.mendes@tracecore.local','ana.souza@tracecore.local','paulo.andrade@tracecore.local');

INSERT INTO user_roles (user_id, role_id)
SELECT u.id, r.id
FROM users u
JOIN roles r
WHERE r.name = 'Especialista'
  AND u.email IN ('juliana.prado@tracecore.local','rafael.nogueira@tracecore.local','marina.lopes@tracecore.local','diego.martins@tracecore.local','fernanda.rocha@tracecore.local','rodrigo.alves@tracecore.local');

INSERT INTO user_roles (user_id, role_id)
SELECT u.id, r.id FROM users u JOIN roles r
WHERE r.name = 'Revisor' AND u.email = 'talita.freitas@tracecore.local';

-- ============================================================
-- SECAO 6: CAUSAS RAIZ (30)
-- ============================================================
INSERT INTO root_causes (code, name, category, description, created_at) VALUES
  ('RC-CTE-REJECT','CT-e rejeitado pela SEFAZ','Fiscal / CT-e','Rejeicao por regra de negocio ou schema invalido no leiaute.','2025-01-01 00:00:00'),
  ('RC-CTE-CERT','Certificado digital vencido','Seguranca / Certificados','Certificado A1 vencido ou sem acesso ao arquivo .pfx.','2025-01-01 00:00:00'),
  ('RC-NFE-DUP','Duplicidade de NF-e','Fiscal / NF-e','NF-e ja autorizada emitida novamente pelo mesmo pedido.','2025-01-01 00:00:00'),
  ('RC-MDFE-BLOCK','MDF-e bloqueado na SEFAZ','Fiscal / MDF-e','MDF-e com manifestacao pendente ou bloqueio fiscal.','2025-01-01 00:00:00'),
  ('RC-FIREBIRD-LOCK','Firebird com arquivo travado','Banco de Dados / Firebird','Arquivo .fdb com lock ou instancia antiga ativa.','2025-01-01 00:00:00'),
  ('RC-FIREBIRD-CORRUPT','Banco Firebird corrompido','Banco de Dados / Firebird','Banco com validacao checada e pagina invalida.','2025-01-01 00:00:00'),
  ('RC-FIREBIRD-DBSCHEMA','Schema Firebird desatualizado','Banco de Dados / Firebird','Banco antigo sem DDLs da versao nova do TMS.','2025-01-01 00:00:00'),
  ('RC-SYNC-FALHA','Falha na sincronizacao local-nuvem','Integracao / Sincronizacao','Fila de sincronizacao acumulada ou credencial invalida.','2025-01-01 00:00:00'),
  ('RC-SAP-RFC-ERRO','Erro em chamada RFC SAP','Integracao / SAP','RFC com falha de auth, destino ou material inexistente.','2025-01-01 00:00:00'),
  ('RC-TOTVS-API','Falha na API do TOTVS','Integracao / TOTVS','API Protheus fora do ar ou credencial incorreta.','2025-01-01 00:00:00'),
  ('RC-EDI-REJECT','EDI rejeitado pelo parceiro','Integracao / EDI','Arquivo EDI com estrutura invalida ou cnpj divergente.','2025-01-01 00:00:00'),
  ('RC-BANK-SETTLE','Falha na conciliacao bancaria','Integracao / Bancos','Layout do arquivo bancario incompativel ou saldo divergente.','2025-01-01 00:00:00'),
  ('RC-GEO-ADDRESS','Endereco sem geocodificacao','Mobile / Geocoding','CEPs novos nao mapeados ou API de maps indisponivel.','2025-01-01 00:00:00'),
  ('RC-GPS-SEMDADO','Sem dados de posicao GPS','Mobile / Rastreio','Provedor sem retorno ou aparelho com SIM inativo.','2025-01-01 00:00:00'),
  ('RC-MOB-CRASH','App fecha inesperadamente','Mobile / Aplicativo','Crash por memoria em rotas grandes ou versao antiga.','2025-01-01 00:00:00'),
  ('RC-MOB-PERM','Permissao bloqueada no mobile','Mobile / Permissoes','GPS/camera sem permissao no Android/iOS.','2025-01-01 00:00:00'),
  ('RC-FREIGHT-CALC','Erro no calculo de frete','Calculo / Frete','Tabela de frete vencida ou divisor incorreto por eixo.','2025-01-01 00:00:00'),
  ('RC-WRONG-PARAM','Parametrizacao incorreta','Configuracao / Cliente','Parametro da filial divergente do ambiente SEFAZ.','2025-01-01 00:00:00'),
  ('RC-PERM-ERR','Permissao de usuario incorreta','Seguranca / Permissoes','Usuario sem papel ou papel sem permissao no modulo.','2025-01-01 00:00:00'),
  ('RC-WEB-SLOW','Portal com lentidao','Web / Performance','Query lenta ou volume de dados sem indice.','2025-01-01 00:00:00'),
  ('RC-WEB-SESSION','Sessao Web expirada','Web / Sessoes','SSO expira durante operacao longa.','2025-01-01 00:00:00'),
  ('RC-API-504','Timeout no gateway','API / Infraestrutura','Gateway excede SLA em chamadas de sincronizacao.','2025-01-01 00:00:00'),
  ('RC-COTA-EXTRAPOLADA','Quota da API extrapolada','API / Quotas','Integrador excede limite de chamadas por minuto.','2025-01-01 00:00:00'),
  ('RC-TOKEN-EXP','Token de integracao expirado','Seguranca / Tokens','Token OAuth/API expirado sem renovacao automatica.','2025-01-01 00:00:00'),
  ('RC-SSL-HANDSHAKE','Falha no handshake SSL/TLS','Infraestrutura / Rede','Certificado intermediario ausente ou TLS desatualizado.','2025-01-01 00:00:00'),
  ('RC-DB-CONN','Esgotamento do pool de conexoes','Banco de Dados / Conexao','Sessao aberta sem dispose em fluxo de relatorio.','2025-01-01 00:00:00'),
  ('RC-DEPLOY-REG','Regressao em deploy','Deploy / CI-CD','Versao publicada com comportamento regressivo.','2025-01-01 00:00:00'),
  ('RC-DUPLIC-REPORT','Relatorio duplicado','Web / Relatorios','Consulta sem agrupamento gerou itens duplicados.','2025-01-01 00:00:00'),
  ('RC-EMAIL-SPAM','Emails caindo em spam','Integracao / Notificacao','SPF/DKIM ausente no dominio do remetente.','2025-01-01 00:00:00'),
  ('RC-DET-CAT','Evento de cancelamento falhou','Fiscal / Eventos','Cancelamento fora da janela ou evento duplicado.','2025-01-01 00:00:00');

-- ============================================================
-- SECAO 7: TAGS (24)
-- ============================================================
INSERT INTO tags (name) VALUES
  ('cte'),('cte-rejeicao'),('nfe'),('mdfe'),('frete'),('roteirizacao'),
  ('sincronizacao'),('mobile'),('offline'),('gps'),('rastreio'),('sap'),
  ('totvs'),('edi'),('integracao'),('firebird'),('banco-de-dados'),
  ('certificado-digital'),('seguranca'),('web'),('performance'),
  ('timeout'),('deploy'),('parametrizacao');

-- ============================================================
-- SECAO 8: FLUXOS DE DIAGNOSTICO
-- ============================================================
INSERT INTO diagnostic_flows (code, name, description, entry_keywords, status, created_by, created_at) VALUES
  ('FLOW-LOGIN-ISSUES','Falha de Autenticacao e Acesso ao Sistema','Fluxo para diagnosticar problemas de login/acesso no TMS.','login,senha,acesso,401,credencial,blocked,certificado', 'Active', 1, NOW()),
  ('FLOW-CTE-REJEICAO','Rejeicao de CT-e na SEFAZ','Fluxo para diagnosticar rejeicoes e erros de emissao de CT-e.','cte,rejeicao,sezaf,556,656,emissao,caminhao', 'Active', 1, NOW()),
  ('FLOW-SYNC-MOBILE','Falha de Sincronizacao Mobile','Fluxo para diagnosticar falhas de sync do app e da filial.','sync,sincronizacao,mobile,offline,app,pendente', 'Active', 1, NOW()),
  ('FLOW-INTEG-SAP','Falha de Integracao com SAP','Fluxo para diagnosticar erros de integracao via RFC/API com o SAP.','sap,rfc,integracao,nota,erro,sap erp', 'Active', 1, NOW());

INSERT INTO diagnostic_flow_hypotheses (flow_id, title, description, associated_component_id)
SELECT f.id, h.title, h.description,
       (SELECT co.id FROM components co WHERE co.code = h.comp)
FROM diagnostic_flows f
JOIN (SELECT 'FLOW-LOGIN-ISSUES' flow, 'Bloqueio ou expiracao de credencial' title, 'Conta bloqueada por tentativas ou senha expirada.' description, 'CORE-ACESSO' comp
      UNION ALL SELECT 'FLOW-LOGIN-ISSUES','Indisponibilidade do servico de autenticacao','Login online indisponivel por servico fora do ar.','CORE-ACESSO'
      UNION ALL SELECT 'FLOW-LOGIN-ISSUES','Erro de configuracao no gateway','Erro de deploy ou configuracao impedindo a autenticacao.','API-GW'
      UNION ALL SELECT 'FLOW-LOGIN-ISSUES','Bloqueio de rede do cliente','Requisicao bloqueada por firewall/proxy local.','SRV-SYNC'
      UNION ALL SELECT 'FLOW-CTE-REJEICAO','Leiaute/schema divergente do ambiente','XML fora do padrao exigido pela SEFAZ destino.','FAT-CTE'
      UNION ALL SELECT 'FLOW-CTE-REJEICAO','Certificado digital vencido','Certificado A1 expirado ou sem acesso ao .pfx.','FAT-CTE'
      UNION ALL SELECT 'FLOW-CTE-REJEICAO','Parametro de ambiente errado','Filial apontando para ambiente de homologacao.','INT-CTE-GOV'
      UNION ALL SELECT 'FLOW-SYNC-MOBILE','Fila de sync acumulada','Fila de eventos presa por conflito ou credencial.','SRV-SYNC'
      UNION ALL SELECT 'FLOW-SYNC-MOBILE','Versao do app desatualizada','App antigo incompativel com a API atual.','MOB-SYNC'
      UNION ALL SELECT 'FLOW-SYNC-MOBILE','Banco local com lock','Firebird da filial com lock impedindo leitura.','DB-FIREBIRD'
      UNION ALL SELECT 'FLOW-INTEG-SAP','Falha de autenticacao na RFC','Credencial SAP invalida ou destino fora do ar.','INT-SAP'
      UNION ALL SELECT 'FLOW-INTEG-SAP','Material/CNPJ divergente','Material ou estabelecimento nao encontrado no SAP.','INT-SAP'
     ) h ON h.flow = f.code;

INSERT INTO diagnostic_checks (flow_id, code, title, question_text, check_type, cost, risk_level, created_at)
SELECT f.id, c.code, c.title, c.question, c.check_type, c.cost, c.risk_level, NOW()
FROM diagnostic_flows f
JOIN (SELECT 'FLOW-LOGIN-ISSUES' flow, 'CHK-SCOPE' code, 'Escopo da falha' title, 'Quantos usuarios sao afetados? Apenas um ou todos?' question, 'Question' check_type, 1 cost, 'Low' risk_level
      UNION ALL SELECT 'FLOW-LOGIN-ISSUES','CHK-SCREEN','Tela da falha','Qual tela/mensagem o usuario ve ao tentar entrar?','Question',1,'Low'
      UNION ALL SELECT 'FLOW-LOGIN-ISSUES','CHK-LOG','Log de autenticacao','Exibir log de autenticacao do servico.','AutomatedCheck',2,'Medium'
      UNION ALL SELECT 'FLOW-LOGIN-ISSUES','CHK-RECENT-CHANGE','Mudanca recente','Houve deploy ou alteracao de parametro recente?','Question',1,'Medium'
      UNION ALL SELECT 'FLOW-CTE-REJEICAO','CHK-COD-REJ','Codigo de rejeicao','Qual codigo e mensagem retornados pela SEFAZ?','Question',1,'High'
      UNION ALL SELECT 'FLOW-CTE-REJEICAO','CHK-ENV','Ambiente configurado','Filial esta apontando para Produção ou Homologação?','AutomatedCheck',2,'High'
      UNION ALL SELECT 'FLOW-CTE-REJEICAO','CHK-CER','Validade do certificado','Certificado A1 esta dentro da validade?','AutomatedCheck',2,'High'
      UNION ALL SELECT 'FLOW-SYNC-MOBILE','CHK-FILA','Tamanho da fila','Quantos eventos estao pendentes na fila?','AutomatedCheck',2,'Medium'
      UNION ALL SELECT 'FLOW-SYNC-MOBILE','CHK-VER','Versao do app','Versoes instaladas nos dispositivos afetados.','Question',1,'Low'
      UNION ALL SELECT 'FLOW-SYNC-MOBILE','CHK-FB-LOCK','Lock do Firebird','Banco local apresenta lock de arquivo?','AutomatedCheck',3,'Medium'
      UNION ALL SELECT 'FLOW-INTEG-SAP','CHK-RFC-AUTH','Autenticacao RFC','Credencial e destino da RFC validos?','AutomatedCheck',2,'High'
      UNION ALL SELECT 'FLOW-INTEG-SAP','CHK-DADOS','Dados do documento','Material/CNPJ existem no SAP?','Question',1,'Medium'
     ) c ON c.flow = f.code;

INSERT INTO diagnostic_check_options (check_id, option_text, order_no)
SELECT ch.id, o.option_text, o.order_no
FROM diagnostic_checks ch
JOIN (SELECT 'CHK-SCOPE' code, 'Apenas o usuario' option_text, 1 order_no
      UNION ALL SELECT 'CHK-SCOPE','Todos os usuarios da filial',2
      UNION ALL SELECT 'CHK-SCREEN','Tela de login nao responde',1
      UNION ALL SELECT 'CHK-SCREEN','Erro de credencial apresentado',2
      UNION ALL SELECT 'CHK-LOG','Sem tentativa de autenticacao',1
      UNION ALL SELECT 'CHK-LOG','Autenticacao rejeitada',2
      UNION ALL SELECT 'CHK-RECENT-CHANGE','Deploy recente',1
      UNION ALL SELECT 'CHK-RECENT-CHANGE','Sem mudanca recente',2
      UNION ALL SELECT 'CHK-COD-REJ','556 - CT-e ja existente',1
      UNION ALL SELECT 'CHK-COD-REJ','656 - XML invalido',2
      UNION ALL SELECT 'CHK-COD-REJ','Outro codigo',3
      UNION ALL SELECT 'CHK-ENV','Homologacao',1
      UNION ALL SELECT 'CHK-ENV','Producao',2
      UNION ALL SELECT 'CHK-CER','Dentro da validade',1
      UNION ALL SELECT 'CHK-CER','Vencido',2
      UNION ALL SELECT 'CHK-FILA','Ate 100 eventos',1
      UNION ALL SELECT 'CHK-FILA','Mais de 100 eventos',2
      UNION ALL SELECT 'CHK-VER','Versao atual',1
      UNION ALL SELECT 'CHK-VER','Versao antiga',2
      UNION ALL SELECT 'CHK-FB-LOCK','Com lock',1
      UNION ALL SELECT 'CHK-FB-LOCK','Sem lock',2
      UNION ALL SELECT 'CHK-RFC-AUTH','Valida',1
      UNION ALL SELECT 'CHK-RFC-AUTH','Invalida',2
      UNION ALL SELECT 'CHK-DADOS','Existe',1
      UNION ALL SELECT 'CHK-DADOS','Nao existe',2
     ) o ON o.code = ch.code;

INSERT INTO diagnostic_check_impacts (check_option_id, flow_hypothesis_id, impact_type, weight)
SELECT co.id, fh.id, i.impact_type, i.weight
FROM diagnostic_check_options co
JOIN diagnostic_checks ch ON co.check_id = ch.id
JOIN diagnostic_flows f ON ch.flow_id = f.id
JOIN diagnostic_flow_hypotheses fh ON fh.flow_id = f.id
JOIN (SELECT 'FLOW-LOGIN-ISSUES' flow, 'CHK-SCOPE' ck, 'Apenas o usuario' opt, 'Bloqueio ou expiracao de credencial' hyp, 'Favors' impact_type, 1.50 weight
      UNION ALL SELECT 'FLOW-LOGIN-ISSUES','CHK-SCOPE','Todos os usuarios da filial','Indisponibilidade do servico de autenticacao','Favors',1.50
      UNION ALL SELECT 'FLOW-LOGIN-ISSUES','CHK-SCREEN','Erro de credencial apresentado','Bloqueio ou expiracao de credencial','Favors',1.30
      UNION ALL SELECT 'FLOW-LOGIN-ISSUES','CHK-LOG','Sem tentativa de autenticacao','Bloqueio de rede do cliente','Favors',1.40
      UNION ALL SELECT 'FLOW-LOGIN-ISSUES','CHK-LOG','Autenticacao rejeitada','Bloqueio ou expiracao de credencial','Favors',1.20
      UNION ALL SELECT 'FLOW-LOGIN-ISSUES','CHK-RECENT-CHANGE','Deploy recente','Erro de configuracao no gateway','Favors',1.60
      UNION ALL SELECT 'FLOW-CTE-REJEICAO','CHK-COD-REJ','656 - XML invalido','Leiaute/schema divergente do ambiente','Favors',1.50
      UNION ALL SELECT 'FLOW-CTE-REJEICAO','CHK-COD-REJ','556 - CT-e ja existente','Parametro de ambiente errado','Discards',1.20
      UNION ALL SELECT 'FLOW-CTE-REJEICAO','CHK-ENV','Homologacao','Parametro de ambiente errado','Favors',2.00
      UNION ALL SELECT 'FLOW-CTE-REJEICAO','CHK-CER','Vencido','Certificado digital vencido','Favors',1.80
      UNION ALL SELECT 'FLOW-SYNC-MOBILE','CHK-FILA','Mais de 100 eventos','Fila de sync acumulada','Favors',1.50
      UNION ALL SELECT 'FLOW-SYNC-MOBILE','CHK-VER','Versao antiga','Versao do app desatualizada','Favors',1.40
      UNION ALL SELECT 'FLOW-SYNC-MOBILE','CHK-FB-LOCK','Com lock','Banco local com lock','Favors',1.80
      UNION ALL SELECT 'FLOW-INTEG-SAP','CHK-RFC-AUTH','Invalida','Falha de autenticacao na RFC','Favors',1.80
      UNION ALL SELECT 'FLOW-INTEG-SAP','CHK-DADOS','Nao existe','Material/CNPJ divergente','Favors',1.50
     ) i ON i.flow = f.code AND i.ck = ch.code AND i.opt = co.option_text AND i.hyp = fh.title;

-- ============================================================
-- SECAO 9: INTEGRACOES E RUNS
-- ============================================================
INSERT INTO integrations (code, name, integration_type, target_system_description, status, owner_department_id, contract_notes, created_by, created_at, updated_at, health_check_url, health_check_method, health_check_timeout_seconds, health_check_expected_status_code, product_id)
SELECT i.code, i.name, i.type, i.description, i.status,
       (SELECT d.id FROM departments d WHERE d.name = i.dept),
       i.contract, 1, NOW(), NOW(), i.url, i.method, i.timeout, i.expected,
       (SELECT p.id FROM products p WHERE p.code = i.pcode)
FROM (SELECT 'INT-SAP' code, 'Integracao SAP ERP' name, 'Sap' type, 'PRD-INT' pcode, 'Integra notas e numeracao com o SAP.' description, 'Integracoes' dept, 'Configured' status, 'Contrato de manutencao anual.' contract, 'https://sapgw.cliente.local/sap/bc/ping' url, 'Http' method, 5 timeout, 200 expected
      UNION ALL SELECT 'INT-TICKET','Integracao Ticketing','Ticketing','PRD-INT','Sistema de chamados do fornecedor.','Suporte','Configured','SLA de atendimento 4h.',NULL,'Http',5,200
      UNION ALL SELECT 'INT-TOTVS','Integracao TOTVS Protheus','Other','PRD-INT','API de integracao com Protheus.','Integracoes','Configured','Integracao sob demanda.',NULL,'Http',10,201
      UNION ALL SELECT 'INT-EDI','EDI de Parceiros','Notification','PRD-INT','Troca de arquivos EDI com embarcadores.','Integracoes','Active','Acordo de nivel de processamento.',NULL,'Tcp',5,200
      UNION ALL SELECT 'INT-SEFAZ','WS SEFAZ (CT-e/NF-e/MDF-e)','Sap','PRD-INT','Comunicacao com servidores fiscais.','Integracoes','Active','Autorizacao em lote.','https://cte.sefaz.gov.br','Http',15,200
      UNION ALL SELECT 'INT-BANK','Conciliacao Bancaria','Directory','PRD-INT','Arquivos CNAB de bancos.','Integracoes','Configured','Layouts por banco.',NULL,'Http',5,200
      UNION ALL SELECT 'INT-GPS','Provedores de Rastreamento','Telemetry','PRD-INT','Dados de posicao GPS dos veiculos.','Integracoes','Active','Frequencia de 5 minutos.','https://api.provedor-gps.local/ping','Http',10,200
      UNION ALL SELECT 'INT-MON','Monitoria Interna','Monitoring','PRD-SRV','Painel de monitoria dos servicos.','Infraestrutura','Configured','Alertas por e-mail.',NULL,'Http',5,200
     ) i;

INSERT INTO integration_runs (integration_id, started_at, finished_at, status, records_processed, error_message, recorded_by, recorded_at, triggered_by)
SELECT g.id,
       a.started_at,
       CASE WHEN a.status = 'Success' THEN DATE_ADD(a.started_at, INTERVAL a.secs SECOND) ELSE NULL END AS finished_at,
       a.status, a.records, a.error_message,
       (SELECT u.id FROM users u WHERE u.email = a.user_email),
       a.started_at, a.triggered_by
FROM (SELECT 'INT-SEFAZ' code, 'Success' status, 142 records, 45 secs, DATE_SUB(NOW(), INTERVAL 3 DAY) started_at, NULL error_message, 'rafael.nogueira@tracecore.local' user_email, 'Automated' triggered_by
      UNION ALL SELECT 'INT-SEFAZ','Failed',0,30, DATE_SUB(NOW(), INTERVAL 2 DAY),'Timeout de autenticacao no WS da SEFAZ.','rafael.nogueira@tracecore.local','Automated'
      UNION ALL SELECT 'INT-SEFAZ','Success',398,90, DATE_SUB(NOW(), INTERVAL 1 DAY),NULL,'rafael.nogueira@tracecore.local','Automated'
      UNION ALL SELECT 'INT-SEFAZ','Success',211,60, DATE_SUB(NOW(), INTERVAL 6 HOUR),NULL,'rafael.nogueira@tracecore.local','Automated'
      UNION ALL SELECT 'INT-SAP','Failed',0,20, DATE_SUB(NOW(), INTERVAL 4 DAY),'RFC MOVE_OUTBOUND: usuario SAP bloqueado.','juliana.prado@tracecore.local','Manual'
      UNION ALL SELECT 'INT-SAP','Success',76,35, DATE_SUB(NOW(), INTERVAL 3 DAY),NULL,'juliana.prado@tracecore.local','Manual'
      UNION ALL SELECT 'INT-SAP','Partial',55,40, DATE_SUB(NOW(), INTERVAL 20 HOUR),'21 registros com divisor de valor divergente.','juliana.prado@tracecore.local','Automated'
      UNION ALL SELECT 'INT-EDI','Success',310,120, DATE_SUB(NOW(), INTERVAL 5 DAY),NULL,'rafael.nogueira@tracecore.local','Automated'
      UNION ALL SELECT 'INT-EDI','Failed',0,15, DATE_SUB(NOW(), INTERVAL 36 HOUR),'Arquivo EDI com CNPJ divergente do contrato.','rafael.nogueira@tracecore.local','Automated'
      UNION ALL SELECT 'INT-EDI','Success',289,110, DATE_SUB(NOW(), INTERVAL 80 MINUTE),NULL,'rafael.nogueira@tracecore.local','Automated'
      UNION ALL SELECT 'INT-BANK','Failed',0,10, DATE_SUB(NOW(), INTERVAL 7 DAY),'Layout CNAB 400 nao suportado para o banco.','marina.lopes@tracecore.local','Manual'
      UNION ALL SELECT 'INT-BANK','Success',64,25, DATE_SUB(NOW(), INTERVAL 2 DAY),NULL,'marina.lopes@tracecore.local','Manual'
      UNION ALL SELECT 'INT-GPS','Partial',1289,18, DATE_SUB(NOW(), INTERVAL 5 HOUR),'74 aparelhos sem retorno de posicao.','diego.martins@tracecore.local','Automated'
      UNION ALL SELECT 'INT-GPS','Success',2431,20, DATE_SUB(NOW(), INTERVAL 1 HOUR),NULL,'diego.martins@tracecore.local','Automated'
      UNION ALL SELECT 'INT-TOTVS','Success',48,32, DATE_SUB(NOW(), INTERVAL 3 DAY),NULL,'rafael.nogueira@tracecore.local','Manual'
      UNION ALL SELECT 'INT-TOTVS','Failed',0,12, DATE_SUB(NOW(), INTERVAL 18 HOUR),'API Protheus retornou HTTP 500 no modulo financeiro.','rafael.nogueira@tracecore.local','Manual'
      UNION ALL SELECT 'INT-MON','Success',15,8, DATE_SUB(NOW(), INTERVAL 2 DAY),NULL,'paulo.andrade@tracecore.local','Automated'
      UNION ALL SELECT 'INT-MON','Success',15,8, DATE_SUB(NOW(), INTERVAL 1 DAY),NULL,'paulo.andrade@tracecore.local','Automated'
     ) a
JOIN integrations g ON g.code = a.code;

-- ============================================================
-- SECAO 10: PROVIDERS / MODELOS DE LLM
-- ============================================================
INSERT INTO llm_providers (name, code, protocol, base_url, authentication_type, has_generation_capability, has_embedding_capability, status, created_by, created_at, updated_by, updated_at) VALUES
  ('Anthropic','anthropic','AnthropicMessages','https://api.anthropic.com/v1','HeaderApiKey',1,1,'Active',1,NOW(),1,NOW()),
  ('OpenAI','openai','OpenAICompatible','https://api.openai.com/v1','BearerApiKey',1,1,'Active',1,NOW(),1,NOW());

INSERT INTO llm_model_configs (purpose, provider_id, model_name, is_active, created_by, created_at, updated_by, updated_at)
SELECT m.purpose, p.id, m.model_name, m.is_active, 1, NOW(), 1, NOW()
FROM llm_providers p
JOIN (SELECT 'anthropic' provider, 'Generation' purpose, 'claude-sonnet-4-5-20250929' model_name, 1 is_active
      UNION ALL SELECT 'anthropic','Embedding','voyage-3',1
      UNION ALL SELECT 'openai','Generation','gpt-4o-mini',0
      UNION ALL SELECT 'openai','Embedding','text-embedding-3-small',0
     ) m ON m.provider = p.code;

INSERT INTO llm_provider_configs (purpose, provider_code, model_name, is_active, created_by, created_at, updated_by, updated_at)
SELECT m.purpose, m.provider, m.model_name, m.is_active, 1, NOW(), 1, NOW()
FROM (SELECT 'anthropic' provider, 'Generation' purpose, 'claude-sonnet-4-5-20250929' model_name, 1 is_active
      UNION ALL SELECT 'anthropic','Embedding','voyage-3',1
      UNION ALL SELECT 'openai','Generation','gpt-4o-mini',0
      UNION ALL SELECT 'openai','Embedding','text-embedding-3-small',0
     ) m;

-- ============================================================
-- SECAO 11: SEQUENCIA DE NUMEROS DE CASO (1..1280)
-- ============================================================
INSERT INTO case_number_seq (id)
WITH RECURSIVE nums AS (
  SELECT 1 AS n
  UNION ALL
  SELECT n + 1 FROM nums WHERE n < 1280
)
SELECT n FROM nums;

-- ============================================================
-- SECAO 12: FAMILIAS CAUSAIS (55) + INSERCAO DOS 1.280 CASOS
-- Distribuicao por status: 75% Resolved | 10% Investigating |
--   5% Open | 5% Reopened | 5% Closed
-- Distribuicao por severidade: Low 15 | Medium 50 | High 28 | Critical 7
-- ============================================================
INSERT INTO cases (
  case_number, external_reference, source_type, client_id, client_unit_id,
  product_id, product_version_id, environment_id, original_report,
  normalized_summary, expected_behavior, observed_behavior, error_code,
  error_message, scope_type, severity, impact_level, status,
  current_owner_user_id, current_department_id, root_cause_status,
  opened_at, first_response_at, resolved_at, closed_at,
  created_at, created_by, updated_at, updated_by, row_version
)
WITH RECURSIVE nums AS (
  SELECT 1 AS n
  UNION ALL
  SELECT n + 1 FROM nums WHERE n < 1280
),
fams AS (
  SELECT 1 AS fid, 1 AS rf, 23 AS rt, 'PRD-TMS' AS pcode, 'FAT-CTE' AS ccode, 'RC-CTE-REJECT' AS rcode,
    'CT-e rejeitado pela SEFAZ na autorizacao (codigo 656, XML invalido).' AS sym,
    'O sistema deveria autorizar o CT-e e retornar a chave esperada.' AS exp,
    'SEFAZ retorna rejeicao com tag obrigatoria ausente.' AS obs,
    'Corrigido o XML pelo ajuste da tag no leiaute e reenviado a SEFAZ.' AS res
  UNION ALL SELECT 2,24,46,'PRD-TMS','FAT-CTE','RC-CTE-CERT','Falha ao transmitir o CT-e por certificado A1 vencido na filial.','A emissao deveria ocorrer sem bloqueio de certificado.','O certificado digital expirado impede assinar o XML.','Certificado A1 substituido no repositorio e emissao normalizada.'
  UNION ALL SELECT 3,47,69,'PRD-TMS','FAT-NFE','RC-NFE-DUP','NF-e duplicada emitida a partir do mesmo pedido de transporte.','O sistema deveria bloquear nova emissao para pedido ja faturado.','Duas NF-e com o mesmo pedido de origem foram autorizadas.','Mapeado o fluxo de reemissao e bloqueado o pedido duplicado.'
  UNION ALL SELECT 4,70,92,'PRD-TMS','FAT-MDFE','RC-MDFE-BLOCK','MDF-e bloqueado por pendencia de manifestacao do veiculo.','Efetivar o MDF-e apos checagem de manifestacao.','Manifesto pendente no cadastro bloqueou a vinculacao.','Regularizado o cadastro do veiculo e efetivado o MDF-e.'
  UNION ALL SELECT 5,93,115,'PRD-TMS','DB-FIREBIRD','RC-FIREBIRD-LOCK','TMS apresenta arquivo de banco em uso ao abrir a filial.','Abrir o banco Firebird normalmente.','Uma instancia antiga mantinha lock no arquivo .fdb.','Encerrado o processo antigo e reassociado a instancia atual.'
  UNION ALL SELECT 6,116,138,'PRD-TMS','DB-FIREBIRD','RC-FIREBIRD-CORRUPT','Banco Firebird da filial corrompido apos queda de energia.','O banco deveria ser recuperado a partir do backup diario.','Validacao apontou pagina invalida no banco de dados.','Restaurado o backup noturno e executado gfix de validacao.'
  UNION ALL SELECT 7,139,161,'PRD-TMS','CORE-ACESSO','RC-PERM-ERR','Usuario sem permissao para abrir o modulo de emissao.','Abrir o modulo conforme o perfil do usuario.','O perfil padrao nao contemplava a rotina de emissao.','Ajustado o vinculo de permissao no perfil do usuario.'
  UNION ALL SELECT 8,162,184,'PRD-TMS','MOD-FIN','RC-FREIGHT-CALC','Calculo de frete divergente da tabela contratada.','Calcular o frete conforme a tabela por UF e peso.','Divisor de eixo incorreto gerou valor acima do contrato.','Corrigida a parametrizacao da tabela de frete.'
  UNION ALL SELECT 9,185,207,'PRD-TMS','MOD-ROTA','RC-WRONG-PARAM','Roteirizacao nao gera a sequencia otima de entrega.','Gerar rota otimizada com sequenciamento correto.','O parametro de radialidade estava desabilitado na filial.','Habilitado o calculo de sequenciamento de rotas.'
  UNION ALL SELECT 10,208,230,'PRD-TMS','MOD-CARGA','RC-WRONG-PARAM','Carga nao fecha por divergencia de dados dos pedidos.','Agrupar pedidos em carga sem divergencias.','O CEP de um destino estava fora da area de cobertura.','Pedido realocado para a carga da rota correta.'
  UNION ALL SELECT 11,231,253,'PRD-SRV','SRV-CTE-EMIS','RC-CTE-REJECT','Lote de CT-e parado com rejeicoes na fila de emissao.','A emissao em lote deveria prosseguir ignorando rejeicoes.','A fila de emissao parou ao encontrar rejeicoes repetidas.','Retirados os CT-e com erro e reprocessado o lote.'
  UNION ALL SELECT 12,254,276,'PRD-SRV','SRV-SYNC','RC-SYNC-FALHA','Sincronizacao local-nuvem acumulando dados pendentes.','Sincronizar os dados da filial em tempo real.','A credencial da filial expirada parou o sincronizador.','Renovada a credencial do sincronizador da filial.'
  UNION ALL SELECT 13,277,299,'PRD-SRV','SRV-INTEGRADOR','RC-TOKEN-EXP','Token de servico expirado parando as integracoes.','Prover acesso continuo dos servicos a API central.','Token OAuth com validade vencida no integrador.','Renovado o token e retomadas as integracoes.'
  UNION ALL SELECT 14,300,322,'PRD-SRV','SRV-BACKUP','RC-DB-CONN','Backup noturno falhou por conexao com o banco.','Realizar backup diario do Firebird sem falhas.','Pool de conexoes estourado na janela de backup.','Ajustado o limite de conexoes e reintentado o backup.'
  UNION ALL SELECT 15,323,345,'PRD-SRV','SRV-WATCHDOG','RC-DEPLOY-REG','Servico em loop de reinicio apos nova versao.','Executar de forma estavel apos o deploy.','Regressao na nova versao derrubava o servico.','Rollback para a versao anterior e correcao da regressao.'
  UNION ALL SELECT 16,346,368,'PRD-WEB','WEB-FRONT','RC-WEB-SLOW','Portal web lento para listar frotas e cargas.','Listar dados em menos de 2 segundos.','Consulta sem indice fez full scan na tabela de eventos.','Criado indice composto na consulta mais usada.'
  UNION ALL SELECT 17,369,391,'PRD-WEB','WEB-API','RC-API-504','API do portal devolvendo timeout em relatorios.','Relatorios deveriam responder dentro do SLA.','Gateway atingiu timeout em chamadas de agregacao.','Corrigido o limite de timeout do endpoint de relatorios.'
  UNION ALL SELECT 18,392,414,'PRD-WEB','WEB-AUTH','RC-SSL-HANDSHAKE','Erro de handshake SSL no login do portal.','Autenticar via SSO sem falha de TLS.','Certificados intermediarios ausentes no servidor.','Instalados os certificados intermediarios corretos.'
  UNION ALL SELECT 19,415,437,'PRD-WEB','WEB-DASH','RC-WEB-SESSION','Dashboard expira a sessao durante operacao longa.','Manter a sessao ativa enquanto o usuario opera.','Tempo de inatividade configurado curto demais.','Ajustado o tempo de expiracao da sessao do portal.'
  UNION ALL SELECT 20,438,460,'PRD-WEB','WEB-REPORT','RC-DUPLIC-REPORT','Relatorio de volumes apresenta linhas duplicadas.','Apresentar totais sem duplicidade.','Consulta com joins duplicou registros por alias.','Corrigido o agrupamento da consulta do relatorio.'
  UNION ALL SELECT 21,461,483,'PRD-WEB','WEB-GEO','RC-GEO-ADDRESS','Endereco novo nao geocodificado no mapa.','Geocodificar automaticamente os CEPS novos.','CEPS da area ampliada ausentes na base de mapas.','Adicionados os CEPS e reprocessado o geocoding.'
  UNION ALL SELECT 22,484,506,'PRD-MOB','MOB-SYNC','RC-MOB-PERM','App nao envia a posicao por permissao de GPS bloqueada.','Enviar a posicao de forma continua.','Permissao de GPS negada na instalacao do app.','Orientado o ajuste de permissoes e o app normalizado.'
  UNION ALL SELECT 23,507,529,'PRD-MOB','MOB-ROTA','RC-MOB-CRASH','App fecha ao abrir rota com mais de 80 paradas.','Manter o app estavel em rotas grandes.','Consumo de memoria excedido na renderizacao da rota.','Paginado o roteiro e reduzido o consumo de memoria.'
  UNION ALL SELECT 24,530,552,'PRD-MOB','MOB-COLETA','RC-SYNC-FALHA','Coleta realizada nao sincroniza no servidor.','Sincronizar a coleta apos ficar online.','Fila de eventos presa por conflito de versao.','Conflito resolvido e eventos da coleta sincronizados.'
  UNION ALL SELECT 25,553,575,'PRD-MOB','MOB-RASTRO','RC-GPS-SEMDADO','Veiculo sem posicao GPS no rastreamento.','Exibir a posicao atualizada a cada 5 minutos.','Comunicacao com o provedor de rastreio interrompida.','Reconectado o provedor e atualizado o cadastro do aparelho.'
  UNION ALL SELECT 26,576,598,'PRD-API','API-GW','RC-API-504','API com timeout em chamadas de consulta de pedidos.','Responder em ate 3 segundos por chamada.','Sobrecarga no gateway em horario de pico.','Escalonado o gateway e adicionado cache de consultas.'
  UNION ALL SELECT 27,599,621,'PRD-API','API-RATE','RC-COTA-EXTRAPOLADA','Integrador bloqueado por exceder a quota de chamadas.','Permitir o volume contratado sem bloqueio.','Quota por minuto menor que o volume do contrato.','Ajustada a quota do cliente conforme o contrato.'
  UNION ALL SELECT 28,622,644,'PRD-API','API-AUTH','RC-TOKEN-EXP','Token de integracao rejeitado pela API.','Validar tokens renovados automaticamente.','Integrador utilizava token antigo apos a rotacao.','Corrigida a rotacao automatica de tokens.'
  UNION ALL SELECT 29,645,667,'PRD-API','API-GW','RC-SSL-HANDSHAKE','Falha de TLS em consumidores externos da API.','Atender consumidores com TLS moderno.','Cliente com TLS 1.0 incompativel com a politica.','Atualizado o lado do cliente e documentada a politica TLS.'
  UNION ALL SELECT 30,668,690,'PRD-INT','INT-SAP','RC-SAP-RFC-ERRO','Notas nao confirmadas no SAP por erro de RFC.','Integrar as notas ao SAP sem falhas.','Destino da RFC sem autorizacao para a funcao usada.','Corrigida a autorizacao do destino na RFC.'
  UNION ALL SELECT 31,691,713,'PRD-INT','INT-TOTVS','RC-TOTVS-API','Integracao com Protheus falhando na API.','Sincronizar os dados com o Protheus.','Endpoint do modulo financeiro fora do ar.','Reparado o endpoint no lado do cliente.'
  UNION ALL SELECT 32,714,736,'PRD-INT','INT-EDI','RC-EDI-REJECT','Arquivo EDI 210 rejeitado pelo embarcador.','Processar o EDI conforme o leiaute do parceiro.','CNPJ divergente entre contrato e arquivo.','Alinhado o CNPJ no cadastro do parceiro.'
  UNION ALL SELECT 33,737,759,'PRD-INT','INT-BANK','RC-BANK-SETTLE','Conciliacao de fretes nao fechou com o banco.','Conciliar os pagamentos com o arquivo bancario.','Layout bancario divergente para a agencia.','Ajustado o layout e reprocessada a conciliacao.'
  UNION ALL SELECT 34,760,782,'PRD-INT','INT-CTE-GOV','RC-WRONG-PARAM','Filial emitindo em ambiente errado da SEFAZ.','Emitir sempre em producao.','O parametro da filial apontava para homologacao.','Corrigido o ambiente no cadastro da filial.'
  UNION ALL SELECT 35,783,805,'PRD-INT','INT-RASTREO','RC-GPS-SEMDADO','Provedor de rastreio sem dados por URL invalida.','Integrar os dados de posicao do provedor.','URL de eventos do provedor alterada sem aviso.','Atualizada a URL do provedor no conector.'
  UNION ALL SELECT 36,806,828,'PRD-INT','INT-WHATS','RC-EMAIL-SPAM','Notificacoes ao embarcador caindo em spam.','Entregar as notificacoes na caixa principal.','Registro SPF ausente para o dominio do remetente.','Configurado SPF e DKIM no dominio.'
  UNION ALL SELECT 37,829,851,'PRD-TMS','MOD-OC','RC-WRONG-PARAM','Ordem de coleta nao gera picking na filial.','Gerar o picking automaticamente no recebimento.','Parametro de geracao automatica desabilitado.','Habilitado o fluxo automatico de ordem de coleta.'
  UNION ALL SELECT 38,852,874,'PRD-TMS','MOD-OS','RC-PERM-ERR','Ordem de servico acessivel com permissao restrita.','Liberar a rotina somente para o papel adequado.','Perfil de supervisor permitia baixa de OS.','Revisto o grupo de permissoes da rotina de OS.'
  UNION ALL SELECT 39,875,897,'PRD-TMS','FAT-CTE','RC-DET-CAT','Cancelamento de CT-e fora da janela legal.','Cancelar e retornar o evento com sucesso.','Evento de cancelamento excedeu a janela temporal.','Reclassificado como evento de encerramento.'
  UNION ALL SELECT 40,898,920,'PRD-SRV','SRV-MDFE-MON','RC-MDFE-BLOCK','Monitor de MDF-e parou de acompanhar o ciclo de vida.','Acompanhar o encerramento dos MDF-e.','Servico parado por incompatibilidade apos deploy.','Atualizado o servico para a versao atual.'
  UNION ALL SELECT 41,921,943,'PRD-WEB','WEB-API','RC-DB-CONN','API do portal com esgotamento de conexoes.','Acumular e liberar conexoes do pool.','Conexoes sem dispose em fluxo de consulta.','Corrigido o dispose e reduzida a janela do pool.'
  UNION ALL SELECT 42,944,966,'PRD-WEB','WEB-FRONT','RC-EMAIL-SPAM','Email de recuperacao de senha nao chega.','Entregar o link de recuperacao imediatamente.','Provedor de email do cliente bloqueando o remetente.','Ajustado o remetente e a politica de entrega.'
  UNION ALL SELECT 43,967,989,'PRD-MOB','MOB-ROTA','RC-WEB-SLOW','Roteiro do motorista demora a carregar.','Exibir o roteiro em poucos segundos.','API de consulta da rota sem paginacao.','Adicionada a paginacao no endpoint de roteiro.'
  UNION ALL SELECT 44,990,1012,'PRD-TMS','CORE-CALC','RC-FREIGHT-CALC','Tabela de frete vencida usada em novos pedidos.','Usar a tabela vigente do contrato.','Vigencia da tabela expirada sem nova publicacao.','Publicada nova tabela de frete vigente.'
  UNION ALL SELECT 45,1013,1035,'PRD-SRV','SRV-SYNC','RC-FIREBIRD-DBSCHEMA','Sincronizador incompativel com o banco antigo da filial.','Sincronizar com o schema da versao atual.','Faltavam DDLs aplicadas somente nas filiais novas.','Aplicado o script de migracao de schema na filial.'
  UNION ALL SELECT 46,1036,1058,'PRD-API','API-RATE','RC-PERM-ERR','Chamada da API retorna 401 sem chave valida.','Autorizar somente chaves ativas.','Chave de integracao revogada sem aviso.','Reemitida a chave de integracao do cliente.'
  UNION ALL SELECT 47,1059,1081,'PRD-INT','INT-EDI','RC-TOKEN-EXP','Arquivo EDI interrompido por token vencido.','Autenticar no diretorio de troca EDI.','Token de acesso do parceiro expirado.','Renovado o acesso do parceiro EDI.'
  UNION ALL SELECT 48,1082,1104,'PRD-TMS','MOD-FROTA','RC-FIREBIRD-LOCK','Gestao de frota travada por banco em uso.','Encerrar e liberar o banco da frota.','Instancia orfa manteve o arquivo da filial.','Encerrada a instancia orfa e liberado o .fdb.'
  UNION ALL SELECT 49,1105,1127,'PRD-TMS','DB-FIREBIRD','RC-FIREBIRD-DBSCHEMA','Falha de conexao apontando cliente antigo.','Conectar com o Firebird 5 instalado.','Cliente de conexao antigo no terminal.','Atualizado o driver Firebird e reconectada a filial.'
  UNION ALL SELECT 50,1128,1150,'PRD-WEB','WEB-AUTH','RC-TOKEN-EXP','Sessao SSO expirada durante transmissao.','Manter autenticado durante uso ativo.','Tempo de vida do token menor que a sessao.','Alinhado o tempo de vida do token com a sessao.'
  UNION ALL SELECT 51,1151,1173,'PRD-MOB','MOB-SYNC','RC-API-504','Sync offline falha por timeout na API.','Sincronizar em segundo plano sem erro.','Chamadas de sync excedendo o limite do gateway.','Ajustado o timeout e repulgiada a fila de sync.'
  UNION ALL SELECT 52,1174,1196,'PRD-SRV','SRV-INTEGRADOR','RC-COTA-EXTRAPOLADA','Integrador bloqueado pela quota da API central.','Orquestrar o volume contratado.','Quota configurada abaixo do volume real.','Revisada a quota do integrador central.'
  UNION ALL SELECT 53,1197,1219,'PRD-INT','INT-SAP','RC-SAP-RFC-ERRO','Nota de frete rejeitada no SAP por material invalido.','Confirmar a nota no SAP com material valido.','Material nao cadastrado no exercicio vigente.','Corrigido o cadastro do material no SAP.'
  UNION ALL SELECT 54,1220,1242,'PRD-TMS','FAT-NFE','RC-CTE-CERT','Emissao de NF-e bloqueada por certificado expirado.','Autorizar a NF-e com certificado valido.','Arquivo do certificado fora do repositorio da filial.','Reinstalado o certificado A1 da filial.'
  UNION ALL SELECT 55,1243,1280,'PRD-MOB','MOB-COLETA','RC-MOB-PERM','Digitalizacao sem acesso a camera no tablet.','Capturar o comprovante com a camera.','Permissao de camera negada no perfil do aparelho.','Autorizadas as permissoes no gerenciador do device.'
),
months AS (
  SELECT 1 AS mf, 40 AS mt, '2025-04-01 00:00:00' AS ms
  UNION ALL SELECT 41,75,'2025-05-01 00:00:00'
  UNION ALL SELECT 76,115,'2025-06-01 00:00:00'
  UNION ALL SELECT 116,175,'2025-07-01 00:00:00'
  UNION ALL SELECT 176,245,'2025-08-01 00:00:00'
  UNION ALL SELECT 246,335,'2025-09-01 00:00:00'
  UNION ALL SELECT 336,390,'2025-10-01 00:00:00'
  UNION ALL SELECT 391,450,'2025-11-01 00:00:00'
  UNION ALL SELECT 451,525,'2025-12-01 00:00:00'
  UNION ALL SELECT 526,620,'2026-01-01 00:00:00'
  UNION ALL SELECT 621,690,'2026-02-01 00:00:00'
  UNION ALL SELECT 691,765,'2026-03-01 00:00:00'
  UNION ALL SELECT 766,850,'2026-04-01 00:00:00'
  UNION ALL SELECT 851,940,'2026-05-01 00:00:00'
  UNION ALL SELECT 941,1035,'2026-06-01 00:00:00'
  UNION ALL SELECT 1036,1150,'2026-07-01 00:00:00'
  UNION ALL SELECT 1151,1210,'2026-08-01 00:00:00'
  UNION ALL SELECT 1211,1280,'2026-09-01 00:00:00'
),
prods AS (SELECT id, code, name FROM products),
envs2 AS (SELECT id, environment_type FROM environments),
clients_r AS (
  SELECT id, code, name, ROW_NUMBER() OVER (ORDER BY id) rn FROM clients
),
mat_units AS (SELECT client_id, MAX(id) AS id FROM client_units WHERE code LIKE '%-MAT' GROUP BY client_id),
users_r AS (
  SELECT id, ROW_NUMBER() OVER (ORDER BY id) rn
  FROM users WHERE email <> 'admin@tracecore.local'
),
owner_dept AS (
  SELECT u.id AS uid, ud.department_id AS did
  FROM users u JOIN user_departments ud ON ud.user_id = u.id
),
versions_r AS (
  SELECT p.code AS pcode, pv.id, pv.version_label AS vl,
         ROW_NUMBER() OVER (PARTITION BY pv.product_id ORDER BY pv.released_at) AS rn,
         COUNT(*) OVER (PARTITION BY pv.product_id) AS cnt
  FROM product_versions pv JOIN products p ON pv.product_id = p.id
),
rep_extra AS (
  SELECT 1 AS rn, 'Usuarios da filial relatam travamentos mesmo apos reiniciar a estacao.' AS txt
  UNION ALL SELECT 2, 'O problema ocorre no periodo da manha, durante a primeira rodada de emissao.'
  UNION ALL SELECT 3, 'Tentativa repetida sem sucesso desde a ultima atualizacao do sistema.'
  UNION ALL SELECT 4, 'Comportamento ocorre apos mudanca de parametro feita pela padronizacao.'
  UNION ALL SELECT 5, 'Equipe do cliente tentou contornar sem sucesso alterando configuracoes.'
  UNION ALL SELECT 6, 'A falha aparece esporadicamente em horarios de pico.'
  UNION ALL SELECT 7, 'Varios terminais afetados ao mesmo tempo na mesma filial.'
  UNION ALL SELECT 8, 'Cliente pede prioridade pois a operacao esta impactada.'
),
stats AS (
  SELECT n,
    CASE WHEN MOD(n - 1, 100) <= 6 THEN 'Critical'
         WHEN MOD(n - 1, 100) <= 34 THEN 'High'
         WHEN MOD(n - 1, 100) <= 84 THEN 'Medium'
         ELSE 'Low' END AS severity,
    CASE WHEN MOD(n - 1, 100) <= 74 THEN 'Resolved'
         WHEN MOD(n - 1, 100) <= 84 THEN 'Investigating'
         WHEN MOD(n - 1, 100) <= 89 THEN 'Open'
         WHEN MOD(n - 1, 100) <= 94 THEN 'Reopened'
         ELSE 'Closed' END AS status
  FROM nums
),
opens AS (
  SELECT s.n, s.status,
    LEAST(DATE_ADD(DATE_ADD(DATE_ADD(mo.ms,
                INTERVAL (1 + MOD(s.n * 7, 20)) DAY),
                INTERVAL (8 + MOD(s.n * 3, 12)) HOUR),
                INTERVAL MOD(s.n * 11, 60) MINUTE), '2026-09-18 23:59:00') AS opened_at
  FROM stats s JOIN months mo ON s.n BETWEEN mo.mf AND mo.mt
),
timings AS (
  SELECT s.n, s.status, o.opened_at,
    CASE WHEN s.status IN ('Resolved','Closed')
         THEN LEAST(DATE_ADD(DATE_ADD(o.opened_at, INTERVAL (3 + MOD(s.n, 30)) HOUR),
                             INTERVAL (2 + MOD(s.n * 13, 130)) HOUR), '2026-09-19 08:00:00')
         WHEN s.status = 'Reopened'
         THEN LEAST(DATE_ADD(DATE_ADD(o.opened_at, INTERVAL (3 + MOD(s.n, 30)) HOUR),
                             INTERVAL (2 + MOD(s.n * 13, 130)) HOUR), '2026-09-14 23:59:00')
         ELSE NULL END AS resolved_at
  FROM stats s JOIN opens o ON o.n = s.n
)
SELECT
  st.n AS case_number,
  CONCAT('LOG-2026-CASE-', LPAD(st.n, 6, '0')) AS external_reference,
  ELT(MOD(st.n - 1, 7) + 1, 'Manual','Email','Phone','Portal','Manual','Api','Automated') AS source_type,
  cl.id AS client_id,
  mu.id AS client_unit_id,
  pr.id AS product_id,
  vv.id AS product_version_id,
  (SELECT e2.id FROM envs2 e2 WHERE e2.environment_type =
     CASE WHEN MOD(st.n - 1, 10) < 7 THEN 'Production'
          WHEN MOD(st.n - 1, 10) < 9 THEN 'Staging'
          ELSE 'Development' END) AS environment_id,
  CONCAT_WS(' ', 'Cliente', cl.name, 'reportou:', f.sym, re.txt) AS original_report,
  CONCAT('Resumo da ocorrencia: ', f.sym, ' (', pr.name, ' ', vv.vl, ') - ', cl.code) AS normalized_summary,
  f.exp AS expected_behavior,
  CONCAT(f.obs, ' ', f.sym) AS observed_behavior,
  ELT(MOD(st.n - 1, 14) + 1, 'HTTP 502','HTTP 504','SAP-RFC-100','R656','R556','SQLCODE 902','FIREBIRD-LOCK','401','403','TIMEOUT','SSL-HANDSHAKE','QUOTA-EXCEEDED','DX-9R9','E-DUP') AS error_code,
  CONCAT('Mensagem observada: ', re.txt) AS error_message,
  ELT(MOD(st.n - 1, 4) + 1, 'Client','Product','Process','Infrastructure') AS scope_type,
  st.severity,
  CASE WHEN st.severity = 'Critical' THEN 'Corporativo'
       WHEN st.severity = 'High' THEN 'Setorial'
       WHEN MOD(st.n, 3) = 0 THEN 'Setorial' ELSE 'Pontual' END AS impact_level,
  st.status,
  us.id AS current_owner_user_id,
  od.did AS current_department_id,
  CASE WHEN st.status IN ('Resolved','Closed') THEN IF(MOD(st.n,10) < 8, 'Confirmed', 'NotConfirmed') ELSE 'NotEvaluated' END AS root_cause_status,
  t.opened_at,
  CASE WHEN st.status <> 'Open' THEN LEAST(DATE_ADD(t.opened_at, INTERVAL (1 + MOD(st.n, 48)) HOUR), '2026-09-19 05:59:00') ELSE NULL END AS first_response_at,
  t.resolved_at,
  CASE WHEN st.status = 'Closed' THEN LEAST(DATE_ADD(t.resolved_at, INTERVAL (1 + MOD(st.n, 7)) DAY), '2026-09-19 22:00:00') ELSE NULL END AS closed_at,
  t.opened_at AS created_at,
  us.id AS created_by,
  IFNULL(t.resolved_at, t.opened_at) AS updated_at,
  us.id AS updated_by,
  1 AS row_version
FROM stats st
JOIN nums ON nums.n = st.n
JOIN fams f ON st.n BETWEEN f.rf AND f.rt
JOIN timings t ON t.n = st.n
JOIN clients_r cl ON cl.rn = MOD(st.n - 1, 12) + 1
JOIN mat_units mu ON mu.client_id = cl.id
JOIN prods pr ON pr.code = f.pcode
JOIN versions_r vv ON vv.pcode = f.pcode AND vv.rn = MOD(st.n - 1, vv.cnt) + 1
JOIN users_r us ON us.rn = MOD(st.n - 1, 10) + 1
JOIN owner_dept od ON od.uid = us.id
JOIN rep_extra re ON re.rn = MOD(st.n * 7, 8) + 1;

-- ============================================================
-- SECAO 13: ITERACOES DE CASO
-- Iteracao 1 para todos; iteracao 2 para casos reabertos.
-- ============================================================
INSERT INTO case_iterations (case_id, sequence_number, opened_at, opened_by, reason, closed_at, status)
SELECT c.id, 1, c.opened_at, c.current_owner_user_id, 'Abertura inicial do caso',
       CASE WHEN c.status IN ('Resolved','Closed','Reopened') THEN c.resolved_at ELSE NULL END,
       CASE WHEN c.status IN ('Resolved','Closed','Reopened') THEN 'Resolved' ELSE 'Open' END
FROM cases c;

INSERT INTO case_iterations (case_id, sequence_number, opened_at, opened_by, reason, closed_at, status)
SELECT c.id,
       2,
       LEAST(DATE_ADD(c.resolved_at, INTERVAL (1 + MOD(c.id, 9)) DAY), '2026-09-19 00:00:00'),
       c.current_owner_user_id,
       CONCAT('Cliente reabriu o caso (origem ', c.external_reference, '): ocorrencia recorrente.'),
       NULL, 'Open'
FROM cases c WHERE c.status = 'Reopened';

-- ============================================================
-- SECAO 14: SINTOMAS DE CASO (2..4 por caso)
-- Sintoma 1 = frase principal derivada do relato original;
-- sintomas 2..4 = sintomas complementares deterministicos.
-- ============================================================
INSERT INTO case_symptoms (case_id, symptom_code, symptom_text, source, confirmed)
SELECT c.id,
       CONCAT('SYM-', LPAD(c.case_number, 6, '0'), '-', sk.k),
       CASE sk.k
         WHEN 1 THEN LEFT(SUBSTRING_INDEX(c.original_report, '.', 1), 400)
         ELSE ELT(MOD(c.id + sk.k, 8) + 1,
           'Ocorrencia iniciada apos reinicio das estacoes da filial.',
           'Sintoma se repete em horarios de pico da operacao.',
           'Cliente observa comportamento divergente do esperado.',
           'Falha reproduzida em mais de um terminal.',
           'Evento registrado pelo monitor de servicos automaticamente.',
           'Procedimento de contorno tentado pelo cliente sem sucesso.',
           'Situacao agravada apos alteracao de parametros locais.',
           'Registro do ponto de controle indica impacto continuo.')
       END,
       CASE WHEN sk.k = 1 THEN 'Human'
            ELSE ELT(MOD(c.id + sk.k, 3) + 1, 'Human','Automated','Phone') END,
       1
FROM cases c
CROSS JOIN (SELECT 1 k UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4) sk
WHERE sk.k <= 2 + MOD(c.id, 3);

-- ============================================================
-- MAPA DE FAMILIAS CAUSAIS (reutilizado nas secoes seguintes)
-- Tabela temporaria: faixas 1..1280, causa raiz e componente.
-- ============================================================
CREATE TEMPORARY TABLE tmp_fam (fid INT PRIMARY KEY, a INT NOT NULL, b INT NOT NULL, rcode VARCHAR(50) NOT NULL, ccode VARCHAR(50) NULL);
INSERT INTO tmp_fam (fid, a, b, rcode, ccode) VALUES
  (1,1,23,'RC-CTE-REJECT','FAT-CTE'),
  (2,24,46,'RC-CTE-CERT','FAT-CTE'),
  (3,47,69,'RC-NFE-DUP','FAT-NFE'),
  (4,70,92,'RC-MDFE-BLOCK','FAT-MDFE'),
  (5,93,115,'RC-FIREBIRD-LOCK','DB-FIREBIRD'),
  (6,116,138,'RC-FIREBIRD-CORRUPT','DB-FIREBIRD'),
  (7,139,161,'RC-PERM-ERR','CORE-ACESSO'),
  (8,162,184,'RC-FREIGHT-CALC','MOD-FIN'),
  (9,185,207,'RC-WRONG-PARAM','MOD-ROTA'),
  (10,208,230,'RC-WRONG-PARAM','MOD-CARGA'),
  (11,231,253,'RC-CTE-REJECT','SRV-CTE-EMIS'),
  (12,254,276,'RC-SYNC-FALHA','SRV-SYNC'),
  (13,277,299,'RC-TOKEN-EXP','SRV-INTEGRADOR'),
  (14,300,322,'RC-DB-CONN','SRV-BACKUP'),
  (15,323,345,'RC-DEPLOY-REG','SRV-WATCHDOG'),
  (16,346,368,'RC-WEB-SLOW','WEB-FRONT'),
  (17,369,391,'RC-API-504','WEB-API'),
  (18,392,414,'RC-SSL-HANDSHAKE','WEB-AUTH'),
  (19,415,437,'RC-WEB-SESSION','WEB-DASH'),
  (20,438,460,'RC-DUPLIC-REPORT','WEB-REPORT'),
  (21,461,483,'RC-GEO-ADDRESS','WEB-GEO'),
  (22,484,506,'RC-MOB-PERM','MOB-SYNC'),
  (23,507,529,'RC-MOB-CRASH','MOB-ROTA'),
  (24,530,552,'RC-SYNC-FALHA','MOB-COLETA'),
  (25,553,575,'RC-GPS-SEMDADO','MOB-RASTRO'),
  (26,576,598,'RC-API-504','API-GW'),
  (27,599,621,'RC-COTA-EXTRAPOLADA','API-RATE'),
  (28,622,644,'RC-TOKEN-EXP','API-AUTH'),
  (29,645,667,'RC-SSL-HANDSHAKE','API-GW'),
  (30,668,690,'RC-SAP-RFC-ERRO','INT-SAP'),
  (31,691,713,'RC-TOTVS-API','INT-TOTVS'),
  (32,714,736,'RC-EDI-REJECT','INT-EDI'),
  (33,737,759,'RC-BANK-SETTLE','INT-BANK'),
  (34,760,782,'RC-WRONG-PARAM','INT-CTE-GOV'),
  (35,783,805,'RC-GPS-SEMDADO','INT-RASTREO'),
  (36,806,828,'RC-EMAIL-SPAM','INT-WHATS'),
  (37,829,851,'RC-WRONG-PARAM','MOD-OC'),
  (38,852,874,'RC-PERM-ERR','MOD-OS'),
  (39,875,897,'RC-DET-CAT','FAT-CTE'),
  (40,898,920,'RC-MDFE-BLOCK','SRV-MDFE-MON'),
  (41,921,943,'RC-DB-CONN','WEB-API'),
  (42,944,966,'RC-EMAIL-SPAM','WEB-FRONT'),
  (43,967,989,'RC-WEB-SLOW','MOB-ROTA'),
  (44,990,1012,'RC-FREIGHT-CALC','CORE-CALC'),
  (45,1013,1035,'RC-FIREBIRD-DBSCHEMA','SRV-SYNC'),
  (46,1036,1058,'RC-PERM-ERR','API-RATE'),
  (47,1059,1081,'RC-TOKEN-EXP','INT-EDI'),
  (48,1082,1104,'RC-FIREBIRD-LOCK','MOD-FROTA'),
  (49,1105,1127,'RC-FIREBIRD-DBSCHEMA','DB-FIREBIRD'),
  (50,1128,1150,'RC-TOKEN-EXP','WEB-AUTH'),
  (51,1151,1173,'RC-API-504','MOB-SYNC'),
  (52,1174,1196,'RC-COTA-EXTRAPOLADA','SRV-INTEGRADOR'),
  (53,1197,1219,'RC-SAP-RFC-ERRO','INT-SAP'),
  (54,1220,1242,'RC-CTE-CERT','FAT-NFE'),
  (55,1243,1280,'RC-MOB-PERM','MOB-COLETA');

-- ============================================================
-- SECAO 15: SESSOES DE DIAGNOSTICO
-- 1 sessao por caso na iteracao 1; nova sessao na iteracao 2
-- para casos reabertos.
-- ============================================================
INSERT INTO diagnostic_sessions (case_id, status, started_at, started_by, ended_at, case_iteration_id)
SELECT c.id,
       CASE WHEN c.status IN ('Resolved','Closed','Reopened') THEN 'Closed' ELSE 'Open' END,
       DATE_ADD(c.opened_at, INTERVAL (10 + MOD(c.id, 90)) MINUTE),
       c.current_owner_user_id,
       CASE WHEN c.status IN ('Resolved','Closed','Reopened') THEN c.resolved_at ELSE NULL END,
       ci1.id
FROM cases c
JOIN case_iterations ci1 ON ci1.case_id = c.id AND ci1.sequence_number = 1;

INSERT INTO diagnostic_sessions (case_id, status, started_at, started_by, ended_at, case_iteration_id)
SELECT c.id, 'Open', ci2.opened_at, c.current_owner_user_id, NULL, ci2.id
FROM cases c
JOIN case_iterations ci2 ON ci2.case_id = c.id AND ci2.sequence_number = 2;

-- ============================================================
-- SECAO 16: HIPOTESES DE CASO (1..4 na iteracao 1)
-- Hipotesis 1 sempre ligada a familia causal; as demais sao
-- alternativas determinísticas. Casos reabertos ganham nova serie.
-- ============================================================
INSERT INTO case_hypotheses (case_id, component_id, title, description, status, source_type, justification, created_at, created_by, updated_at, case_iteration_id)
SELECT c.id,
       (SELECT cmp.id FROM components cmp WHERE cmp.code = fm.ccode),
       CASE h.h
         WHEN 1 THEN CONCAT('Causa raiz provavel: ',
                   IFNULL((SELECT rc.name FROM root_causes rc WHERE rc.code = fm.rcode), 'falha operacional'))
         ELSE ELT(MOD(c.id + h.h, 6) + 1,
           'Erro de configuracao de ambiente ou parametro local.',
           'Falha na comunicacao com servico ou integracao externa.',
           'Problema em componente ou versao publicada recentemente.',
           'Defeito de dados armazenados ou cadastro inconsistente.',
           'Regressao introduzida em publicacao de versao.',
           'Indisponibilidade de infraestrutura ou rede na origem.') END,
       CASE h.h
         WHEN 1 THEN CONCAT('Hipotese derivada do relato original: ', c.normalized_summary)
         ELSE CONCAT('Alternativa de investigacao para o caso ', c.external_reference) END,
       CASE WHEN c.status IN ('Resolved','Closed') AND h.h = 1 THEN 'Supported'
            WHEN MOD(c.id + h.h, 5) = 0 THEN 'Discarded'
            ELSE 'Proposed' END,
       ELT(MOD(c.id + h.h, 3) + 1, 'Human','Automated','Human'),
       CASE WHEN h.h = 1 THEN CONCAT('Justificativa baseada em ', c.expected_behavior)
            ELSE CONCAT('Hipotese alternativa registrada para ', c.external_reference) END,
       c.opened_at, c.current_owner_user_id,
       IFNULL(c.resolved_at, c.opened_at), ci1.id
FROM cases c
JOIN tmp_fam fm ON c.case_number BETWEEN fm.a AND fm.b
JOIN case_iterations ci1 ON ci1.case_id = c.id AND ci1.sequence_number = 1
CROSS JOIN (SELECT 1 h UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4) h
WHERE h.h <= 2 + MOD(c.id, 3);

-- Hipoteses da segunda iteracao (casos reabertos)
INSERT INTO case_hypotheses (case_id, component_id, title, description, status, source_type, justification, created_at, created_by, updated_at, case_iteration_id)
SELECT c.id,
       (SELECT cmp.id FROM components cmp WHERE cmp.code = fm.ccode),
       CONCAT('Reinvestigacao: ', ELT(h2.h, 'recurso de configuracoes da filial', 'troca de componente ou versao')),
       CONCAT('Nova investigacao da ocorrencia recorrente (origem ', c.external_reference, ').'),
       'Proposed',
       ELT(MOD(c.id + h2.h, 2) + 1, 'Human','Automated'),
       CONCAT('Cliente reabriu o caso relatando recorrencia do sintoma original.'),
       ci2.opened_at, c.current_owner_user_id, ci2.opened_at, ci2.id
FROM cases c
JOIN tmp_fam fm ON c.case_number BETWEEN fm.a AND fm.b
JOIN case_iterations ci2 ON ci2.case_id = c.id AND ci2.sequence_number = 2
CROSS JOIN (SELECT 1 h UNION ALL SELECT 2) h2;

-- ============================================================
-- SECAO 17: PASSOS DE DIAGNOSTICO (2..6 por sessao)
-- ============================================================
INSERT INTO diagnostic_steps (diagnostic_session_id, sequence_no, step_type, hypothesis_id, title, objective, instruction, input_evidence_summary, result_summary, outcome, risk_level, duration_seconds, performed_by, performed_at, metadata_json)
SELECT ds.id,
       st.seq,
       ELT(MOD(c.id + st.seq, 6) + 1, 'Verification','Attempt','Observation','GuidedQuestion','RecommendationIgnored','AutomatedCheck'),
       CASE WHEN MOD(c.id + st.seq, 3) = 0
            THEN (SELECT MIN(h2.id) FROM case_hypotheses h2 WHERE h2.case_id = c.id AND h2.case_iteration_id = ds.case_iteration_id)
            ELSE NULL END,
       CONCAT('Passo ', st.seq, ' de investigacao de ', c.external_reference),
       CASE WHEN st.seq = 1 THEN 'Coletar contexto inicial e confirmar o sintoma relatado.'
            WHEN MOD(st.seq, 2) = 0 THEN 'Executar verificacao orientada para a hipotese em curso.'
            ELSE 'Registrar observacao coletada durante o atendimento.' END,
       'Executar o procedimento descrito e anotar o resultado observado no caso.',
       CONCAT('Entrada: ', LEFT(c.original_report, 500)),
       CONCAT('Resultado: etapa executada por ', u.name, ' durante o diagnostico.'),
       CASE WHEN c.status IN ('Resolved','Closed') AND st.seq <= 2 THEN 'Worked'
            WHEN c.status IN ('Resolved','Closed') THEN ELT(MOD(c.id + st.seq, 4) + 1, 'Worked','DidNotWork','PartiallyWorked','Worked')
            WHEN st.seq = 1 THEN 'NotApplicable'
            ELSE ELT(MOD(c.id + st.seq, 4) + 1, 'Inconclusive','Worked','DidNotWork','Inconclusive') END,
       ELT(MOD(c.id + st.seq, 3) + 1, 'Low','Medium','Low'),
       (60 + MOD(c.id * st.seq, 3600)),
       c.current_owner_user_id,
       DATE_ADD(ds.started_at, INTERVAL (st.seq * 5) MINUTE),
       NULL
FROM diagnostic_sessions ds
JOIN cases c ON c.id = ds.case_id
JOIN users u ON u.id = c.current_owner_user_id
CROSS JOIN (SELECT 1 seq UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL SELECT 5 UNION ALL SELECT 6) st
WHERE st.seq <= 2 + MOD(c.id, 5)
  AND (ds.case_iteration_id = (SELECT ci.id FROM case_iterations ci WHERE ci.case_id = c.id AND ci.sequence_number = 1)
       OR MOD(c.id, 13) <> 0);

-- ============================================================
-- SECAO 18: EVIDENCIAS (1..3 por caso) + VINCULOS COM HIPOTESES
-- ============================================================
INSERT INTO case_evidences (case_id, evidence_type, description, created_by, created_at, case_iteration_id)
SELECT c.id,
  ELT(MOD(c.id + ev.k, 6) + 1, 'Log','Screenshot','Database','Network','Config','Report'),
  CONCAT('Evidencia capturada no caso ', c.external_reference, ' - ',
         ELT(MOD(c.id + ev.k, 6) + 1,
           'trecho de log do servico no momento da falha',
           'imagem do erro apresentado ao operador',
           'consulta executada no banco local da filial',
           'resultado de conectividade com o destino',
           'conteudo de parametros vigentes na filial',
           'relatorio de auditoria do modulo envolvido')),
  c.current_owner_user_id,
  CASE WHEN MOD(c.id + ev.k, 2) = 0 THEN DATE_ADD(c.opened_at, INTERVAL (30 + MOD(c.id, 240)) MINUTE)
       ELSE DATE_ADD(c.opened_at, INTERVAL (240 + MOD(c.id, 720)) MINUTE) END,
  ci1.id
FROM cases c
JOIN case_iterations ci1 ON ci1.case_id = c.id AND ci1.sequence_number = 1
CROSS JOIN (SELECT 1 k UNION ALL SELECT 2 UNION ALL SELECT 3) ev
WHERE ev.k <= 1 + MOD(c.id, 3);

INSERT INTO case_hypothesis_evidence (evidence_id, hypothesis_id, relation_type, justification, created_by, created_at)
SELECT ce.id,
       (SELECT MIN(h2.id) FROM case_hypotheses h2 WHERE h2.case_id = ce.case_id),
       ELT(MOD(ce.id, 4) + 1, 'Supports','Contradicts','Inconclusive','Confirms'),
       'Evidencia coletada durante o atendimento e confrontada com a hipotese.',
       ce.created_by, ce.created_at
FROM case_evidences ce;

INSERT INTO case_hypothesis_evidence (evidence_id, hypothesis_id, relation_type, justification, created_by, created_at)
SELECT ce.id,
       (SELECT MAX(h2.id) FROM case_hypotheses h2 WHERE h2.case_id = ce.case_id),
       ELT(MOD(ce.id + 1, 4) + 1, 'Supports','Contradicts','Inconclusive','Confirms'),
       'Segunda confrontacao da evidencia com hipotese alternativa.',
       ce.created_by, ce.created_at
FROM case_evidences ce
WHERE MOD(ce.id, 3) <> 0
  AND (SELECT MIN(h2.id) FROM case_hypotheses h2 WHERE h2.case_id = ce.case_id)
    <> (SELECT MAX(h2.id) FROM case_hypotheses h2 WHERE h2.case_id = ce.case_id);

-- ============================================================
-- SECAO 19: RESOLUCOES (Resolved/Closed/Reopened - iteracao 1)
-- ============================================================
INSERT INTO case_resolutions (case_id, resolution_summary, validation_summary, root_cause_id, root_cause_confirmed, responsible_department_id, resolution_type, recurrence_risk, recurrence_notes, preventive_actions, effort_minutes, resolved_by, resolved_at, case_iteration_id)
SELECT c.id,
  CONCAT('Resolucao do caso ', c.external_reference, ' com aplicacao de ', IFNULL((SELECT rc.name FROM root_causes rc WHERE rc.code = fm.rcode), 'ajustes'), '.'),
  CONCAT('Validacao executada e confirmacao registrada junto ao cliente em ', DATE_FORMAT(c.resolved_at, '%d/%m/%Y'), '.'),
  rc.id,
  CASE WHEN c.root_cause_status = 'Confirmed' THEN 1 ELSE 0 END,
  c.current_department_id,
  'Definitive',
  ELT(MOD(c.id, 4) + 1, 'Low','Medium','Low','High'),
  'Risco de recorrencia avaliado no encerramento do atendimento.',
  'Acompanhar o comportamento por 30 dias e revisitar o procedimento se recorrencia.',
  (60 + MOD(c.id, 600)),
  c.current_owner_user_id,
  c.resolved_at,
  ci1.id
FROM cases c
JOIN tmp_fam fm ON c.case_number BETWEEN fm.a AND fm.b
JOIN case_iterations ci1 ON ci1.case_id = c.id AND ci1.sequence_number = 1
LEFT JOIN root_causes rc ON rc.code = fm.rcode
WHERE c.status IN ('Resolved','Closed','Reopened');

-- ============================================================
-- SECAO 20: RELACOES ENTRE CASOS (~560)
-- CommonCause (440) + Recurrence (55) + Duplicate (20) + Similar (45)
-- ============================================================
INSERT INTO case_relations (source_case_id, target_case_id, relation_type, similarity_score, matched_factors_json, created_by, created_at)
WITH offs AS (
  SELECT 1 o UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
  UNION ALL SELECT 5 UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8
),
links AS (
  SELECT f.a AS s, f.a + o.o AS t FROM tmp_fam f CROSS JOIN offs o
)
SELECT (SELECT id FROM cases WHERE case_number = l.s),
       (SELECT id FROM cases WHERE case_number = l.t),
       'CommonCause', 0.9, '{"factor":"root_cause"}', 1, '2026-09-19 10:00:00'
FROM links l;

INSERT INTO case_relations (source_case_id, target_case_id, relation_type, similarity_score, matched_factors_json, created_by, created_at)
WITH synth AS (
  SELECT 1 rn UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL SELECT 5
  UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9 UNION ALL SELECT 10
  UNION ALL SELECT 11 UNION ALL SELECT 12 UNION ALL SELECT 13 UNION ALL SELECT 14 UNION ALL SELECT 15
  UNION ALL SELECT 16 UNION ALL SELECT 17 UNION ALL SELECT 18 UNION ALL SELECT 19 UNION ALL SELECT 20
  UNION ALL SELECT 21 UNION ALL SELECT 22 UNION ALL SELECT 23 UNION ALL SELECT 24 UNION ALL SELECT 25
  UNION ALL SELECT 26 UNION ALL SELECT 27 UNION ALL SELECT 28 UNION ALL SELECT 29 UNION ALL SELECT 30
  UNION ALL SELECT 31 UNION ALL SELECT 32 UNION ALL SELECT 33 UNION ALL SELECT 34 UNION ALL SELECT 35
  UNION ALL SELECT 36 UNION ALL SELECT 37 UNION ALL SELECT 38 UNION ALL SELECT 39 UNION ALL SELECT 40
  UNION ALL SELECT 41 UNION ALL SELECT 42 UNION ALL SELECT 43 UNION ALL SELECT 44 UNION ALL SELECT 45
  UNION ALL SELECT 46 UNION ALL SELECT 47 UNION ALL SELECT 48 UNION ALL SELECT 49 UNION ALL SELECT 50
  UNION ALL SELECT 51 UNION ALL SELECT 52 UNION ALL SELECT 53 UNION ALL SELECT 54 UNION ALL SELECT 55
),
links AS (
  SELECT f.a AS s, f.a + 12 AS t, 'Recurrence' AS rt, 0.7 AS sc FROM tmp_fam f JOIN synth ON synth.rn = f.fid
  UNION ALL SELECT f.a, f.a + 10, 'Similar', 0.6 FROM tmp_fam f JOIN synth ON synth.rn = f.fid WHERE f.fid <= 45
  UNION ALL SELECT f.a + 1, f.a + 2, 'Duplicate', 0.95 FROM tmp_fam f JOIN synth ON synth.rn = f.fid WHERE f.fid <= 20
)
SELECT (SELECT id FROM cases WHERE case_number = s),
       (SELECT id FROM cases WHERE case_number = t),
       rt, sc, CASE rt WHEN 'Duplicate' THEN '{"factor":"evidence_duplicate"}' WHEN 'Recurrence' THEN '{"factor":"client_recurrence"}' ELSE '{"factor":"symptom_similarity"}' END,
       1, '2026-09-19 10:00:00'
FROM links;

-- ============================================================
-- SECAO 21: ITENS DE CONHECIMENTO (200)
-- 160 Published | 20 InReview | 12 Draft | 6 Deprecated | 2 Superseded
-- ============================================================
INSERT INTO knowledge_items (knowledge_code, knowledge_type, title, summary, status, confidentiality, owner_user_id, owner_department_id, current_version_no, provenance_type, provenance_case_id, provenance_reference, review_due_at, last_reviewed_at, published_at, deprecated_at, created_at, created_by, updated_at)
WITH RECURSIVE nums AS (
  SELECT 1 AS k UNION ALL SELECT k + 1 FROM nums WHERE k < 200
),
usr AS (
  SELECT id, ROW_NUMBER() OVER (ORDER BY id) AS rn,
         (SELECT ud.department_id FROM user_departments ud WHERE ud.user_id = u.id) AS did
  FROM users u WHERE email <> 'admin@tracecore.local'
)
SELECT
  CONCAT('KB-', LPAD(k, 4, '0')),
  ELT(MOD(k - 1, 5) + 1, 'Solution','Runbook','FAQ','Guide','InvestigationTemplate'),
  CONCAT('Orientacao: ', IFNULL((SELECT rc.name FROM root_causes rc WHERE rc.code = fm.rcode), 'Operacao'), ' - ', p.name),
  CONCAT('Procedimento consolidado para tratar o sintoma relacionado a ',
         IFNULL((SELECT rc.name FROM root_causes rc WHERE rc.code = fm.rcode), 'parametrizacao'), '.'),
  CASE WHEN k <= 160 THEN 'Published'
       WHEN k <= 180 THEN 'InReview'
       WHEN k <= 192 THEN 'Draft'
       WHEN k <= 198 THEN 'Deprecated'
       ELSE 'Superseded' END,
  ELT(MOD(k, 4) + 1, 'Internal','Internal','Client','Internal'),
  (SELECT u2.id FROM usr u2 WHERE u2.rn = MOD(k - 1, 10) + 1) AS owner_user_id,
  (SELECT u2.did FROM usr u2 WHERE u2.rn = MOD(k - 1, 10) + 1) AS owner_department_id,
  1,
  'Case',
  (SELECT id FROM cases WHERE case_number = 1 + MOD(k * 37, 1280)) AS provenance_case_id,
  (SELECT external_reference FROM cases WHERE case_number = 1 + MOD(k * 37, 1280)) AS provenance_reference,
  CASE WHEN k <= 180 THEN DATE_ADD(DATE_ADD('2025-03-01', INTERVAL MOD(k * 13, 500) DAY), INTERVAL 200 DAY) ELSE NULL END,
  CASE WHEN k <= 180 THEN DATE_ADD('2025-03-01', INTERVAL MOD(k * 13, 500) DAY) ELSE NULL END,
  CASE WHEN k <= 198 THEN DATE_ADD(DATE_ADD('2025-03-01', INTERVAL MOD(k * 13, 500) DAY), INTERVAL (5 + MOD(k, 40)) DAY) ELSE NULL END,
  CASE WHEN k > 192 AND k <= 198 THEN DATE_ADD('2026-08-01', INTERVAL MOD(k, 40) DAY) ELSE NULL END,
  DATE_ADD('2025-03-01', INTERVAL MOD(k * 13, 500) DAY),
  1,
  DATE_ADD(DATE_ADD('2025-03-01', INTERVAL MOD(k * 13, 500) DAY), INTERVAL (5 + MOD(k, 40)) DAY)
FROM nums
JOIN tmp_fam fm ON fm.fid = MOD(k - 1, 55) + 1
JOIN products p ON p.id = 1 + MOD(k * 7, 6);

-- ============================================================
-- SECAO 22: VERSOES, PASSOS, SINTOMAS, APLICABILIDADE, TAGS, TECNOLOGIAS
-- ============================================================
INSERT INTO knowledge_versions (knowledge_item_id, version_no, content_markdown, problem_description, root_cause_summary, validation_method, risk_warning, rollback_plan, change_summary, status, content_hash, created_at, created_by, approved_at, approved_by)
SELECT ki.id, 1,
  CONCAT('# ', ki.title, '\n\n', ki.summary, '\n\n## Sintomas\n- Validar o sintoma relatado\n- Executar a verificacao orientada\n- Registrar o resultado no caso\n\n## Aplicacao\nSeguir o procedimento com autorizacao do responsavel.'),
  CONCAT('Problema descrito: ', ki.summary),
  CONCAT('Causa raiz consolidada: ', IFNULL((SELECT rc.name FROM root_causes rc WHERE rc.code = fm.rcode), 'ajuste operacional')),
  'Validacao pratica em ambiente de homologacao.',
  'Aplicar somente sob procedimento autorizado.',
  'Restaurar parametros anteriores em caso de falha.',
  'Versao inicial do item de conhecimento.',
  ki.status,
  SHA2(CONCAT(ki.knowledge_code, ki.title), 256),
  ki.created_at, 1,
  IF(ki.status IN ('Published','Superseded'), ki.published_at, NULL),
  1
FROM knowledge_items ki
JOIN tmp_fam fm ON fm.fid = MOD(ki.id - 1, 55) + 1;

INSERT INTO knowledge_steps (knowledge_version_id, sequence_no, step_type, title, description, command, expected_output)
SELECT kv.id, st.seq,
  ELT(MOD(kv.knowledge_item_id + st.seq, 3) + 1, 'Verification','Solution','Solution'),
  CONCAT('Passo ', st.seq, ' - ', ELT(st.seq,
     'Validar pre-requisitos','Aplicar procedimento recomendado','Confirmar resolucao',
     'Consolidar conclusao','Finalizar e registrar','Validar ambiente')),
  CONCAT('Descricao do passo ', st.seq, ' para o item ', ki.knowledge_code, '.'),
  NULL, NULL
FROM knowledge_versions kv
JOIN knowledge_items ki ON ki.id = kv.knowledge_item_id
CROSS JOIN (SELECT 1 seq UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL SELECT 5 UNION ALL SELECT 6) st
WHERE st.seq <= 3 + MOD(kv.knowledge_item_id, 4);

INSERT INTO knowledge_symptoms (knowledge_version_id, symptom_text)
SELECT kv.id, ELT(1 + MOD(kv.knowledge_item_id + sy.k, 9),
  'Ocorrencia identificada em horario de pico da operacao.',
  'Erro relatado de forma recorrente pelo cliente.',
  'Falha em comunicacao com servico externo.',
  'Impacto parcial nao bloqueia a operacao principal.',
  'Sintoma observado em mais de um terminal da filial.',
  'Comportamento divergente do esperado pelo operador.',
  'Retorno negativo de integracao entre sistemas.',
  'Lentidao perceptivel em rotina especifica.',
  'Registro de evento no monitor de servicos.')
FROM knowledge_versions kv
CROSS JOIN (SELECT 1 k UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4) sy
WHERE sy.k <= 2 + MOD(kv.knowledge_item_id, 3);

INSERT INTO knowledge_applicability (knowledge_item_id, product_id, product_version_id, component_id, environment_id, applicability_type, notes)
SELECT ki.id,
  p.id,
  (SELECT MIN(pv2.id) FROM product_versions pv2 WHERE pv2.product_id = p.id),
  (SELECT cmp.id FROM components cmp WHERE cmp.code = fm.ccode),
  (SELECT e2.id FROM environments e2 WHERE e2.environment_type = 'Production'),
  'Applies',
  NULL
FROM knowledge_items ki
JOIN products p ON p.id = 1 + MOD(ki.id * 7, 6)
JOIN tmp_fam fm ON fm.fid = MOD(ki.id - 1, 55) + 1;

INSERT INTO knowledge_applicability (knowledge_item_id, product_id, product_version_id, component_id, environment_id, applicability_type, notes)
SELECT ki.id, p.id, NULL, NULL, NULL, 'Applies', 'Aplicavel ao modulo correspondente da familia causal.'
FROM knowledge_items ki
JOIN products p ON p.id = 1 + MOD(ki.id * 7, 6);

INSERT INTO knowledge_tags (knowledge_item_id, tag_id)
SELECT ki.id, t.id
FROM knowledge_items ki
CROSS JOIN (SELECT 1 k UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4) tk
JOIN (SELECT id, ROW_NUMBER() OVER (ORDER BY id) rn FROM tags) t ON t.rn = 1 + MOD(ki.id + tk.k * 7, 24)
WHERE tk.k <= 2 + MOD(ki.id, 3);

INSERT INTO knowledge_technologies (knowledge_item_id, technology_id)
SELECT ki.id, t.id
FROM knowledge_items ki
JOIN (SELECT id, ROW_NUMBER() OVER (ORDER BY id) rn FROM technologies) t ON t.rn = 1 + MOD(ki.id, 16);

-- ============================================================
-- SECAO 23: USOS DE CONHECIMENTO (320)
-- ============================================================
INSERT INTO knowledge_usages (knowledge_item_id, knowledge_version_id, case_id, used_by, used_at, outcome, notes, context_match_json)
WITH RECURSIVE nums AS (
  SELECT 1 AS u UNION ALL SELECT u + 1 FROM nums WHERE u < 320
)
SELECT ki.id,
  (SELECT MIN(kv.id) FROM knowledge_versions kv WHERE kv.knowledge_item_id = ki.id),
  (SELECT id FROM cases WHERE case_number = 1 + MOD(n.u * 7, 1280)),
  (SELECT u2.id FROM (SELECT id, ROW_NUMBER() OVER (ORDER BY id) rn FROM users WHERE email <> 'admin@tracecore.local') u2 WHERE u2.rn = 1 + MOD(n.u, 10)),
  DATE_ADD('2025-05-01 00:00:00', INTERVAL (10 + MOD(n.u * 11, 490)) DAY),
  ELT(1 + MOD(n.u, 4), 'Success','Partial','NoMatch','Success'),
  CONCAT('Uso do item ', ki.knowledge_code, ' registrado durante o atendimento.'),
  NULL
FROM knowledge_items ki
JOIN nums n ON ki.id = 1 + MOD(n.u - 1, 200);

-- ============================================================
-- SECAO 24: INDICE CONTEUDO BUSCAVEL
-- ValidatedKnowledge (itens publicados) + HistoricalCase (resolvidos)
-- ============================================================
INSERT INTO searchable_content_entries (source_type, source_id, source_version_id, title, normalized_content, content_hash, validation_status, quality_status, visibility, client_id, product_id, component_ids_json, metadata_json, created_at, updated_at, source_updated_at, indexed_at)
SELECT 'ValidatedKnowledge', ki.id, kv.id, ki.title,
  CONCAT_WS(' ', ki.title, ki.summary, kv.content_markdown),
  SHA2(CONCAT_WS(' ', ki.title, ki.summary, kv.content_markdown), 256),
  'Validated', 'Validated',
  CASE WHEN ki.confidentiality = 'Client' THEN 'Client' ELSE 'Internal' END,
  NULL, ka.product_id, NULL, NULL,
  ki.created_at, ki.updated_at, ki.published_at, ki.published_at
FROM knowledge_items ki
JOIN knowledge_versions kv ON kv.knowledge_item_id = ki.id
JOIN (SELECT DISTINCT knowledge_item_id, product_id FROM knowledge_applicability) ka ON ka.knowledge_item_id = ki.id
WHERE ki.status = 'Published';

INSERT INTO searchable_content_entries (source_type, source_id, source_version_id, title, normalized_content, content_hash, validation_status, quality_status, visibility, client_id, product_id, component_ids_json, metadata_json, created_at, updated_at, source_updated_at, indexed_at)
SELECT 'HistoricalCase', c.id, NULL, CONCAT('Caso resolvido ', c.external_reference),
  CONCAT_WS(' ', c.original_report, c.normalized_summary, c.expected_behavior, c.observed_behavior, c.error_message),
  SHA2(CONCAT_WS(' ', c.original_report, c.normalized_summary, c.expected_behavior, c.observed_behavior, c.error_message), 256),
  CASE WHEN c.root_cause_status = 'Confirmed' THEN 'Validated' ELSE 'Draft' END,
  'Validated', 'Internal',
  c.client_id, c.product_id, NULL, NULL,
  c.created_at, c.updated_at, c.updated_at, c.updated_at
FROM cases c
WHERE c.status IN ('Resolved','Closed');

-- ============================================================
-- SECAO 25: BUSCA (sessoes, consultas e interacoes com resultados)
-- ============================================================
INSERT INTO search_sessions (user_id, started_at, context_json)
WITH RECURSIVE ss AS (
  SELECT 1 AS s UNION ALL SELECT s + 1 FROM ss WHERE s < 120
)
SELECT
  (SELECT u2.id FROM (SELECT id, ROW_NUMBER() OVER (ORDER BY id) rn FROM users WHERE email <> 'admin@tracecore.local') u2 WHERE u2.rn = 1 + MOD(s, 10)),
  DATE_ADD('2025-06-01 00:00:00', INTERVAL MOD(s * 29, 470) DAY),
  NULL
FROM ss;

INSERT INTO search_queries (search_session_id, query_text, filters_json, result_count, duration_ms, executed_at)
WITH jq AS (
  SELECT 1 rn, 'CT-e rejeitado' txt
  UNION ALL SELECT 2,'falha de sincronizacao'
  UNION ALL SELECT 3,'certificado expirado'
  UNION ALL SELECT 4,'tabela de frete vencida'
  UNION ALL SELECT 5,'MDF-e bloqueado'
  UNION ALL SELECT 6,'Firebird banco em uso'
  UNION ALL SELECT 7,'login portal lento'
  UNION ALL SELECT 8,'posicao GPS ausente'
  UNION ALL SELECT 9,'token expirado API'
  UNION ALL SELECT 10,'EDI rejeitado parceiro'
  UNION ALL SELECT 11,'erro RFC SAP'
  UNION ALL SELECT 12,'quota de API excedida'
  UNION ALL SELECT 13,'dashboards demorando'
  UNION ALL SELECT 14,'roteiro mobile travando'
  UNION ALL SELECT 15,'NF-e duplicada'
  UNION ALL SELECT 16,'backup noturno falhou'
)
SELECT s2.id, jq.txt, NULL, 1 + MOD(s2.id + q.k, 9), 60 + MOD(s2.id * q.k, 900),
  DATE_ADD(s2.started_at, INTERVAL (q.k * 2) MINUTE)
FROM search_sessions s2
CROSS JOIN (SELECT 1 k UNION ALL SELECT 2 UNION ALL SELECT 3) q
JOIN jq ON jq.rn = 1 + MOD(s2.id + q.k, 16)
WHERE q.k <= 2 + MOD(s2.id, 2);

INSERT INTO search_result_interactions (search_query_id, result_type, result_id, position, opened_at, feedback_useful)
SELECT sq2.id,
  ELT(1 + MOD(sq2.id, 3), 'Case','Knowledge','RootCause'),
  CASE 1 + MOD(sq2.id, 3)
    WHEN 1 THEN 1 + MOD(sq2.id * 3, 1280)
    WHEN 2 THEN 1 + MOD(sq2.id * 5, 200)
    ELSE 1 + MOD(sq2.id * 7, 30) END,
  1, DATE_ADD(sq2.executed_at, INTERVAL (8 + sq2.duration_ms) SECOND),
  CASE WHEN MOD(sq2.id, 3) = 0 THEN NULL ELSE MOD(sq2.id, 2) END
FROM search_queries sq2;

INSERT INTO search_result_interactions (search_query_id, result_type, result_id, position, opened_at, feedback_useful)
SELECT sq2.id,
  ELT(1 + MOD(sq2.id, 2), 'Knowledge','Case'),
  CASE 1 + MOD(sq2.id, 2)
    WHEN 1 THEN 1 + MOD(sq2.id * 11, 200)
    ELSE 1 + MOD(sq2.id * 7, 1280) END,
  2, DATE_ADD(sq2.executed_at, INTERVAL (20 + sq2.duration_ms) SECOND),
  MOD(sq2.id, 2)
FROM search_queries sq2
WHERE MOD(sq2.id, 3) <> 0;

-- ============================================================
-- SECAO 26: INTERACOES DE IA, FONTES E FEEDBACK
-- ============================================================
INSERT INTO ai_interactions (user_id, query_text, response_text, provider_code, model_name, tokens_used, latency_ms, created_at, metadata_json)
WITH RECURSIVE ss AS (
  SELECT 1 AS s UNION ALL SELECT s + 1 FROM ss WHERE s < 120
),
jq AS (
  SELECT 1 rn, 'como resolver CT-e rejeitado' txt
  UNION ALL SELECT 2,'por que a sincronizacao parou'
  UNION ALL SELECT 3,'certificado A1 vencido'
  UNION ALL SELECT 4,'tabela de frete divergente'
  UNION ALL SELECT 5,'MDF-e bloqueado como destravar'
  UNION ALL SELECT 6,'Firebird travado arquivo'
  UNION ALL SELECT 7,'portal lento dashboard'
  UNION ALL SELECT 8,'falta de posicao GPS'
  UNION ALL SELECT 9,'token API invalido'
  UNION ALL SELECT 10,'EDI rejeitado motivo'
  UNION ALL SELECT 11,'erro RFC SAP material'
  UNION ALL SELECT 12,'quota excedida integrador'
  UNION ALL SELECT 13,'relatorios duplicados'
  UNION ALL SELECT 14,'roteiro demora para abrir'
  UNION ALL SELECT 15,'NF-e emitida duas vezes'
  UNION ALL SELECT 16,'como corrigir permissao'
)
SELECT
  (SELECT u2.id FROM (SELECT id, ROW_NUMBER() OVER (ORDER BY id) rn FROM users WHERE email <> 'admin@tracecore.local') u2 WHERE u2.rn = 1 + MOD(s, 10)),
  jq.txt,
  CONCAT('Resposta sintetizada com base nas fontes disponiveis para: ', jq.txt),
  ELT(1 + MOD(s, 2), 'anthropic','openai'),
  CASE 1 + MOD(s, 2) WHEN 1 THEN 'claude-sonnet-4-v2' ELSE 'gpt-5-mini' END,
  300 + MOD(s * 13, 2400),
  500 + MOD(s * 37, 2500),
  DATE_ADD('2025-07-01 00:00:00', INTERVAL MOD(s * 97, 500) DAY),
  NULL
FROM ss
JOIN jq ON jq.rn = 1 + MOD(s * 3, 16);

INSERT INTO ai_sources (ai_interaction_id, searchable_content_entry_id, rank, similarity_score, created_at, source_type, source_ref_id, source_url, source_title, match_score)
SELECT ii.id, sce.id, ss.k,
  0.55 + ROUND(MOD(ii.id * ss.k, 40) / 100, 2),
  ii.created_at,
  ELT(1 + MOD(ii.id + ss.k, 2), 'Knowledge','Case'),
  sce.source_id, NULL, sce.title,
  ROUND(0.5 + MOD(ii.id * 7, 45) / 100, 3)
FROM ai_interactions ii
CROSS JOIN (SELECT 1 k UNION ALL SELECT 2 UNION ALL SELECT 3) ss
JOIN (SELECT id, source_id, title, ROW_NUMBER() OVER (ORDER BY id) rn FROM searchable_content_entries) sce ON sce.rn = 1 + MOD(ii.id * 13 + ss.k * 31, 1000)
WHERE ss.k <= 2 + MOD(ii.id, 2);

INSERT INTO ai_interaction_feedback (ai_interaction_id, user_id, useful, comment, created_at)
WITH RECURSIVE fb AS (
  SELECT 1 AS f UNION ALL SELECT f + 1 FROM fb WHERE f < 40
)
SELECT 1 + MOD(f * 3, 120),
  (SELECT u2.id FROM (SELECT id, ROW_NUMBER() OVER (ORDER BY id) rn FROM users WHERE email <> 'admin@tracecore.local') u2 WHERE u2.rn = 1 + MOD(f, 10)),
  MOD(f, 2),
  CASE WHEN MOD(f, 3) = 0 THEN 'Resposta ajudou a resolver o caso.' ELSE 'Resposta precisa de refinamento.' END,
  DATE_ADD('2025-07-05 00:00:00', INTERVAL MOD(f * 7, 480) DAY)
FROM fb;

-- ============================================================
-- SECAO 27: TRI LHA DE AUDITORIA (~500 eventos)
-- ============================================================
INSERT INTO audit_events (occurred_at, actor_user_id, actor_type, action, entity_type, entity_id, correlation_id, ip_address, user_agent_summary, before_json, after_json, metadata_json)
WITH RECURSIVE aa AS (
  SELECT 1 AS n UNION ALL SELECT n + 1 FROM aa WHERE n < 500
)
SELECT
  DATE_ADD((SELECT c2.opened_at FROM cases c2 WHERE c2.case_number = 1 + MOD(n * 3, 1280)), INTERVAL MOD(n, 90) HOUR),
  (SELECT u2.id FROM (SELECT id, ROW_NUMBER() OVER (ORDER BY id) rn FROM users WHERE email <> 'admin@tracecore.local') u2 WHERE u2.rn = 1 + MOD(n, 10)),
  'User',
  ELT(1 + MOD(n, 10), 'case.created','case.assigned','case.commented','case.evidence_added','case.hypothesis_added','case.resolved','case.status_changed','case.updated','case.reopened','case.closed'),
  'Case',
  CONCAT('', 1 + MOD(n * 3, 1280)),
  CONCAT('CORR-', LPAD(n, 6, '0')),
  ELT(1 + MOD(n, 8), '10.0.0.11','10.0.0.24','10.0.0.35','10.0.5.7','10.0.5.18','10.0.7.3','10.0.9.2','10.20.1.1'),
  ELT(1 + MOD(n, 3), 'web','desktop','mobile'),
  NULL, NULL, NULL
FROM aa;