Memória atualizada. Agora o prompt da Fase 7:

```text
Você está desenvolvendo a Plataforma Corporativa de Conhecimento, Diagnóstico e Lições
Aprendidas (projeto TraceCore). Fases 1 a 6 executadas e verificadas no código real
(migrations M01 a M08 em src/TraceCore.Infrastructure/Migrations). UI real é ASP.NET
Core Razor Pages + Bootstrap. Layout: cards com borda de destaque colorida, badges,
modais Bootstrap, duas colunas — nunca CRUD genérico.

Leia primeiro, nesta ordem:
1. 01_REGRAS_DE_NEGOCIO.md (BR-060 a BR-065)
2. 03_MODULOS_DO_SISTEMA.md (M06 — Pesquisa e Similaridade)
3. 10_MODELO_DE_DADOS.md (grupo "Busca/IA": search_sessions, search_queries,
   search_result_interactions)
4. 25_MATRIZ_PERFIS_PERMISSOES.md
5. src/TraceCore.Web/Pages/Shared/_Layout.cshtml (linha ~297-302 — já existe uma
   caixa de busca global `#tc-global-search` com atalho Ctrl+K, hoje decorativa/sem
   destino — é nela que a Fase 7 se conecta)
6. src/TraceCore.Web/Security/PermissionAuthorizationHandler.cs e
   PermissionPolicyProvider.cs (entender o mecanismo de permissão antes de mexer nos
   seeds)

TAREFA: implementar a Fase 7 — Pesquisa (M06). NÃO implementar ainda: busca
semântica/embeddings (M11, ADR-P006/P007), motor de diagnóstico adaptativo (M07).

## 0. Ajustes pendentes reportados pelo usuário (fazer ANTES do Bloco 7.x)

### 0.1 — Usuário de desenvolvimento com poder total
O seed `admin@tracecore.local` só tem o papel "Admin Segurança", que por desenho
(25_MATRIZ_PERFIS_PERMISSOES.md) NÃO inclui `caso.criar`/`caso.editar`/
`caso.diagnosticar`/`solucao.*` — isso é RBAC correto para esse papel, mas trava o
teste manual (ex.: `/Cases/Create` retorna 403). Criar migration nova:
- Novo papel `Super Admin (Dev)` (nome claramente identificado como exceção —
  25_MATRIZ já orienta que superadmin deve ser "excepcional, auditado e
  preferencialmente temporário").
- Conceder TODAS as permissões a esse papel de forma DINÂMICA
  (`INSERT INTO role_permissions SELECT r.id, p.id FROM roles r, permissions p
  WHERE r.name = 'Super Admin (Dev)'` — nunca uma lista hardcoded, para continuar
  completo conforme novas permissões forem criadas em fases futuras, incluindo as
  que esta própria Fase 7 for adicionar).
- Vincular `admin@tracecore.local` a esse papel ADICIONALMENTE ao "Admin Segurança"
  já existente (não remover o papel original).
- Confirmar que o código que monta as claims de permissão no login agrega
  corretamente as permissões de TODOS os papéis do usuário (não só do primeiro
  encontrado) — se não agregar, corrigir isso é parte desta tarefa.

### 0.2 — Não confundir "sem permissão" com "ainda não implementado"
O mecanismo de permissão em si está correto (BR-102). O problema é só que páginas
stub/mockup (`Pages/Placeholder.cshtml`, `Pages/Diagnosis/Index.cshtml`) não podem
depender da tela `/acesso-negado` (403) para sinalizar "isso não existe ainda".
Audite essas páginas: se estiverem atrás de `[Authorize(Policy="...")]` para uma
permissão que não corresponde a uma funcionalidade real ainda, troque por um estado
"Em Construção" explícito usando a partial `_EmptyState.cshtml` já existente, sem
depender do fluxo de autorização para comunicar isso.

### 0.3 — Harmonia da tela de login
Em `Pages/Login.cshtml`, remover (ou reposicionar de forma que faça sentido) o texto
solto "Controle RBAC" que hoje flutua ao lado do rótulo "Senha de Acesso" sem
propósito claro — está quebrando o equilíbrio visual do formulário (ver print
enviado pelo usuário). É um ajuste pequeno e pontual, não uma redesenho da tela.
NÃO mexer no aviso de credenciais de desenvolvimento (`Docs/DEV_CREDENTIALS.md`,
gated por `Env.IsDevelopment()`) — o usuário pediu explicitamente para deixar como
está durante o desenvolvimento.

### 0.4 — "PostgreSQL" incorreto no dashboard
Em `Pages/Index.cshtml` (~linha 304) e
`Pages/Shared/Partials/_SystemDependencyGraph.cshtml` (~linha 45), o texto mockado
do card "Cadeia de Dependência Ativa" mostra "PostgreSQL Core DB" /
"PostgreSQL / Persistence". O projeto usa MySQL (ADR-0002). Trocar os literais para
"MySQL Core DB" / "MySQL / Persistence". Esse card inteiro ainda é dado mockado (não
está ligado a `component_dependencies` da Fase 2) — não precisa religar para dado
real agora, só corrigir o texto errado; registre `TODO` citando M08/dashboard para
quando essa seção for religada a dado real.

### 0.5 — Regras operacionais gerais (valem a partir de agora)
- NÃO fazer commit — o projeto fica intencionalmente sem commit por enquanto. Não
  perca tempo checando `git status`/`git log` como sinal de verificação; para saber
  se algo já foi implementado de verdade, leia o código/migrations diretamente.
- AO FINAL da fase, deixe o `TraceCore.Web` DE FATO RODANDO (não só compilado) para
  o usuário testar imediatamente — suba o processo (`dotnet run` ou equivalente) e
  confirme que a URL raiz responde antes de encerrar.
- Fase 7 é ÍMPAR: sem exigência de testes automatizados nesta fase (cadência
  combinada — cobertura fica para a Fase 8, junto com o que a Fase 7 deixar
  pendente).

## 1. Decisões de modelagem

- **ID strategy:** `BIGINT` autoincremento, consistente com o restante do projeto.
- **Busca é federada por tipo de entidade, não uma tabela única de índice:** a
  consulta faz fan-out real contra `cases` (FULLTEXT já existe:
  `original_report`/`normalized_summary`/`error_message`), `knowledge_items` +
  `knowledge_versions` (FULLTEXT já existe: `title`/`summary`/`content_markdown`),
  `products` (sistemas), `components`, `root_causes` (causas), e valores distintos
  de `error_code` em `cases` (erros). Não construa uma tabela de índice denormalizada
  nesta fase — isso é exatamente o gancho que fica para ADR-P006 (engine vetorial)
  se um dia for necessário.
- **`search_result_interactions` (citada em 10_MODELO_DE_DADOS.md sem colunas
  detalhadas) fecha agora:** id, search_query_id, result_type
  (`Case`/`Solution`/`System`/`Component`/`Error`/`RootCause`), result_id, position,
  opened_at nullable, feedback_useful nullable (bool) — cobre BR-064 ("resultado
  exibido, item aberto e feedback de utilidade").
- **Contexto de origem opcional (`contextCaseId`) alimenta o ranking (Bloco 7.3):**
  quando a busca é disparada de dentro de um caso (ex.: durante investigação,
  Fase 4), passar o `case_id` de origem para dar boost a resultados que compartilham
  produto/componente/versão/error_code com esse caso — é isso que operacionaliza
  "mesmo sistema + mesmo módulo + mesma versão + mesma mensagem de erro" do Bloco
  7.3. Busca fora de um caso (barra global do header) funciona sem esse contexto,
  só com relevância textual + filtros explícitos.
- **BR-062 (penalizar/ocultar incompatibilidade de versão/ambiente):** usar
  `knowledge_applicability.applicability_type` — valor `DoesNotApply` (catálogo
  aberto já existente desde a Fase 6) para excluir/penalizar explicitamente um
  resultado de conhecimento quando o filtro de versão/ambiente da busca colide com
  uma aplicabilidade negativa declarada.

## 2. Modelo de dados desta fase

Migration versionada (mesma ferramenta das fases anteriores), além da seção 0.1:

- `search_sessions` — já documentada (10_MODELO_DE_DADOS.md): id, user_id nullable,
  started_at, context_json nullable.
- `search_queries` — já documentada: id, search_session_id, query_text, filters_json,
  result_count, duration_ms, executed_at. `result_count = 0` deve ser sempre
  persistido corretamente (BR-065 — busca sem resultado precisa ser mensurável;
  não implementar o relatório de lacunas agora, só garantir que o dado fica
  disponível para uma fase futura de analytics consultar).
- `search_result_interactions` — conforme seção 1.

Índices mínimos: `search_queries(executed_at)`, `search_result_interactions
(search_query_id)`.

## 3. Bloco 7.1 — Pesquisa textual

- `SearchQuery` (Application) recebendo `freeText`, filtros (seção 4),
  `contextCaseId` opcional, paginação.
- Handler federado: dispara sub-consultas por tipo (FULLTEXT nas tabelas que já têm
  índice; `LIKE`/prefix em `products`/`components`/`root_causes` que ainda não têm
  FULLTEXT — não adicionar FULLTEXT novo nesta fase se não houver no schema atual,
  registrar `TODO` se a performance exigir depois), cada um retornando um
  `SearchResultItem` com `Type` (`Case`/`Solution`/`System`/`Component`/`Error`/
  `RootCause`), `Id`, `Title`, `Snippet`, `Score`, `MatchedFactors` (lista curta —
  BR-061: explicar por que aquele resultado apareceu, ex.: "mesmo error_code",
  "título contém o termo").
- Persistir `search_sessions`/`search_queries` a cada busca de verdade disparada
  pelo usuário (não a cada tecla digitada) — BR-064.
- Autorizar por `caso.visualizar` como piso mínimo (resultados de tipo `Case`
  respeitam ainda a autorização normal de conteúdo restrito, se aplicável — BR-102);
  resultados de `Solution` só aparecem se publicados, exceto para quem tiver
  `solucao.validar`/`solucao.publicar` (rascunhos visíveis a quem pode revisá-los).

## 4. Bloco 7.2 — Filtros

Implementar filtros combináveis exatamente como listado: sistema (product_id),
versão (product_version_id), cliente (client_id), departamento (department_id via
`current_department_id`/`owner_department_id` conforme o tipo de resultado),
tecnologia (technology_id, via `knowledge_technologies` para soluções — casos ainda
não têm vínculo com tecnologia, aplicar o filtro só onde fizer sentido e ignorar
silenciosamente onde não se aplica), módulo (component_id), período (data
inicial/final sobre `opened_at`/`created_at` conforme o tipo), erro (error_code),
causa (root_cause_id), status (status do tipo de entidade correspondente).

- **BR-063** — qualquer filtro pré-sugerido automaticamente (ex.: quando a busca
  parte de um `contextCaseId` e o sistema pré-marca produto/versão do caso de
  origem) precisa aparecer como chip removível na UI, nunca fixo/travado.

## 5. Bloco 7.3 — Ranking

Score = combinação, não só data:
1. relevância textual (score nativo do MySQL `MATCH...AGAINST` quando disponível;
   score simples baseado em posição do termo para os tipos sem FULLTEXT);
2. boost se `contextCaseId` informado e o resultado compartilha produto;
3. boost adicional se compartilha componente/módulo;
4. boost adicional se compartilha `product_version_id`;
5. boost adicional se compartilha `error_code` exatamente;
6. para resultados do tipo `Solution`: boost por frequência de uso
   (`COUNT(knowledge_usages)`) e por sucesso histórico (taxa calculada como já
   estabelecido na Fase 6 — SEMPRE com amostra, nunca percentual solto, mesmo dentro
   do resultado de busca).

Pesos exatos ficam a seu critério de implementação nesta primeira versão (o próprio
Bloco 7.3 diz "começar com algo simples... depois sofisticamos") — documente os
pesos escolhidos no código/relatório final para poderem ser ajustados depois sem
arqueologia.

## 6. UI — conectar a caixa de busca global existente

- `#tc-global-search` no `_Layout.cshtml` (com atalho Ctrl+K) hoje não tem destino
  funcional — conectar a uma página `/Search` nova (esta SIM justifica página
  própria, não é extensão de uma entidade existente — é uma feature transversal).
  Seguir o mesmo padrão visual (cards, badges por tipo de resultado com cores
  distintas por `Type`, filtros como chips, sem tabela CRUD crua).
- Implementar o atalho de teclado Ctrl+K para focar o campo, se ainda não existir em
  `site.js`.
- Página de resultados agrupada por tipo (ou com abas/filtro de tipo), mostrando
  `MatchedFactors` de forma visível (ex.: badges pequenos "mesmo sistema", "mesmo
  erro") para atender BR-061.
- Ao abrir um resultado a partir da lista, registrar a interação
  (`search_result_interactions.opened_at`) — BR-064.

## 7. Critério de aceite manual (sem exigência de teste automatizado — fase ímpar)

1. logar como `admin@tracecore.local` e confirmar que NENHUMA tela das fases 1-6
   retorna 403 para essa conta (seção 0.1 resolvida);
2. buscar `"não conecta"` pela caixa global e obter resultados combinando pelo menos
   dois tipos diferentes (ex.: casos e soluções), cada um com `MatchedFactors`
   visível;
3. combinar 2+ filtros do Bloco 7.2 e confirmar que a lista se restringe
   corretamente;
4. abrir uma busca a partir de um caso específico (`contextCaseId`) e confirmar que
   resultados do mesmo sistema/componente/versão aparecem ranqueados acima de
   resultados sem relação de contexto;
5. confirmar que uma busca sem resultado nenhum ainda assim grava
   `search_queries.result_count = 0` (BR-065);
6. conferir visualmente: login sem o texto solto "Controle RBAC" fora de lugar, e
   dashboard mostrando "MySQL" em vez de "PostgreSQL".
7. Confirmar que `TraceCore.Web` está rodando e acessível ao final da execução.

## 8. Ao concluir, reporte no formato do guia (16 §4)

Objetivo:
Regras atendidas: BR-060 a BR-065, M06
Arquivos/projetos alterados:
Migrations:
Testes adicionados: N/A — fase ímpar, cobertura concentrada na Fase 8
Comandos executados:
Resultados (build): + confirmação de que o `TraceCore.Web` ficou rodando
Decisões tomadas: (pesos de ranking escolhidos, papel Super Admin (Dev), ajustes de
UI da seção 0)
Pendências/riscos: (religar dependency graph do dashboard a dado real — TODO M08;
FULLTEXT em products/components/root_causes se performance exigir)
Documentação atualizada: (se aplicável)
```

Quer que eu já verifique se algo mais das fases anteriores está com comportamento divergente do esperado antes de você rodar esse prompt, ou seguimos direto?