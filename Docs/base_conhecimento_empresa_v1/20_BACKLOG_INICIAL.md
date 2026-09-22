# 20 — Backlog inicial sugerido

## Épico E0 — Fundação

- BK-001 Criar solution e projetos.
- BK-002 Configurar MySQL e migrations.
- BK-003 Implementar convenções de erro/Problem Details.
- BK-004 Implementar autenticação baseline.
- BK-005 Implementar RBAC.
- BK-006 Implementar auditoria base.
- BK-007 Implementar OpenTelemetry/log estruturado.
- BK-008 Implementar outbox e worker.
- BK-009 Configurar CI e testes com Testcontainers.
- BK-010 Criar layout Blazor e navegação.

## Épico E1 — Organização

- BK-020 CRUD departamentos.
- BK-021 ~~CRUD equipes~~ (extinto — `teams` removido na Migration 10).
- BK-022 gestão de usuários.
- BK-023 gestão de papéis/permissões.
- BK-024 tela de permissões efetivas.
- BK-025 tela administrativa de usuário.

## Épico E2 — Catálogo técnico

- BK-040 clientes/unidades.
- BK-041 produtos/módulos.
- BK-042 versões.
- BK-043 componentes.
- BK-044 tecnologias.
- BK-045 integrações.
- BK-046 dependências.
- BK-047 responsáveis/escalonamento.
- BK-048 visualização de grafo.

## Épico E3 — Casos

- BK-060 criar caso.
- BK-061 sintomas.
- BK-062 anexos/evidências.
- BK-063 hipóteses.
- BK-064 sessão de diagnóstico.
- BK-065 passos/tentativas.
- BK-066 timeline.
- BK-067 handoff.
- BK-068 relações.
- BK-069 resolução/validação.
- BK-070 causa raiz.
- BK-071 reabertura.

## Épico E4 — Conhecimento

- BK-080 criar artigo/solução.
- BK-081 editor Markdown.
- BK-082 aplicabilidade.
- BK-083 steps/riscos/rollback.
- BK-084 versionamento.
- BK-085 revisão/aprovação.
- BK-086 depreciação.
- BK-087 uso de solução em caso.
- BK-088 estatística de resultado.
- BK-089 proposta a partir de caso.

## Épico E5 — Pesquisa

- BK-100 índice textual.
- BK-101 busca global.
- BK-102 filtros.
- BK-103 ranking.
- BK-104 explicabilidade.
- BK-105 histórico de busca.
- BK-106 feedback.
- BK-107 sugestões de casos relacionados.

## Épico E6 — Diagnóstico guiado

- BK-120 cadastro de verificações.
- BK-121 vínculo hipótese-verificação.
- BK-122 perguntas adaptativas.
- BK-123 priorização.
- BK-124 pacote de escalonamento.
- BK-125 5 fluxos piloto.

## Épico E7 — Analytics

- BK-140 eventos/fatos.
- BK-141 agregações.
- BK-142 executivo.
- BK-143 operacional.
- BK-144 conhecimento.
- BK-145 pesquisa.
- BK-146 tecnologia.
- BK-147 usuário/departamento.
- BK-148 drill-down.

## Épico E8 — IA/RAG

- BK-160 abstrações de provider.
- BK-161 pipeline de chunking.
- BK-162 embeddings.
- BK-163 índice vetorial homologado.
- BK-164 busca híbrida.
- BK-165 dataset de avaliação.
- BK-166 assistente com citações.
- BK-167 feedback.
- BK-168 rascunho de conhecimento.

## Priorização inicial

Primeiro release útil recomendado: E0 + E1 + E2 + E3 básico + E4 básico + E5 básico. Diagnóstico, analytics e IA entram sobre dados reais gerados pelo uso, evitando construir algoritmos sobre hipóteses não validadas.

