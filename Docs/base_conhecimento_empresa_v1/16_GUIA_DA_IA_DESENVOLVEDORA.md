# 16 — Guia da IA desenvolvedora

Este documento deve ser entregue a qualquer agente de IA que trabalhe no código.

## 1. Ordem obrigatória de leitura

Antes de implementar:
1. `README.md`;
2. regras de negócio;
3. requisitos funcionais;
4. módulo relacionado;
5. arquitetura;
6. modelo de dados;
7. critérios de aceite;
8. ADRs existentes.

## 2. Regras de comportamento

### DEV-AI-001
Não invente regra de negócio ausente. Quando uma decisão afetar comportamento, registre a dúvida ou proponha ADR.

### DEV-AI-002
Não altere regra de negócio para facilitar código.

### DEV-AI-003
Não introduza microserviço, broker, Redis, Elasticsearch/OpenSearch, vector DB, Kubernetes ou framework adicional sem necessidade demonstrada e ADR.

### DEV-AI-004
Não acople domínio a Razor Pages, MySQL, SDK de IA, SAP ou biblioteca de infraestrutura.

### DEV-AI-005
Toda operação de escrita deve validar autorização no servidor.

### DEV-AI-006
Toda feature relevante deve considerar auditoria e observabilidade.

### DEV-AI-007
Mudança de schema exige migration versionada e teste.

### DEV-AI-008
Não apague dados históricos de caso/conhecimento para “simplificar”.

### DEV-AI-009
Use UTC na persistência.

### DEV-AI-010
Não registre segredo em log, auditoria, fixture ou teste.

### DEV-AI-011
Não execute SQL destrutivo em produção como parte de instrução automática.

### DEV-AI-012
Sempre implemente testes compatíveis com o risco da mudança.

## 3. Fluxo por tarefa

1. Identifique IDs `BR`, `FR` e `AC` envolvidos.
2. Descreva arquivos que serão alterados.
3. Confirme dependências.
4. Implemente domínio/aplicação antes da UI quando aplicável.
5. Implemente persistência.
6. Implemente endpoint/UI.
7. Adicione autorização.
8. Adicione auditoria.
9. Adicione logs/métricas úteis.
10. Adicione testes.
11. Execute build/test.
12. Atualize documentação se comportamento mudou.
13. Liste riscos e decisões abertas.

## 4. Padrão de resposta de uma IA ao concluir tarefa

```text
Objetivo:
Regras atendidas: BR-..., FR-..., AC-...
Arquivos alterados:
Migrations:
Testes adicionados:
Comandos executados:
Resultados:
Decisões tomadas:
Pendências/riscos:
Documentação atualizada:
```

## 5. Convenções de código

- nullable reference types habilitado;
- async em I/O;
- `CancellationToken` em operações externas/longas;
- tipos de domínio para estados importantes;
- não usar strings mágicas para status;
- DTOs separados de entidades;
- validação na fronteira e invariantes no domínio;
- queries parametrizadas;
- métodos pequenos e nomeados por intenção;
- comentários explicam “por quê”, não repetem código.

## 6. Arquitetura de dependência

Permitido:
```text
Web/API -> Application -> Domain
Infrastructure -> Application/Domain abstractions
Worker -> Application/Infrastructure
```

Proibido:
```text
Domain -> Infrastructure
Domain -> Razor Pages / camada de apresentação
Domain -> MySQL SDK
Application -> componente concreto de LLM
```

## 7. Banco

- SQL sempre parametrizado;
- índices devem ser justificados por consulta;
- evitar N+1;
- transação explícita quando múltiplas gravações formam uma unidade;
- migrations forward-only;
- alteração destrutiva requer plano;
- `EXPLAIN` em queries críticas.

## 8. IA/RAG

Nenhum código de IA deve:
- publicar conhecimento;
- mudar causa raiz confirmada;
- executar comando destrutivo;
- elevar permissão;
- recuperar conteúdo sem ACL.

## 9. Prompt-base para iniciar uma tarefa

```text
Você está desenvolvendo a Plataforma Corporativa de Conhecimento, Diagnóstico e Lições Aprendidas (TraceCore).
Estado real da implementação: Fase 17 (provedores de IA desacoplados), 29 migrations FluentMigrator, 105 testes de integração.
Leia primeiro README.md, 01_REGRAS_DE_NEGOCIO.md, 02_REQUISITOS_FUNCIONAIS.md,
09_ARQUITETURA_TECNICA_DOTNET_MYSQL.md, 19_CRITERIOS_DE_ACEITE_E_RASTREABILIDADE.md
e os ADRs aplicáveis. Migrations em src/TraceCore.Infrastructure/Migrations; testes em tests/TraceCore.IntegrationTests.

Antes de codificar, identifique as regras BR/FR/AC relacionadas e apresente um plano curto.
Não invente regra. Não mude arquitetura sem ADR. Preserve histórico, autorização, auditoria,
observabilidade, migrations e testes. Ao terminar, reporte exatamente o que foi alterado e os testes executados.
```

