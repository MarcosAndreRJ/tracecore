# FASE 02 — INTEGRAÇÕES: CADASTRO, EDIÇÃO E RELACIONAMENTO COM SISTEMA

> Depende da **Fase 01** (catálogos `ComponentType`/`IntegrationType`, campos `Responsibility`/`HostingLocation`/`Direction`, auditoria em `IntegrationService`). Confirme que a Fase 01 foi de fato implementada (build/testes passando) antes de começar.

---

## CONTEXTO

O módulo de Integrações (`Pages/Integrations/Index.cshtml`) permite criar, mudar status, configurar health-check e registrar execução manual — mas **não permite editar** a integração depois de criada, **não permite escolher o sistema** relacionado ao criar, e usa tipo/responsabilidade/hospedagem/direção fixos ou ausentes. A Fase 01 já criou a base de dados para resolver isso. Esta fase entrega a funcionalidade de verdade.

## OBJETIVO

1. Implementar edição completa da integração.
2. Adicionar seleção de Sistema (`ProductId`) no formulário "Nova Integração" — hoje ausente apesar do campo já existir no domínio.
3. Trocar os `<select>` fixos de "Tipo de Integração" pelos catálogos da Fase 01.
4. Adicionar campos de Responsabilidade/Hospedagem/Direção ao formulário.
5. Revisar a nomenclatura "Histórico de Execuções" para refletir que hoje mistura execução manual, execução automática e verificação técnica (health-check).

## ESTADO ATUAL CONFIRMADO

- `src/TraceCore.Application/Services/IIntegrationService.cs:8-16` — interface tem `CreateIntegrationAsync`, `UpdateIntegrationStatusAsync` (só status), `ConfigureHealthCheckAsync` (só health-check), `RegisterRunAsync`. **Não existe `UpdateIntegrationAsync`** para editar nome/código/tipo/produto/descrição/responsável/notas de contrato nesta camada.
- **Achado importante que reduz o escopo desta fase**: `IIntegrationRepository.UpdateIntegrationAsync` (Domain) e sua implementação em `MySqlIntegrationRepository.cs:129-151` **já existem e já atualizam TODOS os campos**, incluindo `product_id` (a coluna já é gravada no UPDATE, linha 139). Ou seja, o repositório está pronto — o que falta é só a camada `IIntegrationService` (método novo que monta a entidade e chama o repositório) e a UI. Não recrie o UPDATE no repositório; só confirme se `InMemoryIntegrationRepository` (em `InMemoryRepositories.cs`) tem o equivalente — se não tiver, complete-o para não quebrar os testes que rodam contra o provider InMemory.
- `src/TraceCore.Web/Pages/Integrations/Index.cshtml:309-374` — modal "Nova Integração" pede Código, Nome, Tipo (fixo), Sistema Alvo/Descrição (texto livre), Departamento Responsável, Notas de Contrato. **Não tem campo de Sistema/Produto**, mesmo `Integration.ProductId` existindo no domínio desde a Fase de Contexto Técnico do Copiloto.
- Não existe nenhum modal/botão "Editar Integração" na página.
- `Integrations/Index.cshtml:196-198` já rotula a seção como "Histórico de Execuções" contendo, misturados: execuções via "Registrar Execução" (manual, `TriggeredBy=Manual`) e testes de health-check via botão "Testar Health-Check" (que dispara `IIntegrationHealthCheckService.ExecuteHealthCheckAsync`, gravando `IntegrationRun` com `TriggeredBy=Automated`). Confirme se ambos aparecem na mesma tabela ao implementar — pela leitura do `.cshtml`, sim (mesma coleção `integration.Runs`).

## ESCOPO

- `UpdateIntegrationAsync` completo no backend (Domain→Infrastructure→Application), reaproveitando `Integration` e o repositório já existentes.
- Modal "Editar Integração" espelhando os campos de "Nova Integração" mais os novos da Fase 01.
- Campo de seleção de Sistema (`ProductId`) em "Nova Integração" **e** em "Editar Integração", com opção explícita "Sem sistema associado" (`NULL` continua válido — integração pode ser global).
- `<select>` de Tipo de Integração passa a ser preenchido a partir do catálogo `integration_types` da Fase 01 (não mais lista fixa no `.cshtml`).
- Campos de Responsabilidade, Hospedagem, Direção no formulário (usando os valores controlados definidos na Fase 01).
- Renomear/reestruturar visualmente a seção "Histórico de Execuções" — sugestão do pedido original: **"Histórico de Execuções e Verificações"**, com uma coluna ou badge deixando claro se a linha veio de execução manual, execução automatizada ou teste de health-check (o campo `IntegrationRun.TriggeredBy` já existe — a UI só precisa exibir melhor). Avalie durante a implementação se um rótulo diferente comunica melhor; documente a decisão final na entrega.
- "Desvincular do Sistema" como ação distinta de excluir (`ProductId = NULL`, mantendo a integração e seu histórico intactos).

## NÃO ESCOPO

- Não mexer em `IntegrationRun` para adicionar contexto de diagnóstico (`RunContext`, ligação com `CaseId`/`DiagnosticStep`) — isso é Fase 05.
- Não implementar "Nova Integração"/"Vincular Existente" dentro da tela de detalhe do Sistema — isso é Fase 04 (mas a Fase 04 vai reutilizar o `UpdateIntegrationAsync`/formulário criados aqui, então mantenha o design reaproveitável: parâmetros de `ProductId` pré-selecionável).
- Não corrigir a falta de proteção SSRF em `IntegrationHealthCheckService` — fora do escopo desta rodada de fases, mas **se você tocar no fluxo de configuração de health-check nesta fase, reaproveite `ExternalUrlSafetyValidator`** (`src/TraceCore.Infrastructure/Services/ExternalResearch/ExternalUrlSafetyValidator.cs`, criado na Fase 4 do Copiloto) para validar a URL informada antes de salvar — é um componente já pronto e testado, só falta ser chamado aqui. Se decidir não fazer isso agora, documente explicitamente como pendência na entrega final; não ignore silenciosamente.

## ARQUIVOS A ANALISAR ANTES DE IMPLEMENTAR

```
src/TraceCore.Domain/Entities/IntegrationEntities.cs
src/TraceCore.Domain/Repositories/IIntegrationRepository.cs
src/TraceCore.Application/Services/IIntegrationService.cs / IntegrationService.cs
src/TraceCore.Application/DTOs/IntegrationDtos.cs
src/TraceCore.Infrastructure/Persistence/Repositories/MySqlIntegrationRepository.cs
src/TraceCore.Infrastructure/Persistence/InMemory/InMemoryRepositories.cs (InMemoryIntegrationRepository)
src/TraceCore.Web/Pages/Integrations/Index.cshtml / Index.cshtml.cs
src/TraceCore.Web/Pages/Catalog/Products/Index.cshtml.cs   (padrão de modal Criar/Editar já usado no projeto — reaproveitar o mesmo estilo)
src/TraceCore.Infrastructure/Services/ExternalResearch/ExternalUrlSafetyValidator.cs   (se for reaproveitar para o health-check)
resultado da Fase 01 (arquivos de catálogo criados)
```

## ALTERAÇÕES ESPERADAS

### BANCO / MIGRATION

Só se a Fase 01 não tiver coberto tudo que esta fase precisa (confira antes). Se precisar de coluna nova aqui, siga o mesmo padrão: aditiva, migration nova, nunca alterar as anteriores.

### DOMAIN

`Integration` já tem os campos necessários após a Fase 01 (`ProductId`, `IntegrationType`, `Responsibility`, `HostingLocation`, `Direction`). Nenhuma mudança de entidade esperada aqui além de eventuais ajustes finos identificados durante a implementação.

### APPLICATION

- `IIntegrationService`: adicionar `UpdateIntegrationAsync(UpdateIntegrationCommand command, long? currentUserId, CancellationToken ct)` — busca a integração via `GetIntegrationByIdAsync`, aplica os campos editáveis, chama `IIntegrationRepository.UpdateIntegrationAsync` (já existe, ver achado acima), audita.
- `UpdateIntegrationCommand` (novo DTO): Id, Code, Name, IntegrationType, ProductId, TargetSystemDescription, OwnerDepartmentId, ContractNotes, Responsibility, HostingLocation, Direction.
- Auditar `integration.update` com `before`/`after` (padrão já estabelecido na Fase 01).

### INFRASTRUCTURE

- Nada a fazer aqui: `InMemoryIntegrationRepository.UpdateIntegrationAsync` (em `InMemoryRepositories.cs`) já existe e já está completo, assim como o `MySqlIntegrationRepository` (ver achado acima). Toda a Fase 02 acontece em `IIntegrationService`/`IntegrationService` (Application) e na UI (Web).

### WEB

- `Integrations/Index.cshtml.cs`: `EditIntegrationInput` (espelhando `NewIntegration`, mais os campos novos), handler `OnPostUpdateIntegrationAsync`.
- `Integrations/Index.cshtml`: 
  - modal "Nova Integração" ganha `<select>` de Sistema (`Model.ProductsList`, análogo ao já usado em `Catalog/Components/Index.cshtml:307-315`) e `<select>`s de Tipo/Responsabilidade/Hospedagem/Direção;
  - novo modal "Editar Integração", acessível por um botão "Editar" em cada card de integração;
  - botão "Desvincular do Sistema" quando a integração tiver `ProductId` preenchido;
  - ajuste de rótulo/visual da seção de histórico conforme decidido em ESCOPO.
- Preservar padrão visual (`tc-card`, `tc-btn`, `tc-input`, `tc-select`, `tc-badge`, modais Bootstrap) — sem redesenho.

### AUDITORIA

`integration.update`, `integration.unlink_from_product`.

### PERMISSÕES

Reutilizar `integracao.gerenciar` — não criar nova.

### TESTES

- Editar integração persiste todos os campos.
- Criar integração com `ProductId` selecionado persiste corretamente.
- Desvincular integração de sistema zera `ProductId` sem apagar `IntegrationRun`s associados.
- Auditoria de `integration.update` registrada com `before`/`after`.
- `GetInvestigationContextAsync` (Prompt 2 do Copiloto, `IProductTechnicalContextService`) continua retornando a integração corretamente após edição — não deve quebrar a listagem de integrações por produto já usada pelo Copiloto.

## CRITÉRIOS DE ACEITE

- Build limpo, suíte de testes completa passando (não só os testes novos).
- É possível criar uma integração já associada a um sistema, editar todos os seus campos depois, e desvincular sem perder histórico.
- `<select>` de tipo de integração reflete o catálogo administrável da Fase 01, não mais uma lista fixa no HTML.
- Teste manual na aplicação real (subir o app, logar, criar/editar/desvincular uma integração pela UI) antes de declarar concluído — não basta passar nos testes automatizados.

## ENTREGA FINAL

1. arquivos criados/alterados;
2. confirmação de que `UpdateIntegrationAsync` existe ponta a ponta (Domain→Web);
3. como o formulário agora usa os catálogos da Fase 01;
4. como ficou a nomenclatura da seção de histórico e por quê;
5. decisão tomada sobre reaproveitar ou não `ExternalUrlSafetyValidator` no health-check;
6. resultado do build e dos testes;
7. confirmação de teste manual na aplicação rodando.

**NÃO pare após analisar. Implemente, execute build, execute testes, corrija erros até passar.**
