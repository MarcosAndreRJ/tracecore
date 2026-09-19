-- ============================================================
-- TRACECORE - MASSA DE TESTES
-- SOMENTE DESENVOLVIMENTO
-- NAO EXECUTAR EM PRODUCAO
-- ============================================================
-- Versao: 1.0 | Data: 2026-09-19
-- Popula o banco com ~500 casos sinteticos e dados correlatos.
-- ============================================================
SET NAMES utf8mb4;
SET CHARACTER SET utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;
SET @next_case_number = (SELECT COALESCE(MAX(case_number), 0) FROM cases) + 1;
-- ============================================================
-- SECAO 1: CLIENTES SINTETICOS (8 novos)
-- ============================================================
INSERT INTO clients (code, name, status, created_at) VALUES
  ('SYN-CLI-AUR','Grupo Aurora','Active',NOW()),
  ('SYN-CLI-HOR','Comercial Horizonte','Active',NOW()),
  ('SYN-CLI-ATL','Distribuidora Atlas','Active',NOW()),
  ('SYN-CLI-SAN','Clinica Santa Helena','Active',NOW()),
  ('SYN-CLI-VRT','Industria Vertice','Active',NOW()),
  ('SYN-CLI-BOR','Logistica Boreal','Active',NOW()),
  ('SYN-CLI-PIO','Mercado Pioneiro','Active',NOW()),
  ('SYN-CLI-MER','Servicos Meridian','Active',NOW());

-- ============================================================
-- SECAO 2: UNIDADES DE CLIENTES (16)
-- ============================================================
INSERT INTO client_units (client_id, code, name, status, created_at)
SELECT c.id, CONCAT('UC-',c.code,'-01'), CONCAT(c.name,' - Matriz'),'Active',NOW()
FROM clients c WHERE c.code LIKE 'SYN-CLI-%';
INSERT INTO client_units (client_id, code, name, status, created_at)
SELECT c.id, CONCAT('UC-',c.code,'-02'), CONCAT(c.name,' - Filial'),'Active',NOW()
FROM clients c WHERE c.code LIKE 'SYN-CLI-%';
-- ============================================================
-- SECAO 3: COMPONENTES ADICIONAIS (8 novos)
-- ============================================================
INSERT INTO components (product_id,code,name,component_type,description,status,created_at)
SELECT p.id,'AUTH-SERVICE','Servico de Autenticacao','Servico','Servico centralizado de autenticacao IAM','Active',NOW() FROM products p WHERE p.code='PRD-WEB';
INSERT INTO components (product_id,code,name,component_type,description,status,created_at)
SELECT p.id,'DB-POOL','Pool de Conexoes','Infraestrutura','Pool de conexoes MySQL compartilhado','Active',NOW() FROM products p WHERE p.code='PRD-API';
INSERT INTO components (product_id,code,name,component_type,description,status,created_at)
SELECT p.id,'DNS-RESOLVER','Resolver DNS','Infraestrutura','Servico de resolucao de nomes DNS','Active',NOW() FROM products p WHERE p.code='PRD-API';
INSERT INTO components (product_id,code,name,component_type,description,status,created_at)
SELECT p.id,'CERT-MGR','Gerenciador de Certificados','Seguranca','Gestao de certificados TLS/mTLS','Active',NOW() FROM products p WHERE p.code='PRD-WEB';
INSERT INTO components (product_id,code,name,component_type,description,status,created_at)
SELECT p.id,'SAP-CONNECTOR','Conector SAP','Integracao','Conector de integracao SAP ERP via RFC','Active',NOW() FROM products p WHERE p.code='PRD-API';
INSERT INTO components (product_id,code,name,component_type,description,status,created_at)
SELECT p.id,'QUEUE-MGR','Gerenciador de Filas','Servico','Gerenciamento de filas RabbitMQ/Kafka','Active',NOW() FROM products p WHERE p.code='PRD-API';
INSERT INTO components (product_id,code,name,component_type,description,status,created_at)
SELECT p.id,'FW-RULES','Regras de Firewall','Infraestrutura','Regras de controle de acesso de rede','Active',NOW() FROM products p WHERE p.code='PRD-API';
INSERT INTO components (product_id,code,name,component_type,description,status,created_at)
SELECT p.id,'FILE-IMP','Importador de Arquivos','Servico','Importacao e processamento CSV/XML','Active',NOW() FROM products p WHERE p.code='PRD-ERP';
-- ============================================================
-- SECAO 4: USUARIOS SINTETICOS (5) e vinculos
-- ============================================================
INSERT INTO users (name,email,password_hash,status,created_at,row_version) VALUES
  ('Ana Martins','ana.martins@tracecore.local','','Active',NOW(),1),
  ('Bruno Costa','bruno.costa@tracecore.local','','Active',NOW(),1),
  ('Carla Souza','carla.souza@tracecore.local','','Active',NOW(),1),
  ('Diego Lima','diego.lima@tracecore.local','','Active',NOW(),1),
  ('Fernanda Alves','fernanda.alves@tracecore.local','','Active',NOW(),1);
INSERT INTO user_departments (user_id,department_id)
SELECT u.id,d.id FROM users u
JOIN (SELECT id,ROW_NUMBER() OVER(ORDER BY id) rn FROM users WHERE email!='admin@tracecore.local') ur ON u.id=ur.id
JOIN (SELECT id,ROW_NUMBER() OVER(ORDER BY id) rn FROM departments WHERE name IN('Suporte','Infraestrutura','Desenvolvimento Web','Banco de Dados','Integrações')) d ON ur.rn=d.rn;
INSERT INTO user_roles (user_id,role_id)
SELECT u.id,r.id FROM users u CROSS JOIN roles r
WHERE u.email!='admin@tracecore.local' AND r.name='Usuário Técnico' AND u.id>1;

-- ============================================================
-- SECAO 5: CAUSAS RAIZ ADICIONAIS (15)
-- ============================================================
INSERT INTO root_causes (code,name,category,description,created_at) VALUES
  ('RC-AUTH-LOCK','Bloqueio de Credencial','Seguranca / Autenticacao','Conta bloqueada por exceder limite de tentativas',NOW()),
  ('RC-PWD-EXPIRED','Credencial Expirada','Seguranca / Autenticacao','Senha atingiu data de validade',NOW()),
  ('RC-IAM-DOWN','Servico IAM Indisponivel','Infraestrutura / Autenticacao','Servico de identidade fora do ar',NOW()),
  ('RC-API-TIMEOUT','Timeout de Gateway API','Rede / Gateway','Gateway nao responde dentro do SLA',NOW()),
  ('RC-DB-POOL-EXH','Esgotamento de Pool','Banco de Dados / Pool','Pool MySQL atingiu limite maximo',NOW()),
  ('RC-DB-DEADLOCK','Deadlock em Transacao','Banco de Dados / Concorrencia','Deadlock entre transacoes concorrentes',NOW()),
  ('RC-DB-SLOW','Performance Degradada','Banco de Dados / Performance','Query sem indice causando full scan',NOW()),
  ('RC-DNS-FAIL','Falha de Resolucao DNS','Rede / DNS','Servidor DNS primario inacessivel',NOW()),
  ('RC-CERT-EXPIRED','Certificado TLS Expirado','Seguranca / Certificados','Certificado digital expirado',NOW()),
  ('RC-DEPLOY-REG','Regressao em Deploy','Deploy / CI-CD','Versao publicou comportamento inesperado',NOW()),
  ('RC-MOB-SYNC','Falha Sincronizacao Mobile','Mobile / Sync','Dados nao sincronizaram entre app e servidor',NOW()),
  ('RC-SAP-INT','Erro Integracao SAP','Integracao / SAP','Conector SAP falhou durante RFC',NOW()),
  ('RC-PERM-ERR','Permissao Incorreta','Seguranca / Permissoes','Usuario sem permissao necessaria',NOW()),
  ('RC-FW-BLOCK','Bloqueio de Firewall','Infraestrutura / Rede','Regra bloqueando acesso legitimo',NOW()),
  ('RC-QUEUE-BACK','Backlog de Filas','Mensageria / Filas','Fila acumulou mensagens lentamente',NOW());

-- ============================================================
-- SECAO 6: TAGS ADICIONAIS (12)
-- ============================================================
INSERT INTO tags (name) VALUES
  ('auth-lock'),('password-expired'),('iam-down'),('api-502'),('api-504'),
  ('dns-fail'),('deploy-regression'),('mobile-sync'),('sap-connector'),
  ('queue-backlog'),('permission-denied'),('file-import');
-- ============================================================
-- SECAO 7: CASE_NUMBERS VIA case_number_seq (500 registros)
-- ============================================================
INSERT INTO case_number_seq VALUES
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),
(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL),(NULL);
-- ============================================================
-- SECAO 8: INSERCAO DOS 500 CASOS
-- Distribuicao por familia causal (500 total):
--   AUTH-CREDENTIAL-LOCK 60 | API-TIMEOUT 55 | DB-CONNECTION-POOL 50
--   API-502 45 | PERMISSION-ERROR 40 | DB-SLOW-QUERY 35
--   MOBILE-SYNC 30 | SAP-INTEGRATION 30 | WEB-DEPLOY-REGRESSION 25
--   DNS-RESOLUTION 20 | CERTIFICATE-EXPIRED 15 | DB-DEADLOCK 15
--   FIREWALL-BLOCK 15 | QUEUE-BACKLOG 15 | MISC-INCIDENT 50
-- ============================================================

-- Usar CTE recursiva para gerar numeros 1..500 e INSERT-SELECT massivo
-- Atribuicao deterministica por faixa de sequencia

-- MariaDB 10.5: aceita INSERT ... WITH ... SELECT (e exige expressoes
-- constantes em LIMIT/OFFSET). Usamos CTEs com ranking no lugar de OFFSET.
INSERT INTO cases (
  case_number, external_reference, source_type, client_id, product_id,
  product_version_id, environment_id, original_report, normalized_summary,
  severity, impact_level, status, current_owner_user_id, current_department_id,
  root_cause_status, opened_at, first_response_at, resolved_at, closed_at,
  created_at, created_by, updated_at, updated_by, row_version
)
WITH RECURSIVE nums AS (
  SELECT 1 AS n
  UNION ALL
  SELECT n + 1 FROM nums WHERE n < 500
),
clients_r AS (
  SELECT id, ROW_NUMBER() OVER (ORDER BY id ASC) AS rn FROM clients
),
products_r AS (
  SELECT id, ROW_NUMBER() OVER (ORDER BY id ASC) AS rn FROM products
),
versions_r AS (
  SELECT pv.id, ROW_NUMBER() OVER (ORDER BY p.id ASC, pv.id ASC) AS rn
  FROM product_versions pv
  JOIN products p ON pv.product_id = p.id
),
envs_r AS (
  SELECT id, ROW_NUMBER() OVER (ORDER BY id ASC) AS rn FROM environments
),
users_r AS (
  SELECT id, ROW_NUMBER() OVER (ORDER BY id ASC) AS rn FROM users
),
depts_r AS (
  SELECT id, ROW_NUMBER() OVER (ORDER BY id ASC) AS rn FROM departments
)
SELECT
  @next_case_number + n - 1 AS case_number,
  CONCAT('SYN-DEMO-2026-CASE-', LPAD(CAST(n AS CHAR), 6, '0')) AS external_reference,
  'Synthetic' AS source_type,
  -- client_id: round-robin entre 11 clientes (3 existentes + 8 sinteticos)
  cl.id AS client_id,
  -- product_id: round-robin entre 4 produtos
  pr.id AS product_id,
  -- product_version_id: versao do produto correspondente
  vv.id AS product_version_id,
  -- environment_id: Producao(70%), Homologacao(20%), Desenvolvimento(10%)
  e.id AS environment_id,
  -- original_report: relatos variados por familia
  CASE
    WHEN n <= 60 THEN CASE (n % 15)
      WHEN 0 THEN 'Usuario nao consegue acessar o sistema. Mensagem: credencial invalida.'
      WHEN 1 THEN 'Conta bloqueada apos multiplas tentativas de login.'
      WHEN 2 THEN 'Login falha com erro HTTP 401. Credencial parece correta.'
      WHEN 3 THEN 'Funcionario relata que senha parou de funcionar. Conta bloqueada.'
      WHEN 4 THEN 'Autenticacao retorna erro de credencial invalida.'
      WHEN 5 THEN 'Acesso negado ao sistema. Conta bloqueada por seguranca.'
      WHEN 6 THEN 'Tela de login apresenta erro 401 repetidamente.'
      WHEN 7 THEN 'Credencial de servico expirou. Aplicacao nao autentica.'
      WHEN 8 THEN 'Multiplas contas reportam falha de login simultaneamente.'
      WHEN 9 THEN 'Token de autenticacao recusado. Sistema sugere redefinir senha.'
      WHEN 10 THEN 'Usuario tenta login e recebe mensagem de conta bloqueada.'
      WHEN 11 THEN 'Credencial nao aceita no portal. Senha alterada recentemente.'
      WHEN 12 THEN 'Erro 403 ao acessar modulo. Conta pode estar bloqueada.'
      WHEN 13 THEN 'Servico de login retorna timeout. Usuarios sem entrar.'
      ELSE 'Falha de autenticacao SSO. Contas vinculadas nao respondem.'
    END
    WHEN n <= 115 THEN CASE ((n - 60) % 10)
      WHEN 0 THEN 'Pedido fica processando e termina com timeout.'
      WHEN 1 THEN 'Cliente informa que confirmacao da venda demora e falha.'
      WHEN 2 THEN 'API nao responde dentro do tempo esperado.'
      WHEN 3 THEN 'Gateway retorna 504 durante o fechamento do pedido.'
      WHEN 4 THEN 'Operacao permanece carregando ate erro de comunicacao.'
      WHEN 5 THEN 'Requisicao POST excedeu timeout de 30 segundos.'
      WHEN 6 THEN 'Consulta a API de precos retorna timeout intermitente.'
      WHEN 7 THEN 'Sistema de pedidos travado durante pico de demanda.'
      WHEN 8 THEN 'API externa demora mais de 60 segundos para responder.'
      ELSE 'Multiplos endpoints retornam timeout entre 14h e 16h.'
    END
    WHEN n <= 165 THEN CASE ((n - 115) % 8)
      WHEN 0 THEN 'Erro too many connections no banco de dados.'
      WHEN 1 THEN 'Pool de conexoes esgotado. Aplicacao sem resposta.'
      WHEN 2 THEN 'Servico caiu por falta de conexoes disponiveis no MySQL.'
      WHEN 3 THEN 'Leak de conexoes detectado. Pool atingiu limite maximo.'
      WHEN 4 THEN 'Consulta retorna timeout de conexao. Pool saturado.'
      WHEN 5 THEN 'Aplicacao reporta Connection pool exhausted.'
      WHEN 6 THEN 'Erro de conexao MySQL: Too many connections.'
      ELSE 'Servico de integracao caiu por esgotamento do pool.'
    END
    WHEN n <= 210 THEN CASE ((n - 165) % 8)
      WHEN 0 THEN 'Gateway retorna erro 502 Bad Gateway.'
      WHEN 1 THEN 'Servico indisponivel. Resposta HTTP 502.'
      WHEN 2 THEN 'API retorna 502 durante horario de pico.'
      WHEN 3 THEN 'Nginx retorna 502 para todas as requisicoes da API.'
      WHEN 4 THEN 'Balanceador de carga reporta upstream timeout.'
      WHEN 5 THEN 'Chamada a API retorna 502. Servico parece fora do ar.'
      WHEN 6 THEN 'Portal exibe erro 502 ao tentar acessar dados.'
      ELSE 'Erros 502 intermitentes na API comercial.'
    END
    WHEN n <= 250 THEN CASE ((n - 210) % 8)
      WHEN 0 THEN 'Usuario nao tem permissao para acessar este modulo.'
      WHEN 1 THEN 'Erro 403 Forbidden ao acessar relatorio.'
      WHEN 2 THEN 'Funcionario reporta que opcao de menu sumiu.'
      WHEN 3 THEN 'Permissao negada ao tentar editar caso.'
      WHEN 4 THEN 'Acesso ao painel administrativo bloqueado.'
      WHEN 5 THEN 'Usuario nao consegue visualizar analytics. Permissao ausente.'
      WHEN 6 THEN 'Erro de autorizacao ao acessar endpoint da API.'
      ELSE 'Relatorio de gerencia inacessivel para gestor.'
    END
    WHEN n <= 285 THEN CASE ((n - 250) % 8)
      WHEN 0 THEN 'Consulta de relatorio demora mais de 2 minutos.'
      WHEN 1 THEN 'Sistema lento durante geracao de fechamento mensal.'
      WHEN 2 THEN 'Query de busca de pedidos retornando lentamente.'
      WHEN 3 THEN 'Performance degradada no modulo financeiro.'
      WHEN 4 THEN 'Relatorio de vendas demora para carregar.'
      WHEN 5 THEN 'Lentidao generalizada. Processos SQL consumindo CPU.'
      WHEN 6 THEN 'Aplicacao travando durante consultas complexas.'
      ELSE 'Full table scan detectado em tabela de movimentos.'
    END
    WHEN n <= 315 THEN CASE ((n - 285) % 8)
      WHEN 0 THEN 'App mobile nao sincroniza dados com o servidor.'
      WHEN 1 THEN 'Vendedor de campo relata que pedidos nao atualizam.'
      WHEN 2 THEN 'Sincronizacao mobile falha com erro de rede.'
      WHEN 3 THEN 'Dados offline nao fazem upload quando voltando online.'
      WHEN 4 THEN 'App apresenta dados desatualizados apos sincronizacao.'
      WHEN 5 THEN 'Erro de sincronizacao: conflito de dados.'
      WHEN 6 THEN 'Force de vendas nao consegue enviar pedidos pelo app.'
      ELSE 'Sincronizacao em background consome muita bateria.'
    END
    WHEN n <= 345 THEN CASE ((n - 315) % 8)
      WHEN 0 THEN 'Conector SAP retorna erro durante chamada RFC.'
      WHEN 1 THEN 'Pedidos nao exportam para o SAP. Integracao falhou.'
      WHEN 2 THEN 'Erro de comunicacao com SAP: connection refused.'
      WHEN 3 THEN 'Sincronizacao de estoque com SAP interrompida.'
      WHEN 4 THEN 'BAPI do SAP retorna erro inesperado de formato.'
      WHEN 5 THEN 'Fila de mensagens SAP acumulou. Processos travados.'
      WHEN 6 THEN 'Importacao de dados do SAP falhou com timeout.'
      ELSE 'Integracao SAP retorna erro de autenticacao RFC.'
    END
    WHEN n <= 370 THEN CASE ((n - 345) % 8)
      WHEN 0 THEN 'Deploy da versao 2.5 quebrou formulario de pedidos.'
      WHEN 1 THEN 'Apos deploy, botao de confirmacao nao funciona.'
      WHEN 2 THEN 'Regressao: tela de relatorios nao carrega mais.'
      WHEN 3 THEN 'Versao publicada causou erro 500 em multiplas paginas.'
      WHEN 4 THEN 'Deploy de hotfix introduziu bug na tela de clientes.'
      WHEN 5 THEN 'Apos atualizacao, modulo de faturamento parou.'
      WHEN 6 THEN 'Release v2.5.1 causou regressao no fluxo de checkout.'
      ELSE 'Deploy afetou integracao com gateway de pagamento.'
    END
    WHEN n <= 390 THEN CASE ((n - 370) % 7)
      WHEN 0 THEN 'Sistema nao resolve nome do servidor de banco.'
      WHEN 1 THEN 'DNS primario inacessivel. Servicos nao conectam.'
      WHEN 2 THEN 'Erro de resolucao DNS intermitente.'
      WHEN 3 THEN 'Aplicacao nao encontra endpoint da API externa.'
      WHEN 4 THEN 'Nslookup retorna IP antigo. DNS desatualizado.'
      WHEN 5 THEN 'Timeout na resolucao de hostname do servidor.'
      ELSE 'Servico de DNS retornando NXDOMAIN para hostname valido.'
    END
    WHEN n <= 405 THEN CASE ((n - 390) % 6)
      WHEN 0 THEN 'Certificado TLS do servidor expirou.'
      WHEN 1 THEN 'Browser avisa que conexao nao e segura. Cert vencido.'
      WHEN 2 THEN 'Handshake TLS falha. Cadeia de certificados invalida.'
      WHEN 3 THEN 'Certificado mTLS expirou. Servicos nao comunicam.'
      WHEN 4 THEN 'Certificado CA intermediaria venceu.'
      ELSE 'Integracao falha com erro de certificado expirado.'
    END
    WHEN n <= 420 THEN CASE ((n - 405) % 5)
      WHEN 0 THEN 'Deadlock detectado entre duas transacoes concorrentes.'
      WHEN 1 THEN 'Erro de deadlock ao processar pedido de compra.'
      WHEN 2 THEN 'Transacao falhou por deadlock. Operacao revertida.'
      WHEN 3 THEN 'Multiplos deadlocks detectados durante fechamento mensal.'
      ELSE 'Servico de faturamento reporta deadlock recorrente.'
    END
    WHEN n <= 435 THEN CASE ((n - 420) % 5)
      WHEN 0 THEN 'Firewall bloqueando acesso ao servidor de aplicacao.'
      WHEN 1 THEN 'Regra de firewall impedindo comunicacao entre servicos.'
      WHEN 2 THEN 'Acesso externo bloqueado. Regra de seguranca ativa.'
      WHEN 3 THEN 'Integracao caiu por bloqueio de firewall.'
      ELSE 'Novo servidor nao alcanca API. Firewall precisa de regra.'
    END
    WHEN n <= 450 THEN CASE ((n - 435) % 5)
      WHEN 0 THEN 'Fila de processamento acumulou 12000 mensagens.'
      WHEN 1 THEN 'Consumer de fila parou de processar. Backlog crescente.'
      WHEN 2 THEN 'Mensagens de integracao ficam pendentes na fila.'
      WHEN 3 THEN 'Fila RabbitMQ com lag de processamento.'
      ELSE 'Evento de notificacao nao entregue. Fila travada.'
    END
    ELSE CASE ((n - 450) % 10)
      WHEN 0 THEN 'Erro inesperado no modulo de importacao de arquivos.'
      WHEN 1 THEN 'Lentidao geral no sistema durante horario de pico.'
      WHEN 2 THEN 'Servico de relatorios caiu sem erro aparente.'
      WHEN 3 THEN 'Integracao com gateway de pagamento instavel.'
      WHEN 4 THEN 'Erro intermitente na tela de cadastro de clientes.'
      WHEN 5 THEN 'Relatorio gerencial nao gera PDF. Timeout no servidor.'
      WHEN 6 THEN 'Importacao de arquivo CSV falha com erro de encoding.'
      WHEN 7 THEN 'Modulo de busca nao retorna resultados corretos.'
      WHEN 8 THEN 'Erro 500 interno ao acessar detalhes do caso.'
      ELSE 'Lentidao no carregamento do dashboard.'
    END
  END AS original_report,
  -- normalized_summary: versoes curtas dos relatos
  CASE
    WHEN n <= 60 THEN 'Falha de autenticacao - credencial bloqueada ou invalida'
    WHEN n <= 115 THEN 'Timeout na API - gateway nao responde no prazo'
    WHEN n <= 165 THEN 'Pool de conexoes MySQL esgotado'
    WHEN n <= 210 THEN 'Erro HTTP 502 Bad Gateway na API'
    WHEN n <= 250 THEN 'Erro de permissao - acesso negado ao modulo'
    WHEN n <= 285 THEN 'Query MySQL com performance degradada'
    WHEN n <= 315 THEN 'Falha de sincronizacao dados mobile-servidor'
    WHEN n <= 345 THEN 'Erro de integracao com SAP via RFC'
    WHEN n <= 370 THEN 'Regressao apos deploy de versao'
    WHEN n <= 390 THEN 'Falha de resolucao DNS'
    WHEN n <= 405 THEN 'Certificado TLS/CA expirado'
    WHEN n <= 420 THEN 'Deadlock em transacao concorrente'
    WHEN n <= 435 THEN 'Bloqueio de firewall impede acesso'
    WHEN n <= 450 THEN 'Backlog de fila de processamento'
    ELSE 'Incidente diverso - investigacao necessaria'
  END AS normalized_summary,
  -- severity: ~7% Critical, ~28% High, ~50% Medium, ~15% Low (mod 100)
  CASE
    WHEN (n % 100) < 7 THEN 'Critical'
    WHEN (n % 100) < 35 THEN 'High'
    WHEN (n % 100) < 85 THEN 'Medium'
    ELSE 'Low'
  END AS severity,
  -- impact_level
  CASE (n % 20)
    WHEN 0 THEN 'Severe' WHEN 1 THEN 'Severe' WHEN 2 THEN 'Severe'
    WHEN 3 THEN 'Significant' WHEN 4 THEN 'Significant' WHEN 5 THEN 'Significant'
    WHEN 6 THEN 'Significant' WHEN 7 THEN 'Moderate' WHEN 8 THEN 'Moderate'
    WHEN 9 THEN 'Moderate' WHEN 10 THEN 'Moderate' WHEN 11 THEN 'Moderate'
    WHEN 12 THEN 'Moderate' WHEN 13 THEN 'Minor' WHEN 14 THEN 'Minor'
    WHEN 15 THEN 'Minor' WHEN 16 THEN 'Minor'
    ELSE 'None'
  END AS impact_level,
  -- status: ~82% Resolved, ~13% Open, ~5% Reopened (mod 100)
  CASE
    WHEN (n % 100) >= 95 THEN 'Reopened'
    WHEN (n % 100) >= 82 THEN 'Open'
    ELSE 'Resolved'
  END AS status,
  -- current_owner_user_id: round-robin entre 6 usuarios
  uo.id AS current_owner_user_id,
  -- current_department_id: round-robin entre 8 departamentos
  d.id AS current_department_id,
  -- root_cause_status
  CASE
    WHEN (n % 100) >= 82 AND (n % 100) <= 94 THEN 'NotEvaluated'
    WHEN n % 4 = 0 THEN 'NotConfirmed'
    ELSE 'Confirmed'
  END AS root_cause_status,
  -- opened_at: distribuido sobre 12 meses (out/2025 - set/2026)
  -- Mes 1(11 atras):32, Mes 2(10):37, Mes 3(9):42, Mes 4(8):38,
  -- Mes 5(7):45, Mes 6(6):40, Mes 7(5):48, Mes 8(4):43,
  -- Mes 9(3):35, Mes 10(2):30, Mes 11(1):38, Mes 12(atual):72
  DATE_ADD(
    DATE_ADD('2025-10-01', INTERVAL (
      CASE
        WHEN n <= 32  THEN 0
        WHEN n <= 69  THEN 1
        WHEN n <= 111 THEN 2
        WHEN n <= 149 THEN 3
        WHEN n <= 194 THEN 4
        WHEN n <= 234 THEN 5
        WHEN n <= 282 THEN 6
        WHEN n <= 325 THEN 7
        WHEN n <= 360 THEN 8
        WHEN n <= 390 THEN 9
        WHEN n <= 428 THEN 10
        ELSE 11
      END
    ) MONTH),
    INTERVAL ((n - 1) % 28) DAY
  ) AS opened_at,
  -- first_response_at: 15 a 240 min apos opened_at
  DATE_ADD(
    DATE_ADD('2025-10-01', INTERVAL (
      CASE
        WHEN n <= 32  THEN 0 WHEN n <= 69  THEN 1 WHEN n <= 111 THEN 2
        WHEN n <= 149 THEN 3 WHEN n <= 194 THEN 4 WHEN n <= 234 THEN 5
        WHEN n <= 282 THEN 6 WHEN n <= 325 THEN 7 WHEN n <= 360 THEN 8
        WHEN n <= 390 THEN 9 WHEN n <= 428 THEN 10 ELSE 11
      END
    ) MONTH),
    INTERVAL (((n - 1) % 28) * 1440 + 15 + (n * 7) % 225) MINUTE
  ) AS first_response_at,
  -- resolved_at: Resolved e Reopened tem resolved_at; Open nao tem
  CASE
    WHEN ((n % 100) >= 82 AND (n % 100) <= 94) THEN NULL
    ELSE DATE_ADD(
      DATE_ADD('2025-10-01', INTERVAL (
        CASE
          WHEN n <= 32  THEN 0 WHEN n <= 69  THEN 1 WHEN n <= 111 THEN 2
          WHEN n <= 149 THEN 3 WHEN n <= 194 THEN 4 WHEN n <= 234 THEN 5
          WHEN n <= 282 THEN 6 WHEN n <= 325 THEN 7 WHEN n <= 360 THEN 8
          WHEN n <= 390 THEN 9 WHEN n <= 428 THEN 10 ELSE 11
        END
      ) MONTH),
      INTERVAL (((n - 1) % 28) * 1440 + 15 + (n * 7) % 225 + 60 + (n * 13) % 1440) MINUTE
    )
  END AS resolved_at,
  -- closed_at: apenas Resolved tem closed_at; Reopened e Open nao tem
  CASE
    WHEN ((n % 100) >= 82) THEN NULL
    ELSE DATE_ADD(
      DATE_ADD('2025-10-01', INTERVAL (
        CASE
          WHEN n <= 32  THEN 0 WHEN n <= 69  THEN 1 WHEN n <= 111 THEN 2
          WHEN n <= 149 THEN 3 WHEN n <= 194 THEN 4 WHEN n <= 234 THEN 5
          WHEN n <= 282 THEN 6 WHEN n <= 325 THEN 7 WHEN n <= 360 THEN 8
          WHEN n <= 390 THEN 9 WHEN n <= 428 THEN 10 ELSE 11
        END
      ) MONTH),
      INTERVAL (((n - 1) % 28) * 1440 + 15 + (n * 7) % 225 + 60 + (n * 13) % 1440 + 5) MINUTE
    )
  END AS closed_at,
  NOW() AS created_at,
  uo.id AS created_by,
  NOW() AS updated_at,
  uo.id AS updated_by,
  1 AS row_version
FROM nums
JOIN clients_r cl ON cl.rn = 1 + ((n - 1) % 11)
JOIN products_r pr ON pr.rn = 1 + ((n - 1) % 4)
JOIN versions_r vv ON vv.rn = 1 + ((n - 1) % 4)
JOIN envs_r e ON e.rn = CASE WHEN n % 10 < 7 THEN 1 WHEN n % 10 < 9 THEN 2 ELSE 3 END
JOIN users_r uo ON uo.rn = 1 + ((n - 1) % 6)
JOIN depts_r d ON d.rn = 1 + ((n - 1) % 8);
-- ============================================================
-- SECAO 9: CASE_ITERATIONS (500 iteracoes iniciais)
-- Todo caso tem Iteracao 1 com status igual ao do caso
-- ============================================================
INSERT INTO case_iterations (case_id, sequence_number, opened_at, opened_by, reason, closed_at, status)
SELECT
  c.id,
  1,
  c.opened_at,
  COALESCE(c.created_by, (SELECT id FROM users ORDER BY id ASC LIMIT 1)),
  'Abertura inicial do caso (sintetico)',
  c.resolved_at,
  'Resolved'
FROM cases c
WHERE c.external_reference LIKE 'SYN-DEMO-2026%'
  AND c.status IN ('Resolved', 'Reopened');

-- Iteracao 1 para casos OPEN: status Open, sem closed_at
INSERT INTO case_iterations (case_id, sequence_number, opened_at, opened_by, reason, closed_at, status)
SELECT
  c.id,
  1,
  c.opened_at,
  COALESCE(c.created_by, (SELECT id FROM users ORDER BY id ASC LIMIT 1)),
  'Abertura inicial do caso (sintetico)',
  NULL,
  'Open'
FROM cases c
WHERE c.external_reference LIKE 'SYN-DEMO-2026%'
  AND c.status = 'Open';

-- Iteracao 2 para casos REABERTOS: criar iteracao resolvida + nova aberta
INSERT INTO case_iterations (case_id, sequence_number, opened_at, opened_by, reason, closed_at, status)
SELECT
  c.id, 2,
  DATE_ADD(c.resolved_at, INTERVAL 1 HOUR),
  COALESCE(c.created_by, (SELECT id FROM users ORDER BY id ASC LIMIT 1)),
  'Iteracao de reabertura (sintetico)',
  NULL,
  'Open'
FROM cases c
WHERE c.external_reference LIKE 'SYN-DEMO-2026%'
  AND c.status = 'Reopened';
-- ============================================================
-- SECAO 10: CASE_SYMPTOMS (~1200 registros, 2-3 por caso)
-- ============================================================
-- Cada caso sintetico recebe 2 sintomas baseados na familia
INSERT INTO case_symptoms (case_id, symptom_code, symptom_text, source, confirmed)
SELECT c.id, NULL,
  CASE
    WHEN c.original_report LIKE '%credencial%' OR c.original_report LIKE '%login%'
      OR c.original_report LIKE '%bloqueada%' OR c.original_report LIKE '%autenticacao%'
      OR c.original_report LIKE '%token%' OR c.original_report LIKE '%senha%'
      THEN 'falha de login'
    WHEN c.original_report LIKE '%timeout%' OR c.original_report LIKE '%504%'
      OR c.original_report LIKE '%tempo%' OR c.original_report LIKE '%carregando%'
      THEN 'timeout'
    WHEN c.original_report LIKE '%pool%' OR c.original_report LIKE '%connections%'
      OR c.original_report LIKE '%esgotado%' OR c.original_report LIKE '%saturado%'
      THEN 'too many connections'
    WHEN c.original_report LIKE '%502%' OR c.original_report LIKE '%Bad Gateway%'
      OR c.original_report LIKE '%upstream%' OR c.original_report LIKE '%indisponivel%'
      THEN 'HTTP 502'
    WHEN c.original_report LIKE '%permissao%' OR c.original_report LIKE '%403%'
      OR c.original_report LIKE '%Forbidden%' OR c.original_report LIKE '%bloqueado%'
      THEN 'acesso negado'
    WHEN c.original_report LIKE '%lentidao%' OR c.original_report LIKE '%lento%'
      OR c.original_report LIKE '%CPU%' OR c.original_report LIKE '%full table%'
      OR c.original_report LIKE '%Consulta%minutos%'
      THEN 'consulta lenta'
    WHEN c.original_report LIKE '%mobile%' OR c.original_report LIKE '%sincroni%'
      OR c.original_report LIKE '%offline%' OR c.original_report LIKE '%app%'
      THEN 'sincronizacao falhou'
    WHEN c.original_report LIKE '%SAP%' OR c.original_report LIKE '%RFC%'
      OR c.original_report LIKE '%BAPI%'
      THEN 'erro SAP'
    WHEN c.original_report LIKE '%deploy%' OR c.original_report LIKE '%regressao%'
      OR c.original_report LIKE '%versao%' OR c.original_report LIKE '%atualizacao%'
      THEN 'deploy quebrou'
    WHEN c.original_report LIKE '%DNS%' OR c.original_report LIKE '%nslookup%'
      OR c.original_report LIKE '%NXDOMAIN%' OR c.original_report LIKE '%hostname%'
      THEN 'DNS falhou'
    WHEN c.original_report LIKE '%certificado%' OR c.original_report LIKE '%TLS%'
      OR c.original_report LIKE '%handshake%' OR c.original_report LIKE '%CA%'
      THEN 'certificado expirado'
    WHEN c.original_report LIKE '%deadlock%' OR c.original_report LIKE '%lock%'
      OR c.original_report LIKE '%revertida%'
      THEN 'deadlock'
    WHEN c.original_report LIKE '%firewall%' OR c.original_report LIKE '%IP%'
      OR c.original_report LIKE '%regra%'
      THEN 'acesso bloqueado'
    WHEN c.original_report LIKE '%fila%' OR c.original_report LIKE '%backlog%'
      OR c.original_report LIKE '%consumer%' OR c.original_report LIKE '%mensagem%'
      THEN 'fila cheia'
    ELSE 'erro inesperado'
  END,
  'Human', 1
FROM cases c
WHERE c.external_reference LIKE 'SYN-DEMO-2026%';

-- Segundo sintoma por caso
INSERT INTO case_symptoms (case_id, symptom_code, symptom_text, source, confirmed)
SELECT c.id, NULL,
  CASE
    WHEN c.original_report LIKE '%credencial%' OR c.original_report LIKE '%login%'
      OR c.original_report LIKE '%bloqueada%' OR c.original_report LIKE '%autenticacao%'
      OR c.original_report LIKE '%token%' OR c.original_report LIKE '%senha%'
      THEN 'erro 401'
    WHEN c.original_report LIKE '%timeout%' OR c.original_report LIKE '%504%'
      OR c.original_report LIKE '%tempo%' OR c.original_report LIKE '%carregando%'
      THEN 'HTTP 504'
    WHEN c.original_report LIKE '%pool%' OR c.original_report LIKE '%connections%'
      OR c.original_report LIKE '%esgotado%' OR c.original_report LIKE '%saturado%'
      THEN 'conexao recusada'
    WHEN c.original_report LIKE '%502%' OR c.original_report LIKE '%Bad Gateway%'
      OR c.original_report LIKE '%upstream%' OR c.original_report LIKE '%indisponivel%'
      THEN 'servico indisponivel'
    WHEN c.original_report LIKE '%permissao%' OR c.original_report LIKE '%403%'
      OR c.original_report LIKE '%Forbidden%' OR c.original_report LIKE '%bloqueado%'
      THEN 'erro 403'
    WHEN c.original_report LIKE '%lentidao%' OR c.original_report LIKE '%lento%'
      OR c.original_report LIKE '%CPU%' OR c.original_report LIKE '%full table%'
      OR c.original_report LIKE '%Consulta%minutos%'
      THEN 'CPU alta'
    WHEN c.original_report LIKE '%mobile%' OR c.original_report LIKE '%sincroni%'
      OR c.original_report LIKE '%offline%' OR c.original_report LIKE '%app%'
      THEN 'dados desatualizados'
    WHEN c.original_report LIKE '%SAP%' OR c.original_report LIKE '%RFC%'
      OR c.original_report LIKE '%BAPI%'
      THEN 'conexao SAP'
    WHEN c.original_report LIKE '%deploy%' OR c.original_report LIKE '%regressao%'
      OR c.original_report LIKE '%versao%' OR c.original_report LIKE '%atualizacao%'
      THEN 'erro 500'
    WHEN c.original_report LIKE '%DNS%' OR c.original_report LIKE '%nslookup%'
      OR c.original_report LIKE '%NXDOMAIN%' OR c.original_report LIKE '%hostname%'
      THEN 'timeout DNS'
    WHEN c.original_report LIKE '%certificado%' OR c.original_report LIKE '%TLS%'
      OR c.original_report LIKE '%handshake%' OR c.original_report LIKE '%CA%'
      THEN 'conexao nao segura'
    WHEN c.original_report LIKE '%deadlock%' OR c.original_report LIKE '%lock%'
      OR c.original_report LIKE '%revertida%'
      THEN 'lock wait timeout'
    WHEN c.original_report LIKE '%firewall%' OR c.original_report LIKE '%IP%'
      OR c.original_report LIKE '%regra%'
      THEN 'conexao recusada'
    WHEN c.original_report LIKE '%fila%' OR c.original_report LIKE '%backlog%'
      OR c.original_report LIKE '%consumer%' OR c.original_report LIKE '%mensagem%'
      THEN 'mensagem pendente'
    ELSE 'lentidao'
  END,
  'Human', 1
FROM cases c
WHERE c.external_reference LIKE 'SYN-DEMO-2026%';
-- ============================================================
-- SECAO 11: CASE_COMPONENTS (~600 registros, 1-2 por caso)
-- ============================================================
INSERT INTO case_components (case_id, component_id, relation_type, confidence_label)
SELECT c.id,
  (SELECT comp.id FROM components comp WHERE comp.code = CASE
    WHEN c.original_report LIKE '%credencial%' OR c.original_report LIKE '%login%'
      OR c.original_report LIKE '%autenticacao%' OR c.original_report LIKE '%token%'
      OR c.original_report LIKE '%senha%' OR c.original_report LIKE '%bloqueada%'
      THEN 'AUTH-SERVICE'
    WHEN c.original_report LIKE '%timeout%' OR c.original_report LIKE '%504%'
      OR c.original_report LIKE '%tempo%' OR c.original_report LIKE '%carregando%'
      THEN 'API-GATEWAY'
    WHEN c.original_report LIKE '%pool%' OR c.original_report LIKE '%connections%'
      OR c.original_report LIKE '%esgotado%'
      THEN 'DB-POOL'
    WHEN c.original_report LIKE '%502%' OR c.original_report LIKE '%Bad Gateway%'
      OR c.original_report LIKE '%upstream%'
      THEN 'API-GATEWAY'
    WHEN c.original_report LIKE '%permissao%' OR c.original_report LIKE '%403%'
      OR c.original_report LIKE '%Forbidden%'
      THEN 'MOD-AUTH-WEB'
    WHEN c.original_report LIKE '%lentidao%' OR c.original_report LIKE '%CPU%'
      OR c.original_report LIKE '%full table%'
      THEN 'DB-POOL'
    WHEN c.original_report LIKE '%mobile%' OR c.original_report LIKE '%sincroni%'
      OR c.original_report LIKE '%app%'
      THEN 'SRV-SYNC'
    WHEN c.original_report LIKE '%SAP%' OR c.original_report LIKE '%RFC%'
      OR c.original_report LIKE '%BAPI%'
      THEN 'SAP-CONNECTOR'
    WHEN c.original_report LIKE '%deploy%' OR c.original_report LIKE '%regressao%'
      THEN 'MOD-AUTH-WEB'
    WHEN c.original_report LIKE '%DNS%'
      THEN 'DNS-RESOLVER'
    WHEN c.original_report LIKE '%certificado%' OR c.original_report LIKE '%TLS%'
      THEN 'CERT-MGR'
    WHEN c.original_report LIKE '%deadlock%' OR c.original_report LIKE '%lock%'
      THEN 'DB-POOL'
    WHEN c.original_report LIKE '%firewall%'
      THEN 'FW-RULES'
    WHEN c.original_report LIKE '%fila%' OR c.original_report LIKE '%backlog%'
      THEN 'QUEUE-MGR'
    ELSE 'API-GATEWAY'
  END LIMIT 1),
  'Affected', 'High'
FROM cases c
WHERE c.external_reference LIKE 'SYN-DEMO-2026%';

-- Segundo componente para ~40% dos casos
INSERT INTO case_components (case_id, component_id, relation_type, confidence_label)
SELECT c.id,
  (SELECT comp.id FROM components comp WHERE comp.code = CASE
    WHEN c.original_report LIKE '%credencial%' OR c.original_report LIKE '%login%'
      THEN 'MOD-AUTH-WEB'
    WHEN c.original_report LIKE '%timeout%' OR c.original_report LIKE '%504%'
      THEN 'DB-POOL'
    WHEN c.original_report LIKE '%pool%'
      THEN 'API-GATEWAY'
    WHEN c.original_report LIKE '%502%'
      THEN 'SRV-SYNC'
    WHEN c.original_report LIKE '%permissao%'
      THEN 'MOD-FIN'
    WHEN c.original_report LIKE '%lentidao%'
      THEN 'MOD-FIN'
    WHEN c.original_report LIKE '%mobile%'
      THEN 'MOD-AUTH-WEB'
    WHEN c.original_report LIKE '%SAP%'
      THEN 'API-GATEWAY'
    WHEN c.original_report LIKE '%deploy%'
      THEN 'MOD-FIN'
    WHEN c.original_report LIKE '%DNS%'
      THEN 'AUTH-SERVICE'
    WHEN c.original_report LIKE '%certificado%'
      THEN 'AUTH-SERVICE'
    WHEN c.original_report LIKE '%deadlock%'
      THEN 'MOD-FIN'
    WHEN c.original_report LIKE '%firewall%'
      THEN 'DNS-RESOLVER'
    WHEN c.original_report LIKE '%fila%'
      THEN 'SRV-SYNC'
    ELSE 'FILE-IMP'
  END LIMIT 1),
  'Affected', 'Medium'
FROM cases c
WHERE c.external_reference LIKE 'SYN-DEMO-2026%'
  AND c.id % 5 IN (0, 1);
-- ============================================================
-- SECAO 12: CASE_EVIDENCES (~700 registros)
-- ============================================================
-- Evidencias para ~70% dos casos sinteticos
INSERT INTO case_evidences (case_id, case_iteration_id, diagnostic_step_id, evidence_type, description, created_by, created_at)
SELECT
  c.id,
  (SELECT ci.id FROM case_iterations ci WHERE ci.case_id = c.id AND ci.sequence_number = 1 LIMIT 1),
  (SELECT ds2.id FROM diagnostic_sessions ds2
   JOIN diagnostic_steps dst ON dst.diagnostic_session_id = ds2.id
   WHERE ds2.case_id = c.id AND dst.sequence_no = 1 LIMIT 1),
  CASE (c.id % 5)
    WHEN 0 THEN 'Log'
    WHEN 1 THEN 'ErrorText'
    WHEN 2 THEN 'Screenshot'
    WHEN 3 THEN 'Link'
    ELSE 'Other'
  END,
  CASE
    WHEN c.original_report LIKE '%credencial%' OR c.original_report LIKE '%login%'
      THEN CONCAT('Log de autenticacao registra erro ', COALESCE(c.error_code, 'E-0001'), ' as ', DATE_FORMAT(c.opened_at, '%H:%i'), '.')
    WHEN c.original_report LIKE '%timeout%' OR c.original_report LIKE '%504%'
      THEN CONCAT('Gateway retornou timeout as ', DATE_FORMAT(c.opened_at, '%H:%i'), '. Response time > 30s.')
    WHEN c.original_report LIKE '%pool%' OR c.original_report LIKE '%connections%'
      THEN CONCAT('Pool de conexoes atingiu max_connections as ', DATE_FORMAT(c.opened_at, '%H:%i'), '.')
    WHEN c.original_report LIKE '%502%' OR c.original_report LIKE '%Bad Gateway%'
      THEN CONCAT('Nginx registrou 502 upstream timeout as ', DATE_FORMAT(c.opened_at, '%H:%i'), '.')
    WHEN c.original_report LIKE '%permissao%'
      THEN CONCAT('Endpoint retornou 403 Forbidden. Usuario sem permissao necessaria.')
    WHEN c.original_report LIKE '%lentidao%' OR c.original_report LIKE '%CPU%'
      THEN CONCAT('Query identificada com tempo de execucao > 120s. Full table scan detectado.')
    WHEN c.original_report LIKE '%mobile%'
      THEN CONCAT('Logs do app mostram erro de sincronizacao HTTP 408.')
    WHEN c.original_report LIKE '%SAP%'
      THEN CONCAT('RFC callback retornou status ', COALESCE(c.error_code, 'RFC-FAIL'), '. Mensagem: ', COALESCE(c.error_message, 'sem detalhes'), '.')
    WHEN c.original_report LIKE '%deploy%'
      THEN CONCAT('Release v2.5 deploy as ', DATE_FORMAT(c.opened_at, '%H:%i'), '. Regressao detectada.')
    WHEN c.original_report LIKE '%DNS%'
      THEN CONCAT('nslookup retornou NXDOMAIN para hostname do servico.')
    WHEN c.original_report LIKE '%certificado%'
      THEN CONCAT('Certificado expirou em ', DATE_FORMAT(DATE_ADD(c.opened_at, INTERVAL -30 DAY), '%d/%m/%Y'), '.')
    WHEN c.original_report LIKE '%deadlock%'
      THEN CONCAT('Error log registra deadlock entre transactions PID ', c.id % 1000, ' e PID ', (c.id * 7) % 1000, '.')
    WHEN c.original_report LIKE '%firewall%'
      THEN CONCAT('Packet trace mostra SYN rejeitado na porta 3306. IP bloqueado.')
    WHEN c.original_report LIKE '%fila%'
      THEN CONCAT('Fila acumulou ', (c.id * 247) % 50000, ' mensagens pendentes.')
    ELSE CONCAT('Registro de erro capturado no servico as ', DATE_FORMAT(c.opened_at, '%H:%i'), '.')
  END,
  COALESCE(c.created_by, (SELECT id FROM users ORDER BY id ASC LIMIT 1)),
  c.created_at
FROM cases c
WHERE c.external_reference LIKE 'SYN-DEMO-2026%'
  AND c.id % 10 != 9;

-- Segunda evidencia para ~40% dos casos
INSERT INTO case_evidences (case_id, case_iteration_id, diagnostic_step_id, evidence_type, description, created_by, created_at)
SELECT
  c.id,
  (SELECT ci.id FROM case_iterations ci WHERE ci.case_id = c.id AND ci.sequence_number = 1 LIMIT 1),
  (SELECT ds2.id FROM diagnostic_sessions ds2
   JOIN diagnostic_steps dst ON dst.diagnostic_session_id = ds2.id
   WHERE ds2.case_id = c.id AND dst.sequence_no = 2 LIMIT 1),
  'ErrorText',
  CASE
    WHEN c.original_report LIKE '%credencial%' THEN 'Conta consta bloqueada no servico de autenticacao.'
    WHEN c.original_report LIKE '%timeout%' THEN 'Health check do gateway retornou ok apos timeout.'
    WHEN c.original_report LIKE '%pool%' THEN 'SHOW PROCESSLIST revelou 100 conexoes simultaneas.'
    WHEN c.original_report LIKE '%502%' THEN 'Upstream retornou 504 que foi convertido para 502.'
    WHEN c.original_report LIKE '%permissao%' THEN 'RBAC do usuario nao inclui permissao necessaria.'
    WHEN c.original_report LIKE '%lentidao%' THEN 'EXPLAIN mostra table scan na tabela movements.'
    WHEN c.original_report LIKE '%mobile%' THEN 'Endpoint de sync retornou 408 Request Timeout.'
    WHEN c.original_report LIKE '%SAP%' THEN 'SAP RFC retornou RC=8 SY-SUBRC=0.'
    WHEN c.original_report LIKE '%deploy%' THEN 'Git diff mostra alteracao no componente de formulario.'
    WHEN c.original_report LIKE '%DNS%' THEN 'nslookup retornou endereco IP antigo 10.0.1.50.'
    WHEN c.original_report LIKE '%certificado%' THEN 'openssl s_client mostra cert expired.'
    WHEN c.original_report LIKE '%deadlock%' THEN 'InnoDB deadlock log: wait for graph found.'
    WHEN c.original_report LIKE '%firewall%' THEN 'iptables -L mostra REJECT na cadeia INPUT.'
    WHEN c.original_report LIKE '%fila%' THEN 'RabbitMQ management mostra queue length > 10000.'
    ELSE 'Stack trace registra NullReferenceException no modulo afetado.'
  END,
  COALESCE(c.created_by, (SELECT id FROM users ORDER BY id ASC LIMIT 1)),
  c.created_at
FROM cases c
WHERE c.external_reference LIKE 'SYN-DEMO-2026%'
  AND c.id % 5 IN (0, 1, 2);
-- ============================================================
-- SECAO 13: CASE_HYPOTHESES (~1200 registros)
-- ============================================================
-- H1 para todos os casos sinteticos
INSERT INTO case_hypotheses (case_id, case_iteration_id, title, description, status, source_type, created_at, created_by, updated_at)
SELECT c.id,
  (SELECT ci.id FROM case_iterations ci WHERE ci.case_id = c.id AND ci.sequence_number = 1 LIMIT 1),
  CASE
    WHEN c.original_report LIKE '%credencial%' THEN 'H1: Credencial bloqueada por tentativas'
    WHEN c.original_report LIKE '%timeout%' THEN 'H1: Indisponibilidade transitoria da API'
    WHEN c.original_report LIKE '%pool%' THEN 'H1: Pool de conexoes dimensionado inadequadamente'
    WHEN c.original_report LIKE '%502%' THEN 'H1: Backend retornou erro para o balanceador'
    WHEN c.original_report LIKE '%permissao%' THEN 'H1: Permissao nao concedida ao papel do usuario'
    WHEN c.original_report LIKE '%lentidao%' THEN 'H1: Query sem indice causando full scan'
    WHEN c.original_report LIKE '%mobile%' THEN 'H1: Timeout na API de sincronizacao'
    WHEN c.original_report LIKE '%SAP%' THEN 'H1: Conector SAP com credenciais incorretas'
    WHEN c.original_report LIKE '%deploy%' THEN 'H1: Regressao na versao publicada'
    WHEN c.original_report LIKE '%DNS%' THEN 'H1: DNS primario inacessivel'
    WHEN c.original_report LIKE '%certificado%' THEN 'H1: Certificado TLS expirado'
    WHEN c.original_report LIKE '%deadlock%' THEN 'H1: Ordem incorreta de acesso a tabelas'
    WHEN c.original_report LIKE '%firewall%' THEN 'H1: Regra de firewall bloqueando IP'
    WHEN c.original_report LIKE '%fila%' THEN 'H1: Consumer de fila com problema'
    ELSE 'H1: Componente central com falha transitaria'
  END,
  'Hipotese primaria de investigacao do caso.',
  'Supported', 'Human', c.opened_at,
  COALESCE(c.created_by, (SELECT id FROM users ORDER BY id ASC LIMIT 1)),
  c.opened_at
FROM cases c
WHERE c.external_reference LIKE 'SYN-DEMO-2026%';
-- H2 para ~60% dos casos
INSERT INTO case_hypotheses (case_id, case_iteration_id, title, description, status, source_type, created_at, created_by, updated_at)
SELECT c.id,
  (SELECT ci.id FROM case_iterations ci WHERE ci.case_id = c.id AND ci.sequence_number = 1 LIMIT 1),
  CASE
    WHEN c.original_report LIKE '%credencial%' THEN 'H2: Servico IAM indisponivel'
    WHEN c.original_report LIKE '%timeout%' THEN 'H2: Pool de conexoes esgotado'
    WHEN c.original_report LIKE '%pool%' THEN 'H2: Leak de conexoes no servico'
    WHEN c.original_report LIKE '%502%' THEN 'H2: Servico backend fora do ar'
    WHEN c.original_report LIKE '%permissao%' THEN 'H2: Permissao removida por engano'
    WHEN c.original_report LIKE '%lentidao%' THEN 'H2: Volume de dados acima do esperado'
    WHEN c.original_report LIKE '%mobile%' THEN 'H2: Rede instavel no campo'
    WHEN c.original_report LIKE '%SAP%' THEN 'H2: Ambiente SAP indisponivel'
    WHEN c.original_report LIKE '%deploy%' THEN 'H2: Dependencia quebrada em biblioteca'
    WHEN c.original_report LIKE '%DNS%' THEN 'H2: Cache DNS desatualizado'
    WHEN c.original_report LIKE '%certificado%' THEN 'H2: CA intermediaria invalida'
    WHEN c.original_report LIKE '%deadlock%' THEN 'H2: Transacao longa bloqueando recursos'
    WHEN c.original_report LIKE '%firewall%' THEN 'H2: Migracao de IP nao atualizada'
    WHEN c.original_report LIKE '%fila%' THEN 'H2: Mensagem malformada travando consumer'
    ELSE 'H2: Configuracao incorreta de timeout'
  END,
  'Hipotese secundaria levantada durante investigacao.',
  CASE WHEN c.id % 3 = 0 THEN 'Discarded' ELSE 'Proposed' END,
  'Human', DATE_ADD(c.opened_at, INTERVAL 2 HOUR),
  COALESCE(c.created_by, (SELECT id FROM users ORDER BY id ASC LIMIT 1)),
  DATE_ADD(c.opened_at, INTERVAL 2 HOUR)
FROM cases c
WHERE c.external_reference LIKE 'SYN-DEMO-2026%' AND c.id % 5 != 4;
-- H3 para ~30% dos casos
INSERT INTO case_hypotheses (case_id, case_iteration_id, title, description, status, source_type, created_at, created_by, updated_at)
SELECT c.id,
  (SELECT ci.id FROM case_iterations ci WHERE ci.case_id = c.id AND ci.sequence_number = 1 LIMIT 1),
  CASE
    WHEN c.original_report LIKE '%credencial%' THEN 'H3: Falha de DNS na resolucao do IAM'
    WHEN c.original_report LIKE '%timeout%' THEN 'H3: Cadeia de certificados vencida'
    WHEN c.original_report LIKE '%pool%' THEN 'H3: Configuracao max_connections insuficiente'
    WHEN c.original_report LIKE '%502%' THEN 'H3: Certificado mTLS expirado'
    WHEN c.original_report LIKE '%permissao%' THEN 'H2: Cache de permissoes desatualizado'
    WHEN c.original_report LIKE '%lentidao%' THEN 'H3: Indice corrompido na tabela'
    WHEN c.original_report LIKE '%mobile%' THEN 'H3: Versao do app incompativel com API'
    WHEN c.original_report LIKE '%SAP%' THEN 'H3: Firewall bloqueando conexao RFC'
    WHEN c.original_report LIKE '%deploy%' THEN 'H3: Variavel de ambiente nao configurada'
    WHEN c.original_report LIKE '%DNS%' THEN 'H3: Rotas de rede alteradas'
    WHEN c.original_report LIKE '%certificado%' THEN 'H3: Bundle CA incompleto'
    WHEN c.original_report LIKE '%deadlock%' THEN 'H3: Isolation level incorreto'
    WHEN c.original_report LIKE '%firewall%' THEN 'H3: NAT remapeando portas'
    WHEN c.original_report LIKE '%fila%' THEN 'H3: Exchange/p queue configurado incorretamente'
    ELSE 'H3: Falha em dependencia externa'
  END,
  'Hipotese terciaria descartada.',
  'Discarded', 'Human', DATE_ADD(c.opened_at, INTERVAL 4 HOUR),
  COALESCE(c.created_by, (SELECT id FROM users ORDER BY id ASC LIMIT 1)),
  DATE_ADD(c.opened_at, INTERVAL 4 HOUR)
FROM cases c
WHERE c.external_reference LIKE 'SYN-DEMO-2026%' AND c.id % 3 = 0;
-- ============================================================
-- SECAO 13B: CASE_HYPOTHESIS_EVIDENCE (~600 registros)
-- Evidencias ligadas a hipoteses com tipo de relacao
-- ============================================================
-- Evidencias ligadas a H1 (Supported) -> Supports
INSERT INTO case_hypothesis_evidence (evidence_id, hypothesis_id, relation_type, justification, created_by, created_at)
SELECT
  ev.id,
  ch.id,
  'Supports',
  'Evidencia coletada durante investigacao confirma a hipotese primaria.',
  COALESCE(ev.created_by, (SELECT id FROM users ORDER BY id ASC LIMIT 1)),
  ev.created_at
FROM case_evidences ev
JOIN case_hypotheses ch ON ch.case_id = ev.case_id
  AND ch.status = 'Supported'
  AND ch.case_iteration_id = ev.case_iteration_id
WHERE ev.case_id IN (SELECT id FROM cases WHERE external_reference LIKE 'SYN-DEMO-2026%')
  AND ev.id % 3 = 0;

-- Evidencias ligadas a H2 -> Contradicts ou Inconclusive
INSERT INTO case_hypothesis_evidence (evidence_id, hypothesis_id, relation_type, justification, created_by, created_at)
SELECT
  ev.id,
  ch.id,
  CASE WHEN ev.id % 4 = 0 THEN 'Contradicts' ELSE 'Inconclusive' END,
  CASE WHEN ev.id % 4 = 0
    THEN 'Evidencia descarta a hipotese secundaria.'
    ELSE 'Evidencia nao conclusiva para a hipotese secundaria.'
  END,
  COALESCE(ev.created_by, (SELECT id FROM users ORDER BY id ASC LIMIT 1)),
  ev.created_at
FROM case_evidences ev
JOIN case_hypotheses ch ON ch.case_id = ev.case_id
  AND ch.case_iteration_id = ev.case_iteration_id
  AND ch.title LIKE 'H2:%'
WHERE ev.case_id IN (SELECT id FROM cases WHERE external_reference LIKE 'SYN-DEMO-2026%')
  AND ev.id % 4 IN (0, 1);

-- ============================================================
-- SECAO 14: DIAGNOSTIC_SESSIONS (~350 registros)
-- ============================================================
INSERT INTO diagnostic_sessions (case_id, case_iteration_id, status, started_at, started_by, ended_at)
SELECT
  c.id,
  (SELECT ci.id FROM case_iterations ci WHERE ci.case_id = c.id AND ci.sequence_number = 1 LIMIT 1),
  CASE WHEN c.status = 'Open' THEN 'Open' ELSE 'Closed' END,
  DATE_ADD(c.opened_at, INTERVAL 30 MINUTE),
  COALESCE(c.created_by, (SELECT id FROM users ORDER BY id ASC LIMIT 1)),
  CASE WHEN c.status != 'Open' THEN DATE_ADD(c.opened_at, INTERVAL (4 + (c.id % 48)) HOUR) ELSE NULL END
FROM cases c
WHERE c.external_reference LIKE 'SYN-DEMO-2026%' AND c.id % 10 != 8;

-- ============================================================
-- SECAO 15: DIAGNOSTIC_STEPS (~1800 registros, 2-6 por sessao)
-- ============================================================
INSERT INTO diagnostic_steps (diagnostic_session_id, sequence_no, step_type, title, objective, input_evidence_summary, result_summary, outcome, performed_by, performed_at)
SELECT
  ds.id,
  nums.seq,
  'Verification',
  CASE nums.seq
    WHEN 1 THEN 'Verificar saude da API'
    WHEN 2 THEN 'Consultar logs de erro'
    WHEN 3 THEN 'Validar DNS'
    WHEN 4 THEN 'Consultar pool de conexoes'
    WHEN 5 THEN 'Executar teste de autenticacao'
    WHEN 6 THEN 'Verificar certificado'
  END,
  CASE nums.seq
    WHEN 1 THEN 'Confirmar se o servico esta respondendo'
    WHEN 2 THEN 'Identificar padroes de erro nos logs'
    WHEN 3 THEN 'Verificar resolucao de nomes'
    WHEN 4 THEN 'Validar capacidade do pool'
    WHEN 5 THEN 'Testar fluxo de autenticacao'
    WHEN 6 THEN 'Validar cadeia de certificados'
  END,
  'Relato do usuario e evidencias iniciais do caso.',
  CASE nums.seq
    WHEN 1 THEN 'Servico retornou status 200 apos reinicio.'
    WHEN 2 THEN 'Logs revelam erros recorrentes no periodo.'
    WHEN 3 THEN 'DNS resolveu corretamente apos atualizacao.'
    WHEN 4 THEN 'Pool atingiu 95% de utilizacao.'
    WHEN 5 THEN 'Autenticacao funcionou apos desbloqueio.'
    WHEN 6 THEN 'Certificado venceu. Renovacao necessaria.'
  END,
  CASE
    WHEN nums.seq <= 2 THEN 'Worked'
    WHEN nums.seq = 3 THEN 'PartiallyWorked'
    WHEN nums.seq = 4 THEN 'Worked'
    ELSE 'DidNotWork'
  END,
  COALESCE(
    (SELECT c.created_by FROM cases c JOIN diagnostic_sessions ds2 ON ds2.case_id = c.id WHERE ds2.id = ds.id LIMIT 1),
    (SELECT id FROM users ORDER BY id ASC LIMIT 1)
  ),
  DATE_ADD(ds.started_at, INTERVAL (nums.seq * 15) MINUTE)
FROM diagnostic_sessions ds
JOIN cases c ON ds.case_id = c.id
CROSS JOIN (
  SELECT 1 AS seq UNION SELECT 2 UNION SELECT 3 UNION SELECT 4 UNION SELECT 5 UNION SELECT 6
) nums
WHERE c.external_reference LIKE 'SYN-DEMO-2026%'
  AND nums.seq <= 2 + (c.id % 5);
-- ============================================================
-- SECAO 16: CASE_RESOLUTIONS (~390 registros, para Resolved/Reopened)
-- ============================================================
INSERT INTO case_resolutions (
  case_id, case_iteration_id, resolution_summary, validation_summary,
  root_cause_id, root_cause_confirmed, responsible_department_id,
  resolution_type, recurrence_risk, recurrence_notes, preventive_actions,
  effort_minutes, resolved_by, resolved_at
)
SELECT
  c.id,
  (SELECT ci.id FROM case_iterations ci WHERE ci.case_id = c.id AND ci.sequence_number = 1 LIMIT 1),
  CASE
    WHEN c.original_report LIKE '%credencial%' THEN 'Conta desbloqueada e credencial redefinida pelo usuario.'
    WHEN c.original_report LIKE '%timeout%' THEN 'Timeout do gateway ajustado de 30s para 120s.'
    WHEN c.original_report LIKE '%pool%' THEN 'Max connections aumentado e leak de conexao corrigido.'
    WHEN c.original_report LIKE '%502%' THEN 'Servico backend reiniciado e health check validado.'
    WHEN c.original_report LIKE '%permissao%' THEN 'Permissao concedida ao papel do usuario.'
    WHEN c.original_report LIKE '%lentidao%' THEN 'Indice criado na tabela movements. Query otimizada.'
    WHEN c.original_report LIKE '%mobile%' THEN 'Timeout da API de sync aumentado e retry adicionado.'
    WHEN c.original_report LIKE '%SAP%' THEN 'Credenciais RFC atualizadas e conector reiniciado.'
    WHEN c.original_report LIKE '%deploy%' THEN 'Rollback para versao anterior e hotfix aplicado.'
    WHEN c.original_report LIKE '%DNS%' THEN 'Registro DNS atualizado e cache limpo.'
    WHEN c.original_report LIKE '%certificado%' THEN 'Certificado TLS renovado e servicos reiniciados.'
    WHEN c.original_report LIKE '%deadlock%' THEN 'Ordem de acesso a tabelas corrigida.'
    WHEN c.original_report LIKE '%firewall%' THEN 'Regra de firewall adicionada para IP legitimo.'
    WHEN c.original_report LIKE '%fila%' THEN 'Consumer reiniciado e mensagens obsoletas removidas.'
    ELSE 'Causa identificada e correcao aplicada.'
  END,
  'Operacao validada em ambiente de homologacao apos correcao.',
  (SELECT id FROM root_causes WHERE code = CASE
    WHEN c.original_report LIKE '%credencial%' THEN 'RC-AUTH-LOCK'
    WHEN c.original_report LIKE '%timeout%' OR c.original_report LIKE '%502%' THEN 'RC-API-TIMEOUT'
    WHEN c.original_report LIKE '%pool%' OR c.original_report LIKE '%deadlock%' THEN 'RC-DB-POOL-EXH'
    WHEN c.original_report LIKE '%permissao%' THEN 'RC-PERM-ERR'
    WHEN c.original_report LIKE '%lentidao%' THEN 'RC-DB-SLOW'
    WHEN c.original_report LIKE '%mobile%' THEN 'RC-MOB-SYNC'
    WHEN c.original_report LIKE '%SAP%' THEN 'RC-SAP-INT'
    WHEN c.original_report LIKE '%deploy%' THEN 'RC-DEPLOY-REG'
    WHEN c.original_report LIKE '%DNS%' THEN 'RC-DNS-FAIL'
    WHEN c.original_report LIKE '%certificado%' THEN 'RC-CERT-EXPIRED'
    WHEN c.original_report LIKE '%firewall%' THEN 'RC-FW-BLOCK'
    WHEN c.original_report LIKE '%fila%' THEN 'RC-QUEUE-BACK'
    ELSE NULL
  END LIMIT 1),
  CASE WHEN c.id % 4 != 0 THEN 1 ELSE 0 END,
  c.current_department_id,
  CASE WHEN c.id % 5 = 0 THEN 'Workaround' ELSE 'Definitive' END,
  CASE WHEN c.id % 3 = 0 THEN 'High' WHEN c.id % 3 = 1 THEN 'Medium' ELSE 'Low' END,
  'Risco de recorrencia avaliado durante resolucao.',
  CASE WHEN c.id % 4 = 0 THEN 'Monitorar proximos 30 dias.' ELSE NULL END,
  30 + (c.id % 240),
  COALESCE(c.created_by, (SELECT id FROM users ORDER BY id ASC LIMIT 1)),
  c.resolved_at
FROM cases c
WHERE c.external_reference LIKE 'SYN-DEMO-2026%'
  AND c.status IN ('Resolved', 'Reopened');
-- ============================================================
-- SECAO 17: CASE_RELATIONS (~200 registros)
-- Relacoes entre casos do mesmo cluster (Similar, Recurrence, CommonCause)
-- ============================================================
-- Relacoes Similar: casos adjacentes na mesma familia
INSERT INTO case_relations (source_case_id, target_case_id, relation_type, similarity_score, created_by, created_at)
SELECT c1.id, c2.id, 'Similar', 0.7 + (c1.id % 3) * 0.1,
  (SELECT id FROM users ORDER BY id ASC LIMIT 1), NOW()
FROM cases c1
JOIN cases c2 ON c2.id > c1.id
  AND c2.original_report LIKE CONCAT(LEFT(c1.original_report, 15), '%')
  AND c2.id = c1.id + 1
WHERE c1.external_reference LIKE 'SYN-DEMO-2026%'
  AND c2.external_reference LIKE 'SYN-DEMO-2026%'
  AND c1.id % 8 = 0;

-- Relacoes Recurrence: casos da mesma familia com intervalo > 30 dias
INSERT INTO case_relations (source_case_id, target_case_id, relation_type, similarity_score, created_by, created_at)
SELECT c1.id, c2.id, 'Recurrence', 0.8,
  (SELECT id FROM users ORDER BY id ASC LIMIT 1), NOW()
FROM cases c1
JOIN cases c2 ON c2.id > c1.id + 20
  AND c2.original_report LIKE CONCAT(LEFT(c1.original_report, 15), '%')
WHERE c1.external_reference LIKE 'SYN-DEMO-2026%'
  AND c2.external_reference LIKE 'SYN-DEMO-2026%'
  AND c1.id % 12 = 0
  AND c2.id % 12 = 0;

-- Relacoes CommonCause: casos de familias diferentes mas mesmos componentes
INSERT INTO case_relations (source_case_id, target_case_id, relation_type, similarity_score, created_by, created_at)
SELECT DISTINCT c1.id, c2.id, 'CommonCause', 0.6,
  (SELECT id FROM users ORDER BY id ASC LIMIT 1), NOW()
FROM cases c1
JOIN cases c2 ON c2.id = c1.id + 5
JOIN case_components cc1 ON cc1.case_id = c1.id
JOIN case_components cc2 ON cc2.case_id = c2.id AND cc2.component_id = cc1.component_id
WHERE c1.external_reference LIKE 'SYN-DEMO-2026%'
  AND c2.external_reference LIKE 'SYN-DEMO-2026%'
  AND c1.id % 20 = 5;
-- ============================================================
-- SECAO 18: KNOWLEDGE_ITEMS (80 artigos)
-- ============================================================
-- 40 Published, 20 Draft, 10 Review, 5 Deprecated, 5 Archived
INSERT INTO knowledge_items (
  knowledge_code, knowledge_type, title, summary, status, confidentiality,
  owner_user_id, owner_department_id, current_version_no,
  provenance_type, provenance_case_id, provenance_reference,
  published_at, created_at, created_by, updated_at
)
WITH kiu AS (
  SELECT id, ROW_NUMBER() OVER (ORDER BY id ASC) AS rn FROM users
),
kid AS (
  SELECT id, ROW_NUMBER() OVER (ORDER BY id ASC) AS rn FROM departments
),
kic AS (
  SELECT id, ROW_NUMBER() OVER (ORDER BY id ASC) AS rn
  FROM cases WHERE external_reference LIKE 'SYN-DEMO-2026%'
)
SELECT
  CONCAT('KB-', LPAD(CAST(n AS CHAR), 4, '0')),
  'Solution',
  CASE
    WHEN n <= 5 THEN 'Tratamento de esgotamento do pool de conexoes MySQL'
    WHEN n <= 10 THEN 'Resolucao de timeout em API Gateway'
    WHEN n <= 15 THEN 'Correcao de erro HTTP 502 Bad Gateway'
    WHEN n <= 20 THEN 'Diagnosticos de autenticacao SSO'
    WHEN n <= 25 THEN 'Otimizacao de queries MySQL com EXPLAIN'
    WHEN n <= 30 THEN 'Correcao de falha de sincronizacao mobile'
    WHEN n <= 35 THEN 'Integracao SAP: tratamento de erros RFC'
    WHEN n <= 40 THEN 'Rollback seguro apos deploy com regressao'
    WHEN n <= 45 THEN 'Resolucao de falha de resolucao DNS'
    WHEN n <= 50 THEN 'Renovacao preventiva de certificados TLS'
    WHEN n <= 55 THEN 'Prevencao de deadlocks em MySQL'
    WHEN n <= 60 THEN 'Gerenciamento de regras de firewall'
    WHEN n <= 65 THEN 'Monitoramento de filas RabbitMQ'
    WHEN n <= 70 THEN 'Correcao de permissoes RBAC'
    WHEN n <= 75 THEN 'Importacao de arquivos CSV/XML em lote'
    ELSE 'Procedimento de emergencia para servicos criticos'
  END,
  CONCAT('Artigo de solucao para ', CASE
    WHEN n <= 5 THEN 'pool de conexoes'
    WHEN n <= 10 THEN 'timeout de API'
    WHEN n <= 15 THEN 'erro 502'
    WHEN n <= 20 THEN 'autenticacao SSO'
    WHEN n <= 25 THEN 'queries lentas'
    WHEN n <= 30 THEN 'sincronizacao mobile'
    WHEN n <= 35 THEN 'integracao SAP'
    WHEN n <= 40 THEN 'deploy seguro'
    WHEN n <= 45 THEN 'resolucao DNS'
    WHEN n <= 50 THEN 'certificados TLS'
    WHEN n <= 55 THEN 'deadlocks MySQL'
    WHEN n <= 60 THEN 'firewall'
    WHEN n <= 65 THEN 'filas RabbitMQ'
    WHEN n <= 70 THEN 'permissoes RBAC'
    WHEN n <= 75 THEN 'importacao de arquivos'
    ELSE 'servicos criticos'
  END, '.'),
  CASE
    WHEN n <= 40 THEN 'Published'
    WHEN n <= 60 THEN 'Draft'
    WHEN n <= 70 THEN 'Review'
    WHEN n <= 75 THEN 'Deprecated'
    ELSE 'Archived'
  END,
  'Internal',
  u2.id AS owner_user_id,
  d1.id AS owner_department_id,
  1,
  CASE WHEN n <= 40 THEN 'Case' ELSE 'Documentation' END,
  sc.id AS provenance_case_id,
  NULL,
  CASE WHEN n <= 40 THEN DATE_ADD('2025-10-01', INTERVAL (n % 12) MONTH) ELSE NULL END,
  DATE_ADD('2025-10-01', INTERVAL (n % 12) MONTH),
  u2.id AS created_by,
  DATE_ADD('2025-10-01', INTERVAL (n % 12) MONTH)
FROM (
  SELECT a.n + b.n * 10 + 1 AS n
  FROM (SELECT 0 AS n UNION SELECT 1 UNION SELECT 2 UNION SELECT 3 UNION SELECT 4
        UNION SELECT 5 UNION SELECT 6 UNION SELECT 7 UNION SELECT 8 UNION SELECT 9) a
  CROSS JOIN (SELECT 0 AS n UNION SELECT 1 UNION SELECT 2 UNION SELECT 3 UNION SELECT 4
              UNION SELECT 5 UNION SELECT 6 UNION SELECT 7) b
) nums
LEFT JOIN kic sc ON sc.rn = 1 + (n % 500) AND n <= 40
JOIN kiu u2 ON u2.rn = 1 + (n % 6)
LEFT JOIN kid d1 ON d1.rn = 1 + (n % 8);
-- ============================================================
-- SECAO 19: KNOWLEDGE_VERSIONS (80 versoes, 1 por item)
-- ============================================================
INSERT INTO knowledge_versions (
  knowledge_item_id, version_no, content_markdown, problem_description,
  root_cause_summary, validation_method, risk_warning, change_summary,
  status, content_hash, created_at, created_by, approved_at, approved_by
)
SELECT
  ki.id, 1,
  CONCAT('# ', ki.title, '\n\n## Descricao do Problema\n\n', ki.summary,
    '\n\n## Passos de Resolucao\n\n1. Verificar logs\n2. Identificar causa\n3. Aplicar correcao\n4. Validar\n\n## Validacao\n\nOperacao testada em homologacao.'),
  ki.summary,
  'Causa identificada e documentada.',
  'Teste em ambiente de homologacao com cenarios similares.',
  'Executar em horario de baixa demanda.',
  'Versao inicial do artigo.',
  CASE
    WHEN ki.status = 'Published' THEN 'Approved'
    WHEN ki.status = 'Review' THEN 'InReview'
    WHEN ki.status = 'Archived' THEN 'Deprecated'
    ELSE ki.status
  END,
  SHA2(CONCAT(ki.title, ki.summary), 256),
  ki.created_at, ki.created_by,
  CASE WHEN ki.status = 'Published' THEN ki.published_at ELSE NULL END,
  CASE WHEN ki.status = 'Published' THEN ki.created_by ELSE NULL END
FROM knowledge_items ki;

-- ============================================================
-- SECAO 20: KNOWLEDGE_STEPS (~400 registros, 5 por versao)
-- ============================================================
INSERT INTO knowledge_steps (knowledge_version_id, sequence_no, step_type, title, description)
SELECT kv.id, nums.seq, 'Solution',
  CASE nums.seq
    WHEN 1 THEN 'Verificar logs do servico'
    WHEN 2 THEN 'Identificar causa raiz'
    WHEN 3 THEN 'Aplicar correcao'
    WHEN 4 THEN 'Reiniciar servico'
    WHEN 5 THEN 'Validar operacao'
  END,
  CASE nums.seq
    WHEN 1 THEN 'Consultar logs para identificar padroes de erro e timestamps.'
    WHEN 2 THEN 'Analisar evidencias e correlacionar com sintomas reportados.'
    WHEN 3 THEN 'Aplicar a correcao identificada seguindo boas praticas.'
    WHEN 4 THEN 'Reiniciar o servico afetado para aplicar as mudancas.'
    WHEN 5 THEN 'Validar que o problema foi resolvido e a operacao normalizada.'
  END
FROM knowledge_versions kv
CROSS JOIN (SELECT 1 AS seq UNION SELECT 2 UNION SELECT 3 UNION SELECT 4 UNION SELECT 5) nums;
-- ============================================================
-- SECAO 21: KNOWLEDGE_SYMPTOMS (~200 registros)
-- ============================================================
INSERT INTO knowledge_symptoms (knowledge_version_id, symptom_text)
SELECT kv.id,
  CASE (kv.id % 15)
    WHEN 0 THEN 'timeout' WHEN 1 THEN 'too many connections'
    WHEN 2 THEN 'HTTP 502' WHEN 3 THEN 'acesso negado'
    WHEN 4 THEN 'consulta lenta' WHEN 5 THEN 'deadlock'
    WHEN 6 THEN 'certificado expirado' WHEN 7 THEN 'DNS falhou'
    WHEN 8 THEN 'erro SAP' WHEN 9 THEN 'deploy quebrou'
    WHEN 10 THEN 'sincronizacao falhou' WHEN 11 THEN 'firewall bloqueado'
    WHEN 12 THEN 'fila cheia' WHEN 13 THEN 'erro 401'
    ELSE 'erro inesperado'
  END
FROM knowledge_versions kv;

-- Segundo sintoma para metade dos itens
INSERT INTO knowledge_symptoms (knowledge_version_id, symptom_text)
SELECT kv.id,
  CASE (kv.id % 12)
    WHEN 0 THEN 'HTTP 504' WHEN 1 THEN 'conexao recusada'
    WHEN 2 THEN 'bad gateway' WHEN 3 THEN 'erro 403'
    WHEN 4 THEN 'CPU alta' WHEN 5 THEN 'lock wait timeout'
    WHEN 6 THEN 'handshake error' WHEN 7 THEN 'NXDOMAIN'
    WHEN 8 THEN 'RFC falhou' WHEN 9 THEN 'regressao'
    WHEN 10 THEN 'offline' WHEN 11 THEN 'IP bloqueado'
    ELSE 'mensagem pendente'
  END
FROM knowledge_versions kv
WHERE kv.id % 2 = 0;

-- ============================================================
-- SECAO 22: KNOWLEDGE_APPLICABILITY (~120 registros)
-- ============================================================
INSERT INTO knowledge_applicability (knowledge_item_id, product_id, component_id, applicability_type)
WITH kap_products AS (
  SELECT id, ROW_NUMBER() OVER (ORDER BY id ASC) AS rn FROM products
),
kap_components AS (
  SELECT id, ROW_NUMBER() OVER (ORDER BY id ASC) AS rn FROM components
)
SELECT ki.id, p.id, cc.id, 'Applies'
FROM knowledge_items ki
JOIN kap_products p ON p.rn = 1 + (ki.id % 4)
JOIN kap_components cc ON cc.rn = 1 + (ki.id % 13);

-- Applicability adicional para ~40% dos itens
INSERT INTO knowledge_applicability (knowledge_item_id, product_id, applicability_type)
WITH kap_products AS (
  SELECT id, ROW_NUMBER() OVER (ORDER BY id ASC) AS rn FROM products
)
SELECT ki.id, p.id, 'Recommended'
FROM knowledge_items ki
JOIN kap_products p ON p.rn = 1 + ((ki.id + 1) % 4)
WHERE ki.id % 5 = 0;
-- ============================================================
-- SECAO 23: KNOWLEDGE_USAGES (~120 registros)
-- ============================================================
-- Usos de conhecimento em casos posteriores a publicacao
INSERT INTO knowledge_usages (knowledge_item_id, knowledge_version_id, case_id, used_by, used_at, outcome, notes)
WITH ku_items AS (
  SELECT id, ROW_NUMBER() OVER (ORDER BY id ASC) AS rn
  FROM knowledge_items WHERE status = 'Published'
),
ku_versions AS (
  SELECT kv.id, ROW_NUMBER() OVER (ORDER BY kv.id ASC) AS rn
  FROM knowledge_versions kv
  JOIN knowledge_items ki ON kv.knowledge_item_id = ki.id
  WHERE ki.status = 'Published'
),
ku_cases AS (
  SELECT id, ROW_NUMBER() OVER (ORDER BY opened_at ASC) AS rn
  FROM cases WHERE external_reference LIKE 'SYN-DEMO-2026%'
  AND status IN ('Resolved','Reopened')
),
ku_users AS (
  SELECT id, ROW_NUMBER() OVER (ORDER BY id ASC) AS rn FROM users
)
SELECT
  ki2.id,
  kv2.id,
  kc.id,
  ku.id,
  DATE_ADD('2026-01-01', INTERVAL (n % 9) MONTH),
  CASE n % 5
    WHEN 0 THEN 'Worked'
    WHEN 1 THEN 'Worked'
    WHEN 2 THEN 'Worked'
    WHEN 3 THEN 'PartiallyWorked'
    ELSE 'DidNotWork'
  END,
  'Artigo consultado durante resolucao do caso.'
FROM (
  SELECT a.n + b.n * 10 + 1 AS n
  FROM (SELECT 0 AS n UNION SELECT 1 UNION SELECT 2 UNION SELECT 3 UNION SELECT 4
        UNION SELECT 5 UNION SELECT 6 UNION SELECT 7 UNION SELECT 8 UNION SELECT 9) a
  CROSS JOIN (SELECT 0 AS n UNION SELECT 1 UNION SELECT 2 UNION SELECT 3 UNION SELECT 4
              UNION SELECT 5 UNION SELECT 6 UNION SELECT 7 UNION SELECT 8 UNION SELECT 9
              UNION SELECT 10 UNION SELECT 11) b
) nums
JOIN ku_items ki2 ON ki2.rn = 1 + (n % 40)
JOIN ku_versions kv2 ON kv2.rn = 1 + (n % 40)
JOIN ku_cases kc ON kc.rn = 1 + (n % 390)
JOIN ku_users ku ON ku.rn = 1 + (n % 6);

-- ============================================================
-- SECAO 24: KNOWLEDGE_TAGS (~100 registros)
-- ============================================================
INSERT INTO knowledge_tags (knowledge_item_id, tag_id)
SELECT ki.id, t.id
FROM knowledge_items ki
JOIN tags t ON (ki.id + t.id) % 3 = 0
WHERE ki.id <= 80 AND t.id <= 20;

-- ============================================================
-- SECAO 25: KNOWLEDGE_TECHNOLOGIES (~120 registros)
-- ============================================================
INSERT INTO knowledge_technologies (knowledge_item_id, technology_id)
SELECT ki.id, tech.id
FROM knowledge_items ki
CROSS JOIN technologies tech
WHERE ki.id <= 80
  AND (ki.id + tech.id) % 5 = 0;

-- ============================================================
-- SECAO 26: SEARCHABLE_CONTENT_ENTRIES (~580 registros)
-- ============================================================
-- 500 entries para cases + 80 para knowledge
INSERT INTO searchable_content_entries (
  source_type, source_id, title, normalized_content, content_hash,
  validation_status, quality_status, visibility,
  client_id, product_id, created_at, updated_at, source_updated_at
)
SELECT
  'HistoricalCase', c.id,
  CONCAT('Caso #', c.case_number, ' - ', c.normalized_summary),
  CONCAT(c.original_report, ' ', c.normalized_summary),
  SHA2(CONCAT(c.original_report, c.normalized_summary), 256),
  'Validated', 'Complete', 'Internal',
  c.client_id, c.product_id,
  c.created_at, c.updated_at, c.updated_at
FROM cases c
WHERE c.external_reference LIKE 'SYN-DEMO-2026%';

INSERT INTO searchable_content_entries (
  source_type, source_id, source_version_id, title, normalized_content,
  content_hash, validation_status, quality_status, visibility,
  created_at, updated_at, source_updated_at
)
SELECT
  'ValidatedKnowledge', ki.id, kv.id,
  ki.title,
  CONCAT(ki.summary, ' ', kv.content_markdown),
  kv.content_hash,
  'Validated', 'Complete', 'Internal',
  ki.created_at, ki.updated_at, ki.created_at
FROM knowledge_items ki
JOIN knowledge_versions kv ON kv.knowledge_item_id = ki.id
WHERE ki.status = 'Published';
-- ============================================================
-- SECAO 27: FINALIZACAO
-- ============================================================
-- Reabilitar verificacao de chaves estrangeiras
SET FOREIGN_KEY_CHECKS = 1;

-- ============================================================
-- RESUMO DA CARGA:
--   Clientes: 8 sinteticos (3 existentes preservados)
--   Client Units: 16
--   Componentes: 8 novos (5 existentes preservados)
--   Usuarios: 5 sinteticos (1 admin preservado)
--   Causas Raiz: 15 novas (4 existentes preservadas)
--   Tags: 12 novas (8 existentes preservadas)
--   Cases: 500 sinteticos
--   Case Iterations: ~500+ (1 por caso + extras para reabertos)
--   Case Symptoms: ~1000 (2 por caso)
--   Case Components: ~700 (1-2 por caso)
--   Case Evidences: ~900 (1-2 por caso)
--   Case Hypotheses: ~1700 (2-3 por caso)
--   Case Hypothesis Evidence: ~600 (evidencias ligadas a hipoteses)
--   Diagnostic Sessions: ~450 (90% dos casos)
--   Diagnostic Steps: ~1800 (2-6 por sessao)
--   Case Resolutions: ~400 (todos Resolved/Reopened)
--   Case Relations: ~200 (Similar, Recurrence, CommonCause)
--   Knowledge Items: 80
--   Knowledge Versions: 80
--   Knowledge Steps: 400
--   Knowledge Symptoms: ~120
--   Knowledge Applicability: ~120
--   Knowledge Usages: ~120
--   Knowledge Tags: ~100
--   Knowledge Technologies: ~120
--   Searchable Content Entries: ~580
-- ============================================================
-- FIM DO SCRIPT DE POPULACAO
-- ============================================================