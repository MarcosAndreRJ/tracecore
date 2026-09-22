# 14 — Testes e qualidade

## 0. Status atual da suíte

**105 testes de integração aprovados** em `tests/TraceCore.IntegrationTests` (xUnit + MySQL/Testcontainers), que comprovamente detectaram bugs reais só visíveis contra banco de verdade (colunas inexistentes, materialização Dapper de records posicionais, `utf8mb4_unicode_ci` em MariaDB 10.5). As seções abaixo descrevem a pirâmide alvo e práticas; a suíte já cobre repositories, migrations, FULLTEXT e queries analíticas críticas.

## 1. Pirâmide

### Unitários
Regras puras:
- transições de status;
- cálculo de métricas;
- elegibilidade de publicação;
- autorização de domínio quando aplicável;
- ranking determinístico;
- regras de aplicabilidade.

### Integração
Com MySQL real via Testcontainers:
- repositories;
- transactions;
- concorrência;
- migrations;
- FULLTEXT;
- outbox;
- queries analíticas críticas.

### Contrato
- APIs internas/externas;
- webhooks;
- adapters SAP/chamados.

### E2E
Playwright:
- abrir caso;
- diagnosticar;
- resolver;
- promover solução;
- revisar/publicar;
- pesquisar/reutilizar;
- gestão de usuário/permissão.

## 2. Testes de autorização

Toda feature sensível precisa de teste negativo:
- usuário sem permissão não acessa endpoint;
- esconder botão não é suficiente;
- busca não vaza título/snippet de item restrito;
- RAG não recupera conteúdo sem ACL.

## 3. Testes de busca

Manter corpus fixo e consultas esperadas:
- erro exato;
- sinônimos;
- versões incompatíveis;
- casos semelhantes;
- termo ambíguo;
- item obsoleto.

## 4. Testes de RAG

Dataset versionado com:
- pergunta;
- contexto/filtros;
- fontes esperadas;
- fatos obrigatórios;
- fatos proibidos/incompatíveis.

Mudança de modelo/chunking/ranking roda avaliação antes do deploy.

## 5. Performance

Cenários:
- busca global;
- dashboard executivo;
- timeline com muitos eventos;
- auditoria;
- publicação/indexação;
- concorrência de uso normal.

Definir SLOs após baseline. Evitar inventar números sem teste de infraestrutura real.

## 6. Definition of Done

Uma história não está pronta se faltar qualquer item aplicável:
- regra implementada;
- autorização;
- validação;
- teste;
- auditoria;
- observabilidade;
- migration;
- documentação;
- critério de aceite;
- tratamento de erro;
- acessibilidade básica;
- revisão de segurança.

