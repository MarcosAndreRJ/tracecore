# FASE 07 — VALIDAÇÃO FINAL (AUDITORIA DE TODO O PLANO)

> Depende de **todas as fases anteriores (01-06)** estarem concluídas, com build e testes passando em cada uma. Esta fase é diferente das anteriores: é uma **auditoria**, não uma fase de implementação nova.

---

## CONTEXTO

Esta é a última fase do plano de ajuste dos módulos do Ecossistema (Sistemas/Componentes/Dependências/Integrações). Antes de considerar o trabalho pronto, é preciso confirmar — com evidência real, não com suposição — que tudo que foi prometido nas Fases 01-06 está de fato implementado, testado e funcionando junto.

## OBJETIVO

Auditar de ponta a ponta: Sistema, Componentes, Dependências, Integrações, Diagnóstico (teste de integração durante investigação), Contexto Técnico, Copiloto.

## DIFERENÇA EM RELAÇÃO ÀS FASES ANTERIORES

Esta fase é primariamente de **verificação**. Corrija o que encontrar quebrado (build, teste, regressão), mas **não implemente funcionalidade nova** aqui — se faltar algo do escopo original, registre como pendência explícita na matriz final em vez de improvisar uma implementação não planejada e não revisada pelas fases anteriores.

## ESCOPO

1. Reler o pedido original completo (contexto operacional da empresa, exemplos reais de Sistema/Integração citados) e confirmar, item por item, que a implementação final atende.
2. Rodar a suíte de testes completa do projeto (não só os testes de cada fase isoladamente).
3. Rodar a aplicação real (MySQL real, não InMemory) e validar manualmente os fluxos principais:
   - criar um Sistema com contexto técnico básico;
   - editar o Sistema;
   - adicionar um Componente pela aba do Sistema, editar, inativar;
   - adicionar uma Integração pela aba do Sistema, editar, testar health-check, desvincular;
   - abrir um Caso, testar uma Integração durante a investigação, confirmar o passo diagnóstico e a evidência gerados;
   - validar uma solução reaproveitando o mesmo mecanismo de teste de integração;
   - consultar o Copiloto sobre um Sistema e confirmar que a resposta reflete os dados novos (tipo do sistema, tecnologia, responsabilidade/hospedagem de uma integração) — só possível se houver um provedor de LLM configurado no ambiente; se não houver, documente isso como limitação de verificação, não como falha.
4. Confirmar migrations: todas aditivas, nenhuma alterou uma migration histórica, todas rodam em banco já populado sem apagar dados (confira contra a massa de teste em `Docs/sql_mockup`, sem alterar esses arquivos).
5. Confirmar FKs: nenhuma órfã, nenhuma exclusão em cascata indevida sobre histórico que deveria ser preservado (`IntegrationRun`, `DiagnosticStep`, `CaseEvidence` ao desvincular/inativar componentes e integrações).
6. Confirmar auditoria: toda operação de escrita relevante grava evento (especial atenção ao `IntegrationService`, que não tinha nenhuma auditoria antes da Fase 01).
7. Confirmar permissões: nenhuma tela/ação nova ficou acessível sem a policy correta, nenhuma ação passou a exigir uma permissão mais restritiva do que deveria (ex.: testar integração durante investigação não deve exigir `integracao.gerenciar`, conforme decidido na Fase 05).
8. Confirmar padrão visual: `tc-card`, `tc-btn`, `tc-input`, `tc-select`, `tc-table`, `tc-badge` e modais em todas as telas novas/alteradas — nenhuma tabela HTML "crua", nenhum CRUD fora do padrão do projeto.
9. Confirmar Razor Pages preservado — nenhuma fase migrou nada para SPA/Blazor/React/Angular.

## MATRIZ DE VALIDAÇÃO

Preencher com o resultado real (não copiar "OK" sem verificar):

| Requisito | Backend | Banco | UI | Teste | Resultado |
|---|---|---|---|---|---|
| Edição completa de Integração | | | | | |
| Sistema selecionável ao criar Integração | | | | | |
| Catálogo de Tipo de Integração | | | | | |
| Responsabilidade/Hospedagem/Direção da Integração | | | | | |
| Auditoria do módulo de Integrações | | | | | |
| Nomenclatura "Histórico de Execuções e Verificações" | | | | | |
| Edição de Componente | | | | | |
| Inativação de Componente | | | | | |
| Catálogo de Tipo de Componente (incl. "Módulo") | | | | | |
| Menu Componentes/Dependências sem duplicidade | | | | | |
| Cadastro inicial de Sistema com contexto técnico básico | | | | | |
| Edição de Sistema simétrica à criação | | | | | |
| Aba Componentes do Sistema interativa | | | | | |
| Aba Integrações do Sistema interativa | | | | | |
| Desvincular (não excluir) Componente/Integração do Sistema | | | | | |
| `IntegrationRun` com contexto/origem | | | | | |
| Teste de integração durante investigação (Diagnostic) | | | | | |
| Reaproveitamento do teste para validação de solução | | | | | |
| `ProductInvestigationContextDto` atualizado | | | | | |
| Contexto exposto ao Copiloto atualizado | | | | | |

## CRITÉRIOS DE ACEITE

- Toda a suíte de testes automatizados passando.
- Build limpo.
- Todos os fluxos manuais do ESCOPO item 3 executados e confirmados na aplicação real.
- Matriz de validação preenchida por completo, sem células em branco.
- Nenhum dado da massa de teste (`Docs/sql_mockup`) foi apagado ou corrompido pelo conjunto das 6 fases.
- Qualquer item da matriz marcado como pendente vem acompanhado de explicação clara do motivo e do que falta.

## ENTREGA FINAL

1. matriz de validação completa;
2. resultado da suíte de testes completa (contagem final: quantos passaram, quantos falharam, se algum foi corrigido nesta fase);
3. lista de bugs encontrados e corrigidos durante a auditoria (se houver);
4. lista de pendências que ficam explicitamente em aberto, com justificativa;
5. confirmação de que nenhuma migration histórica foi alterada;
6. confirmação de que a massa de teste em `Docs/sql_mockup` está intacta;
7. parecer final: o conjunto das 6 fases anteriores está pronto para uso, ou ainda há bloqueadores.
