# FASE 06 — CONSOLIDAÇÃO: NOMENCLATURA, NAVEGAÇÃO E CONTEXTO PARA O COPILOTO

> Depende de **todas as fases anteriores (01-05)**. Esta fase não cria funcionalidade nova — consolida o que as fases anteriores construíram e garante que o Copiloto (`IInvestigationCopilotService`/`IProductTechnicalContextService`, construídos na iniciativa anterior "Nova Estratégia do Copiloto") enxergue os dados novos.

---

## CONTEXTO

As Fases 01-05 adicionaram campos, catálogos e relacionamentos novos (`ComponentType`, `IntegrationType`, `Responsibility`, `HostingLocation`, `Direction`, `SystemType`, `IntegrationRun.RunContext`/`CaseId`). Nenhum deles é decorativo — o pedido original é explícito: "os dados precisam ser recuperáveis pelos serviços de contexto técnico" para que o Copiloto possa futuramente raciocinar sobre onde um sistema está hospedado, quem é responsável, qual tecnologia usa, qual integração está envolvida.

## OBJETIVO

1. Revisar nomenclatura de UI que ficou inconsistente entre as fases (rótulos, labels de badge, textos de ajuda).
2. Evoluir `ProductInvestigationContextDto` (e DTOs relacionados) para expor os campos novos — **só agora**, de forma consolidada, não espalhado pelas fases anteriores (conforme pedido explícito: "não espalhar alteração do DTO por todas as fases").
3. Confirmar que a navegação do Ecossistema está coerente de ponta a ponta.
4. **Não reimplementar o Copiloto** — só garantir que o contexto que ele consulta está correto e completo.

## ESTADO ATUAL CONFIRMADO (antes das Fases 01-05 — reconfirme após elas rodarem)

- `ProductInvestigationContextDto` (`src/TraceCore.Application/DTOs/ProductTechnicalContextDtos.cs`) hoje expõe: `ProductId, ProductName, ProductCode, ProductDescription, IsExternal, SelectedVersion, TechnicalProfile, Technologies, Components, Dependencies, Integrations, TechnicalSources, ExternalResearchPolicy, AllowedDomains`.
- `IntegrationSummaryDto` (mesmo arquivo): hoje só `Id, Code, Name, IntegrationType, Status` — **não expõe** `Responsibility`/`HostingLocation`/`Direction` (campos que só existirão após a Fase 01/02).
- `ComponentSummaryDto`: hoje `Id, Name, ComponentType, Status` — já suficiente, `ComponentType` continua sendo string livre mesmo após virar catálogo administrável (a Fase 01 não muda o tipo do campo, só de onde vêm as opções na UI).
- `TechnicalProfile` (dentro do DTO consolidado) não tem `SystemType` até a Fase 04 implementar.
- `InvestigationCopilotService` (`src/TraceCore.Application/Services/InvestigationCopilotService.cs`) já consome `IProductTechnicalContextService.GetInvestigationContextAsync` através da tool `GetProductContext` — qualquer campo novo adicionado ao DTO consolidado já chega automaticamente ao Copiloto sem trabalho extra nessa camada, **desde que o texto serializado para o modelo (`ExecuteGetProductContextAsync`, no mesmo arquivo) também seja atualizado** para incluir os campos novos — hoje ele serializa só um subconjunto (`ProductName, ProductDescription, BusinessPurpose, ArchitectureSummary, Technologies, Components, Dependencies, Integrations, HasTechnicalProfile`). Releia esse método exato antes de decidir o que adicionar.

## ESCOPO

- Estender `IntegrationSummaryDto` com `Responsibility`, `HostingLocation`, `Direction` (da Fase 01/02).
- Estender o `TechnicalProfile` consolidado (ou o `ProductInvestigationContextDto` diretamente) com `SystemType` (da Fase 04).
- Atualizar `InvestigationCopilotService.ExecuteGetProductContextAsync` para incluir os campos novos no JSON que o LLM recebe — sem virar um "prompt pronto" (continuar sendo dado estruturado, princípio já estabelecido na iniciativa do Copiloto).
- Revisão de nomenclatura na UI: conferir que os rótulos usados nas Fases 02-05 (ex.: "Histórico de Execuções e Verificações", badges de `RunContext`, seção "Contexto Técnico Básico") estão consistentes entre si e com o resto do projeto.
- Revisão de navegação: conferir que o menu Ecossistema (corrigido na Fase 03) continua coerente depois de todas as mudanças, e que os links entre a tela de Sistema e as telas globais de Componentes/Integrações (Fase 04) fazem sentido nos dois sentidos.
- Testes cobrindo o DTO consolidado com os campos novos.

## NÃO ESCOPO

- Não alterar o loop de tool-calling do Copiloto, os prompts de sistema, nem a lógica de políticas de pesquisa externa — isso pertence à iniciativa "Nova Estratégia do Copiloto", já concluída, e não é o assunto deste plano de fases.
- Não criar campos novos de domínio — esta fase só expõe o que as Fases 01-05 já criaram.
- Não redesenhar telas — só ajustar textos/rótulos identificados como inconsistentes.

## ARQUIVOS A ANALISAR ANTES DE IMPLEMENTAR

```
src/TraceCore.Application/DTOs/ProductTechnicalContextDtos.cs
src/TraceCore.Application/Services/ProductTechnicalContextService.cs (método GetInvestigationContextAsync)
src/TraceCore.Application/Services/InvestigationCopilotService.cs (método ExecuteGetProductContextAsync)
src/TraceCore.Web/Pages/Shared/_Layout.cshtml
Resultado de todas as fases 01-05 (arquivos criados/alterados, conforme cada ENTREGA FINAL)
```

## ALTERAÇÕES ESPERADAS

### BANCO / MIGRATION

Nenhuma esperada — esta fase só lê e expõe dados já persistidos pelas fases anteriores.

### DOMAIN

Nenhuma mudança de entidade.

### APPLICATION

- `IntegrationSummaryDto`, `TechnicalProfile`/`ProductInvestigationContextDto`: campos novos conforme ESCOPO.
- `ProductTechnicalContextService.GetInvestigationContextAsync`: mapear os campos novos ao montar o DTO.
- `InvestigationCopilotService.ExecuteGetProductContextAsync`: incluir os campos novos no JSON retornado à ferramenta `GetProductContext`.

### INFRASTRUCTURE

Nenhuma mudança esperada, a menos que alguma query de leitura precise ser ajustada para trazer os campos novos junto (confirme durante a implementação).

### WEB

Só ajustes de rótulo/texto identificados na revisão — sem mudança estrutural de tela.

### AUDITORIA

Nenhuma ação nova.

### PERMISSÕES

Nenhuma mudança.

### TESTES

- `GetInvestigationContextAsync` retorna os campos novos corretamente para um produto totalmente preenchido (cenário de teste único e representativo: sistema com tipo, integração com responsabilidade/hospedagem/direção, componente com tipo do catálogo).
- `InvestigationCopilotService` (tool `GetProductContext`) inclui os campos novos no resultado retornado ao modelo — teste via o mesmo padrão de provedor de LLM roteirizado (fake) já usado nos testes existentes de `InvestigationCopilotIntegrationTests.cs`, se aplicável, ou verificação direta do JSON produzido por `ExecuteGetProductContextAsync`.

## CRITÉRIOS DE ACEITE

- Build limpo, suíte completa passando.
- `ProductInvestigationContextDto` reflete integralmente o que as Fases 01-05 construíram.
- Nenhuma tela redesenhada além de ajustes pontuais de texto.

## ENTREGA FINAL

1. arquivos alterados;
2. lista exata dos campos novos expostos no DTO consolidado e no contexto do Copiloto;
3. inconsistências de nomenclatura encontradas e corrigidas (lista antes/depois);
4. resultado do build e dos testes;
5. confirmação de que o Copiloto não foi reimplementado, só alimentado com mais contexto.

**NÃO pare após analisar. Implemente, execute build, execute testes, corrija erros até passar.**
