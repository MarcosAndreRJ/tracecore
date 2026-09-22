# AJUSTE DOS MÓDULOS DO ECOSSISTEMA — ÍNDICE E PLANO DE EXECUÇÃO

Este diretório contém o plano de implementação, dividido em fases, para corrigir modelagem, nomenclatura, UX e relacionamento entre entidades no módulo **Ecossistema** (Sistemas / Componentes / Dependências / Integrações) do TraceCore.

**Este índice e as 7 fases foram gerados por análise do código real do repositório em `K:\Trabalho\Projetos\TraceCore`, não por suposição.** Cada arquivo de fase documenta exatamente o que foi confirmado no código (com caminho de arquivo e, quando relevante, linha) no momento da análise. Releia o estado real antes de implementar cada fase — o código pode ter mudado entre a análise e a execução.

---

## REGRA DE EXECUÇÃO

```
Executar um prompt por vez.
Somente avançar para a próxima fase após build e testes da fase anterior passarem.
Cada fase precisa ser testada manualmente na aplicação rodando antes de ser considerada concluída.
Nenhum prompt deve fazer commit/push automaticamente — controle de versão é do usuário.
```

---

## OBJETIVO GERAL

Uso real da aplicação revelou que o módulo Ecossistema tem: tipos hardcoded em `<select>` que deveriam ser catálogos administráveis; a entidade `Integration` sem campos que já existem no mundo real da empresa (responsabilidade, hospedagem, direção); cadastro de Sistema raso demais; edição incompleta ou ausente em Integrações e Componentes; um menu com item duplicado; e nenhuma ligação estrutural entre "testar uma integração" e a investigação de um caso. O objetivo é corrigir tudo isso de forma coerente, sem quebrar o que já funciona (Prompts 1-4 da iniciativa anterior "Nova Estratégia do Copiloto", já implementados e testados).

## PROBLEMAS ENCONTRADOS (confirmados no código real)

| # | Problema | Onde | Confirmado |
|---|---|---|---|
| 1 | `IIntegrationService` não tem `UpdateIntegrationAsync` | `IIntegrationService.cs` | Sim — só `UpdateIntegrationStatusAsync`/`ConfigureHealthCheckAsync` existem |
| 2 | Repositório de Integração **já tem** UPDATE completo (achado que reduz o escopo da Fase 02) | `MySqlIntegrationRepository.cs:129-151`, `InMemoryIntegrationRepository` | Sim — inclusive `product_id` já é gravado |
| 3 | Formulário "Nova Integração" não permite escolher Sistema | `Integrations/Index.cshtml:309-374` | Sim — apesar de `Integration.ProductId` já existir no domínio |
| 4 | `IntegrationService` não audita nada | `IntegrationService.cs` | Sim — zero chamadas a `IAuditService`, diferente de `CatalogService` que audita tudo |
| 5 | Tipo de Integração e Tipo de Componente hardcoded em `<select>` | `Integrations/Index.cshtml:334-345`, `Catalog/Components/Index.cshtml:296-304` | Sim |
| 6 | Sem campos de Responsabilidade/Hospedagem/Direção na Integração | `IntegrationEntities.cs` | Sim — nenhum dos três existe |
| 7 | Edição de Componente existe no backend, ausente na UI | `CatalogService.UpdateComponentAsync` existe e é auditado; `Catalog/Components/Index.cshtml` não tem botão de editar | Sim |
| 8 | Menu "Componentes" e "Dependências" duplicados | `_Layout.cshtml:173-193` | Sim — mesma URL (`/Catalog/Components/Index`), mesma condição de item ativo |
| 9 | Cadastro inicial de Sistema raso (4 campos) | `Catalog/Products/Index.cshtml.cs:36-52` | Sim — contexto técnico só acessível depois, na tela de Detalhes |
| 10 | Abas Componentes/Integrações da tela de Sistema são só leitura | `Catalog/Products/Details.cshtml(.cs)` | Sim — só resumo + link para tela global |
| 11 | Sem campo "Tipo do Sistema" em lugar nenhum | `Product`, `ProductTechnicalProfile` | Sim — nenhuma entidade tem esse campo hoje |
| 12 | `IntegrationRun` sem contexto/origem nem ligação com Caso | `IntegrationEntities.cs`, `DiagnosticStep.cs`, `CaseEvidence.cs` | Sim — investigação dedicada confirmou zero campos de integração em `DiagnosticStep`/`CaseEvidence` |
| 13 | Motor de diagnóstico automático já testa integração de verdade, mas só liga ao passo diagnóstico por texto solto | `DiagnosticEngineService.cs:361-435` | Sim — `InputEvidenceSummary: "Integração #{id}"`, sem FK |

## DECISÕES CONCEITUAIS TOMADAS NO PLANEJAMENTO

- `ComponentType`/`IntegrationType` → **catálogo administrável** (tabela própria). `Responsibility`/`HostingLocation`/`Direction`/`SystemType`/`IntegrationRun.RunContext` → **strings controladas simples**, validadas em código, sem tabela nova (poucos valores estáveis, sem necessidade de administração dinâmica).
- Nenhum catálogo genérico (`generic_lookup_table`) — cada conceito tem nome e tabela próprios, legíveis.
- "Tipo do Sistema" vive em `ProductTechnicalProfile`, não em `Product` (mantém `Product` enxuto, consistente com decisões anteriores do projeto).
- Teste de integração durante investigação e validação de solução usam **um único mecanismo** (`IntegrationRun` com `RunContext`), nunca três modelos separados — ligação estrutural real (FK), substituindo o hack textual hoje existente no motor de diagnóstico automático.
- `Product` continua sendo `Product` no domínio; a UI continua chamando de "Sistema". Nenhuma renomeação em massa.
- Nenhuma migração de tecnologia (Razor Pages, padrão visual `tc-*` preservados em todas as fases).

## FASES E ORDEM OBRIGATÓRIA

```
01_modelagem_e_catalogos.md              (base — catálogos, campos novos, auditoria de Integrations)
        ↓
02_integracoes_cadastro_edicao.md        (depende de 01)
        ↓
03_componentes_e_dependencias.md         (depende de 01 — pode rodar em paralelo conceitual com 02, mas
                                           execute em sequência para simplificar controle de regressão)
        ↓
04_sistemas_cadastro_e_relacionamentos.md (depende de 01, 02 e 03 — reaproveita edição de Integração e Componente)
        ↓
05_integracoes_no_diagnostico.md          (depende de 01 e 02 — fase mais sensível de modelagem)
        ↓
06_consolidacao_ux_e_contexto_ia.md       (depende de 01-05 — consolida DTOs e nomenclatura)
        ↓
07_validacao_final.md                     (depende de 01-06 — auditoria, não implementação nova)
```

Execute nessa ordem exata. Não pule fases. Não implemente a Fase 01 automaticamente ao ler este índice — cada fase é um prompt separado, executado um de cada vez.

## ARQUIVOS PROVAVELMENTE AFETADOS (visão consolidada)

```
Domain:
  src/TraceCore.Domain/Entities/CatalogEntities.cs
  src/TraceCore.Domain/Entities/IntegrationEntities.cs
  src/TraceCore.Domain/Entities/ProductTechnicalContextEntities.cs
  src/TraceCore.Domain/Entities/DiagnosticStep.cs
  src/TraceCore.Domain/Entities/CaseEvidence.cs
  src/TraceCore.Domain/Repositories/ICatalogRepository.cs
  src/TraceCore.Domain/Repositories/IIntegrationRepository.cs

Application:
  src/TraceCore.Application/Services/ICatalogService.cs / CatalogService.cs
  src/TraceCore.Application/Services/IIntegrationService.cs / IntegrationService.cs
  src/TraceCore.Application/Services/ICaseInvestigationService.cs / CaseInvestigationService.cs
  src/TraceCore.Application/Services/ICaseResolutionService.cs / CaseResolutionService.cs
  src/TraceCore.Application/Services/DiagnosticEngineService.cs
  src/TraceCore.Application/Services/ProductTechnicalContextService.cs
  src/TraceCore.Application/Services/InvestigationCopilotService.cs
  src/TraceCore.Application/DTOs/IntegrationDtos.cs, InvestigationDTOs.cs, CaseResolutionDtos.cs, ProductTechnicalContextDtos.cs

Infrastructure:
  src/TraceCore.Infrastructure/Persistence/Repositories/MySqlCatalogRepository.cs
  src/TraceCore.Infrastructure/Persistence/Repositories/MySqlIntegrationRepository.cs
  src/TraceCore.Infrastructure/Persistence/Repositories/MySqlProductTechnicalContextRepository.cs
  src/TraceCore.Infrastructure/Persistence/Repositories/(repositório de diagnóstico/evidência — confirmar nome real)
  src/TraceCore.Infrastructure/Persistence/InMemory/InMemoryRepositories.cs
  src/TraceCore.Infrastructure/Migrations/M2026____ (múltiplas novas, uma por fase que alterar schema)

Web:
  src/TraceCore.Web/Pages/Integrations/Index.cshtml(.cs)
  src/TraceCore.Web/Pages/Catalog/Components/Index.cshtml(.cs)
  src/TraceCore.Web/Pages/Catalog/Products/Index.cshtml(.cs)
  src/TraceCore.Web/Pages/Catalog/Products/Details.cshtml(.cs)
  src/TraceCore.Web/Pages/Cases/Details.cshtml(.cs)
  src/TraceCore.Web/Pages/Shared/_Layout.cshtml
```

## MIGRATIONS ESPERADAS POR FASE

| Fase | Gera migration? | Conteúdo |
|---|---|---|
| 01 | Sim | `component_types`, `integration_types`, `integrations.responsibility/hosting_location/direction` |
| 02 | Provavelmente não | Reaproveita schema da Fase 01 |
| 03 | Não (a menos que normalização de `dependency_type` seja necessária) | — |
| 04 | Sim | `product_technical_profiles.system_type` |
| 05 | Sim | `integration_runs.run_context/case_id`, `diagnostic_steps.integration_run_id`, `case_evidences.integration_run_id` |
| 06 | Não | Só leitura/exposição de dados já existentes |
| 07 | Não | Auditoria |

Todas as migrations: aditivas, numeração sequencial nova (descubra a última migration real antes de numerar — no momento desta análise a última é `M20260919_23`, mas confirme de novo antes de cada fase, já que fases anteriores do próprio plano vão criar migrations novas), nunca alteram migration histórica, nunca apagam dados.

## RISCOS PRINCIPAIS

1. **Fase 05 é a mais arriscada estruturalmente** — mexe em `DiagnosticStep`/`CaseEvidence`/`IntegrationRun`, tabelas com dados reais na massa de teste. Migration precisa ser estritamente aditiva (`ADD COLUMN` nullable), nunca reescrever linhas existentes além de um backfill opcional e bem documentado.
2. **`IntegrationService` sem auditoria hoje** — a Fase 01 corrige isso, mas até lá qualquer teste manual feito durante a análise deste plano não deixou rastro de auditoria (não é um problema causado por este plano, só uma lacuna pré-existente confirmada).
3. **`IntegrationHealthCheckService` sem proteção contra SSRF** — pré-existente, fora do escopo deste plano, mas citado nas Fases 02 e 05 como oportunidade de reaproveitar `ExternalUrlSafetyValidator` (já construído e testado na iniciativa do Copiloto) caso o fluxo de health-check seja tocado de qualquer forma.
4. **Coexistência com a massa de teste em `Docs/sql_mockup`** — nenhuma fase deve gerar dados fictícios novos nem alterar os scripts existentes; migrations precisam rodar sobre a base já populada sem quebrar nada.
5. **Nomenclatura `IntegrationType` colidindo com o nome da nova entidade de catálogo** (Fase 01) — resolver a colisão de nomes com clareza (documentado no próprio prompt da Fase 01).
6. **Possível trabalho concorrente de outro processo/IA no mesmo repositório** — já aconteceu antes neste projeto (registrado em decisões anteriores). Sempre reconfirme o estado real do código no início de cada fase antes de assumir que o que este plano descreve ainda é verdade.

## DEFINIÇÃO DE PRONTO (para o conjunto das 7 fases)

- Build limpo e suíte de testes completa passando após a Fase 07.
- Todos os fluxos manuais listados na Fase 07 confirmados na aplicação real.
- Matriz de validação da Fase 07 sem células em branco.
- Nenhuma migration histórica alterada, nenhum dado da massa de teste perdido.
- `IntegrationService` audita todas as suas operações.
- Menu Ecossistema sem itens duplicados.
- Sistema, Componente e Integração editáveis de ponta a ponta pela UI.
- Teste de integração durante investigação e durante validação de solução funcionando com rastreabilidade estrutural (FK), não texto solto.
- `ProductInvestigationContextDto` e o contexto consultado pelo Copiloto refletindo os dados novos.

---

Dúvidas sobre uma fase específica: releia o arquivo `NN_*.md` correspondente — cada um é autossuficiente e não depende de você lembrar desta conversa.
