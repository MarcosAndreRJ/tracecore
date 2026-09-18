## Ordem de desenvolvimento do TraceCore

### Fase 0 — Preparar o projeto

Antes de criar funcionalidades, fixe a fundação.

**Bloco 0.1 — Estrutura da solução**

* Criar solution `.NET`.
* Criar projetos separados por responsabilidade.
* Definir arquitetura.
* Configurar Git.
* Criar `.gitignore`.
* Configurar `appsettings`.
* Definir ambientes Development / Staging / Production.

Sugestão inicial:

```text
TraceCore.sln

src/
 ├─ TraceCore.Web
 ├─ TraceCore.Application
 ├─ TraceCore.Domain
 ├─ TraceCore.Infrastructure
 └─ TraceCore.Shared

tests/
 ├─ TraceCore.UnitTests
 └─ TraceCore.IntegrationTests
```

**Bloco 0.2 — Banco**

* MySQL.
* Criar conexão.
* Migrations/versionamento.
* Convenções de tabelas.
* Datas em UTC.
* Soft delete onde necessário.
* Campos de auditoria.

**Bloco 0.3 — Infraestrutura básica**

* Logging.
* Tratamento global de exceções.
* Health checks.
* configuração de DI.
* respostas padronizadas.
* validação.
* OpenAPI/Swagger para APIs internas.

**Só avance quando:** aplicação subir, banco conectar, testes executarem e os ambientes estiverem funcionando.

---

# Fase 1 — Identidade e controle de acesso

Não comece pelos incidentes ainda.

### Bloco 1.1 — Usuários

Criar:

* usuário;
* login;
* logout;
* recuperação de senha;
* alteração de senha;
* ativação/desativação;
* último acesso;
* status;
* foto/avatar, se desejado.

### Bloco 1.2 — Departamentos

Exemplos:

```text
Desenvolvimento Web
Desenvolvimento Desktop
Mobile
Infraestrutura
Banco de Dados
Suporte
Integrações
Gestão
```

Um usuário poderá pertencer a um ou mais departamentos, dependendo da regra definida.

### Bloco 1.3 — Perfis e permissões

Não fazer apenas:

```text
Administrador
Usuário
```

Criar RBAC real.

Exemplos de permissões:

```text
caso.visualizar
caso.criar
caso.editar
caso.encerrar

solucao.criar
solucao.validar
solucao.publicar

analytics.visualizar
analytics.departamento

usuario.gerenciar
permissao.gerenciar

auditoria.visualizar
```

**Só avance quando:** você conseguir criar um usuário, atribuí-lo a um departamento, definir permissões e confirmar que telas e operações são realmente bloqueadas conforme o perfil.

---

# Fase 2 — Catálogo técnico da empresa

Esse módulo é fundamental para o futuro diagnóstico.

A plataforma precisa conhecer o ecossistema antes de tentar diagnosticar problemas.

### Bloco 2.1 — Sistemas

Cadastrar coisas como:

```text
ERP Desktop
Portal Web
Aplicativo Mobile
API Comercial
API Financeira
SAP
Banco principal
Servidor de autenticação
```

### Bloco 2.2 — Componentes

Um sistema pode possuir:

```text
Módulo Financeiro
Login
Faturamento
Sincronização
API
Serviço Windows
Banco
Fila
Job
```

### Bloco 2.3 — Dependências

Exemplo:

```text
App Mobile
      ↓
API
      ↓
Serviço de autenticação
      ↓
MySQL
```

Ou:

```text
Sistema Desktop
      ↓
API Integração
      ↓
SAP
```

Isso será extremamente importante depois.

O TraceCore deverá conseguir responder:

> "Se esse componente falhar, o que pode ser afetado?"

---

# Fase 3 — Cadastro de ocorrências

Aqui começa o coração do sistema.

Evite chamar tudo de "solução".

Primeiro existe um **caso**.

### Bloco 3.1 — Abrir caso

Exemplo:

> Cliente relata que não consegue entrar no sistema.

Registrar:

* cliente;
* sistema;
* ambiente;
* versão;
* módulo;
* data;
* descrição;
* mensagem de erro;
* usuário responsável;
* prioridade;
* impacto;
* evidências;
* anexos.

Não obrigue o usuário a saber antecipadamente:

> "É problema de infraestrutura."

Isso será descoberto depois.

---

# Fase 4 — Investigação do caso

Esta parte diferencia o TraceCore de uma wiki.

### Bloco 4.1 — Linha de investigação

O usuário registra:

```text
Hipótese 1
↓
Teste executado
↓
Resultado
↓
Hipótese descartada ou confirmada
```

Exemplo:

```text
Hipótese:
Servidor indisponível.

Teste:
Ping no servidor.

Resultado:
Servidor responde normalmente.

Conclusão:
Hipótese descartada.
```

Depois:

```text
Hipótese:
API de autenticação indisponível.

Teste:
Health endpoint.

Resultado:
HTTP 500.

Conclusão:
Possível origem identificada.
```

---

# Fase 5 — Resolução e lição aprendida

Quando o problema for resolvido, o caso não deve simplesmente ser "fechado".

### Bloco 5.1 — Encerramento estruturado

Registrar:

* causa raiz;
* solução aplicada;
* componente responsável;
* departamento;
* tempo gasto;
* tentativas realizadas;
* tentativas que falharam;
* solução definitiva ou paliativa;
* possibilidade de recorrência;
* medidas preventivas.

Isso transforma:

> "Chamado resolvido."

em:

> "Conhecimento reutilizável."

---

# Fase 6 — Base de soluções

Somente agora eu criaria o módulo de **Soluções**.

Um caso pode gerar uma solução reutilizável.

Exemplo:

```text
Problema:
Desktop não conecta após atualização.

Sintomas:
Timeout
Erro X
Versão Y

Causa:
Parâmetro Z incorreto.

Diagnóstico:
1. Verificar X
2. Testar Y
3. Consultar Z

Solução:
Alterar ...

Casos relacionados:
#154
#221
#289
```

A solução deve possuir:

* versão;
* autor;
* revisores;
* status;
* data da última revisão;
* sistemas afetados;
* tecnologias;
* palavras-chave;
* casos que comprovaram a solução.

---

# Fase 7 — Pesquisa

Aqui começa o ganho operacional forte.

### Bloco 7.1 — Pesquisa textual

Pesquisar por:

```text
"não conecta"
```

E retornar:

```text
casos
soluções
sistemas
componentes
erros
causas
```

### Bloco 7.2 — Filtros

Permitir combinar:

```text
Sistema
+ versão
+ cliente
+ departamento
+ tecnologia
+ módulo
+ período
+ erro
+ causa
+ status
```

### Bloco 7.3 — Ranking

Não ordenar apenas por data.

Começar com algo simples:

```text
similaridade textual
+ mesmo sistema
+ mesmo módulo
+ mesma versão
+ mesma mensagem de erro
+ frequência da solução
+ sucesso histórico
```

Depois sofisticamos.

---

# Fase 7.A — Saneamento e Consolidação Estrutural

Antes de iniciar Casos Relacionados, o TraceCore deve passar por um **gate de consolidação obrigatório**.

Esta fase existe para validar as Fases 0–7 contra a infraestrutura real e corrigir decisões estruturais identificadas durante a primeira implementação — em especial, o fato de que a aplicação nunca havia sido efetivamente validada contra MySQL, porque `Persistence:Provider` não estava configurado e o DI usava silenciosamente os repositórios InMemory.

**A Fase 8 só é liberada quando toda a Fase 7.A estiver implementada, testada, validada contra banco real e documentada.**

### Bloco 7.A.0 — Persistência MySQL/MariaDB real

* `Persistence:Provider` ausente/inválido passa a falhar explicitamente no startup (nunca mais cai para InMemory silenciosamente).
* InMemory só é aceito quando explicitamente configurado (testes de integração isolados).
* `DatabaseMigrationRunner` passa a ser de fato executado no `Program.cs`, controlado por `Persistence:AutoMigrate`.
* Banco criado do zero (`CREATE DATABASE IF NOT EXISTS`) e todas as migrations históricas executadas contra um banco limpo real.
* Validação retroativa das Fases 0–7 via HTTP real (login, usuários, departamentos, catálogo, abertura de caso, investigação, hipóteses, diagnóstico, resolução, conhecimento, pesquisa).
* Testes de integração ajustados para nunca mais depender de timing implícito entre `WebApplicationFactory` e o `Program.cs` de hosting mínimo — ambiente `Testing` dedicado (`appsettings.Testing.json`) força InMemory de forma determinística nos testes, independente do que o ambiente `Development` usar de verdade.

> **Nota de ambiente (ADR — Persistência revisado):** o servidor real disponibilizado para validar este bloco é **MariaDB 10.5**, não MySQL 8.4 LTS como o ADR-0002 original previa. Mantido como baseline operacional válido por decisão explícita; ver `21_ADRS_E_DECISOES_ABERTAS.md`.

### Bloco 7.A.1 — Cliente, Admin e estrutura organizacional

* Cadastro operacional de Cliente (`ExternalCrmId`, `ClientUnit` opcional, `ClientTechnicalContext` ligando Cliente ao catálogo técnico existente com `EffectiveFrom`/`EffectiveTo`).
* Papel único `Admin` (renomeado de `Super Admin (Dev)`) como administrador raiz — sem SuperAdmin/RootAdmin/SubAdmin.
* Proteção contra desativar/remover o papel do último Admin ativo.
* Remoção de `Team`/`teams`/`user_teams` (código morto, sem uso real) — Departamento passa a ser definitivamente o único conceito organizacional.
* Teste de regressão confirmando que Departamento não cria silo de conhecimento.

### Bloco 7.A.2 — Catálogo técnico e dependências

* `component_dependencies` (dependência real entre componentes, ex.: Desktop → API → Serviço → Banco → SAP).
* `component_owners` (responsabilidade por componente, ligada a Departamento).
* `Product.IsExternal`.
* UI administrativa mínima de manutenção do catálogo (Products, ProductVersions, Environments, Components, Dependencies, Owners).
* Remoção do mock do card de dependências no dashboard — dado real ou empty state, nunca dado inventado.

### Bloco 7.A.3 — CaseIteration e reabertura

* Entidade explícita `CaseIteration` (não `iteration_no` espalhado por várias tabelas).
* Reabertura de caso resolvido cria uma nova iteração; a resolução, investigação e evidências da iteração anterior nunca são sobrescritas.
* `case_resolutions` deixa de ser 1:1 com `cases` — cada iteração pode ter sua própria resolução.
* Backfill: casos já existentes recebem `Iteration 1`.
* Status do caso distingue reabertura (`Reopened` ou equivalente).

### Bloco 7.A.4 — Evidência estruturada

* `CaseEvidence` estendida — relação com `CaseIteration`, `DiagnosticStep` quando aplicável.
* Relação Evidence × Hypothesis **N:N** via tabela intermediária (`CaseHypothesisEvidence` ou equivalente), com `RelationType` qualitativo (`Supports`/`Contradicts`/`Inconclusive`/`Confirms`) — nunca percentual artificial.
* Um passo de diagnóstico pode sugerir uma evidência; o usuário confirma/edita, nunca gravação silenciosa.
* Remoção do card de evidência hardcoded/fake em `Cases/Details.cshtml`.

### Bloco 7.A.5 — Governança do conhecimento

* Versionamento: versão anterior preservada e identificável (`Superseded`) ao publicar uma nova.
* Estados mortos (`Archived`, etc.) recebem fluxo real ou são removidos do modelo ativo.
* Proveniência reforçada: conhecimento sempre rastreável até caso/evidência/autor/revisor de origem.

### Bloco 7.A.6 — Pesquisa híbrida

* Pipeline: reconhecimento de identificadores → busca exata → filtros estruturados → busca textual → ranking contextual → (busca semântica futura, fora de escopo agora).
* Correspondência exata para identificadores técnicos (número de caso, código de erro, HTTP status, versões) com mecanismo extensível, não hardcoded a um único formato.
* `MatchedFactors` reais para todos os tipos de resultado (hoje só Casos/Soluções tinham fatores calculados; Sistemas/Componentes/Causas usavam rótulo fixo).
* `TechnologyId` confirmado/corrigido no filtro real do MySQL.
* Cliente/ClientUnit incorporados aos filtros de pesquisa.

### Bloco 7.A.7 — Consolidação, testes e documentação

* Busca ampla por mocks operacionais remanescentes (dado fake apresentado como real) e remoção.
* Documentação individual atualizada onde houver impacto real; `MASTER_SPECIFICATION.md` regenerado só depois, sem contradizer os documentos individuais.
* ADRs consolidados (Persistência, Administração, Organização, Compartilhamento, Cliente, Catálogo, Reabertura, Evidência, Pesquisa).
* Registrar no documento: **"Gate 7.A concluído — TraceCore liberado para a Fase 8."** ✅ Concluído e verificado em 2026-09-18.

**Critério para liberar a Fase 8:** `dotnet build` ✅, `dotnet test` ✅ (55 testes aprovados), migrations validadas contra banco real ✅, mocks operacionais removidos ✅, checklist do Gate 7.A 100% ✅.

---

# Fase 8 — Casos relacionados

Antes de usar LLM, faça o sistema conseguir relacionar casos de maneira determinística.

Exemplo:

> Caso atual possui características semelhantes a 14 casos anteriores.

Mostrar:

```text
Score: 92 — Caso #1832
Score: 87 — Caso #991
Score: 81 — Caso #1577
```

E principalmente:

> 11 desses 14 casos foram resolvidos verificando primeiro o serviço X.

Isso já gera enorme valor mesmo sem IA.

### Status da Fase 8: ✅ CONCLUÍDA
- **Modelo de Dados**: Tabela `case_relations` criada na migration `M20260918_15_CreateCaseRelationsSchemaAndPermission.cs` com UNIQUE `(source_case_id, target_case_id, relation_type)`.
- **Tipos de Relação (BR-030)**: `Similar` (computado deterministicamente pelo sistema) e asserções manuais de usuários (`Duplicate`, `Recurrence`, `CommonCause`, `Dependency`, `Reference`).
- **Princípio P-006 Respeitado**: Similaridade é correspondência determinística baseada em sinais técnicos (Produto, Componente, Versão, Código de Erro e similaridade léxica), nunca probabilidade (sem falsas alegações de "% de chance"). A interface sempre expõe os `MatchedFactors`.
- **Insight Agregado (BR-048)**: Calculado para casos similares resolvidos com amostra confiável $M \ge 3$. Identifica o passo com `Worked` mais comum ou a ação de resolução predominante. Frase gerada no padrão exigido: `"N de M casos semelhantes foram resolvidos verificando/agindo sobre: <ação/componente>"`.
- **UI & Permissões**: Seção "Casos Relacionados" em `Pages/Cases/Details.cshtml` com card de Insight Agregado, listas com badges de score e fatores, e modal de relacionamento manual protegido pela nova permissão `caso.relacionar`.
- **Auditoria**: Relações manuais auditadas em `audit_events`. Idempotência no cálculo de relações `Similar`.
- **Testes**: 4 testes de integração dedicados em `CaseRelationsIntegrationTests.cs` (59/59 testes passando na suite total).

---

# Fase 9 — Diagnóstico guiado

Aqui nasce aquilo que discutimos sobre **orientar a ponta**.

Ao receber:

> "Não consigo entrar."

TraceCore começa perguntando:

```text
Acontece com:
[ ] um usuário
[ ] vários usuários
[ ] todos
```

Depois:

```text
O sistema abre?
```

Depois:

```text
Existe mensagem de erro?
```

Cada resposta elimina possibilidades.

Não faça uma árvore gigantesca hardcoded.

Modele:

```text
Pergunta
↓
Resposta
↓
Hipóteses favorecidas
↓
Hipóteses descartadas
↓
Próximo teste recomendado
```

Esse módulo deve ser configurável.

### Status da Fase 9: ✅ CONCLUÍDA
- **Modelo de Dados (Migration 16)**: Criadas as tabelas relacionais do grafo configurável em banco: `diagnostic_flows`, `diagnostic_flow_hypotheses`, `diagnostic_checks`, `diagnostic_check_options`, `diagnostic_check_impacts`. Semeado fluxo de produção `FLOW-LOGIN-ISSUES` com 4 hipóteses candidatas e 4 checagens com impactos e saltos inteligentes. Permissão `diagnostico.configurar` criada e atribuída a `Admin` e `Admin Funcional`.
- **Grafo Dinâmico & Anti-padrão Extirpado**: Árvores hardcoded em código C# eliminadas (05 §2 e §9). O motor busca fluxos por correspondência textual de palavras-chave no relato original/resumo e carrega perguntas e impactos do banco de dados.
- **Heurística de Prioridade Causal sem Falsas Certezas (Princípio P-006 & BR-074)**: Removidas porcentagens pseudo-estatísticas ("88% de precisão"). Ordenação causal calculada por:
  $$\text{prioridade} = \frac{\text{poder\_discriminativo} \times \text{confiabilidade}}{\text{custo} + \text{risco} + 1}$$
  Exibida como prioridade investigativa (`Alta`, `Média`, `Baixa`) acompanhada da justificativa do teste.
- **Pulo Inteligente de Perguntas Redundantes**: Perguntas associadas a campos já preenchidos no caso (`Cases.EnvironmentId`, `Cases.ProductVersionId`, `Cases.ErrorCode`) são puladas automaticamente.
- **Integração com a Linha do Tempo e Imutabilidade (Fase 4, BR-071 e BR-075)**: Hipóteses ativadas são instanciadas em `case_hypotheses` com `SourceType = "Guided"`. Cada resposta gera um `DiagnosticStep` (`GuidedQuestion`) com resultados válidos (`Worked`, `DidNotWork`, `Inconclusive`). Opções com impacto $\ge 2.0m$ transicionam formalmente as hipóteses via `Evaluate`. Respostas ignoradas pelo operador exigem motivo e geram passo `RecommendationIgnored`.
- **Escalonamento Inteligente (05 §8)**: Ao atingir $\ge 5$ checagens respondidas sem convergência, o sistema alerta o operador para escalonamento com empacotamento do contexto investigativo.
- **Área Administrativa**: Interface de gestão em `/Diagnosis/Flows` para visualização e cadastro de novos fluxos, hipóteses candidatas e checagens com pesos e impactos.

---

# Fase 10 — Gestão

Agora o TraceCore começa a se tornar também ferramenta gerencial.

### Bloco 10.1 — Dashboard geral

Indicadores como:

```text
Casos abertos
Casos resolvidos
Tempo médio de resolução
Problemas recorrentes
Sistemas com mais ocorrências
Causas mais frequentes
```

### Bloco 10.2 — Departamentos

Exemplo:

```text
Infraestrutura
187 casos
MTTR: 31 min
23 reincidências
```

### Bloco 10.3 — Usuários

Não usar apenas para "ranking de funcionário".

Mostrar contexto:

```text
Casos trabalhados
Casos resolvidos
Conhecimentos publicados
Soluções reutilizadas
Participação em diagnósticos
Tempo médio
Áreas de atuação
```

### Bloco 10.4 — Conhecimento

Indicadores importantes:

```text
Soluções mais utilizadas
Soluções sem revisão
Problemas sem solução documentada
Soluções que mais reduziram tempo de atendimento
Casos recorrentes sem causa raiz definitiva
### Status da Fase 10: ✅ CONCLUÍDA
- **Bloco 10.0 (Fundação das Métricas)**: Implementada camada analítica isolada (`IManagementAnalyticsRepository`, `MySqlManagementAnalyticsRepository` com Dapper e `InMemoryManagementAnalyticsRepository` no store compartilhado). Service centralizador (`ManagementAnalyticsService`) como única fonte de cálculo de fórmulas. Registrado no DI.
- **Fórmulas Centrais Consolidadas**:
  - Casos Abertos: `Status IN ('Open', 'Reopened')`.
  - Casos Resolvidos: caso com `Status = 'Resolved'` na iteração atual.
  - MTTR Operacional por Iteração: `AVG(ClosedAt - OpenedAt)` sobre iterações resolvidas.
  - Mediana de Resolução: projeção ordenada de durações calculada deterministicamente.
  - Recorrências: critério fixado com base em `CaseRelation.RelationType IN ('Recurrence', 'CommonCause')` (excluindo 'Similar').
  - Sem Causa Raiz: casos resolvidos com causa não informada ou não confirmada.
  - Sem Conhecimento: casos resolvidos sem artigo vinculado via `ProvenanceCaseId`.
- **Bloco 10.1 (Dashboard Geral — `/Analytics/Index`)**: Filtros globais unificados (período, cliente, produto, departamento, severidade), cards de KPIs com tendências e tooltips conceituais, gráficos de evolução temporal e distribuição (sistemas, componentes, causas raiz), e tabela "Casos que precisam de atenção".
- **Bloco 10.2 (Departamentos — `/Analytics/Departments`)**: Visão transversal sem silos ou rankings pejorativos, integrando responsabilidade direta do caso, dono de componentes afetados e operadores de diagnóstico.
- **Bloco 10.3 (Usuários — `/Analytics/Users`)**: Visão individual e de equipe contextualizada (casos, resoluções, diagnósticos, hipóteses, evidências e autoria/uso de artigos) com perfil técnico emergente baseado em dados recentes, sem scores artificiais.
- **Bloco 10.4 (Conhecimento — `/Analytics/Knowledge`)**: Eficácia factual extraída de `KnowledgeUsage` (`Worked`, `PartiallyWorked`, `DidNotWork`), controle de revisão (`LastReviewedAt IS NULL`, vencidos) e lacunas documentais.
- **Bloco 10.5 (Drill-down e Navegação)**: Menu lateral "Inteligência" atualizado com links protegidos por `analytics.visualizar`. Todo card e gráfico de KPI possui link de drill-down direto para `/Cases/Index` com parâmetros mapeados (`WithoutRootCause`, `ComponentId`, `OwnerUserId`, etc.).
- **Bloco 10.6 (Testes e Documentação)**: 5 testes de integração dedicados em `ManagementAnalyticsIntegrationTests.cs` cobrindo cálculo exato de MTTR por iteração (sem acúmulo), mediana par/ímpar, isolamento de recorrência, casos sem causa/conhecimento e estados abertos. Suite total de testes: **68/68 aprovados**. Documentos `26_CATALOGO_DE_KPIS.md`, `03_MODULOS_DO_SISTEMA.md`, `21_ADRS_E_DECISOES_ABERTAS.md` e `MASTER_SPECIFICATION.md` sincronizados.

---

# Fase 11 — Auditoria

Registrar ações relevantes.

Exemplo:

```text
14:31
João alterou a solução #177

14:33
Maria aprovou a revisão.

14:37
Carlos utilizou a solução no caso #418.
```

Não apenas login/logout.

---

# Fase 12 — Preparação para IA

Ainda não coloque a LLM.

Primeiro prepare a informação.

Adicionar:

* classificação adequada;
* conteúdo limpo;
* tags;
* metadados;
* permissões;
* versionamento;
* qualidade documental.

Separar claramente:

```text
Conhecimento validado
Casos históricos
Documentos
Conteúdo sugerido por IA
```

---

# Fase 13 — RAG

Agora entra a recuperação semântica.

Fluxo:

```text
Usuário descreve o problema
        ↓
TraceCore interpreta a consulta
        ↓
Busca informações relevantes
        ↓
Seleciona casos e soluções
        ↓
Envia contexto para a LLM
        ↓
LLM produz orientação
```

Importante:

> A IA não deve responder somente com conhecimento próprio.

Ela deve se apoiar prioritariamente no conhecimento da empresa.

Como você decidiu utilizar MySQL, eu manteria a busca vetorial desacoplada da camada principal. Assim poderemos avaliar posteriormente qual mecanismo vetorial será usado sem alterar o domínio do TraceCore.

---

# Fase 14 — Copiloto TraceCore

Somente aqui eu implementaria o chat.

Exemplo:

> "Cliente Alfa não consegue acessar o Financeiro desde a atualização 8.2."

TraceCore poderia responder:

> Encontrei 9 ocorrências semelhantes. Em 6 delas o problema estava relacionado ao serviço de autenticação. Antes de alterar configurações, verifique estes três pontos...

E apresentar as fontes:

```text
Caso #182
Caso #771
Solução #45
Procedimento #17
```

---

# Fase 15 — Integrações automáticas

Depois podemos permitir que o TraceCore busque automaticamente:

```text
versão do sistema
logs
status das APIs
health checks
telemetria
serviços
banco
monitoramento
```

Isso reduz perguntas humanas.

Em vez de perguntar:

> "A API está funcionando?"

TraceCore consulta sozinho.

---

# Fase 16 — Inteligência analítica

Depois de existir histórico suficiente:

```text
Esse problema aumentou 42% após a versão 8.4.

73% desses casos estão associados ao módulo financeiro.

Essa solução reduz o MTTR médio de 52 para 17 minutos.
```

É aqui que o sistema passa de repositório para ferramenta de decisão.

---

# Checklist mestre

Use este como seu painel de progresso:

### Fundação

* [ ] Solution .NET criada
* [ ] Estrutura de projetos definida
* [ ] MySQL configurado
* [ ] Migrations configuradas
* [ ] Logging configurado
* [ ] Tratamento de erros
* [ ] Testes básicos
* [ ] Git configurado

### Segurança e organização

* [ ] Autenticação
* [ ] Usuários
* [ ] Departamentos
* [ ] Perfis
* [ ] Permissões
* [ ] RBAC

### Catálogo técnico

* [ ] Sistemas
* [ ] Módulos
* [ ] Componentes
* [ ] Tecnologias
* [ ] Ambientes
* [ ] Dependências
* [ ] Clientes

### Casos

* [ ] Abrir ocorrência
* [ ] Editar ocorrência
* [ ] Evidências
* [ ] Anexos
* [ ] Histórico
* [ ] Hipóteses
* [ ] Testes
* [ ] Resultados
* [ ] Causa raiz
* [ ] Resolução
* [ ] Encerramento

### Conhecimento

* [ ] Soluções
* [ ] Versionamento
* [ ] Aprovação
* [ ] Revisão
* [ ] Tags
* [ ] Casos relacionados
* [ ] Lições aprendidas
* [ ] O que não funcionou

### Pesquisa

* [ ] Busca textual
* [ ] Filtros
* [ ] Busca combinada
* [ ] Ranking
* [ ] Casos semelhantes
* [ ] Histórico de pesquisa

### Gate 7.A — Consolidação estrutural

* [x] MySQL/MariaDB é provider operacional real (fail-fast, sem fallback silencioso para InMemory)
* [x] `DatabaseMigrationRunner` executado no startup (`Persistence:AutoMigrate`)
* [x] Migrations validadas em banco limpo, do zero, contra banco real
* [x] Testes de integração com ambiente `Testing` dedicado (InMemory determinístico, sem depender de timing do WebApplicationFactory)
* [x] Cliente implementado (`ExternalCrmId`, `ClientUnit`, `ClientTechnicalContext`)
* [x] Admin consolidado (papel único, sem SuperAdmin/RootAdmin/SubAdmin)
* [x] Proteção do último Admin implementada
* [x] Team removido (entidade, tabelas, código morto)
* [x] Departamento consolidado como único conceito organizacional; teste de não-silo
* [x] Dependências entre componentes implementadas (`component_dependencies`)
* [x] Owners de componentes implementados (`component_owners`, ligado a Departamento)
* [x] `Product.IsExternal` implementado
* [x] Manutenção mínima do catálogo disponível (UI)
* [x] `CaseIteration` implementado
* [x] Reabertura preserva histórico (investigação, evidências, resolução anteriores intactas)
* [x] `case_resolutions` suporta múltiplas iterações
* [x] Evidência estruturada, N:N com hipótese, `RelationType` implementado
* [x] Cards fake de evidência/dependências removidos
* [x] Governança do conhecimento revisada (versionamento, estados mortos resolvidos)
* [x] Busca exata implementada (identificadores técnicos)
* [x] `MatchedFactors` reais para todos os tipos de resultado
* [x] `TechnologyId` e Cliente/ClientUnit no filtro de pesquisa
* [x] Mocks operacionais remanescentes removidos
* [x] Documentação sincronizada (individuais primeiro, depois `MASTER_SPECIFICATION.md`)
* [x] ADRs consolidados registrados
* [x] **Gate 7.A concluído — TraceCore liberado para a Fase 8.**

### Diagnóstico

* [x] Sintomas
* [x] Hipóteses
* [x] Perguntas
* [x] Testes
* [x] Eliminação de hipóteses
* [x] Fluxo guiado
* [x] Escalonamento

### Gestão

* [x] Dashboard geral
* [x] Dashboard de usuários
* [x] Dashboard de departamentos
* [x] Dashboard de sistemas
* [x] Dashboard de causas
* [x] Dashboard de soluções
* [x] KPIs
* [x] Drill-down
* [ ] Exportação

### Governança

* [ ] Auditoria
* [ ] Histórico de alterações
* [ ] Aprovação de conhecimento
* [ ] Controle de versões
* [ ] LGPD
* [ ] Retenção de dados

### IA

* [ ] Preparação documental
* [ ] Embeddings
* [ ] Busca semântica
* [ ] RAG
* [ ] Citações/fontes
* [ ] Controle de permissões no RAG
* [ ] Copiloto
* [ ] Feedback da resposta
* [ ] Avaliação da qualidade

### Operação

* [ ] Health checks
* [ ] Logs estruturados
* [ ] Métricas
* [ ] OpenTelemetry
* [ ] Backup
* [ ] CI/CD
* [ ] Staging
* [ ] Produção
* [ ] Monitoramento

## Como usar isso com uma IA desenvolvedora

Eu recomendo **uma conversa por bloco**. Não mande os 48 documentos toda vez.

Por exemplo, na primeira conversa você entrega somente:

```text
MASTER_SPECIFICATION
arquitetura
modelo de dados
regras de negócio pertinentes

Tarefa:
Executar exclusivamente o Bloco 0.1.
Não implementar funcionalidades futuras.
```

Depois que terminar:

```text
1. revisar;
2. testar;
3. corrigir;
4. fazer commit;
5. atualizar documentação;
6. iniciar nova conversa para o próximo bloco.
```

Isso é especialmente importante. **Não continue por 20 etapas dentro da mesma conversa com a IA de programação.** Conforme o contexto cresce, aumenta a chance de ela esquecer decisões anteriores, duplicar código ou começar a improvisar.

Eu organizaria o desenvolvimento do TraceCore em aproximadamente **40 a 60 tarefas pequenas**, em vez de 10 tarefas gigantes. O que coloquei acima é o mapa macro; o próximo nível deve transformar, por exemplo, o **Bloco 1.1 — Usuários** em `1.1.1`, `1.1.2`, `1.1.3` etc., cada uma suficientemente pequena para uma única sessão de desenvolvimento.

Posso montar em seguida o **Plano de Desenvolvimento Executável do TraceCore**, já numerado como `TC-0001`, `TC-0002`, `TC-0003...`, com **ordem exata, dependências, prompt que você deve mandar para a IA e checkbox para cada tarefa**. Isso provavelmente será o documento que você mais vai usar durante o desenvolvimento.
