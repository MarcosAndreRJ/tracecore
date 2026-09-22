# FASE 04 — SISTEMAS: CADASTRO INICIAL, EDIÇÃO E RELACIONAMENTOS NA TELA DE DETALHE

> Depende das **Fases 01, 02 e 03** (catálogos, edição de integração, edição de componente). Confirme que todas foram implementadas (build/testes passando) antes de começar — esta fase reaproveita o que elas construíram.

---

## CONTEXTO

O cadastro inicial de "Novo Sistema" (`Product`) tem só 4 campos (Nome, Código, Descrição, Externo). O contexto técnico rico (finalidade, arquitetura, tecnologias, banco, hospedagem) já existe — mas só é acessível depois de criar o sistema, entrando na página de Detalhes. A página de Detalhes (`Catalog/Products/Details.cshtml`) já tem abas Geral/Contexto Técnico/Componentes/Dependências/Integrações/Fontes, mas as abas Componentes e Integrações são **somente leitura** — só mostram um resumo e um link para a tela global.

## OBJETIVO

1. Enriquecer o cadastro inicial de sistema com uma seção básica de contexto técnico.
2. Igualar edição e criação (editar não pode expor menos campos do que criar).
3. Tornar as abas Componentes e Integrações da tela de Detalhe operacionais (adicionar/editar/desvincular diretamente de lá), com `ProductId` pré-preenchido.

## ESTADO ATUAL CONFIRMADO

- `src/TraceCore.Web/Pages/Catalog/Products/Index.cshtml.cs:36-52` — `CreateProductInput` e `EditProductInput` têm exatamente os mesmos 4-5 campos: `Name, Code, Description, (Status,) IsExternal`. Confirma o pedido original: editar não expõe mais nem menos do que criar hoje — **mas nenhum dos dois tem contexto técnico**.
- `src/TraceCore.Application/Services/ICatalogService.cs:12-13` — `CreateProductAsync`/`UpdateProductAsync` só recebem esses mesmos campos. Não criam/atualizam `ProductTechnicalProfile`.
- `src/TraceCore.Web/Pages/Catalog/Products/Details.cshtml.cs` (criado no Prompt 2 do Copiloto) — já tem `IProductTechnicalContextService` injetado, já tem `OnPostSaveProfileAsync`/`OnPostSetTechnologiesAsync`/`OnPostAddSourceAsync`/`OnPostAddDomainAsync` funcionando. A aba "Contexto Técnico" já é 100% funcional — **reaproveitar, não recriar**.
- `Details.cshtml` — abas "Componentes" e "Integrações" (ver o arquivo) hoje só renderizam `ctx.Components`/`ctx.Integrations` (vindos de `ProductInvestigationContextDto`, somente leitura) com um link "Gerenciar Componentes"/"Gerenciar Integrações" para a tela global. Não há formulário de criação/edição embutido.
- `ProductTechnicalProfile` (`src/TraceCore.Domain/Entities/ProductTechnicalContextEntities.cs`) já tem `BusinessPurpose, ArchitectureSummary, FrontendStack, BackendStack, PrimaryDatabase, HostingModel` — cobrem "Finalidade", "Resumo da arquitetura", "Tecnologia principal", "Banco principal" e "Hospedagem" pedidos para o cadastro inicial. **Não existe campo "Tipo do Sistema"** (Desktop/Web/Mobile/API/Serviço/SaaS) em nenhuma entidade hoje — nem em `Product`, nem em `ProductTechnicalProfile`.
- `Integration.ProductId` já existe e já é editável de ponta a ponta depois da Fase 02. `ComponentEntity.ProductId` já existe e já é editável depois da Fase 03.

## DECISÃO CONCEITUAL — ONDE VIVE "TIPO DO SISTEMA"

Avaliado conforme o pedido original (seção 18: "avaliar se isso deve ser catálogo ou valor controlado simples"): lista curta e estável (Desktop, Web, Mobile, API, Serviço, SaaS, Outro), sem necessidade de administração dinâmica pela empresa como `ComponentType`/`IntegrationType`. **Decisão: string controlada simples**, não catálogo. Local: novo campo `SystemType` em `ProductTechnicalProfile` (não em `Product`) — porque é informação de contexto técnico, mesma natureza de `HostingModel`/`RuntimePlatform` que já vivem ali, e porque `Product` foi deliberadamente mantido enxuto em decisões anteriores do projeto (não acrescentar coluna a `products` sem necessidade).

Isso implica: o cadastro inicial de "Novo Sistema" passa a, na mesma submissão, criar o `Product` **e** o `ProductTechnicalProfile` inicial (hoje só o `Product` é criado; o perfil técnico só existe se o usuário for até Detalhes e preencher). Trate isso como uma operação orquestrada (duas chamadas de serviço já existentes, `CreateProductAsync` + `UpsertTechnicalProfileAsync`, dentro do mesmo handler), não como um novo método que duplica lógica.

## ESCOPO

- Migration aditiva: `product_technical_profiles.system_type VARCHAR(50) NULL`.
- Modal "Novo Sistema" reestruturado em duas seções: **Identificação** (campos atuais) e **Contexto Técnico Básico** (Tipo do Sistema, Tecnologia principal, Banco principal, Hospedagem, Finalidade, Resumo da arquitetura) — todos opcionais, sem bloquear criação rápida.
- Modal "Editar Sistema" espelhando os mesmos campos (identificação + contexto técnico básico), carregando o perfil técnico existente se houver.
- Aba "Componentes" da tela de Detalhe ganha "+ Adicionar Componente" (reaproveitando `CreateComponentAsync` já existente, com `ProductId` pré-preenchido e oculto) e, por linha, "Editar"/"Inativar"/"Desvincular" (reaproveitando o que a Fase 03 construiu).
- Aba "Integrações" da tela de Detalhe ganha "+ Nova Integração" (reaproveitando `CreateIntegrationAsync`, com `ProductId` pré-preenchido) e "+ Vincular Existente" (lista integrações sem produto ou de outro produto, permite associar via `UpdateIntegrationAsync` da Fase 02) e, por linha, "Editar"/"Desvincular"/"Ver Histórico"/"Testar" (reaproveitando o que a Fase 02 construiu).
- "Desvincular" nunca exclui — sempre `ProductId = NULL` mantendo o registro e seu histórico.

## NÃO ESCOPO

- Não mexer em `IntegrationRun`/contexto de diagnóstico — Fase 05.
- Não renomear `Product` para `System`/nada equivalente na arquitetura — manter `Product` no domínio, "Sistema" só na UI (já é o padrão hoje, preservar).
- Não redesenhar a estrutura de abas da página de Detalhes — ela já existe e funciona, só as abas Componentes/Integrações ganham interatividade.

## ARQUIVOS A ANALISAR ANTES DE IMPLEMENTAR

```
src/TraceCore.Domain/Entities/ProductTechnicalContextEntities.cs
src/TraceCore.Application/Services/IProductTechnicalContextService.cs / ProductTechnicalContextService.cs
src/TraceCore.Application/Services/ICatalogService.cs / CatalogService.cs
src/TraceCore.Application/DTOs/ProductTechnicalContextDtos.cs
src/TraceCore.Web/Pages/Catalog/Products/Index.cshtml / Index.cshtml.cs
src/TraceCore.Web/Pages/Catalog/Products/Details.cshtml / Details.cshtml.cs
resultado da Fase 02 (edição de integração) e Fase 03 (edição de componente)
src/TraceCore.Infrastructure/Migrations/M20260919_22_CreateProductTechnicalContextSchema.cs (schema atual de product_technical_profiles)
```

## ALTERAÇÕES ESPERADAS

### BANCO / MIGRATION

```sql
ALTER TABLE product_technical_profiles ADD COLUMN system_type VARCHAR(50) NULL;
```
Migration nova (descubra o número seguinte real), aditiva, sem tocar nas anteriores.

### DOMAIN

- `ProductTechnicalProfile`: adicionar `SystemType` (string?), com lista de valores sugeridos validada em `Application` (mesmo padrão de `ValidExternalResearchPolicies`), sem travar o banco com enum fechado.

### APPLICATION

- `UpsertProductTechnicalProfileCommand`: adicionar `SystemType`.
- Novo fluxo de criação orquestrada: avalie se cabe como um novo método em `ICatalogService` (`CreateProductWithTechnicalProfileAsync` ou nome equivalente) que internamente chama `CreateProductAsync` + `IProductTechnicalContextService.UpsertTechnicalProfileAsync`, ou se a orquestração fica só na página (`Details.cshtml.cs`/`Index.cshtml.cs` chamando os dois serviços em sequência). Decida pela consistência com o resto do projeto (services não costumam depender uns dos outros por injeção cruzada hoje — `CatalogService` não conhece `IProductTechnicalContextService`) — a opção mais alinhada ao padrão atual é orquestrar na página (`PageModel`), não dentro do `CatalogService`. Documente a decisão tomada.

### INFRASTRUCTURE

Nenhuma mudança além do necessário para persistir a nova coluna (`MySqlProductTechnicalContextRepository`/`InMemoryProductTechnicalContextRepository` já fazem upsert do perfil inteiro — só adicionar o campo ao mapeamento).

### WEB

- `Catalog/Products/Index.cshtml.cs`: `CreateProductInput`/`EditProductInput` ganham os campos de contexto técnico básico; handlers passam a orquestrar `Product` + `ProductTechnicalProfile`.
- `Catalog/Products/Index.cshtml`: modais "Novo Sistema"/"Editar Sistema" com as duas seções.
- `Catalog/Products/Details.cshtml.cs`: novos handlers `OnPostAddComponentAsync`, `OnPostEditComponentAsync`, `OnPostDeactivateComponentAsync`, `OnPostUnlinkComponentAsync`, `OnPostAddIntegrationAsync`, `OnPostLinkExistingIntegrationAsync`, `OnPostUnlinkIntegrationAsync` (nomes sugeridos, ajuste conforme o padrão já usado no arquivo).
- `Catalog/Products/Details.cshtml`: abas Componentes/Integrações ganham os formulários/ações inline, reaproveitando os componentes visuais (`tc-card`, `tc-badge`, `tc-btn`, modais) já usados nas outras abas da mesma página.

### AUDITORIA

`product_technical_profile.upsert` já existe (Prompt 2 do Copiloto) e cobre a criação orquestrada. Novas ações: `component.unlink_from_product` (se distinto de inativar), `integration.link_to_product`/`integration.unlink_from_product` (pode já existir da Fase 02 — reaproveitar).

### PERMISSÕES

Reutilizar `catalogo.gerenciar` (sistema/componentes) e `integracao.gerenciar` (ações de integração dentro da tela de sistema — confirme se faz sentido exigir as duas permissões para a aba Integrações do Sistema, ou se `catalogo.gerenciar` basta por já ser a permissão que gate toda a página de Detalhes; documente a decisão).

### TESTES

- Criar sistema com contexto técnico básico preenchido cria `Product` + `ProductTechnicalProfile` numa única operação.
- Criar sistema sem contexto técnico básico (só identificação) continua funcionando — nenhum campo novo é obrigatório.
- Editar sistema atualiza tanto `Product` quanto `ProductTechnicalProfile`.
- Adicionar componente pela aba do sistema já vem com `ProductId` correto.
- Adicionar/vincular integração pela aba do sistema já vem com `ProductId` correto; vincular existente reatribui `ProductId` sem apagar histórico.
- `GetInvestigationContextAsync` (`IProductTechnicalContextService`, usado pelo Copiloto) continua funcionando e agora também expõe `SystemType` quando presente — **não é necessário** alterar `ProductInvestigationContextDto` nesta fase a menos que já esteja natural fazê-lo (ver Fase 06, que é o momento correto para revisar o DTO de forma consolidada); se dor de implementação exigir tocar o DTO aqui, documente exatamente o que mudou.

## CRITÉRIOS DE ACEITE

- Build limpo, suíte completa passando.
- Teste manual completo na aplicação real: criar um sistema com contexto técnico básico, editar, adicionar um componente pela aba do sistema, adicionar uma integração pela aba do sistema, desvincular ambos sem perder histórico.
- Nenhuma regressão nas abas Geral/Contexto Técnico/Dependências/Fontes já existentes.

## ENTREGA FINAL

1. arquivos criados/alterados;
2. migration criada e coluna resultante;
3. decisão tomada sobre onde orquestrar Product+TechnicalProfile;
4. confirmação de que as abas Componentes/Integrações da tela de Detalhe agora são interativas;
5. resultado do build e dos testes;
6. confirmação de teste manual na aplicação rodando;
7. se o `ProductInvestigationContextDto` foi tocado nesta fase, por quê.

**NÃO pare após analisar. Implemente, execute build, execute testes, corrija erros até passar.**
