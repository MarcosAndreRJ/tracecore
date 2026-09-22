# FASE 03 — COMPONENTES E DEPENDÊNCIAS: EDIÇÃO, TIPO E NAVEGAÇÃO

> Depende da **Fase 01** (catálogo `ComponentType`). Confirme que a Fase 01 foi implementada (build/testes passando) antes de começar.

---

## CONTEXTO

`ComponentEntity` já pode ser editado no backend (`CatalogService.UpdateComponentAsync` existe e já é auditado), mas a UI (`Catalog/Components/Index.cshtml`) nunca expõe essa edição. O tipo de componente é uma lista fixa no HTML. O menu lateral tem "Componentes" e "Dependências" como dois itens que apontam para a **mesma URL**, com a **mesma regra de item ativo** — os dois acendem simultaneamente.

## OBJETIVO

1. Expor edição de componente na UI, reaproveitando o backend já existente.
2. Adicionar inativação de componente (sem exclusão física).
3. Trocar o `<select>` de Tipo de Componente pelo catálogo da Fase 01, incluindo "Módulo" como um dos tipos disponíveis (representando módulos funcionais como "Emissão de CT-e", distintos de componentes técnicos como "API"/"Banco de Dados").
4. Corrigir a navegação duplicada Componentes/Dependências no menu lateral.

## ESTADO ATUAL CONFIRMADO

- `src/TraceCore.Application/Services/CatalogService.cs:141-186` (aprox.) — `UpdateComponentAsync` já existe, já audita (`component.update`, confirmado por grep de `action: "component.update"`). **O backend está pronto.**
- `src/TraceCore.Web/Pages/Catalog/Components/Index.cshtml:164-201` — a tabela de componentes lista Código/Nome/Tipo/Status, **sem nenhuma coluna de Ações**, sem botão Editar, sem botão Inativar. Comparar com a tabela de Dependências (linhas 78-141), que tem coluna "Ações" com botão de remover — só falta o equivalente para Componentes.
- `Catalog/Components/Index.cshtml:296-304` — `<select id="newCompType">` com 6 opções fixas: `Service, Frontend, Database, Worker, Gateway, Integration`. Nenhuma opção "Módulo" existe hoje.
- `Catalog/Components/Index.cshtml:380-387` — `<select id="depType">` (Tipo de Comunicação/Dependência) também fixo: `Synchronous, Asynchronous, Database, External, SharedResource`. Note que o comentário da entidade `ComponentDependency.DependencyType` (`CatalogEntities.cs:114`) documenta `Synchronous, Asynchronous, Database, Messaging, External` — **o valor `SharedResource` usado na UI não bate com `Messaging` documentado na entidade**. Confirme qual é o conjunto de valores real em uso no banco (`SELECT DISTINCT dependency_type FROM component_dependencies`) antes de decidir manter, corrigir ou fazer isso também virar catálogo — mas trate esse valor como string aberta simples (não é candidato a catálogo administrável, é baixa cardinalidade e estável), só ajuste a inconsistência de nomenclatura.
- `src/TraceCore.Web/Pages/Shared/_Layout.cshtml:173-193` — confirmado: "Sistemas" (`/Catalog/Products/Index`), "Componentes" (`/Catalog/Components/Index`) e "Dependências" (`/Catalog/Components/Index` — **mesma URL de "Componentes"**) usam a mesma condição `currentPath.StartsWith("/Catalog/Components", ...)` para a classe `active`. Ao abrir a página, os dois itens "Componentes" e "Dependências" ficam marcados como ativos ao mesmo tempo — são dois links idênticos com rótulos diferentes.

## ESCOPO

- UI de edição de componente (modal "Editar Componente"), reaproveitando `ICatalogService.UpdateComponentAsync`.
- UI de inativação de componente (`Status = "Inactive"` via o mesmo `UpdateComponentAsync` — não precisa de método novo no backend, só uma ação na UI que chama update com status alterado).
- `<select>` de Tipo de Componente passa a listar o catálogo `component_types` da Fase 01 (que já inclui "Módulo Desktop" entre os valores seed).
- Corrigir o menu: remover a duplicidade Componentes/Dependências apontando para a mesma URL com a mesma condição de "ativo". Solução sugerida no pedido original — manter só um item de menu ("Componentes", já que a própria página se chama "Componentes & Dependências"), com Dependências existindo como seção/aba dentro da página (ela já é uma seção separada visualmente, só o item de menu duplicado precisa sumir). Você pode escolher outra solução (ex.: usar `#anchor` na URL para diferenciar), desde que os dois itens não fiquem simultaneamente "ativos" e a navegação continue clara.
- Testes cobrindo edição e inativação de componente pela camada de serviço.

## NÃO ESCOPO

- Não reconstruir o grafo de dependências (`ComponentDependency`) — o pedido original é explícito: "não reconstruir o grafo se a estrutura atual funcionar". Só ajustar a inconsistência de nomenclatura identificada acima, se aplicável.
- Não implementar "+ Adicionar Componente"/vínculo automático de `ProductId` a partir da tela de detalhe do Sistema — isso é Fase 04 (mas reaproveita o modal de edição criado aqui).
- Não mexer em `ComponentOwner` além do que já existe.

## ARQUIVOS A ANALISAR ANTES DE IMPLEMENTAR

```
src/TraceCore.Domain/Entities/CatalogEntities.cs
src/TraceCore.Application/Services/ICatalogService.cs / CatalogService.cs
src/TraceCore.Web/Pages/Catalog/Components/Index.cshtml / Index.cshtml.cs
src/TraceCore.Web/Pages/Shared/_Layout.cshtml (linhas ~165-207, grupo Ecossistema)
resultado da Fase 01 (catálogo component_types)
Docs/sql_mockup/*.sql (para checar valores reais hoje gravados em dependency_type, sem alterar o arquivo)
```

## ALTERAÇÕES ESPERADAS

### BANCO / MIGRATION

Nenhuma esperada nesta fase, a menos que a investigação de `dependency_type` acima revele necessidade de normalizar dados existentes — se isso acontecer, trate como `UPDATE` de dados dentro de uma migration aditiva, documentando exatamente o que foi normalizado e por quê.

### DOMAIN

Nenhuma mudança de entidade esperada.

### APPLICATION

Nenhum método novo deve ser necessário — reaproveitar `UpdateComponentAsync` já existente. Se durante a implementação você perceber necessidade real de um método dedicado (ex.: `DeactivateComponentAsync`), pode criar, mas justifique por que o `UpdateComponentAsync` genérico não bastou.

### INFRASTRUCTURE

Nenhuma mudança esperada — camada já suporta tudo que esta fase precisa.

### WEB

- `Catalog/Components/Index.cshtml.cs`: `EditComponentInput`, handler `OnPostUpdateComponentAsync`; ação de inativação (pode ser um botão simples que reenvia o mesmo handler com `Status=Inactive`, ou um handler dedicado — decida pela consistência com o resto do projeto).
- `Catalog/Components/Index.cshtml`: coluna "Ações" na tabela de componentes com botões Editar/Inativar; modal "Editar Componente"; `<select>` de tipo alimentado por `Model.ComponentTypesList` (novo, vindo do catálogo da Fase 01) em vez da lista fixa.
- `_Layout.cshtml`: corrigir a duplicidade do menu conforme decidido em ESCOPO.

### AUDITORIA

Nenhuma ação nova além da já existente `component.update` (que já cobre tanto edição quanto inativação, já que ambas passam pelo mesmo método).

### PERMISSÕES

Reutilizar `catalogo.gerenciar`.

### TESTES

- Editar componente persiste os campos e mantém auditoria.
- Inativar componente muda `Status` sem apagar dependências/histórico associado (reforçar a regra "não deletar histórico" do pedido original, mesmo que a exclusão física nunca tenha sido implementada — o teste serve para deixar isso explícito e protegido contra regressão futura).
- `<select>` de tipo reflete o catálogo da Fase 01.

## CRITÉRIOS DE ACEITE

- Build limpo, suíte completa passando.
- É possível editar e inativar um componente pela UI real (teste manual: subir o app, logar, editar um componente existente).
- Ao visitar `/Catalog/Components/Index`, só um item do menu "Ecossistema" aparece marcado como ativo.

## ENTREGA FINAL

1. arquivos criados/alterados;
2. confirmação de que a edição/inativação de componente usa o backend já existente sem duplicar lógica;
3. como o menu duplicado foi resolvido;
4. o que foi encontrado sobre `dependency_type`/`SharedResource` vs. `Messaging` e o que (se algo) foi feito a respeito;
5. resultado do build e dos testes;
6. confirmação de teste manual na aplicação rodando.

**NÃO pare após analisar. Implemente, execute build, execute testes, corrija erros até passar.**
