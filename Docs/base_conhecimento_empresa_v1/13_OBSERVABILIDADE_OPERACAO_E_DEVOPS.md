# 13 — Observabilidade, operação e DevOps

## 1. Ambientes

Mínimo:
- Development;
- Test/QA;
- Staging/Homologação;
- Production.

Dados reais de produção não devem ser copiados para desenvolvimento sem sanitização e autorização.

## 2. Configuração

12-factor onde aplicável:
- configuração por ambiente;
- segredo fora do código;
- logs estruturados;
- processos stateless quando possível;
- migrações controladas.

## 3. CI

Em pull request:
1. restore;
2. build com warnings relevantes tratados;
3. testes unitários;
4. testes de arquitetura;
5. análise estática;
6. SCA/dependências;
7. testes de integração selecionados;
8. validação de migrations.

## 4. CD

Pipeline:
- gerar artefato imutável;
- promover o mesmo artefato entre ambientes;
- aplicar migrations com etapa controlada;
- health check;
- smoke tests;
- rollback de aplicação;
- migration destrutiva somente com plano específico.

## 5. OpenTelemetry

Instrumentar:
- requests HTTP;
- queries MySQL relevantes;
- chamadas de integração;
- jobs;
- busca;
- RAG/LLM;
- fluxo de diagnóstico.

Propagar `trace_id` e `correlation_id`.

## 6. Logs

Logs estruturados com campos:
- timestamp;
- level;
- service/module;
- event_name;
- correlation_id;
- trace_id;
- user_id quando permitido;
- entity_id;
- duration_ms;
- outcome.

Não logar segredo.

## 7. Métricas técnicas

- request rate;
- latência p50/p95/p99;
- erro 4xx/5xx;
- conexões MySQL;
- slow queries;
- job queue;
- outbox pendente;
- falha de integração;
- index lag;
- uploads;
- latência da busca;
- latência/custo IA.

## 8. Health checks

Endpoints separados:
- liveness;
- readiness.

Readiness pode verificar dependências críticas, com timeout curto e sem causar carga excessiva.

## 9. Backup e recuperação

Definir RPO/RTO antes de produção.

Obrigatório:
- backup automático;
- teste periódico de restore;
- retenção;
- proteção contra exclusão acidental;
- runbook de recuperação;
- backup/replicação do storage de anexos.

## 10. Deploy e containers

A aplicação pode ser containerizada. A documentação não exige Kubernetes. Começar com operação compatível com a infraestrutura real da empresa. Kubernetes somente se houver necessidade organizacional/escala que o justifique.

## 11. Migrações

- versionadas no repositório;
- forward-only por padrão;
- alterações destrutivas em duas ou mais etapas;
- compatibilidade entre versão antiga/nova durante rolling update, se aplicável;
- backup antes de operação crítica.

