# MASTER SPECIFICATION

> Documento consolidado gerado a partir dos arquivos fonte deste pacote. Em caso de conflito, prevalecem os arquivos individuais conforme README.

---
# 00 — Visão geral e princípios do produto

## 1. Problema que o sistema resolve

A empresa possui conhecimento técnico distribuído entre pessoas, conversas, chamados, anotações e memória individual. Um incidente pode ser resolvido hoje e, meses depois, uma ocorrência semelhante consumir novamente várias horas porque a equipe não consegue localizar, relacionar ou reutilizar o aprendizado anterior.

Além disso, uma reclamação do cliente normalmente chega como **sintoma**, não como diagnóstico. “Não consigo entrar”, “o sistema não conecta”, “o pedido não integra”, “a tela trava” ou “o dado não aparece” podem ter origem em infraestrutura, rede, autenticação, aplicação web, desktop, mobile, API, banco de dados, integração SAP ou em uma combinação desses elementos.

Portanto, a plataforma deve reduzir duas incertezas:

- **Onde começar a investigar?**
- **O que já aprendemos em situações parecidas?**

## 2. Objetivo do produto

Criar uma plataforma corporativa única que:

- registre conhecimento técnico validado;
- registre casos reais completos, inclusive tentativas malsucedidas;
- relacione ocorrências semelhantes;
- guie a triagem e o diagnóstico;
- priorize verificações com maior capacidade de eliminar hipóteses;
- recupere soluções por similaridade de contexto e sintoma;
- preserve evidências e histórico de decisão;
- gere indicadores operacionais e gerenciais;
- permita evolução futura para RAG/LLM sem reconstruir a base;
- forneça auditoria de ações, conteúdo e acessos relevantes.

## 3. Princípios não negociáveis

### P-001 — Sintoma antes do departamento
O usuário não deve precisar decidir, no início, se o problema “é da Infra”, “é do Web” ou “é do Banco”. A plataforma coleta evidências e reduz o espaço de hipóteses. O departamento aparece como consequência do diagnóstico e do escalonamento.

### P-002 — Conhecimento estruturado + texto humano
Não armazenar apenas textos livres. O registro deve combinar campos estruturados, necessários para filtros/analytics, com narrativa técnica suficiente para preservar contexto.

### P-003 — Registrar o que falhou
Uma tentativa que não resolveu o problema pode ser tão útil quanto a solução final. O sistema deve armazenar a tentativa, motivo, evidência, resultado e contexto em que falhou.

### P-004 — Fonte de verdade explícita
Informações devem ter origem e estado: rascunho, em revisão, publicado, obsoleto. Conteúdo produzido pela IA deve ser identificável e não pode ser promovido silenciosamente a conhecimento validado.

### P-005 — Evidência acima de palpite
Hipóteses devem ser acompanhadas de sinais favoráveis, sinais contrários, verificações recomendadas e resultado dos testes.

### P-006 — Similaridade não é probabilidade
Um resultado com “87% de similaridade” não significa “87% de chance de ser a causa”. A interface deve usar nomenclatura correta e mostrar por que o item foi recuperado.

### P-007 — Analytics rastreável
Todo indicador relevante deve permitir drill-down até os registros que o compõem. Métrica sem capacidade de auditoria não deve ser usada para decisão crítica.

### P-008 — Menor risco primeiro
O motor de diagnóstico deve preferir verificações seguras, reversíveis e de baixo custo antes de ações destrutivas ou de impacto operacional.

### P-009 — Não automatizar risco sem aprovação
A IA pode sugerir. A execução de comandos, alterações de banco, reinicializações, mudanças em produção, exclusões ou alterações de configuração exige autorização conforme política.

### P-010 — Aprendizado contínuo
Casos resolvidos devem alimentar índices de busca, relacionamentos, estatísticas de sucesso e revisão de artigos.

## 4. Escopo funcional macro

A plataforma possui nove domínios principais:

1. Identidade, usuários, departamentos e permissões.
2. Catálogo técnico: clientes, unidades, contextos técnicos, produtos, versões, componentes, integrações e dependências.
3. Casos/incidentes, iterações, reabertura e sessões de diagnóstico.
4. Base de conhecimento e soluções (versionadas, revisadas, com aplicabilidade declarada).
5. Pesquisa, filtros, similaridade e explicabilidade.
6. Motor de diagnóstico guiado (grafo em banco, heurística determinística).
7. Analytics, relatórios e gestão (queries agregadas determinísticas, drill-down unificado).
8. Auditoria, segurança e governança (append-only, sanitização universal, nomenclatura unificada).
9. IA/RAG e Copiloto Operacional (RAG grounded, tool calling de leitura, modelos dinâmicos).

## 5. Exemplo norteador

### Entrada
Cliente relata: **“não consigo entrar no sistema”**.

### O sistema não faz
- não força “Infra / Dev / Banco” na abertura;
- não mostra uma lista genérica de artigos por palavra “entrar”;
- não deixa a IA responder sem evidência recuperada.

### O sistema faz
1. identifica cliente, produto, ambiente e versão, quando disponíveis;
2. registra texto original do cliente;
3. extrai/seleciona sintomas normalizados;
4. pergunta se afeta um usuário, uma unidade ou todos;
5. verifica se existe mensagem/código de erro;
6. consulta status de serviços/telemetria disponível;
7. correlaciona componentes e dependências;
8. recupera casos anteriores semelhantes;
9. exibe hipóteses e próximos testes de alto valor diagnóstico;
10. registra cada teste e seu resultado;
11. ao resolver, vincula causa raiz, solução e validação;
12. decide se o caso gera/atualiza conhecimento reutilizável.

## 6. Resultado esperado para a empresa

A evolução deve ser mensurável por:

- redução de MTTR;
- aumento de reutilização de soluções existentes;
- redução de reincidência não documentada;
- redução de buscas sem resultado útil;
- aumento da resolução com menos handoffs;
- identificação dos componentes que mais geram incidentes;
- identificação de conhecimento desatualizado;
- melhoria do tempo entre relato e primeira hipótese útil.



---

# 01 — Regras de negócio

As regras abaixo são requisitos soberanos. IDs devem ser usados em código, testes, histórias e critérios de aceite.

## 1. Identidade e organização

**BR-001** — Todo usuário ativo deve possuir identidade única, estado, nome, e-mail/login e pelo menos um vínculo organizacional ou papel global.

**BR-002** — Usuários podem participar de mais de um departamento e podem ter papéis diferentes por escopo.

**BR-003** — Permissão deve ser baseada em capacidade, não apenas em nome de perfil. Perfis agrupam permissões.

**BR-004** — Ações administrativas e alterações de permissão devem gerar auditoria imutável em nível de aplicação.

**BR-005** — Desativar usuário não apaga autoria, comentários, casos, aprovações ou histórico.

**BR-006** — O sistema deve bloquear qualquer tentativa de desativar ou remover o papel de Admin do último usuário ativo que o possui.

## 2. Catálogo técnico

**BR-010** — Cliente, produto, módulo, componente, ambiente, versão, tecnologia e integração são entidades classificáveis e pesquisáveis. Clientes suportam Unidades (`client_units`) e Contextos Técnicos (`client_technical_contexts`).

**BR-011** — Componentes podem depender de outros componentes. A dependência deve registrar direção, tipo, criticidade, bloqueio de autorreferência e período de validade.

**BR-012** — Um componente pode possuir departamentos responsáveis (`component_owners`) com papéis Primário, Secundário e Escalonamento.

**BR-013** — Versão do produto deve ser preservada no caso histórico; alterações posteriores no catálogo não podem reescrever o contexto do incidente passado. Produtos externos são suportados via flag `is_external`.

## 3. Casos e incidentes

**BR-020** — Todo caso deve preservar o relato original do cliente ou da origem, sem sobrescrevê-lo por uma versão “corrigida”.

**BR-021** — O caso deve possuir um resumo normalizado separado do relato original.

**BR-022** — O usuário pode abrir um caso sem saber a área responsável.

**BR-023** — Um caso pode afetar múltiplos componentes e possuir múltiplas hipóteses simultâneas.

**BR-024** — Hipótese é diferente de causa raiz. Hipótese pode ser descartada; causa raiz só deve ser marcada após evidência suficiente ou explicitamente como “não confirmada”.

**BR-025** — Toda tentativa de diagnóstico relevante deve registrar: ação, autor, data/hora, objetivo, evidência anterior, resultado e classificação do resultado.

**BR-026** — Resultados mínimos possíveis: funcionou, funcionou parcialmente, não funcionou, não aplicável, inconclusivo.

**BR-027** — Um caso não pode ser considerado “resolvido” sem registrar a ação de resolução e a forma de validação.

**BR-028** — Um caso pode ser resolvido sem causa raiz confirmada, mas deve ficar explicitamente marcado assim.

**BR-029** — Reabertura preserva a resolução anterior e inicia nova iteração formal (`CaseIteration`); não deve apagar histórico de hipóteses, diagnósticos ou resoluções anteriores.

**BR-030** — Casos podem ser relacionados como duplicado, semelhante, recorrência, causa comum, dependência ou referência.

**BR-031** — Evidências são vinculadas à iteração do caso e podem relacionar-se a hipóteses via relação formal N:N (`Supports`, `Contradicts`, `Inconclusive`, `Confirms`). O registro em passos diagnósticos deve oferecer sugestão estruturada sem persistência implícita automática.

## 4. Conhecimento e soluções

**BR-040** — Conteúdo de conhecimento possui ciclo de vida: rascunho → revisão → publicado → obsoleto/arquivado.

**BR-041** — Publicação deve registrar autor, revisor/aprovador, versão do conteúdo e data.

**BR-042** — Alteração de conteúdo publicado cria nova revisão lógica; a versão anterior deve permanecer auditável.

**BR-043** — Solução deve declarar aplicabilidade: produto, componente, versões, ambiente, pré-condições e restrições quando pertinentes.

**BR-044** — Solução deve conter método de validação do resultado.

**BR-045** — Passo de risco relevante deve conter aviso, pré-condição, impacto potencial e rollback quando aplicável.

**BR-046** — Conhecimento pode nascer de um caso, de documentação oficial ou de iniciativa técnica. Sua origem deve ser registrada.

**BR-047** — Estatística de sucesso deve considerar apenas utilizações registradas e classificadas. Não inferir sucesso pela simples visualização do artigo.

**BR-048** — Taxa de sucesso deve sempre apresentar tamanho da amostra. “95%” sem quantidade de usos é proibido na interface gerencial.

**BR-049** — Conteúdo obsoleto não desaparece das referências históricas; ele apenas deixa de ser recomendado por padrão para novos casos.

## 5. Pesquisa e ranking

**BR-060** — A busca deve combinar texto, metadados, contexto técnico e, futuramente, semântica.

**BR-061** — Resultados devem explicar os principais fatores de correspondência: sintoma, erro, produto, versão, componente, tecnologia, cliente/contexto ou conteúdo semântico.

**BR-062** — Resultados incompatíveis com versão/ambiente devem sofrer penalidade ou ser ocultados quando a incompatibilidade for explícita.

**BR-063** — Usuário deve conseguir remover filtros sugeridos automaticamente.

**BR-064** — O sistema deve registrar consultas de busca, filtros, resultados exibidos, item aberto e feedback de utilidade, respeitando regras de privacidade.

**BR-065** — Busca sem resultado deve ser mensurável para revelar lacunas da base.

## 6. Diagnóstico guiado

**BR-070** — O diagnóstico começa pelo sintoma e pelo contexto, nunca pela obrigação de escolher um departamento.

**BR-071** — Perguntas devem ser adaptativas: respostas anteriores podem alterar próximos passos.

**BR-072** — O motor deve priorizar verificações com boa capacidade de discriminar hipóteses, baixo custo e baixo risco.

**BR-073** — Uma pergunta só deve ser feita manualmente quando a informação não puder ser obtida automaticamente de fonte confiável ou quando confirmação humana for necessária.

**BR-074** — Cada recomendação deve indicar por que está sendo sugerida e quais hipóteses ela ajuda a confirmar ou descartar.

**BR-075** — A pessoa pode ignorar uma recomendação, mas a decisão pode ser registrada com motivo quando relevante.

**BR-076** — O motor não deve executar alterações destrutivas de produção por conta própria.

## 7. IA e RAG

**BR-080** — IA é assistente; a base oficial continua sendo fonte de verdade.

**BR-081** — Resposta de IA deve ser fundamentada em conteúdo recuperado quando tratar de procedimento interno, solução técnica ou política da empresa.

**BR-082** — A interface deve distinguir conteúdo oficial, caso histórico, inferência da IA e informação externa.

**BR-083** — Conteúdo gerado pela IA não pode ser publicado automaticamente como solução validada.

**BR-084** — A IA deve citar os itens internos utilizados na resposta e permitir abertura dos registros de origem.

**BR-085** — A IA não deve inventar comando, tabela, endpoint, credencial, procedimento interno ou versão inexistente. Na ausência de evidência, deve declarar insuficiência de contexto e sugerir coleta objetiva.

**BR-086** — Feedback sobre resposta da IA deve ser armazenado para avaliação de qualidade, sem alterar automaticamente regras ou conhecimento oficial.

## 8. Analytics e gestão

**BR-090** — Indicadores devem permitir filtro temporal e por cliente, produto, componente, departamento, severidade, causa e demais dimensões aplicáveis.

**BR-091** — Métricas de pessoa devem ter finalidade operacional e gerencial legítima e não podem ser apresentadas como “pontuação de valor do funcionário” derivada de algoritmo opaco.

**BR-092** — Deve ser possível analisar participação do usuário: casos atendidos, contribuições, revisões, aprovações, reutilizações e histórico de ações autorizadas.

**BR-093** — Dashboards devem permitir drill-down até os registros de origem, salvo restrição de acesso.

**BR-094** — Alteração do conceito/fórmula de KPI deve ser versionada e documentada.

## 9. Auditoria e segurança

**BR-100** — Auditoria deve registrar, quando aplicável: ator, ação, alvo, data/hora, origem, correlação, valores anteriores e novos ou hash/resumo seguro.

**BR-101** — Logs de auditoria não devem armazenar senha, token, segredo ou dado sensível em texto puro.

**BR-102** — Acesso a conteúdo restrito deve ser controlado por autorização no servidor, nunca apenas por ocultação na interface.

**BR-103** — Exportações relevantes e visualizações de dados sensíveis devem ser auditáveis conforme política.

**BR-104** — Exclusão lógica deve ser preferida para entidades com valor histórico. Exclusão física exige regra explícita de retenção.



---

# 02 — Requisitos funcionais

## 1. Autenticação, usuários e organização

**FR-001** — Autenticar usuário por mecanismo configurado pela empresa.
**FR-002** — Permitir cadastro, ativação, bloqueio e desativação de usuários.
**FR-003** — Gerenciar departamentos (conceito organizacional oficial; `teams`/`user_teams` foram extintos na Migration 10), cargos funcionais, papéis e permissões.
**FR-004** — Permitir escopo de permissão global, por departamento e por domínio funcional.
**FR-005** — Exibir perfil administrativo com vínculos, papéis, contribuições, atividade auditável e sessões recentes conforme permissão.
**FR-006** — Registrar data do último acesso, falhas de login e eventos de segurança sem expor segredo.

## 2. Catálogo técnico

**FR-020** — Cadastrar clientes/unidades.
**FR-021** — Cadastrar produtos/sistemas e módulos.
**FR-022** — Cadastrar versões/releases.
**FR-023** — Cadastrar componentes técnicos e classificar tipo: Web, Desktop, Mobile, API, Banco, Infraestrutura, Rede, Integração, SAP, Serviço, Job, Outro.
**FR-024** — Cadastrar dependências entre componentes e visualizar grafo.
**FR-025** — Cadastrar tecnologias, bancos, protocolos e serviços externos.
**FR-026** — Definir responsáveis e rotas de escalonamento por componente.

## 3. Casos/incidentes

**FR-040** — Criar caso manualmente ou por integração.
**FR-041** — Preservar relato original e permitir resumo normalizado.
**FR-042** — Informar cliente, produto, ambiente, versão, impacto, severidade e escopo, com possibilidade de campos desconhecidos.
**FR-043** — Adicionar sintomas normalizados e texto livre.
**FR-044** — Anexar evidências: imagem, log, arquivo, link, payload sanitizado e observação.
**FR-045** — Criar hipóteses e registrar evidências pró/contra.
**FR-046** — Registrar sequência de passos de diagnóstico e tentativas.
**FR-047** — Registrar escalonamentos e handoffs entre departamentos.
**FR-048** — Relacionar casos.
**FR-049** — Encerrar com resolução, validação, causa raiz confirmada ou não confirmada.
**FR-050** — Reabrir preservando histórico.
**FR-051** — Transformar caso resolvido em proposta de solução/conhecimento.

## 4. Base de conhecimento

**FR-060** — Criar artigo/solução estruturada.
**FR-061** — Suportar conteúdo rico em Markdown, blocos de código, tabelas, imagens e anexos.
**FR-062** — Informar sintomas atendidos, aplicabilidade, pré-condições, diagnóstico, solução, validação, riscos e rollback.
**FR-063** — Versionar conteúdo.
**FR-064** — Fluxo de revisão e aprovação.
**FR-065** — Marcar conteúdo obsoleto, substituído ou arquivado.
**FR-066** — Vincular soluções a casos em que foram usadas.
**FR-067** — Registrar resultado da aplicação de uma solução em um caso.
**FR-068** — Exibir taxa observada de sucesso com quantidade de usos e recorte de contexto.
**FR-069** — Permitir comentários técnicos e sugestões de melhoria com moderação.

## 5. Busca e filtros

**FR-080** — Busca global por texto livre.
**FR-081** — Busca por código/mensagem de erro com tratamento de correspondência exata e parcial.
**FR-082** — Busca em casos, soluções, componentes e documentação, respeitando autorização.
**FR-083** — Filtros combináveis por cliente, produto, versão, componente, ambiente, tecnologia, integração, sintoma, causa, status, data, autor, departamento e severidade.
**FR-084** — Ordenar por relevância, recência, reutilização, taxa observada de sucesso e atualização.
**FR-085** — Exibir “por que este resultado apareceu”.
**FR-086** — Salvar consultas/filtros frequentes.
**FR-087** — Registrar buscas sem clique e buscas sem resultado.
**FR-088** — Sugerir casos relacionados durante edição de um caso.

## 6. Diagnóstico guiado

**FR-100** — Iniciar sessão de diagnóstico a partir de caso ou pesquisa avulsa.
**FR-101** — Apresentar perguntas adaptativas.
**FR-102** — Exibir hipóteses candidatas ranqueadas com evidências e contradições.
**FR-103** — Exibir próximos testes sugeridos e risco operacional.
**FR-104** — Registrar resultado do teste sem sair do fluxo.
**FR-105** — Recalcular ordem de hipóteses após nova evidência.
**FR-106** — Recuperar casos/soluções semelhantes durante o diagnóstico.
**FR-107** — Permitir coleta automática por conectores/telemetria quando disponível.
**FR-108** — Sugerir escalonamento quando atingir condição configurada.

## 7. Analytics

**FR-120** — Dashboard executivo.
**FR-121** — Dashboard operacional.
**FR-122** — Dashboard por departamento. Implementado em `/Analytics/Departments`.
**FR-123** — Dashboard de clientes/produtos/componentes.
**FR-124** — Dashboard de conhecimento.
**FR-125** — Dashboard de pesquisa.
**FR-126** — Dashboard de IA/RAG quando habilitado.
**FR-127** — Tela de análise de usuário para gestores autorizados.
**FR-128** — Exportação CSV/XLSX/PDF quando autorizada.
**FR-129** — Drill-down de métricas.
**FR-130** — Períodos comparativos e filtros persistentes.

## 8. Auditoria e administração

**FR-140** — Pesquisar trilha de auditoria.
**FR-141** — Filtrar por ator, entidade, ação, período e correlação.
**FR-142** — Visualizar alterações de campos quando permitido.
**FR-143** — Configurar taxonomias, status, severidades e parâmetros não estruturais.
**FR-144** — Configurar revisão periódica de conhecimento.
**FR-145** — Configurar limites de upload, retenção e políticas.
**FR-146** — Exibir saúde dos jobs, indexação e integrações.

## 9. IA/RAG

**FR-160** — Assistente de pesquisa com pergunta em linguagem natural.
**FR-161** — Recuperar fontes internas relevantes antes da geração.
**FR-162** — Responder com citações internas clicáveis.
**FR-163** — Permitir restringir pergunta por produto, cliente, versão, componente e período.
**FR-164** — Gerar resumo de caso preservando fatos e separando inferências.
**FR-165** — Sugerir sintomas/tags a partir do relato, exigindo confirmação quando impactarem classificação.
**FR-166** — Sugerir casos relacionados.
**FR-167** — Sugerir rascunho de artigo ao encerrar caso, sem publicar automaticamente.
**FR-168** — Registrar feedback sobre resposta.
**FR-169** — Registrar modelo, prompt versionado, fontes recuperadas e identificador de correlação para auditoria técnica.



---

# 03 — Módulos do sistema

## M01 — Identidade e Acesso

Responsável por autenticação, usuários, papéis, permissões, sessões e políticas de segurança.

Telas principais:
- Meu perfil.
- Usuários.
- Perfis e permissões.
- Sessões/dispositivos, se habilitado.
- Eventos de segurança.

## M02 — Estrutura Organizacional

Departamentos (`departments`), gestores, responsáveis técnicos e matrizes de escalonamento. O conceito oficial único de organização é o **Departamento** (a duplicação com `teams` foi extinta na Fase 7.A). Estruturas transversais são suportadas sem silos rígidos entre áreas.

## M03 — Catálogo Técnico

Representa o ecossistema suportado com telas de gestão integrada (`/Catalog/Products`, `/Catalog/Components`):

- clientes (`clients`) com `external_crm_id`, `notes`, unidades (`client_units`) e contextos técnicos (`client_technical_contexts`);
- produtos (`products`) com suporte a produtos externos (`is_external`);
- componentes técnicos (`components`);
- versões de produto (`product_versions`);
- ambientes (`environments`);
- tecnologias e tags;
- dependências direcionadas entre componentes (`component_dependencies`) com tipo e criticidade;
- responsáveis por componente (`component_owners`) atribuídos a Departamentos com papéis Primário, Secundário e Escalonamento.

O catálogo é essencial porque o mecanismo de similaridade depende do contexto técnico, não apenas do texto.

## M04 — Casos e Incidentes

É a memória factual do que aconteceu. Deve registrar o problema desde a entrada até o encerramento, incluindo linha do tempo.

Submódulos:
- abertura com relato original imutável e resumo normalizado editável;
- triagem e contextualização técnica (cliente, unidade, produto, versão, componente, erro);
- iterações e ciclo de reabertura (`case_iterations`), preservando histórico integral de hipóteses e diagnósticos;
- investigação e hipóteses (`case_hypotheses`);
- passos diagnósticos estruturados (`diagnostic_steps`) com sugestão contextual de evidências;
- evidências estruturadas (`case_evidences`) com relação N:N tipificada com hipóteses (`case_hypothesis_evidence`);
- escalonamentos e sessões de diagnóstico;
- resolução (`case_resolutions`) associada à iteração ativa do caso;
- taxonomia de causa raiz corporativa (`root_causes`);
- relacionamento entre casos (`case_relations`);
- revisão pós-incidente.

## M05 — Base de Conhecimento e Soluções

É a camada de conhecimento reutilizável. Diferente de um caso, uma solução deve ser generalizada e declarar quando se aplica.

Tipos previstos:
- solução/troubleshooting;
- procedimento operacional;
- artigo técnico;
- FAQ;
- runbook;
- alerta conhecido/known issue;
- referência de integração;
- lição aprendida.

## M06 — Pesquisa e Similaridade

Responsável por consulta global, filtros, ranking, sugestões, histórico de busca e explicabilidade.

Deve possuir uma interface única capaz de consultar:
- soluções;
- casos;
- documentação;
- componentes;
- mensagens/códigos de erro;
- causas raiz.

## M07 — Diagnóstico Guiado

Camada operacional que conduz investigação a partir do sintoma. Não é uma árvore fixa gigante. O modelo recomendado é um **grafo de verificações**, em que cada verificação possui custo, risco, pré-condições e relação com hipóteses.

## M08 — Analytics e Inteligência Gerencial

Consolida fatos operacionais e estratégicos através de queries agregadas eficientes no banco (`IManagementAnalyticsRepository`), eliminando o carregamento de tabelas inteiras para memória e cálculos divergentes.

Páginas e Módulos Entregues (Fases 10 e 16):
- **Dashboard Geral (`/Analytics/Index`)**: KPIs de casos abertos (`Open`/`Reopened`), resolvidos, MTTR por iteração (`AVG(ClosedAt - OpenedAt)`), mediana, incidentes recorrentes (`Recurrence`/`CommonCause`), sem causa raiz confirmada e sem conhecimento publicado. Séries temporais, sistemas e componentes mais impactados e tabela de casos que requerem atenção com drill-down unificado para `/Cases/Index`.
- **Inteligência Analítica Determinística (Fase 16 / §31)**: Consultas analíticas puras no banco via `IManagementAnalyticsService`:
  - `GetTrendAfterVersionAsync`: variação de volume e MTTR antes vs. depois da publicação de uma versão de produto, com cálculo determinístico de mediana e suficiência de amostra.
  - `GetComponentAssociationPercentageAsync`: distribuição percentual exata de componentes associados a causas e sintomas técnicos.
  - `GetSolutionEffectivenessComparisonAsync`: comparação factual de mediana de MTTR de casos resolvidos com vs. sem uso de uma solução oficial, declarando expressamente insuficiência estatística quando a amostra é reduzida ($N < 3$).
- **Departamentos (`/Analytics/Departments`)**: Visão transversal sem silos ou rankings pejorativos, mensurando volume ativo, resolvido, MTTR, reaberturas, reincidências e autoria de conhecimento cruzando casos diretos, donos de componentes afetados e operadores de diagnóstico.
- **Usuários (`/Analytics/Users`)**: Métricas de engajamento técnico individual e colaboração (casos, resoluções, passos diagnósticos, hipóteses, evidências, autoria e reutilização de artigos), além de perfil técnico emergente baseado em dados reais de atuação recente, sem pontuações artificiais de desempenho.
- **Conhecimento (`/Analytics/Knowledge`)**: Eficácia factual baseada em `KnowledgeUsage` (desfechos `Worked`, `PartiallyWorked`, `DidNotWork`), ciclo de revisão (nunca revisados, revisões vencidas) e lacunas de documentação (resolvidos sem artigo e recorrentes sem causa raiz).

## M09 — Auditoria e Governança

Trilha imutável de ações, alterações estruturais e eventos de conformidade corporativa (Fase 11):
- **Modelo Append-Only Imutável**: Entidade `AuditEvent` sem endpoints ou métodos de remoção/modificação (`IAuditEventRepository`).
- **Sanitização de Dados Sensíveis (BR-101)**: Máscara automática de senhas, tokens, hashes e segredos (`***REDACTED***`) via `AuditService.RecordAsync`.
- **Convenção Corporativa Unificada**: Nomenclatura no formato `entidade.verbo[_objeto]` (ex.: `user.create`, `product.create`, `component.dependency_create`, `case.reopen`).
- **Fechamento de Gaps**: Auditoria em Catálogo Técnico (`CatalogService` com produtos, versões, componentes, dependências e owners), Casos (`CaseService`), Investigação (`CaseInvestigationService` com vínculo de evidência a hipótese) e Conhecimento.
- **Painel de Auditoria (`/Audit/Index`)**: Consulta paginada com filtros superiores por período, ator/usuário, ação, entidade, ID e busca livre em diffs e metadados, com modal de inspeção de payload (`BeforeJson`, `AfterJson`, `MetadataJson`) e drill-down para entidades navegáveis.

## M10 — Integrações

Módulo de catálogo e conexões operacionais do ecossistema TraceCore (Fases 8 e 15):
- **Catálogo Administrativo**: Cadastro centralizado de integrações (`integrations`) com status manual (`Configured`, `Active`, `Inactive`, `Error`), tipo e notas de contrato. As ADRs P005 (conectores ticketing vendor-specific) e P010 (conectores SAP vendor-specific) permanecem em aberto.
- **Health-Check Real HTTP / TCP (Fase 15)**: Serviço `IIntegrationHealthCheckService` com suporte a sondagem determinística via HTTP (GET/POST/HEAD) e ping de socket TCP com timeout configurável.
- **Princípio de Falha Segura (§26)**: Falhas de conexão, rede inacessível, timeout ou status code inesperado registram expressamente `Status = "Failed"` no histórico (`integration_runs`), NUNCA fingindo sucesso.
- **Origem Auditada**: Histórico de execuções com coluna/badge de origem (`TriggeredBy = "Automated"` vs `"Manual"`).
- **Diagnóstico Guiado Automatizado (BR-073)**: Vinculação de `DiagnosticCheck` (`CheckType = AutomatedCheck`) a uma `IntegrationId`. Quando o motor de diagnóstico alcança esse passo, o health-check executa automaticamente sem intervenção humana, gravando o passo como `AutomatedCheck` e avançando a investigação. Se não houver integração vinculada, recai suavemente para pergunta manual ao operador.

## M11 — IA e Preparação Estrutural de Dados

Módulo desacoplado do núcleo da plataforma, responsável pela estruturação, governança e preparação de dados corporativos para inteligência assistiva e semântica (Fase 12):
- **Entidade `SearchableContentEntry`**: Tabela `searchable_content_entries` com chave natural (`source_type`, `source_id`, `source_version_id`), normalização textual, hash SHA-256 e status explicáveis.
- **Normalização com Preservação Técnica**: Pipeline determinístico em `ContentPreparationService` que preserva termos de engenharia literais (`ORA-12541`, `HTTP 500`, `/api/...`, `v8.2.1`).
- **Ingestão Estruturada de Casos e Conhecimento**:
  - Casos: iteração atual, sintomas, componentes afetados, evidências estruturadas com tipo e hipóteses vinculadas, resolução e causa raiz confirmada.
  - Conhecimento: código, resumo, problema, causa raiz, validação, riscos, rollback, aplicabilidades e tecnologias vinculadas.
- **Prontidão Explicável (Sem Scores Probabilísticos)**: Classificação transparente de prontidão para IA (`Ready`, `NeedsMetadata`, `NeedsReview`, `NotEligible`) baseada em integridade factual, sem scores numéricos ou LLMs nesta fase.
- **Visibilidade de Segurança Desacoplada**: Acesso derivado da confidencialidade real (`Public`, `Internal`, `Confidential`, `Restricted`), sem isolamento artificial por departamento.
- **Painel de Qualidade e Prontidão (`/ContentQuality/Index`)**: Métricas de elegibilidade em tempo real, sincronização em lote de fontes e inspeção de payloads estruturados reaproveitando os componentes visuais corporativos (`_AIContentBadge`, `_KnowledgeProvenance`, `_SourceReferenceChip`, `_ConfidenceIndicator`).

## M12 — Administração e Copiloto Operacional

Configurações gerais, taxonomias, jobs, parâmetros, provedores de IA e Copiloto Operacional (Fases 13, 14 e 16):
- **Copiloto RAG Grounded (BR-080 a BR-086)**: Pipeline assistivo disparado por ação explícita do usuário, com pré-filtro híbrido por visibilidade, ranking de cosseno e citação obrigatória de fontes oficiais.
- **Tool Calling de Leitura (Fase 16)**: Ferramenta de leitura estrita `AnalyzeManagementTrend` em `AiToolDefinitions.ReadingTools`, permitindo ao Copiloto consultar métricas e tendências determinísticas do TraceCore.
- **Preservação Factual (§31)**: Apresentação transparente de painel com os dados brutos calculados (`ToolResults`) e instrução de sistema que proíbe o LLM de inventar indicadores, garantindo que o número exibido venha exclusivamente da base primária do TraceCore.
- **Atalho Contextual**: Acesso direto via query string `?Question=...` a partir das telas de Analytics e Diagnóstico.



---

# 04 — Modelo de conhecimento e casos

## 1. Separação conceitual

### Caso
É um evento concreto: “no cliente X, em 12/09, a versão Y apresentou o sintoma Z”. Preserva contexto histórico.

### Solução
É conhecimento reutilizável: “quando ocorrer A em condições B/C, verificar D; se E estiver presente, executar F e validar G”.

### Procedimento
É uma sequência padronizada, não necessariamente ligada a falha.

### Known Issue
Problema conhecido, possivelmente ainda sem correção definitiva, com workaround e versões afetadas.

### Lição aprendida
Conhecimento de processo ou arquitetura decorrente de um ou mais casos.

## 2. Estrutura mínima de um caso

### Identificação
- ID/código.
- origem do chamado.
- referência externa.
- data/hora de abertura.
- cliente/unidade.
- produto.
- ambiente.
- versão.
- severidade/impacto.
- responsável atual.

### Relato
- relato original imutável;
- resumo normalizado;
- sintomas estruturados;
- mensagem/código de erro;
- comportamento esperado;
- comportamento observado;
- escopo: um usuário, grupo, unidade, todos, desconhecido;
- início do problema e frequência.

### Contexto técnico
- componentes suspeitos;
- componentes confirmados;
- integrações envolvidas;
- mudanças recentes conhecidas;
- release/deploy recente;
- dependências indisponíveis;
- sinais de monitoramento.

### Investigação
Para cada passo:
- sequência;
- timestamp;
- autor;
- tipo: pergunta, coleta, teste, ação, mudança, escalonamento;
- hipótese associada;
- objetivo;
- instrução executada;
- evidência de entrada;
- resultado observado;
- classificação do resultado;
- duração aproximada;
- risco;
- anexos;
- reversão, se houve.

### Encerramento
- solução aplicada;
- causa raiz;
- categoria da causa;
- evidência da causa;
- forma de validação;
- impacto final;
- downtime, se aplicável;
- necessidade de ação preventiva;
- conhecimento criado/atualizado;
- casos relacionados.

## 3. Estrutura mínima de uma solução

1. Título objetivo.
2. Resumo.
3. Sintomas que indicam aplicabilidade.
4. Sintomas que indicam **não** aplicabilidade.
5. Produtos/componentes.
6. Versões afetadas/testadas.
7. Ambientes.
8. Pré-condições.
9. Riscos.
10. Diagnóstico passo a passo.
11. Critérios de decisão entre caminhos.
12. Solução passo a passo.
13. Validação pós-solução.
14. Rollback.
15. Tentativas conhecidas que não resolvem e em quais condições.
16. Causa raiz associada, quando conhecida.
17. Referências.
18. Casos que originaram/validaram a solução.
19. Proprietário do conhecimento.
20. Data de revisão futura.

## 4. Conhecimento negativo

A plataforma deve registrar explicitamente “o que não fazer” de forma contextual. Exemplo:

- reiniciar o serviço resolveu temporariamente, mas não removeu a causa;
- limpar cache não alterou o comportamento;
- aumentar timeout mascarou o problema;
- script X não se aplica a versões posteriores à 4.2;
- troca de credencial foi tentada e descartada porque autenticação estava saudável.

Isso reduz repetição de tentativas improdutivas.

## 5. Relações entre itens

Tipos recomendados:
- `derived_from` — solução derivada de caso;
- `validated_by` — solução validada por caso;
- `similar_to` — similaridade manual/confirmada;
- `duplicate_of` — duplicidade;
- `recurrence_of` — recorrência;
- `supersedes` — substitui conteúdo antigo;
- `caused_by` — relação causal conhecida;
- `depends_on` — dependência técnica;
- `workaround_for` — workaround de known issue.

## 6. Qualidade e confiança

Não usar um único “score mágico”. Exibir dimensões:

- status editorial;
- número de casos em que foi aplicada;
- quantidade de sucessos/parciais/falhas;
- última validação;
- versões em que foi testada;
- proprietário;
- revisão vencida ou não;
- similaridade com o caso atual.

## 7. Promoção de caso para conhecimento

Ao resolver um caso, o sistema pergunta:

1. A solução já existe?
2. Se existe, este caso valida ou exige ajuste?
3. Se não existe, a solução é reutilizável?
4. O caso contém particularidade exclusiva do cliente que deve ser removida/generalizada?
5. Há informação sensível que precisa ser sanitizada?

O sistema pode gerar um rascunho, mas publicação depende do fluxo de governança.



---

# 05 — Motor de diagnóstico guiado

## 1. Objetivo

O motor deve ajudar a pessoa da ponta a chegar mais rapidamente a uma hipótese útil e a uma solução segura, mesmo quando ela não domina todas as camadas do ecossistema.

Ele não é um substituto para especialistas. É um mecanismo de **redução ordenada de incerteza**.

## 2. Entrada do diagnóstico

O fluxo pode começar por:

- caso já aberto;
- texto livre do relato;
- código/mensagem de erro;
- seleção de sintoma;
- alerta de monitoramento;
- incidente importado do sistema de chamados.

Contexto conhecido deve ser pré-carregado: cliente, produto, versão, ambiente e dados técnicos disponíveis.

## 3. Etapas lógicas

### Etapa A — Contextualização
Coletar apenas o necessário para evitar busca genérica:
- quem é afetado;
- onde acontece;
- desde quando;
- produto/versão;
- ambiente;
- mensagem de erro;
- mudança recente;
- frequência/reprodutibilidade.

### Etapa B — Geração de hipóteses
Hipóteses podem vir de:
- regras determinísticas;
- known issues;
- casos anteriores;
- relações do catálogo técnico;
- telemetria;
- RAG/LLM futuramente.

### Etapa C — Priorização de verificações
Cada verificação deve conter:
- hipóteses que confirma/descarta;
- custo aproximado;
- risco;
- necessidade de privilégio;
- tempo esperado;
- dependências;
- possibilidade de coleta automática;
- poder discriminativo histórico.

### Etapa D — Execução e registro
O técnico executa/verifica e informa o resultado. A plataforma atualiza o estado.

### Etapa E — Solução ou escalonamento
Quando houver evidência suficiente, recomendar solução publicada. Se a confiança operacional for insuficiente, recomendar coleta adicional ou escalonamento.

## 4. Modelo de hipótese

Campos:
- título;
- descrição;
- componente/camada provável;
- origem da hipótese;
- sinais favoráveis;
- sinais contrários;
- verificações associadas;
- estado: candidata, fortalecida, enfraquecida, descartada, confirmada;
- justificativa;
- autor humano/algorítmico.

A ordenação de hipóteses deve ser tratada como **prioridade investigativa**, não como probabilidade científica, salvo se houver modelo validado para isso.

## 5. Modelo de verificação

Exemplo:

**Verificação:** testar endpoint `/health` da API autenticada.  
**Objetivo:** separar falha de aplicação cliente de indisponibilidade da API.  
**Pré-condição:** usuário com acesso à ferramenta de diagnóstico.  
**Risco:** baixo.  
**Custo:** baixo.  
**Se falhar:** fortalece indisponibilidade/API/rede entre origem e API.  
**Se responder 200:** enfraquece indisponibilidade total da API e direciona para autenticação, regra funcional ou cliente.

## 6. Perguntas adaptativas

As perguntas não devem ser um formulário gigantesco. Devem aparecer conforme relevância.

Exemplo para “não consigo entrar”:

1. Afeta somente um usuário ou vários?
2. A tela de login abre?
3. Há mensagem de erro? Capturar texto exato.
4. O mesmo usuário acessa por outro dispositivo/canal?
5. Outros usuários do mesmo cliente acessam?
6. O serviço de autenticação está saudável?
7. Houve expiração/bloqueio de credencial?
8. A API de autenticação responde?
9. O banco/serviço de identidade está acessível?
10. Houve mudança de versão/configuração recente?

As perguntas 6–9 podem desaparecer se telemetria confiável já responder automaticamente.

## 7. Cálculo inicial de prioridade de próximo passo

Pode-se usar uma função heurística configurável:

`prioridade = poder_discriminativo * confiabilidade / (custo + risco + 1)`

Isto é apenas uma heurística de ordenação. Não deve ser exposta como “verdade matemática”. Dados históricos podem posteriormente calibrar os pesos.

## 8. Escalonamento inteligente

Condições possíveis:
- severidade crítica;
- risco acima da alçada do usuário;
- ausência de progresso após N verificações;
- necessidade de acesso privilegiado;
- hipótese concentrada em componente de outro departamento;
- incidente recorrente acima de limiar;
- suspeita de segurança;
- necessidade de alteração em produção.

Ao escalar, o sistema deve gerar um pacote de contexto com:
- relato original;
- ambiente/versão;
- sintomas;
- hipóteses atuais;
- tudo que já foi testado;
- resultados;
- evidências;
- links para casos similares.

Isso evita que a equipe seguinte reinicie a investigação do zero.

## 9. Anti-padrões proibidos

- árvore fixa com centenas de nós impossível de manter;
- “se não sabe, manda para Infra”;
- perguntas repetidas quando a resposta já está no caso;
- sugerir reinicialização como primeira resposta universal;
- ação destrutiva sem aviso;
- excluir hipótese sem registrar evidência;
- encerrar caso apenas porque o sintoma desapareceu, sem registrar validação.

## 10. Implementação Técnica da Fase 9 (M07, BR-070 a BR-076)

A Fase 9 consolidou o motor de diagnóstico guiado de acordo com os princípios de grafo configurável em banco de dados e heurística causal explicada:

1. **Grafo de Decisão em Dados (`diagnostic_flows` e tabelas filhas)**:
   - Eliminação de árvores hardcoded em código C#.
   - Fluxos identificados por código e palavras-chave de entrada (matching no relato original e resumo normalizado).
   - Checagens (`diagnostic_checks`) com múltiplos nós de opção (`diagnostic_check_options`) e impactos direcionados (`diagnostic_check_impacts`) ponderando hipóteses (`Favors`, `Discards`, `Neutral`).

2. **Heurística de Triagem sem Falsas Certezas (BR-074, P-006)**:
   - Exclusão de termos pseudo-estatísticos ("88% de probabilidade", "verdade matemática").
   - Fórmula determinística de ordenação causal:
     $$\text{prioridade} = \frac{\text{poder\_discriminativo} \times \text{confiabilidade}}{\text{custo} + \text{risco} + 1}$$
   - Apresentação visual sob rotulagem de prioridade investigativa (`Alta`, `Média`, `Baixa`) acompanhada da justificativa do teste.

3. **Integração com a Linha do Tempo e Imutabilidade (Fase 4 & BR-071/BR-075)**:
   - Hipóteses ativadas pelo motor são instanciadas na tabela `case_hypotheses` com `source_type = 'Guided'`.
   - Perguntas respondidas são gravadas como passos de diagnóstico (`diagnostic_steps`) sob o tipo `GuidedQuestion` com outcomes reais do enum (`Worked`, `DidNotWork`, `Inconclusive`).
   - Recomendações desconsideradas pelo operador (Bypass) exigem justificativa técnica obrigatória e são auditadas na linha do tempo com `step_type = 'RecommendationIgnored'`.
   - Avaliação formal de hipótese (`Supported`/`Discarded`) ocorre quando o peso cumulativo da opção atinge o limiar $\ge 2.0m$, respeitando o método `Evaluate` imutável.

4. **Escalonamento Inteligente (§8)**:
   - Alerta textual proativo de escalonamento exibido quando $\ge 5$ checagens são respondidas sem redução ou convergência das hipóteses ativas, orientando transferência com pacote de contexto para nível 3/especialista.



---

# 06 — Pesquisa, filtros e similaridade

## 1. Objetivo

A pesquisa deve responder a duas necessidades diferentes:

1. **“Eu sei o que procuro.”** — busca por erro, componente, artigo, caso ou palavra-chave.
2. **“Eu tenho um sintoma e quero descobrir o que se parece com isso.”** — busca contextual/similaridade.

## 2. Tipos de busca

### Busca exata
Prioridade para:
- código de erro;
- identificador de caso;
- endpoint;
- nome de tabela/componente;
- exceção;
- trecho de log.

### Busca lexical
MySQL FULLTEXT onde aplicável, combinada com campos normalizados e índices tradicionais.

### Busca estruturada
Filtros por entidades e taxonomias.

### Busca semântica
Fase posterior do RAG. Usa embeddings para recuperar itens conceitualmente próximos mesmo sem mesmas palavras.

### Busca híbrida
Combina lexical + estruturada + semântica + sinais de qualidade.

## 3. Filtros obrigatórios

- período;
- cliente/unidade;
- produto;
- módulo;
- versão;
- ambiente;
- componente;
- tipo de componente;
- tecnologia;
- integração;
- SAP/outro sistema externo;
- sintoma;
- código de erro;
- causa raiz;
- severidade;
- impacto;
- status do caso;
- status editorial do conhecimento;
- departamento;
- autor/revisor;
- “somente conteúdo vigente”;
- “somente soluções já validadas”;
- “com/sem causa raiz confirmada”.

## 4. Ranking inicial sem IA

Sugestão de sinais, com pesos configuráveis e calibráveis:

- correspondência exata de erro/termo: alta prioridade;
- mesmo produto/componente: alta;
- mesma versão/faixa compatível: alta;
- mesmos sintomas: alta;
- mesmo ambiente: média;
- casos relacionados manualmente: alta;
- solução publicada/validada: alta;
- taxa observada de sucesso: média, sempre com amostra;
- recência da última validação: média;
- conteúdo obsoleto: penalização severa;
- incompatibilidade conhecida: exclusão/penalização.

## 5. Ranking híbrido futuro

Uma fórmula de referência, a ser calibrada em dados reais:

- 30% relevância lexical/exata;
- 25% similaridade semântica;
- 20% aderência de contexto técnico;
- 10% aderência de sintomas;
- 10% qualidade/validação histórica;
- 5% atualidade.

Não congelar esses pesos como regra de negócio. Eles devem ser configuração versionada e avaliados por métricas de recuperação.

## 6. Explicabilidade do resultado

Cada resultado deve trazer motivos como:

- “mesmo código de erro”;
- “3 de 4 sintomas coincidem”;
- “mesmo produto e componente”;
- “validado na versão 6.3 e aplicável à 6.x”;
- “caso relacionado manualmente por especialista”;
- “conteúdo semanticamente semelhante”.

## 7. Métricas da pesquisa

- consultas por período;
- zero-result rate;
- zero-click rate;
- tempo até primeiro clique útil;
- posição do item marcado como útil;
- reformulação de consulta;
- filtros mais usados;
- termos que mais geram lacuna;
- taxa de reutilização de solução após busca;
- MRR/NDCG/Recall@K em conjunto de avaliação do mecanismo, quando houver ground truth.

## 8. Feedback

Após usar um resultado:
- útil / não útil;
- resolveu / resolveu parcialmente / não resolveu;
- motivo opcional;
- “não se aplica à minha versão”;
- “desatualizado”;
- “faltou informação”.

Feedback não altera ranking de forma imediata e irrestrita; deve passar por mecanismos de qualidade para evitar ruído/manipulação.

## 9. Sinônimos e vocabulário

Manter dicionário administrável:
- apelidos de sistemas;
- siglas;
- nomes antigos;
- termos de cliente;
- grafias recorrentes;
- equivalência técnica.

Exemplo: “login”, “autenticação”, “acesso”, “entrar no sistema” podem compartilhar relação semântica, mas o sistema não deve assumir que são sempre idênticos.



---

# 07 — Analytics e gestão

## 1. Princípio

Analytics deve apoiar decisões reais: onde há gargalo, o que mais reincide, que conhecimento falta, quais componentes concentram falhas, quanto tempo se perde em handoffs e quais soluções efetivamente reduzem tempo de resolução.

Evitar dashboards decorativos.

## 2. Dashboard executivo

KPIs principais:
- casos abertos/resolvidos por período;
- backlog e envelhecimento;
- MTTA (tempo até primeira atuação);
- MTTR (tempo até resolução);
- reincidência em 7/30/90 dias;
- casos críticos;
- componentes com maior impacto;
- clientes com maior volume/impacto;
- percentual de casos que reutilizaram conhecimento existente;
- economia estimada de tempo por reutilização;
- lacunas críticas de conhecimento.

Gráficos:
- tendência mensal/semanal;
- Pareto de causas;
- Pareto de componentes;
- distribuição de tempo de resolução;
- heatmap por dia/horário;
- fluxo de handoffs;
- top recorrências.

## 3. Dashboard operacional

- casos por estado;
- casos sem responsável;
- casos sem atualização;
- SLA/SLO interno, se existir;
- severidade;
- fila por departamento;
- tempo em cada etapa;
- quantidade de handoffs;
- hipóteses mais frequentes;
- etapas diagnósticas que mais resolvem/eliminam hipóteses;
- casos semelhantes abertos simultaneamente.

## 4. Dashboard de conhecimento

- artigos por estado;
- artigos com revisão vencida;
- conhecimento sem proprietário;
- artigos mais usados;
- artigos com maior sucesso observado;
- artigos com falhas recorrentes;
- artigos que nunca foram usados;
- artigos mais encontrados em busca;
- artigos com feedback negativo;
- soluções por produto/componente;
- lacunas: alta incidência de casos sem solução reutilizável.

Sempre mostrar tamanho da amostra ao falar de taxa de sucesso.

## 5. Dashboard de pesquisa

- volume;
- termos principais;
- zero result;
- zero click;
- consultas reformuladas;
- filtros;
- conteúdo aberto;
- pesquisas que terminaram em solução;
- consultas que geraram escalonamento;
- termos emergentes.

## 6. Dashboard de tecnologia

- incidentes por produto/módulo/componente;
- causa por componente;
- versões com maior incidência;
- incidentes após release;
- integrações com maior falha;
- dependências críticas;
- reincidência pós-correção;
- tempo de resolução por classe técnica.

## 7. Dashboard de cliente

- volume de casos;
- severidade;
- recorrência;
- produtos afetados;
- top sintomas;
- top causas;
- tempo de resolução;
- incidentes específicos do ambiente do cliente versus gerais;
- soluções mais usadas.

## 8. Visão de usuário/colaborador

Para gestores autorizados:
- casos em que participou;
- casos em que foi responsável;
- tempo de permanência em sua etapa, contextualizado;
- contribuições de conhecimento;
- revisões/aprovações;
- soluções utilizadas por outros;
- feedback recebido em conteúdos;
- histórico de ações administrativas/técnicas auditáveis;
- handoffs realizados/recebidos;
- áreas e tecnologias em que mais atuou.

### Importante
Não criar “nota geral do funcionário” ou ranking automático de pessoas. Métricas podem refletir complexidade, alçada, escala de trabalho e perfil de função. A tela deve apoiar gestão e desenvolvimento, não substituir análise humana.

## 9. Dashboard de diagnóstico

- perguntas mais feitas;
- perguntas que mais discriminam hipóteses;
- testes mais usados;
- testes com baixo valor e alto custo;
- caminhos mais comuns;
- momento em que casos são escalados;
- número médio de passos até solução;
- hipóteses frequentemente descartadas;
- caminhos que se repetem e merecem automação.

## 10. Dashboard de IA/RAG

Quando habilitado:
- perguntas realizadas;
- respostas com/sem fonte suficiente;
- latência;
- custo por modelo/provedor;
- tokens;
- taxa de feedback útil;
- taxa de citação aberta;
- casos em que sugestão foi usada;
- respostas rejeitadas;
- falhas de recuperação;
- conteúdos mais recuperados;
- avaliação de groundedness e precisão em dataset interno.

## 11. Modelo analítico

Não executar todos os gráficos diretamente sobre tabelas transacionais complexas. Criar:

- eventos de domínio;
- fatos agregados diários/horários;
- snapshots de backlog;
- tabelas de métricas materializadas por job;
- definições versionadas de KPI.

Exemplo de fatos:
- `fact_case_lifecycle`;
- `fact_case_handoff`;
- `fact_solution_usage`;
- `fact_search_session`;
- `fact_knowledge_review`;
- `fact_ai_interaction`.

## 12. Definições mínimas

**MTTR:** `resolved_at - opened_at`, com segmentação por severidade e exclusões explicitamente documentadas.  
**Reincidência:** novo caso relacionado à mesma causa/componente dentro de janela definida. A janela deve ser configurável e a fórmula versionada.  
**Reutilização de conhecimento:** caso em que uma solução existente foi vinculada e marcada como usada.  
**Sucesso de solução:** número de usos classificados como “funcionou” dividido por usos com resultado classificável; exibir amostra e parciais separadamente.



---

# 08 — RAG e IA

## 1. O que RAG significa neste projeto

RAG (*Retrieval-Augmented Generation*) não é banco de dados e não substitui o MySQL. É um fluxo em que a aplicação:

1. recebe uma pergunta;
2. identifica contexto e restrições;
3. recupera conteúdo relevante da base;
4. seleciona os melhores trechos/fontes;
5. envia pergunta + contexto para a LLM;
6. recebe uma resposta;
7. exibe a resposta com referências;
8. registra feedback e telemetria.

O MySQL permanece como fonte oficial de casos, usuários, relações, soluções, taxonomias, auditoria e permissões.

## 2. Princípio arquitetural

A IA deve ser um módulo substituível. O núcleo do produto deve continuar funcionando se:
- o provedor de LLM estiver indisponível;
- o recurso de embeddings estiver desligado;
- a empresa trocar de provedor;
- o custo de IA exigir limitação temporária.

Interfaces sugeridas:

```csharp
public interface IEmbeddingProvider { }
public interface IVectorSearchProvider { }
public interface ILanguageModelProvider { }
public interface IRagRetriever { }
public interface IAiAnswerService { }
```

Não acoplar regras de negócio a SDK específico de fornecedor.

## 3. Fases de evolução

### Fase IA-0 — Base pronta para IA
Antes de usar LLM:
- dados estruturados;
- artigos segmentáveis;
- permissões;
- taxonomia;
- busca lexical;
- casos relacionados;
- qualidade editorial;
- auditoria.

### Fase IA-1 — Assistência semântica
- embeddings de artigos/casos sanitizados;
- busca semântica;
- sugestão de casos semelhantes;
- sem geração de resposta ainda.

### Fase IA-2 — RAG com resposta fundamentada
- pergunta natural;
- busca híbrida;
- citações;
- resposta com limites claros;
- feedback.

### Fase IA-3 — Copiloto de diagnóstico
- sugere perguntas;
- resume linha do tempo;
- compara hipóteses;
- propõe próximos testes;
- gera rascunho de solução;
- nunca executa ação destrutiva sem fluxo de autorização.

### Fase IA-4 — Avaliação e automações controladas
- conjuntos de testes internos;
- avaliação automática + humana;
- automações somente em ações explicitamente permitidas.

## 4. Chunking

Não quebrar documentos por número arbitrário de caracteres apenas. Preservar unidades semânticas:
- título;
- objetivo;
- pré-condições;
- sintomas;
- passo de diagnóstico;
- passo de solução;
- validação;
- rollback;
- notas de versão.

Metadados obrigatórios em cada chunk:
- `knowledge_item_id`;
- versão do conteúdo;
- título/cabeçalho;
- produto;
- componente;
- versões aplicáveis;
- ambiente;
- classificação de acesso;
- status editorial;
- data de atualização;
- hash do conteúdo.

## 5. Vetores e MySQL

O MySQL será o banco principal. A camada vetorial deve ficar atrás de uma interface. Há três estratégias possíveis:

### Estratégia A — MySQL + busca lexical inicialmente
Recomendada para MVP. Menor complexidade. O produto já entrega valor antes de RAG.

### Estratégia B — MySQL como fonte de verdade + índice vetorial especializado
Quando o volume/latência justificar, embeddings podem ser indexados em mecanismo vetorial externo. O registro canônico continua no MySQL. O índice é reconstruível.

### Estratégia C — Recurso vetorial compatível com a implantação MySQL adotada
Pode ser usado se a edição/serviço MySQL homologado oferecer desempenho e operação adequados. Deve passar por benchmark real antes de virar decisão arquitetural.

O projeto não deve ficar preso à estratégia B ou C desde o primeiro commit.

## 6. Pipeline de indexação

1. conteúdo é publicado/alterado;
2. evento `KnowledgePublished` é gravado no outbox;
3. worker consome evento;
4. sanitiza e segmenta;
5. gera embeddings;
6. grava índice vetorial e metadados;
7. marca versão indexada;
8. em falha, reprocessa com idempotência;
9. conteúdo obsoleto é removido/penalizado do índice ativo sem apagar histórico.

## 7. Recuperação híbrida

Fluxo sugerido:

1. autorização do usuário;
2. normalização da pergunta;
3. identificação de filtros explícitos;
4. busca lexical/exata;
5. busca semântica;
6. fusão de rankings;
7. reranking opcional;
8. aplicação final de ACL;
9. montagem do contexto;
10. geração.

Nunca confiar apenas na ACL do vetor. A autorização final deve ocorrer no servidor sobre IDs canônicos.

## 8. Prompting

O prompt de sistema interno deve impor:
- usar fontes fornecidas;
- separar fato, hipótese e inferência;
- citar fonte interna;
- não inventar procedimento;
- não omitir risco/rollback existente;
- pedir dado objetivo quando contexto for insuficiente;
- preferir solução publicada a rascunho;
- alertar incompatibilidade de versão.

Prompts devem ser versionados e auditáveis.

## 9. Segurança da IA

- remover segredos antes de indexar;
- não enviar credenciais/tokens;
- respeitar classificação de conteúdo;
- limitar dados de cliente ao contexto autorizado;
- armazenar IDs de fontes e hashes, não necessariamente toda a conversa em log técnico;
- proteger contra prompt injection em documentos recuperados;
- tratar conteúdo recuperado como dado, não como instrução de sistema;
- aplicar allowlist de ferramentas se houver function calling.

## 10. Avaliação

Criar dataset interno com perguntas reais e respostas/fontes esperadas.

Métricas:
- Recall@K da recuperação;
- precisão de fonte;
- groundedness;
- completude;
- taxa de resposta sem evidência;
- citação correta;
- taxa de utilidade humana;
- latência;
- custo.

A avaliação deve ocorrer antes de mudanças de modelo, chunking, embedding ou ranking irem para produção.



---

# 09 — Arquitetura técnica — .NET/C# + MySQL

## 1. Stack baseline (implementada)

### Aplicação
- **.NET 10** (`net10.0`).
- **C# 14**.
- **ASP.NET Core 10**.
- **ASP.NET Core Razor Pages** para UI web em C# (revisão de ADR-0004; Blazor Web App era a baseline original, não foi o que se implementou — ver `21_ADRS_E_DECISOES_ABERTAS.md`).
- **SignalR** para notificações/atualizações em tempo real quando necessário (ainda não exercitado em telas de produção).
- **Dapper + MySqlConnector** como caminho de persistência (ADR revisada — `Persistence:Provider` nunca esteve ativo; banco via SQL direto nos repositórios).
- **FluentMigrator** para migrações SQL versionadas (29 migrations: `M20260917_01` a `M20260922_29`).
- `System.Text.Json` para serialização.
- `Microsoft.Extensions.*` para DI, configuração, logging e options.
- Bootstrap 5 + Bootstrap Icons (locais, sem CDN) e Design System próprio (`css/tokens.css`, `css/base.css`, componentes em `css/components/*`).

### Banco
- **MySQL 8.4 LTS** como baseline de produção.
- InnoDB.
- UTF8MB4.
- timezone persistido em UTC; conversão na UI.

### IA / LLM (Fase 17)
- Provedores desacoplados em `llm_providers`/`llm_model_configs` (protocolos `OpenAICompatible` e `AnthropicMessages`).
- Interfaces `ILlmProviderResolver`, `ILlmModelCatalog` (`LlmModelEntry`), `ISecretStore` (chave `llm_apikey_{providerCode}`; implementada por `ProtectedFileSecretStore` via ASP.NET Core Data Protection — criptografado em repouso em `App_Data/Secrets/`, com fallback de configuração `Llm:{providerCode}:ApiKey`).
- Catálogo dinâmico de modelos via API do provedor no painel `Settings/LlmProviders`.

### Observabilidade (direção)
- OpenTelemetry.
- logs estruturados.
- métricas.
- tracing distribuído para integrações.

### Testes (implementado)
- xUnit com **105 testes de integração aprovados** em `tests/TraceCore.IntegrationTests` (Testcontainers/MySQL).
- Playwright for .NET para E2E da UI na direção futura.

## 2. Estilo arquitetural

### Monólito modular
Escolha inicial recomendada e adotada.

Motivos:
- domínio ainda vai amadurecer;
- transações entre módulos são frequentes;
- menor complexidade operacional;
- implantação simples;
- refatoração mais fácil;
- evita microserviços prematuros.

Módulos devem possuir limites claros e não acessar tabelas internas de outro módulo de forma arbitrária.

## 3. Estrutura da solution (real)

```text
TraceCore.sln
src/
  TraceCore.Web/             # Razor Pages UI (Pages/, css Design System, wwwroot)
  TraceCore.Api/             # endpoints HTTP externos/internos
  TraceCore.Application/     # casos de uso e serviços
  TraceCore.Domain/          # entidades, regras puras e contratos de serviço
  TraceCore.Infrastructure/  # MySQL, migrations (FluentMigrator, 29), repositórios, serviços Llm
  TraceCore.Contracts/       # DTOs/eventos públicos
  TraceCore.Worker/          # jobs, indexação, agregações
  TraceCore.Shared/          # somente abstrações realmente comuns
tests/
  TraceCore.IntegrationTests/  # 105 testes aprovados (MySQL/Testcontainers)
```

Se o repositório preferir vertical slices, módulos podem ser subdivididos internamente sem quebrar essa separação macro.

## 4. Módulos lógicos

- Identity
- Organization
- Catalog
- Cases
- Knowledge
- Diagnostics
- Search
- Analytics
- Audit
- Integrations
- Ai
- Administration
- Notifications

Cada módulo deve expor comandos/queries/serviços públicos e ocultar detalhes de persistência.

## 5. Fluxo de uma requisição

```text
Razor Pages/API
  -> Authorization
  -> Application Use Case
  -> Domain Rules
  -> Repository/Query Service
  -> MySQL / Integration
  -> Domain Event / Outbox
  -> Response
```

UI não acessa banco diretamente.

## 6. Padrão de aplicação

Usar commands e queries explicitamente, sem necessidade de introduzir framework CQRS pesado.

Exemplos:
- `CreateCaseCommand`
- `AddDiagnosticStepCommand`
- `ResolveCaseCommand`
- `PublishKnowledgeItemCommand`
- `SearchKnowledgeQuery`
- `GetCaseTimelineQuery`

Handlers devem:
1. validar autorização;
2. validar input;
3. carregar estado necessário;
4. aplicar regra;
5. persistir atomicamente;
6. gerar evento/outbox;
7. registrar auditoria conforme regra.

## 7. Persistência

### Dapper
Usar para comandos e consultas explícitas. Não espalhar SQL em componentes Blazor.

Organização:
```text
Infrastructure/Persistence/
  Migrations/
  Repositories/
  Queries/
  TypeHandlers/
```

### Transações
Criar abstração `IUnitOfWork` para uma conexão/transação por caso de uso quando necessário.

### Concorrência
Entidades editáveis críticas devem possuir `row_version` numérico ou estratégia equivalente de optimistic concurrency.

### Soft delete
Aplicar apenas quando a regra exigir preservação histórica. Preferir estados (`Inactive`, `Archived`) quando semanticamente melhores.

## 8. Outbox

Eventos que disparam processamento assíncrono devem usar Transactional Outbox no MySQL.

Exemplos:
- publicação de conhecimento;
- caso resolvido;
- atualização de índice de busca;
- agregação analítica;
- notificação;
- indexação RAG.

O evento e a alteração principal são gravados na mesma transação.

## 9. Jobs

Worker .NET separado, mas mesmo monorepo/deploy lógico inicialmente.

Responsabilidades:
- consumir outbox;
- agregações analíticas;
- revisão vencida;
- indexação;
- integração externa;
- limpeza de dados temporários;
- health checks programados.

Jobs precisam ser idempotentes e ter política de retry/backoff.

## 10. Cache

Não tornar Redis obrigatório no MVP. Usar cache local para metadados de baixa volatilidade quando útil. Introduzir cache distribuído apenas mediante necessidade medida.

## 11. Arquivos e anexos

Não armazenar arquivos grandes diretamente no MySQL por padrão.

Banco guarda:
- ID;
- nome;
- hash;
- tamanho;
- MIME;
- classificação;
- storage key;
- autor;
- entidade relacionada.

Storage físico deve ser abstrato por `IFileStorage`.

## 12. Feature flags

Recursos de IA, conectores e diagnósticos experimentais devem poder ser habilitados por configuração/feature flag.

## 13. APIs internas e externas

- REST JSON para integrações e automação (endpoints atuais em `/api/cases/...` no pipeline do `TraceCore.Web`; ver ADR-P011).
- endpoints versionados `/api/v1/...` como contrato alvo caso a API seja formalizada em projeto dedicado.
- Problem Details RFC 9457 para erros HTTP.
- idempotency key em operações externas de criação quando necessário.
- correlação por `trace_id`/`correlation_id`.

## 14. Decisão importante sobre EF Core

A baseline deste documento usa Dapper/SQL direto para reduzir risco de compatibilidade entre .NET 10/EF Core 10 e providers MySQL. ADR revisada: `Persistence:Provider` nunca esteve ativo; o domínio não depende de EF Core (`src/TraceCore.Domain` sem referência a banco). Se no futuro a equipe quiser EF Core, executar spike técnico e registrar ADR com provider, versão, suporte, migrações, concorrência e testes.



---

# 10 — Modelo de dados

## 1. Diretrizes

- MySQL é a fonte de verdade transacional.
- IDs: `BIGINT` padronizado em todas as tabelas e chaves estrangeiras.
- Timestamps em UTC.
- `created_at`, `created_by`, `updated_at`, `updated_by` onde fizer sentido.
- dados flexíveis podem usar JSON, mas dimensões importantes para busca/analytics devem ser normalizadas.
- chaves estrangeiras e índices devem refletir integridade e consultas reais.
- não usar JSON para esconder um modelo que deveria ser relacional.

## 2. Grupos de tabelas

### Identidade
- `users`
- `roles`
- `permissions`
- `user_roles`
- `role_permissions`
- `user_sessions`

### Organização (Fase 7.A: Departamento é conceito oficial único; `teams`/`user_teams` dropados)
- `departments`
- `user_departments`

### Catálogo
- `clients`
- `client_units`
- `client_technical_contexts`
- `products` (com `is_external`)
- `product_modules`
- `product_versions`
- `environments`
- `components`
- `component_dependencies`
- `component_owners`
- `technologies`
- `component_technologies`
- `integrations`

### Casos
- `cases` (com `client_unit_id`)
- `case_iterations`
- `case_symptoms`
- `case_components`
- `case_hypotheses`
- `case_hypothesis_evidence`
- `diagnostic_sessions`
- `diagnostic_steps`
- `case_evidences`
- `case_handoffs`
- `case_relations`
- `root_causes`
- `case_resolutions`

### Conhecimento
- `knowledge_items`
- `knowledge_versions`
- `knowledge_applicability`
- `knowledge_symptoms`
- `knowledge_steps`
- `knowledge_relations`
- `knowledge_reviews`
- `knowledge_comments`
- `knowledge_usages`
- `tags`
- `knowledge_tags`

### Busca/IA
- `search_sessions`
- `search_queries`
- `search_result_interactions`
- `ai_interactions`
- `ai_sources`
- `embedding_index_state` (estado canônico, não necessariamente vetor bruto)

### Auditoria/Plataforma
- `audit_events`
- `outbox_messages`
- `attachments`
- `system_settings`
- `feature_flags`
- `integration_runs`

### Analytics
- `agg_case_daily`
- `agg_solution_usage_daily`
- `agg_search_daily`
- `agg_component_daily`
- `agg_user_activity_daily`

## 3. Entidade `cases`

Campos principais:

```text
id
case_number
external_reference
source_type
client_id
client_unit_id
product_id
product_version_id
environment_id
original_report
normalized_summary
expected_behavior
observed_behavior
error_code
error_message
scope_type
severity
impact_level
status
current_owner_user_id
current_department_id
opened_at
first_response_at
resolved_at
closed_at
root_cause_status
created_at / created_by
updated_at / updated_by
row_version
```

## 4. `diagnostic_steps`

```text
id
diagnostic_session_id
sequence_no
step_type
hypothesis_id nullable
title
objective
instruction
input_evidence_summary
result_summary
outcome
risk_level
duration_seconds
performed_by
performed_at
metadata_json
```

`outcome`: `Worked`, `PartiallyWorked`, `DidNotWork`, `NotApplicable`, `Inconclusive`, `ObservationOnly`.

## 5. `knowledge_items`

```text
id
knowledge_code
type
title
summary
status
confidentiality
owner_user_id
owner_department_id
current_version_no
review_due_at
published_at
deprecated_at
replacement_knowledge_id nullable
created_at / created_by
```

O corpo versionado fica em `knowledge_versions`.

## 6. `knowledge_usages`

Registra aplicação real em um caso:

```text
id
knowledge_item_id
knowledge_version_id
case_id
used_by
used_at
outcome
notes
context_match_json
```

É a base correta para estatística de sucesso.

## 7. `component_dependencies`

```text
id
source_component_id
target_component_id
dependency_type
criticality
description
valid_from
valid_to
```

A direção deve ser inequívoca: `source depende de target`.

## 8. `audit_events`

```text
id
occurred_at
actor_user_id nullable
actor_type
action
entity_type
entity_id
correlation_id
ip_hash_or_address conforme política
user_agent_summary
before_json nullable
after_json nullable
metadata_json
```

Segredos devem ser mascarados antes da gravação.

## 9. `outbox_messages`

```text
id
event_type
aggregate_type
aggregate_id
payload_json
occurred_at
processed_at
attempt_count
next_attempt_at
last_error
status
```

## 10. `case_relations` (Fase 8)

```text
id BIGINT PRIMARY KEY AUTO_INCREMENT
source_case_id BIGINT NOT NULL (FK cases)
target_case_id BIGINT NOT NULL (FK cases)
relation_type VARCHAR(64) NOT NULL (Similar, Duplicate, Recurrence, CommonCause, Dependency, Reference)
similarity_score DOUBLE NULL
matched_factors_json JSON NULL
created_by BIGINT NULL (FK users; NULL para relações automáticas 'Similar')
created_at DATETIME NOT NULL
UNIQUE KEY uq_case_relations_pair_type (source_case_id, target_case_id, relation_type)
```

## 11. Motor de Diagnóstico Guiado (Fase 9 / M07)

```text
diagnostic_flows
  id BIGINT PRIMARY KEY AUTO_INCREMENT
  code VARCHAR(64) NOT NULL UNIQUE
  name VARCHAR(255) NOT NULL
  description TEXT NULL
  initial_keywords_json JSON NOT NULL
  is_active BOOLEAN NOT NULL DEFAULT TRUE
  created_at DATETIME NOT NULL
  updated_at DATETIME NULL

diagnostic_flow_hypotheses
  id BIGINT PRIMARY KEY AUTO_INCREMENT
  flow_id BIGINT NOT NULL (FK diagnostic_flows)
  title VARCHAR(255) NOT NULL
  description TEXT NULL
  component_id BIGINT NULL (FK components)
  prior_weight DECIMAL(5,2) NOT NULL DEFAULT 1.00
  sort_order INT NOT NULL DEFAULT 0

diagnostic_checks
  id BIGINT PRIMARY KEY AUTO_INCREMENT
  flow_id BIGINT NOT NULL (FK diagnostic_flows)
  code VARCHAR(64) NOT NULL
  title VARCHAR(255) NOT NULL
  question TEXT NOT NULL
  cost_score INT NOT NULL DEFAULT 1
  risk_level VARCHAR(32) NOT NULL DEFAULT 'Low'
  discriminative_power DECIMAL(5,2) NOT NULL DEFAULT 1.00
  skip_condition_field VARCHAR(128) NULL
  sort_order INT NOT NULL DEFAULT 0
  created_at DATETIME NOT NULL
  UNIQUE KEY uq_diag_flow_check_code (flow_id, code)

diagnostic_check_options
  id BIGINT PRIMARY KEY AUTO_INCREMENT
  check_id BIGINT NOT NULL (FK diagnostic_checks)
  option_text VARCHAR(255) NOT NULL
  sort_order INT NOT NULL DEFAULT 0

diagnostic_check_impacts
  id BIGINT PRIMARY KEY AUTO_INCREMENT
  option_id BIGINT NOT NULL (FK diagnostic_check_options)
  target_hypothesis_id BIGINT NOT NULL (FK diagnostic_flow_hypotheses)
  impact_type VARCHAR(32) NOT NULL (Favors, Discards, Neutral)
  weight DECIMAL(5,2) NOT NULL DEFAULT 1.00
```

## 12. `searchable_content_entries` (Fase 12 / M11)

```text
id BIGINT PRIMARY KEY AUTO_INCREMENT
source_type VARCHAR(50) NOT NULL (ValidatedKnowledge, HistoricalCase, Document, AiSuggestion)
source_id BIGINT NOT NULL
source_version_id BIGINT NULL
title VARCHAR(500) NOT NULL
normalized_content LONGTEXT NOT NULL
content_hash VARCHAR(64) NOT NULL (SHA-256)
validation_status VARCHAR(50) NOT NULL (Validated, PendingValidation, NotValidated, Rejected)
quality_status VARCHAR(50) NOT NULL (Complete, Incomplete, NeedsReview, Validated, Obsolete)
visibility VARCHAR(50) NOT NULL (Public, Internal, Confidential, Restricted)
client_id BIGINT NULL (FK clients)
product_id BIGINT NULL (FK products)
component_ids_json TEXT NULL
metadata_json LONGTEXT NULL
created_at DATETIME NOT NULL
updated_at DATETIME NOT NULL
source_updated_at DATETIME NOT NULL
indexed_at DATETIME NULL (reservado)
embedding_version VARCHAR(50) NULL (reservado)
INDEX ix_searchable_source (source_type, source_id, source_version_id)
INDEX ix_searchable_hash (content_hash)
INDEX ix_searchable_quality (quality_status)
INDEX ix_searchable_validation (validation_status)
INDEX ix_searchable_updated (updated_at DESC)
```

## 13. Índices mínimos

- status + datas em `cases`;
- cliente/produto/componente por tabelas de associação;
- `FULLTEXT` em campos textuais definidos por benchmark;
- `error_code` índice normal;
- `knowledge_items(status, review_due_at)`;
- `knowledge_usages(knowledge_item_id, outcome, used_at)`;
- `audit_events(entity_type, entity_id, occurred_at)`;
- `audit_events(actor_user_id, occurred_at)`;
- `searchable_content_entries(source_type, source_id, source_version_id)`;
- `searchable_content_entries(content_hash)`;
- `searchable_content_entries(quality_status, validation_status)`;
- outbox por `status,next_attempt_at`;
- relações e FKs nos dois sentidos de consultas frequentes;
- fluxos e checagens por `code` e `flow_id`.

## 14. Retenção

A retenção exata depende de política corporativa. No modelo:
- casos: longo prazo;
- versões de conhecimento: histórico completo;
- auditoria: período definido por segurança/compliance;
- interações de busca/IA: retenção menor e anonimização/agregação quando possível;
- anexos: política por classificação.



---

# 11 — API e integrações

## 1. Objetivo

A plataforma deve possuir contratos claros para permitir integração com sistemas de chamados, monitoramento, produtos web/desktop/mobile, SAP, APIs corporativas e futuras automações.

## 2. Convenções REST

### Estado atual da implementação

A arquitetura original previa uma API REST dedicada em `/api/v1`. Na implementação real (ver ADR-P011), os endpoints HTTP foram **implementados diretamente no pipeline do `TraceCore.Web/Program.cs`**, sem o prefixo `/api/v1`:

```text
POST /api/cases                          (caso.criar)
GET  /api/cases/{id}                     (caso.visualizar)
POST /api/cases/{caseId}/hypotheses      (caso.diagnosticar)
POST /api/cases/{caseId}/diagnostic-steps (caso.diagnosticar)
POST /api/hypotheses/{hypothesisId}/evaluate (caso.diagnosticar)
GET  /api/cases/{caseId}/investigation-timeline (caso.visualizar)
POST /api/test/operacao-protegida        (usuario.gerenciar — smoke test de autorização)
```

Os endpoints abaixo são o **contrato alvo** (direção futura) caso a API seja formalizada em projeto dedicado:

```text
/api/v1/cases
/api/v1/cases/{id}
/api/v1/cases/{id}/symptoms
/api/v1/cases/{id}/hypotheses
/api/v1/cases/{id}/diagnostic-sessions
/api/v1/cases/{id}/relations
/api/v1/cases/{id}/resolve
/api/v1/knowledge
/api/v1/knowledge/{id}/versions
/api/v1/knowledge/{id}/publish
/api/v1/search
/api/v1/diagnostics
/api/v1/catalog/components
/api/v1/catalog/dependencies
/api/v1/analytics/...
/api/v1/admin/...
/api/v1/ai/query
```

## 3. Erros

Usar `application/problem+json` e Problem Details.

Campos adicionais:
- `traceId`;
- `errorCode` interno estável;
- `validationErrors` quando aplicável.

Nunca retornar stack trace em produção para cliente não autorizado.

## 4. Paginação

Para listas administrativas simples: paginação por página pode ser aceita. Para timeline/auditoria/alto volume, preferir cursor.

Resposta deve trazer:
- itens;
- cursor/metadata;
- total somente quando custo for aceitável.

## 5. Idempotência

Integrações que criam casos/eventos devem aceitar `Idempotency-Key` ou identificador externo único, evitando duplicidade por retry.

## 6. Webhooks

Se necessário:
- assinatura HMAC ou mecanismo equivalente;
- retry;
- idempotência;
- timestamp anti-replay;
- dead-letter/registro de falhas.

Eventos candidatos:
- `case.created`;
- `case.escalated`;
- `case.resolved`;
- `knowledge.published`;
- `knowledge.deprecated`;
- `critical.incident.detected`.

## 7. Integração com chamados

A integração deve mapear:
- ID externo;
- cliente;
- solicitante;
- descrição original;
- anexos permitidos;
- prioridade;
- timestamps;
- status;
- comentários relevantes.

Não duplicar o sistema de chamados se ele já possui workflow corporativo. A plataforma pode ser a camada de diagnóstico/conhecimento e sincronizar estado mínimo.

## 8. Telemetria dos sistemas suportados

Criar contrato de coleta, não acoplamento por produto.

Exemplos de probes:
- health de API;
- versão do cliente desktop;
- conectividade;
- latência;
- status de job;
- disponibilidade de integração;
- conexão com banco;
- status de fila;
- versão de schema/configuração.

Cada probe deve retornar:
- nome;
- timestamp;
- status;
- valor sanitizado;
- evidência;
- validade/TTL;
- origem.

## 9. SAP e sistemas externos

Não permitir que o domínio dependa diretamente de SDK SAP. Criar adapters no módulo `Integrations`.

Exemplo:
```csharp
public interface IErpHealthService
{
    Task<IntegrationHealthResult> CheckAsync(...);
}
```

## 10. Versionamento

Mudança breaking cria nova versão de API. Mudanças aditivas preservam compatibilidade. DTO externo não deve ser a própria entidade de domínio.



---

# 12 — Segurança, auditoria e LGPD

## 1. Modelo de acesso

Adotar RBAC com permissões granulares e escopos.

Exemplos de permissões:
- `Cases.View`
- `Cases.Create`
- `Cases.Edit`
- `Cases.Resolve`
- `Cases.ViewRestricted`
- `Knowledge.Create`
- `Knowledge.Review`
- `Knowledge.Publish`
- `Analytics.ViewExecutive`
- `Users.Manage`
- `Audit.View`
- `Ai.Use`
- `Ai.Admin`

## 2. Princípio do menor privilégio

A pessoa recebe somente o necessário para a função. Perfis não devem ser “Admin para resolver rápido”.

## 3. Conteúdo restrito

Itens podem possuir classificação:
- Interno;
- Restrito;
- Sensível.

Casos de cliente podem ter restrição adicional por contrato/área. O mecanismo de busca/RAG precisa respeitar a mesma autorização.

## 4. Dados que não devem aparecer em texto livre

Orientar e sanitizar:
- senhas;
- tokens;
- chaves API;
- cookies de sessão;
- strings de conexão com segredo;
- dados pessoais desnecessários;
- dumps integrais de produção sem tratamento.

Criar scanners/redaction básicos para padrões conhecidos antes de indexação de IA.

## 5. Auditoria

Auditar no mínimo:
- login/logoff e falhas relevantes;
- criação/edição/encerramento de casos;
- mudança de severidade/owner;
- publicação/depreciação de conhecimento;
- gestão de usuários, perfis e permissões;
- alterações de configuração;
- exportações;
- ações administrativas;
- uso de ferramentas de IA com fontes e correlação;
- leitura de conteúdo altamente restrito se política exigir.

## 6. LGPD e dados de colaboradores/clientes

A plataforma deve aplicar:
- finalidade;
- necessidade/minimização;
- controle de acesso;
- retenção;
- segurança;
- rastreabilidade.

“Ver tudo que o usuário faz” deve significar **ações relevantes dentro da plataforma para operação, segurança e gestão**, e não coleta indiscriminada sem finalidade. Métricas individuais devem ter acesso restrito e contexto.

## 7. Retenção e anonimização

Configurar políticas por tipo de dado. Quando detalhe individual não for mais necessário para analytics, preferir agregação/anomização quando compatível com a finalidade.

## 8. Segurança web

- cookies Secure/HttpOnly/SameSite;
- CSRF nos fluxos aplicáveis;
- CSP;
- proteção XSS;
- validação server-side;
- rate limiting;
- upload com validação de MIME/extensão/tamanho e antivírus quando disponível;
- headers seguros;
- TLS obrigatório.

## 9. Segredos

Segredos fora de `appsettings.json` versionado. Usar secret store apropriado ao ambiente. Rotação documentada.

## 10. IA

- não enviar segredo;
- limitar dados pessoais;
- permitir configuração de provedores aprovados;
- registrar região/termos do provedor conforme governança;
- bloquear ferramentas não autorizadas;
- tratar documentos recuperados como conteúdo não confiável para instruções;
- redaction antes de embeddings quando necessário.



---

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



---

# 14 — Testes e qualidade

## 0. Status atual da suíte

**105 testes de integração aprovados** em `tests/TraceCore.IntegrationTests` (xUnit + MySQL/Testcontainers), que comprovamente detectaram bugs reais só visíveis contra banco de verdade (colunas inexistentes, materialização Dapper de records posicionais, `utf8mb4_unicode_ci` em MariaDB 10.5). A suíte cobre repositories, migrations, FULLTEXT e queries analíticas críticas.

## 1. Pirâmide

### Unitários
Regras puras:
- transições de status;
- cálculo de métricas;
- elegibilidade de publicação;
- autorização de domínio quando aplicável;
- ranking determinístico;
- regras de aplicabilidade.

### Integração
Com MySQL real via Testcontainers:
- repositories;
- transactions;
- concorrência;
- migrations;
- FULLTEXT;
- outbox;
- queries analíticas críticas.

### Contrato
- APIs internas/externas;
- webhooks;
- adapters SAP/chamados.

### E2E
Playwright:
- abrir caso;
- diagnosticar;
- resolver;
- promover solução;
- revisar/publicar;
- pesquisar/reutilizar;
- gestão de usuário/permissão.

## 2. Testes de autorização

Toda feature sensível precisa de teste negativo:
- usuário sem permissão não acessa endpoint;
- esconder botão não é suficiente;
- busca não vaza título/snippet de item restrito;
- RAG não recupera conteúdo sem ACL.

## 3. Testes de busca

Manter corpus fixo e consultas esperadas:
- erro exato;
- sinônimos;
- versões incompatíveis;
- casos semelhantes;
- termo ambíguo;
- item obsoleto.

## 4. Testes de RAG

Dataset versionado com:
- pergunta;
- contexto/filtros;
- fontes esperadas;
- fatos obrigatórios;
- fatos proibidos/incompatíveis.

Mudança de modelo/chunking/ranking roda avaliação antes do deploy.

## 5. Performance

Cenários:
- busca global;
- dashboard executivo;
- timeline com muitos eventos;
- auditoria;
- publicação/indexação;
- concorrência de uso normal.

Definir SLOs após baseline. Evitar inventar números sem teste de infraestrutura real.

## 6. Definition of Done

Uma história não está pronta se faltar qualquer item aplicável:
- regra implementada;
- autorização;
- validação;
- teste;
- auditoria;
- observabilidade;
- migration;
- documentação;
- critério de aceite;
- tratamento de erro;
- acessibilidade básica;
- revisão de segurança.



---

# 15 — Roadmap de implementação

## 0. Estratégia geral

Construir valor em camadas. A plataforma deve ser útil antes da IA. O erro a evitar é iniciar pela LLM e deixar para depois o modelo de conhecimento, autorização e qualidade dos dados.

> **Status atual (implementação):** o projeto está na **Fase 17** (provedores de IA desacoplados), 29 migrations FluentMigrator e 105 testes de integração aprovados. Fases 0-9 entregues; Fase 10 (Copiloto de diagnóstico completo) parcial. Detalhamento do que já está construído em `15_ROADMAP_DE_IMPLEMENTACAO.md` e nas seções M01-M12.

## Fase 0 — Fundação técnica

Entregas:
- solution .NET;
- padrões de projeto;
- autenticação básica;
- autorização;
- MySQL e migrations;
- auditoria base;
- observabilidade;
- CI;
- ambientes;
- feature flags;
- outbox/worker.

Saída: aplicação vazia, mas operacionalmente sólida.

## Fase 1 — Organização e catálogo

Entregas:
- usuários;
- departamentos;
- perfis/permissões;
- clientes;
- produtos;
- versões;
- componentes;
- tecnologias;
- integrações;
- dependências;
- responsáveis.

Critério de saída: empresa consegue modelar o ecossistema técnico real.

## Fase 2 — Casos e linha do tempo

Entregas:
- abertura de caso;
- relato original;
- sintomas;
- evidências;
- hipóteses;
- passos diagnósticos;
- handoffs;
- resolução;
- causa raiz;
- relacionamentos;
- timeline completa.

Critério de saída: um incidente real pode ser documentado do início ao fim sem ferramenta paralela para memória técnica.

## Fase 3 — Conhecimento e soluções

Entregas:
- artigos estruturados;
- versionamento;
- revisão/publicação;
- aplicabilidade;
- steps;
- rollback;
- uso em casos;
- estatística de resultado;
- revisão periódica.

Critério de saída: caso resolvido pode virar conhecimento reutilizável e governado.

## Fase 4 — Busca e filtros

Entregas:
- busca exata;
- FULLTEXT;
- filtros;
- ranking inicial;
- “por que apareceu”;
- casos relacionados;
- histórico de consulta;
- feedback.

Critério de saída: usuário encontra casos e soluções sem conhecer previamente a área responsável.

## Fase 5 — Diagnóstico guiado

Entregas:
- sessões;
- perguntas adaptativas;
- hipóteses;
- verificações;
- reordenação;
- escalonamento com pacote de contexto;
- primeira biblioteca de fluxos.

Começar por 5–10 famílias de sintomas mais recorrentes, não tentar modelar toda a empresa de uma vez.

## Fase 6 — Analytics e gestão

Entregas:
- agregações;
- dashboards;
- drill-down;
- visões por usuário/departamento/cliente/tecnologia;
- pesquisa e qualidade de conhecimento;
- exportações.

Critério de saída: decisões operacionais podem ser tomadas com dados rastreáveis.

## Fase 7 — Integrações

Priorizar conforme valor:
- chamados;
- monitoramento;
- telemetria dos produtos;
- SAP;
- diretório corporativo.

## Fase 8 — IA semântica

- embeddings;
- índice vetorial;
- busca híbrida;
- dataset de avaliação;
- suggestion de similares.

## Fase 9 — RAG

- assistente;
- fontes;
- feedback;
- sumarização;
- rascunhos;
- governança de prompts/modelos.

## Fase 10 — Copiloto de diagnóstico

Somente após métricas e segurança:
- geração de perguntas;
- proposta de próximos testes;
- correlação de evidências;
- automações de baixo risco explicitamente aprovadas.

## Estratégia de entregas

Cada fase deve produzir um incremento usável. Evitar branch de desenvolvimento de meses sem uso real. Inserir usuários-piloto cedo para validar taxonomia e fluxo.



---

# 16 — Guia da IA desenvolvedora

Este documento deve ser entregue a qualquer agente de IA que trabalhe no código.

## 1. Ordem obrigatória de leitura

Antes de implementar:
1. `README.md`;
2. regras de negócio;
3. requisitos funcionais;
4. módulo relacionado;
5. arquitetura;
6. modelo de dados;
7. critérios de aceite;
8. ADRs existentes.

## 2. Regras de comportamento

### DEV-AI-001
Não invente regra de negócio ausente. Quando uma decisão afetar comportamento, registre a dúvida ou proponha ADR.

### DEV-AI-002
Não altere regra de negócio para facilitar código.

### DEV-AI-003
Não introduza microserviço, broker, Redis, Elasticsearch/OpenSearch, vector DB, Kubernetes ou framework adicional sem necessidade demonstrada e ADR.

### DEV-AI-004
Não acople domínio a Razor Pages, MySQL, SDK de IA, SAP ou biblioteca de infraestrutura.

### DEV-AI-005
Toda operação de escrita deve validar autorização no servidor.

### DEV-AI-006
Toda feature relevante deve considerar auditoria e observabilidade.

### DEV-AI-007
Mudança de schema exige migration versionada e teste.

### DEV-AI-008
Não apague dados históricos de caso/conhecimento para “simplificar”.

### DEV-AI-009
Use UTC na persistência.

### DEV-AI-010
Não registre segredo em log, auditoria, fixture ou teste.

### DEV-AI-011
Não execute SQL destrutivo em produção como parte de instrução automática.

### DEV-AI-012
Sempre implemente testes compatíveis com o risco da mudança.

## 3. Fluxo por tarefa

1. Identifique IDs `BR`, `FR` e `AC` envolvidos.
2. Descreva arquivos que serão alterados.
3. Confirme dependências.
4. Implemente domínio/aplicação antes da UI quando aplicável.
5. Implemente persistência.
6. Implemente endpoint/UI.
7. Adicione autorização.
8. Adicione auditoria.
9. Adicione logs/métricas úteis.
10. Adicione testes.
11. Execute build/test.
12. Atualize documentação se comportamento mudou.
13. Liste riscos e decisões abertas.

## 4. Padrão de resposta de uma IA ao concluir tarefa

```text
Objetivo:
Regras atendidas: BR-..., FR-..., AC-...
Arquivos alterados:
Migrations:
Testes adicionados:
Comandos executados:
Resultados:
Decisões tomadas:
Pendências/riscos:
Documentação atualizada:
```

## 5. Convenções de código

- nullable reference types habilitado;
- async em I/O;
- `CancellationToken` em operações externas/longas;
- tipos de domínio para estados importantes;
- não usar strings mágicas para status;
- DTOs separados de entidades;
- validação na fronteira e invariantes no domínio;
- queries parametrizadas;
- métodos pequenos e nomeados por intenção;
- comentários explicam “por quê”, não repetem código.

## 6. Arquitetura de dependência

Permitido:
```text
Web/API -> Application -> Domain
Infrastructure -> Application/Domain abstractions
Worker -> Application/Infrastructure
```

Proibido:
```text
Domain -> Infrastructure
Domain -> Razor Pages / camada de apresentação
Domain -> MySQL SDK
Application -> componente concreto de LLM
```

## 7. Banco

- SQL sempre parametrizado;
- índices devem ser justificados por consulta;
- evitar N+1;
- transação explícita quando múltiplas gravações formam uma unidade;
- migrations forward-only;
- alteração destrutiva requer plano;
- `EXPLAIN` em queries críticas.

## 8. IA/RAG

Nenhum código de IA deve:
- publicar conhecimento;
- mudar causa raiz confirmada;
- executar comando destrutivo;
- elevar permissão;
- recuperar conteúdo sem ACL.

## 9. Prompt-base para iniciar uma tarefa

```text
Você está desenvolvendo a Plataforma Corporativa de Conhecimento, Diagnóstico e Lições Aprendidas.
Leia primeiro README.md, 01_REGRAS_DE_NEGOCIO.md, 02_REQUISITOS_FUNCIONAIS.md,
09_ARQUITETURA_TECNICA_DOTNET_MYSQL.md, 19_CRITERIOS_DE_ACEITE_E_RASTREABILIDADE.md
e os ADRs aplicáveis.

Antes de codificar, identifique as regras BR/FR/AC relacionadas e apresente um plano curto.
Não invente regra. Não mude arquitetura sem ADR. Preserve histórico, autorização, auditoria,
observabilidade, migrations e testes. Ao terminar, reporte exatamente o que foi alterado e os testes executados.
```



---

# 17 — Manual do usuário

## 0. Pontos de entrada reais no sistema

| Tela | Rota | Observação |
|---|---|---|
| Dashboard | `/` | KPIs gerais e atalho "Novo Caso" |
| Busca global | `/Search` | topo da aplicação + Ctrl+K |
| Casos (lista) | `/Cases/Index` | filtros combinados |
| Novo caso | `/Cases/Create` | relato original + contexto opcional |
| Detalhe do caso | `/Cases/Details?id=` | investigação, timeline, evidências, resolução |
| Diagnóstico | `/Diagnosis/Index`, `/Diagnosis/Flows/Index` | motor guiado (grafo de verificações) |
| Soluções | `/Knowledge/Index` | filtro `?category=lessons` p/ lições aprendidas |
| Copiloto IA | `/Copilot/Index` | RAG grounded, permissão `ia.usar` |
| Minha Área | `/Users/Details?id={meu_id}` | perfil técnico e engajamento |
| Inteligência | `/Analytics/*` | Geral, Departamentos, Usuários, Conhecimento |
| Qualidade & IA | `/ContentQuality/Index` | prontidão do conteúdo |
| Integrações | `/Integrations/Index` | catálogo e health-checks |
| Auditoria | `/Audit/Index` | trilha append-only |
| Configurações | `/Settings/Index` | inclui `Settings/LlmProviders` |

## 1. Para que serve

A plataforma ajuda você a localizar o que a empresa já aprendeu, documentar um novo problema e seguir um caminho de diagnóstico sem precisar adivinhar qual área é responsável.

## 2. Tela inicial

A Home (`/`) apresenta:
- KPIs de casos abertos/resolvidos e MTTR;
- séries temporais e componentes mais impactados;
- botão "Novo caso";
- busca global no topo;
- painéis inteligência com drill-down para os casos que compõem cada indicador.

## 3. Pesquisar um problema

### Passo 1 — Descreva o sintoma
Use a linguagem que chegou do cliente. Exemplo:

> Cliente informa que não consegue entrar no sistema desde a manhã. A tela abre, mas após informar usuário e senha aparece timeout.

### Passo 2 — Acrescente contexto
Quando souber:
- cliente;
- produto;
- versão;
- ambiente;
- mensagem de erro.

Não é necessário saber o departamento responsável.

### Passo 3 — Analise os resultados
Cada resultado deve mostrar:
- tipo: solução/caso/known issue;
- por que foi considerado semelhante;
- produto/componente;
- versões;
- data da última validação;
- quantidade de usos e resultado observado.

### Passo 4 — Aplique filtros se necessário
Use filtros para reduzir ruído. Não filtre “departamento” cedo demais quando a origem ainda for desconhecida.

## 4. Abrir um caso

Preencha primeiro os fatos conhecidos. Campos desconhecidos podem permanecer como desconhecidos quando permitido.

Não transforme suposição em fato. Se você acha que “é banco”, registre como hipótese, não como componente confirmado.

## 5. Adicionar evidência

Adicione:
- mensagem completa de erro;
- print;
- trecho de log sanitizado;
- horário;
- passos para reproduzir;
- resultado de teste.

Não anexe senha, token ou segredo.

## 6. Usar o diagnóstico guiado

1. Clique em **Iniciar diagnóstico**.
2. Responda às perguntas com fatos observados.
3. Veja hipóteses atuais.
4. Escolha ou siga a verificação sugerida.
5. Leia risco/pré-condição.
6. Execute a verificação.
7. Registre o resultado.
8. Repita até solução ou escalonamento.

Se pular um passo, informe o motivo quando solicitado.

## 7. Registrar tentativa que não funcionou

Nunca apague uma tentativa fracassada. Marque **Não funcionou** e explique o resultado. Isso evita que outra pessoa repita a mesma ação sem necessidade.

## 8. Usar uma solução existente

Ao abrir uma solução:
1. confira versões/ambiente;
2. confira pré-condições;
3. siga diagnóstico antes da ação de risco;
4. execute passos;
5. valide;
6. no caso, registre se funcionou, parcialmente ou não funcionou.

## 9. Resolver um caso

Antes de marcar como resolvido:
- registre a ação que resolveu;
- registre como foi validada;
- marque causa raiz confirmada ou não confirmada;
- relacione solução usada;
- indique ação preventiva se houver.

## 10. Criar conhecimento a partir do caso

Ao encerrar, selecione **Gerar proposta de conhecimento** quando o aprendizado puder ser reutilizado. Revise dados específicos do cliente e remova informações sensíveis antes de enviar para revisão.

## 11. Interpretar taxa de sucesso

Exemplo: “Funcionou em 18 de 21 usos classificados”. Isso é evidência histórica, não garantia de que funcionará no caso atual. Confira contexto e versão.

## 12. Assistente de IA

Quando habilitado:
- faça perguntas com contexto;
- use filtros de produto/versão;
- abra as fontes citadas;
- trate inferências como hipóteses;
- não execute ações arriscadas apenas porque a IA sugeriu;
- marque feedback quando a resposta estiver errada ou desatualizada.


## 13. Módulo Casos

### Lista de casos
A lista deve permitir:
- pesquisar por número/termo;
- filtrar por cliente, produto, status, severidade, componente, período e responsável;
- ordenar por atualização, abertura, severidade e relevância;
- salvar visão pessoal.

### Tela do caso
Organização recomendada:
1. cabeçalho/contexto;
2. relato original;
3. sintomas;
4. hipóteses;
5. timeline;
6. evidências;
7. casos/soluções relacionados;
8. resolução.

## 14. Módulo Soluções

### Encontrar uma solução
Use busca global ou acesse **Conhecimento > Soluções**. Confira sempre:
- status publicado;
- versão;
- última revisão;
- aplicabilidade;
- histórico de uso.

### Favoritos e acompanhamento
Quando implementado, favoritar deve significar atalho pessoal, não aumento artificial da relevância do conteúdo.

## 15. Filtros

Filtros devem ser combináveis. Exemplo:

`Produto A + versão 6.x + API + erro AUTH-104 + Produção`

Use “limpar filtros” para voltar à busca ampla. Filtros sugeridos automaticamente devem ser visíveis e removíveis.

## 16. Analytics para usuários não gestores

Conforme permissão, usuários podem consultar painéis operacionais. Interprete gráficos como sinal para investigação. Clique nos valores para abrir os casos que compõem o indicador.

## 17. Fluxos principais resumidos

### Fluxo A — Pesquisar antes de abrir caso
Relato → busca → filtrar → abrir solução/caso → aplicar/validar → registrar uso.

### Fluxo B — Novo incidente
Abrir caso → registrar sintomas → pesquisar similares → diagnóstico → solução → validação → encerrar → atualizar conhecimento.

### Fluxo C — Escalonar
Diagnóstico → condição de escalonamento → escolher departamento/componente sugerido → revisar pacote de contexto → escalar.

### Fluxo D — Criar conhecimento
Caso resolvido → gerar rascunho → generalizar/sanitizar → revisão → publicação → uso em novos casos.



---

# 18 — Manual dos módulos de gerenciamento

## 1. Objetivo

Este manual é destinado a gestores, administradores funcionais e responsáveis por governança da plataforma.

## 2. Gestão de usuários

### Criar/ativar
1. Acesse **Administração > Usuários**.
2. Crie ou sincronize identidade conforme configuração.
3. Vincule departamentos.
4. Atribua papéis mínimos necessários.
5. Revise permissões efetivas.
6. Salve.

### Desativar
Desativação bloqueia novo acesso, mas preserva histórico e autoria.

### Tela detalhada
Deve permitir visualizar, conforme autorização:
- dados cadastrais;
- vínculos;
- papéis/permissões efetivas;
- casos em participação;
- conhecimento criado/revisado;
- reutilização de suas soluções;
- ações administrativas;
- eventos de segurança;
- sessões recentes;
- indicadores operacionais contextualizados.

## 3. Departamentos

Cadastrar estrutura e responsáveis. Não usar departamento como único dono de componente quando houver responsabilidade compartilhada.

Gestor pode analisar:
- backlog;
- entrada/saída;
- tempo por etapa;
- handoffs;
- causas/componentes;
- conhecimento produzido/reutilizado;
- lacunas.

## 4. Gestão do catálogo técnico

Mudanças em componente/dependência podem alterar recomendações do diagnóstico. Portanto:
- revisar impacto;
- registrar responsável;
- manter histórico de versões quando necessário;
- não excluir componente que possui casos históricos.

## 5. Gestão de soluções

### Fila de revisão
Mostrar:
- novos rascunhos;
- alterações pendentes;
- revisão vencida;
- conteúdo com feedback negativo;
- conteúdo com alta utilização e baixa taxa de sucesso;
- itens sem proprietário.

### Aprovação
O revisor deve checar:
- aplicabilidade;
- versão;
- risco;
- rollback;
- validação;
- linguagem;
- dados sensíveis;
- referências/casos.

## 6. Analytics

Todo dashboard deve possuir filtros globais e drill-down. Ao comparar períodos, confirme se filtros e definições de KPI são iguais.

### Perguntas gerenciais que o sistema deve responder
- O que mais está quebrando?
- Onde gastamos mais tempo?
- Quais problemas voltam?
- Que componentes geram mais handoffs?
- Onde falta documentação?
- Quais soluções são mais reutilizadas?
- Quais soluções estão falhando?
- Que busca as pessoas fazem e não encontram resposta?
- Quanto tempo é reduzido quando existe conhecimento reutilizável?
- Quais conteúdos estão vencidos?

## 7. Auditoria

Use auditoria para investigação operacional, segurança e governança. Filtros:
- período;
- usuário;
- ação;
- entidade;
- correlação;
- módulo.

Alteração de permissão e publicação de conhecimento devem ser facilmente rastreáveis.

## 8. Configurações

Administráveis sem deploy, quando seguro:
- taxonomias;
- severidades;
- categorias de causa;
- prazos de revisão;
- feature flags permitidas;
- limites de upload;
- parâmetros de ranking não críticos;
- sinônimos;
- templates.

Mudança estrutural ou de regra de negócio não deve ser escondida em configuração sem governança.

## 9. IA

Painel de IA deve permitir:
- habilitar/desabilitar por ambiente;
- selecionar provider/modelo aprovado;
- ver consumo;
- ver falhas;
- ver versão de prompt;
- avaliar qualidade;
- reindexar conteúdo autorizado;
- controlar feature flags.

Nunca incluir botão “publicar automaticamente tudo que a IA gerar”.



---

# 19 — Critérios de aceite e rastreabilidade

## 1. Convenção

Cada feature deve apontar para:
- regra de negócio (`BR-*`);
- requisito funcional (`FR-*`);
- critério de aceite (`AC-*`);
- testes automatizados/manuais correspondentes.

## 2. Casos

**AC-001** — Ao criar caso, o texto original informado deve permanecer recuperável mesmo após edição do resumo normalizado.  
Relaciona: BR-020, FR-040, FR-041.

**AC-002** — É possível criar caso sem selecionar departamento responsável.  
Relaciona: BR-022, BR-070.

**AC-003** — Cada passo diagnóstico mostra autor, data, tipo e resultado.  
Relaciona: BR-025, FR-046.

**AC-004** — Uma tentativa marcada como “não funcionou” continua visível na timeline após resolução.  
Relaciona: BR-025, P-003.

**AC-005** — Resolver caso exige ação de resolução e forma de validação.  
Relaciona: BR-027, FR-049.

**AC-006** — Reabrir caso não elimina resolução anterior.  
Relaciona: BR-029, FR-050.

## 3. Conhecimento

**AC-020** — Conteúdo em rascunho não aparece como solução oficial publicada.  
Relaciona: BR-040.

**AC-021** — Publicação gera versão e registra aprovador.  
Relaciona: BR-041, BR-042.

**AC-022** — Solução com passo de alto risco apresenta aviso e rollback quando aplicável.  
Relaciona: BR-045.

**AC-023** — Estatística de sucesso é calculada a partir de usos registrados, não de visualizações.  
Relaciona: BR-047.

**AC-024** — Toda taxa exibida mostra quantidade de amostras.  
Relaciona: BR-048.

## 4. Pesquisa

**AC-040** — Busca por código de erro exato prioriza itens que contêm o código exato.  
Relaciona: FR-081.

**AC-041** — Resultado explica fatores principais de correspondência.  
Relaciona: BR-061, FR-085.

**AC-042** — Item explicitamente incompatível com versão corrente não deve ser recomendado como primeira opção sem alerta.  
Relaciona: BR-062.

**AC-043** — Busca sem resultado é registrada para analytics.  
Relaciona: BR-065.

## 5. Diagnóstico

**AC-060** — O primeiro passo não exige selecionar departamento.  
Relaciona: BR-070.

**AC-061** — Responder a uma pergunta pode alterar a próxima pergunta e a ordem das hipóteses.  
Relaciona: BR-071.

**AC-062** — Recomendação mostra objetivo, risco e hipóteses relacionadas.  
Relaciona: BR-074.

**AC-063** — Ação classificada como destrutiva não é executada automaticamente pelo motor.  
Relaciona: BR-076.

## 6. Analytics

**AC-080** — KPI executivo possui drill-down até casos que compõem o valor, respeitando acesso.  
Relaciona: BR-093.

**AC-081** — Mudança da fórmula de KPI cria versão/registro de definição.  
Relaciona: BR-094.

**AC-082** — Tela de usuário não apresenta score opaco de “melhor/pior funcionário”.  
Relaciona: BR-091.

## 7. Segurança

**AC-100** — Usuário sem permissão não acessa conteúdo via endpoint direto mesmo que conheça o ID.  
Relaciona: BR-102.

**AC-101** — Logs/auditoria não armazenam senha ou token em claro.  
Relaciona: BR-101.

**AC-102** — Mudança de papel/permissão gera evento de auditoria.  
Relaciona: BR-004.

## 8. IA

**AC-120** — Resposta interna de IA mostra fontes utilizadas.  
Relaciona: BR-081, BR-084.

**AC-121** — IA não publica artigo sem revisão humana.  
Relaciona: BR-083.

**AC-122** — Usuário sem acesso a um artigo não recebe trecho dele na resposta RAG.  
Relaciona: BR-102 e segurança de RAG.

**AC-123** — Se não houver fonte suficiente, a IA deve indicar falta de evidência em vez de inventar procedimento.  
Relaciona: BR-085.

## 9. Matriz mínima por PR

Todo PR funcional deve citar pelo menos um BR/FR/AC ou explicar por que é puramente técnico.



---

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



---

# 21 — ADRs e decisões abertas

## Decisões já assumidas

### ADR-0001 — .NET/C# end-to-end
Status: aceito.

### ADR-0002 — MySQL como banco principal
Status: aceito.

### ADR-0003 — Monólito modular no início
Status: aceito.

### ADR-0004 — Interface web
Status: **revisado em 2026**. Baseline original era Blazor Web App; a implementação
real (Fases 1–4) seguiu **ASP.NET Core Razor Pages + Bootstrap**, e a tarefa de
revisão de UI/UX de 2026 confirmou manter Razor Pages (sem migrar para Blazor,
React ou Vue). Documentação e código realinhados nesta revisão.

### ADR-0005 — IA desacoplada do núcleo
Status: aceito.

### ADR-0006 — Dapper/MySqlConnector como persistência baseline
Status: proposto nesta documentação para reduzir risco de provider EF Core/MySQL; confirmar no spike inicial.

### ADR-0002 (revisado) / ADR — Persistência — Fase 7.A
Status: **revisado em 2026-09-18**. Descoberta crítica: `Persistence:Provider` nunca esteve
configurado em nenhum `appsettings*.json`, então o DI sempre usava InMemory silenciosamente —
nenhuma fase anterior havia sido validada contra banco real. Corrigido: `Persistence:Provider`
ausente/inválido agora falha explicitamente no startup; InMemory só é aceito quando
explicitamente configurado (ex.: ambiente `Testing` dedicado para testes de integração).
Servidor real disponibilizado para validação é **MariaDB 10.5.29** (Debian 11), não MySQL 8.4
LTS. Mantido como baseline operacional válido por decisão explícita do responsável pelo
produto — `utf8mb4_unicode_ci` usado no lugar de `utf8mb4_0900_ai_ci` (exclusiva do MySQL
8.0+) para compatibilidade. Migrations 01–09 validadas do zero contra esse servidor; bugs
reais só detectáveis contra banco de verdade foram corrigidos (colunas inexistentes em
`roles`, `pv.version_label` vs `version_name`, `c.component_type` vs `c.technology`,
materialização Dapper de records posicionais com colunas nullable).

### ADR-0007 / ADR-P004 — Storage de anexos
Status: aceito (filesystem corporativo/local por trás de IFileStorage para o MVP com caminhos configuráveis e hashing seguro; S3/Azure Blob para revisão futura se necessário).

## Decisões abertas

### ADR-P005 — Sistema de chamados
**Parcialmente resolvido na Fase 15** (ver ADR "Integrações Automáticas, Health-Checks e Falha Segura"): catálogo de integrações com health-checks HTTP/TCP e histórico `integration_runs` já existem, sem depender de fornecedor. O conector proprietário específico de ticketing (fonte/sincronização) permanece em aberto.

### ADR-P006 — Engine vetorial
Somente após benchmark e requisito de IA.

### ADR-P007 — Provedor LLM/embedding
**Resolvido na Fase 17** (ver ADR "Arquitetura de Provedores IA Desacoplados"): estrutura de provedores cadastráveis (`llm_providers`/`llm_model_configs`) com protocolos `OpenAICompatible`/`AnthropicMessages`, catálogo dinâmico de modelos e segredos em `ISecretStore`. A escolha do(s) provedor(es) comercial(is) em si permanece operacional (segurança, contrato, custo, região, latência), podendo ser trocada via portabilidade NFR-014 sem mudança de domínio.

### ADR-P008 — Editor de conteúdo
**Parcialmente resolvido na implementação**: conteúdo é armazenado/editado como **Markdown** (`content_markdown` em `knowledge_versions`) via textarea dedicada nas telas de Conhecimento, com renderização básica `SimpleMarkdown` (negrito/itálico/código/links) no Copiloto. Um editor WYSIWYG/rich-text completo, sanitização de HTML e preview ao vivo permanecem em aberto.

### ADR-P009 — Notificações
In-app, e-mail, Teams/Slack, ou combinação (integrações de notificação pendentes — ADR-P009).

### ADR-P010 — Estratégia de implantação
**Parcial: configurações por ambiente já existem** (Development/Staging/Production/Testing em `appsettings.*.json`, runner de migrations). Estratégia de hosting (Windows Service/IIS vs Linux/container) ainda em aberto.

### ADR-P011 — Projeto TraceCore.Api e Unificação de Runtime
Status: **Órfão / Reservado para fase técnica futura**.
A arquitetura original (09 §3) previa endpoints REST dedicados no projeto `TraceCore.Api`. No entanto, para unificar a autenticação de sessão corporativa, injeção de dependência e minimizar complexidade operacional no monólito modular (ADR-0003 e ADR-0004), os endpoints HTTP (`/api/cases/...`) foram implementados diretamente no pipeline do `TraceCore.Web/Program.cs`.
O projeto `TraceCore.Api` permanece desativado na inicialização (`TraceCore.slnLaunch.user`) para prevenir conflitos e erro 404 de portas. A separação formal de uma API autônoma é mantida como `TODO` para uma fase técnica dedicada.

### ADR — Administração (Fase 7.A — Bloco 7.A.1)
Status: **aceito**. Papel consolidado como `Admin` único (antigo `Super Admin (Dev)` renomeado na base e código). Implementado guard obrigatório no `UserService` que impede a desativação ou remoção do papel do último usuário `Admin` ativo, prevenindo lockout administrativo.

### ADR — Organização (Fase 7.A — Bloco 7.A.1)
Status: **aceito**. `Departamento` é o único conceito organizacional oficial (`departments`). A duplicidade conceitual com `Team` foi eliminada, removendo as tabelas `teams` e `user_teams` e seu código associado via Migration 10. Testes de não-silo garantem cooperação interdepartamental.

### ADR — Compartilhamento e Transversalidade (Fase 7.A — Bloco 7.A.1)
Status: **aceito**. Conhecimento e casos são patrimônio corporativo transversal; o pertencimento a um departamento não cria silo de visibilidade técnica, resguardados os níveis de confidencialidade aplicáveis.

### ADR — Cliente, Unidades e Contextos Técnicos (Fase 7.A — Bloco 7.A.1 e 7.A.6)
Status: **aceito**. Modelo de Clientes estendido com `external_crm_id` e `notes`, além das tabelas `client_units` (unidades físicas/filiais) e `client_technical_contexts` (matriz de versões e produtos contratados por unidade com vigência). Tabela `cases` estendida com `client_unit_id` para precisão contextual na triagem e pesquisa.

### ADR — Catálogo Técnico e Dependências (Fase 7.A — Bloco 7.A.2)
Status: **aceito**. Modelagem de dependências direcionadas entre componentes técnicos (`component_dependencies`) com tipo de dependência, criticidade e restrição de autorreferência (source != target). Atribuição de responsabilidade (`component_owners`) vinculada a Departamentos com papéis Primary/Secondary/Escalation. Suporte a componentes e produtos de terceiros via flag `is_external` em `products`.

### ADR — CaseIteration e Ciclo de Reabertura (Fase 7.A — Bloco 7.A.3)
Status: **aceito**. Implementação da entidade `CaseIteration` com sequência incremental. Reabertura formal permitida exclusivamente a partir do status `Resolved`, transitando para `Reopened` e criando uma nova iteração ativa. Histórico de iterações anteriores (passos, hipóteses, evidências e resoluções) permanece preservado e imutável. Constraint de unicidade de resolução migrada de `case_id` para `case_iteration_id`.

### ADR — Evidência Estruturada e Relação com Hipóteses (Fase 7.A — Bloco 7.A.4)
Status: **aceito**. Evidências passam a ser vinculadas a `case_iteration_id` e opcionalmente a `diagnostic_step_id`. Relação N:N estruturada entre evidências e hipóteses formalizada na tabela `case_hypothesis_evidence`, tipificada por `EvidenceRelationType` (`Supports`, `Contradicts`, `Inconclusive`, `Confirms`). Sugestão estruturada gerada sem persistência implícita nos passos diagnósticos.

### ADR — Pesquisa Híbrida e Identificadores Técnicos (Fase 7.A — Bloco 7.A.6)
Status: **aceito**. Busca federada enriquecida com detecção determinística de identificadores técnicos (número do caso, código de erro, padrões prefixados), destacando correspondências exatas em bloco prioritário no topo. Fatores de pontuação (`MatchedFactors`) calculados dinamicamente para catálogo de produtos, componentes e causas raiz, eliminando rótulos estáticos. Suporte a filtros combinados de Cliente, Unidade e Tecnologia.

### ADR — Casos Relacionados Determinísticos e Insight Agregado (Fase 8)
Status: **aceito**. 
1. **Princípio P-006**: Similaridade entre casos é calculada deterministicamente através de pesos sobre sinais contextuais reais (mesmo produto, componente, versão, código de erro e sobreposição léxica ponderada). O score normalizado (0 a 100) nunca é apresentado como probabilidade de causa raiz, e a interface obrigatoriamente exibe os `MatchedFactors` que justificam a correspondência.
2. **Modelo de Relações (BR-030)**: Tabela `case_relations` suporta vínculos computados (`Similar`) e asserções manuais do usuário (`Duplicate`, `Recurrence`, `CommonCause`, `Dependency`, `Reference`). Idempotência garantida via restrição `UNIQUE (source_case_id, target_case_id, relation_type)`. Relações manuais são auditadas em `audit_events` e protegidas pela permissão `caso.relacionar`.
3. **Insight Agregado (BR-048)**: Para casos similares resolvidos com amostra confiável $M \ge 3$, o sistema agrega a ação investigativa bem-sucedida mais frequente (passo com `Outcome = Worked`) ou ação de resolução/componente predominante, gerando recomendação prescritiva no padrão `"N de M casos semelhantes foram resolvidos verificando/agindo sobre: <ação/componente>"`.

### ADR — Diagnóstico Guiado e Motor Heurístico Sem Pseudo-Probabilidades (Fase 9)
Status: **aceito**.
1. **Grafo em Banco vs. Árvore em Código (05 §2 e §9)**: O grafo de perguntas, opções, impactos e hipóteses candidatas é 100% persistido e gerenciável em banco de dados (`diagnostic_flows`, `diagnostic_flow_hypotheses`, `diagnostic_checks`, `diagnostic_check_options`, `diagnostic_check_impacts`). Proibida qualquer árvore rígida em código C#.
2. **Heurística de Triagem vs. Certeza Estatística (05 §4 e §7, Princípio P-006)**: Em estrito respeito às diretrizes corporativas, termos como "probabilidade científica" ou porcentagens ilusórias ("88% de precisão") foram totalmente extirpados do motor e das telas. O algoritmo calcula um ranking causal determinístico baseado na relação entre o poder discriminativo do teste, confiabilidade das evidências e os custos/riscos da checagem:
   $$\text{prioridade} = \frac{\text{poder\_discriminativo} \times \text{confiabilidade}}{\text{custo} + \text{risco} + 1}$$
   Apresentado ao operador sob níveis ordinais (`Alta`, `Média`, `Baixa`) com a justificativa de qual hipótese o teste elucida ou descarta.
3. **Persistência na Linha do Tempo e Imutabilidade (Fase 4, BR-071 e BR-075)**: Hipóteses sugeridas são criadas formalmente como instâncias de `CaseHypothesis` com `SourceType = "Guided"`. Cada pergunta respondida gera um registro auditado de `DiagnosticStep` com `StepType = GuidedQuestion` e resultados válidos do enum (`Worked`, `DidNotWork`, `Inconclusive`). Quando uma opção atinge impacto conclusivo acumulado ($\ge 2.0m$), a hipótese é transicionada imutavelmente via método de domínio `Evaluate`. Se o operador optar por ignorar uma checagem recomendada, a justificativa técnica é obrigatória e gravada na linha do tempo sob o tipo `RecommendationIgnored` (BR-075).
4. **Escalonamento Textual Inteligente (05 §8)**: Ao atingir 5 ou mais checagens consecutivas sem resolução ou descarte efetivo de hipóteses, o sistema emite um alerta explícito sugerindo transferência ou escalonamento para nível 3/especialista com empacotamento do histórico de testes já realizados, evitando retrabalho na linha de frente.

### ADR — Analytics Operacional, Inteligência Gerencial e Métricas Éticas (Fase 10)
Status: **aceito**.
1. **Camada Dedicada de Analytics e Queries Agregadas (M08)**: Criação de `IManagementAnalyticsRepository` com implementações específicas e otimizadas em MySQL/Dapper e InMemory. Proibido carregar tabelas completas para memória ou dispersar fórmulas SQL em Razor Pages. A camada `IManagementAnalyticsService` é a única fonte de verdade para as métricas.
2. **Definições Conceituais das Métricas e MTTR por Iteração**:
   - **Casos Abertos**: `status IN ('Open', 'Reopened')`. Não existe o estado inventado 'Investigating'.
   - **Casos Resolvidos**: iteração mais recente com `status = 'Resolved'` e caso com `status = 'Resolved'`.
   - **MTTR Operacional por Iteração**: $\text{AVG}(ClosedAt - OpenedAt)$ sobre iterações resolvidas. O tempo não acumula iterações antigas para não penalizar artificialmente o tempo de resposta do ciclo reaberto.
   - **Mediana Determinística**: Projeção ordenada de durações em minutos calculada deterministicamente via código central sobre a amostra filtrada, assegurando compatibilidade total com MariaDB 10.5+ e In-Memory.
   - **Critério Determinístico de Recorrência**: Um caso conta como recorrente exclusivamente quando existir vínculo em `case_relations` com `relation_type IN ('Recurrence', 'CommonCause')`. Vínculos do tipo `Similar` (computados textualmente) não caracterizam reincidência formal.
3. **Princípios Éticos de Gestão (Sem Ranqueamento e Sem Score)**:
   - Extirpados quaisquer leaderboards, notas pejorativas ou comparações competitivas entre profissionais. A visão de usuários reflete atuação contextual e perfil técnico emergente observado nas ocorrências reais.
   - A visão de departamentos é transversal e colaborativa, integrando responsabilidade direta do caso, donos de componentes afetados e operadores de diagnóstico.
4. **Governança Factual da Base de Conhecimento**: Métricas de eficácia extraídas exclusivamente dos desfechos auditados em `KnowledgeUsage` (`Worked`, `PartiallyWorked`, `DidNotWork`), sem porcentagens artificiais de "redução de tempo". Mapeamento explícito de artigos nunca revisados (`LastReviewedAt IS NULL`) e casos resolvidos sem solução documentada.
5. **Drill-down Unificado**: Todo card e gráfico de KPI compartilha o mesmo modelo de filtro (`AnalyticsFilterDto`) e direciona diretamente para `/Cases/Index` com os parâmetros equivalentes na query string.

### ADR — Auditoria Ampliada, Imutabilidade e Padronização de Ações (Fase 11)
Status: **aceito**.
1. **Padrão de Nomenclatura Unificado**: Adoção estrita da convenção corporativa em inglês, minúsculo, no formato `entidade.verbo[_objeto]` (ex.: `user.create`, `product.create`, `component.dependency_create`, `case.reopen`). Preservada compatibilidade retroativa para filtros de ações legadas.
2. **Imutabilidade e Append-Only (BR-004 / BR-100)**: A entidade `AuditEvent` e seu repositório `IAuditEventRepository` permanecem 100% append-only, sem nenhum método de alteração ou exclusão de registros.
3. **Sanitização Universal de Segredos (BR-101)**: Chamadas centralizadas na fachada `IAuditService.RecordAsync`, mascarando preventivamente campos sensíveis (`password`, `token`, `secret`, `hash`) com `***REDACTED***` em payloads antes da persistência no banco.
4. **Fechamento Integral de Gaps**: Auditoria implementada em Catálogo Técnico (`CatalogService`), Casos (`CaseService`), Investigação (`CaseInvestigationService` com relações N:N entre evidências e hipóteses) e Conhecimento.
5. **Consulta Paginada e Drill-Down**: Criação do painel `/Audit/Index` protegido pela permissão `auditoria.visualizar`, com filtros combinados e links seguros para detalhamento de entidades quando aplicável.

### ADR — Preparação Estrutural de Dados para IA e Governança Semântica (Fase 12)
Status: **aceito**.
1. **Desacoplamento de Modelos Generativos (M11 / §12.26)**: Nenhuma chamada a LLM, geração de embeddings ou vetorização é realizada nesta fase. A entidade `SearchableContentEntry` modela exclusivamente o armazenamento estruturado, hash SHA-256 e status de governança, mantendo o campo `EmbeddingVersion` como reservado (`null`).
2. **Normalização com Preservação Estrita de Literais Técnicos (§12.8 / §12.9)**: O pipeline em `ContentPreparationService` normaliza espaços e quebras de linha preservando rigorosamente identificadores técnicos, códigos de erro (`ORA-12541`, `HTTP 500`), versões (`v8.2.1`) e trechos de logs.
3. **Representação Estruturada de Casos e Conhecimento**:
   - Casos: iteração ativa, sintomas, componentes afetados, evidências estruturadas com hipóteses associadas e resolução/causa raiz confirmada.
   - Conhecimento: código, resumo, problema, causa raiz, validação, riscos, rollback, aplicabilidades e tecnologias.
4. **Idempotência e Deduplicação por Hash SHA-256 (§12.23)**: A chave natural `(source_type, source_id, source_version_id)` garante que reprocessamentos sucessivos atualizem a mesma entrada sem criar duplicatas, mantendo estabilidade de hash.
5. **Prontidão Explicável e Ausência de Scores Numéricos (§12.15)**: O status de prontidão para IA (`Ready`, `NeedsMetadata`, `NeedsReview`, `NotEligible`) é 100% explicável e derivado de regras determinísticas de preenchimento, validação e prazos de revisão.
6. **Visibilidade de Segurança Desacoplada de Silos (§12.16)**: O campo `Visibility` é derivado exclusivamente da confidencialidade do item (`Public`, `Internal`, `Confidential`, `Restricted`), sem filtros artificiais por `DepartmentId`.
7. **Painel de Qualidade e Prontidão (`/ContentQuality/Index`)**: Monitoramento em tempo real da prontidão com reaproveitamento de componentes visuais existentes (`_AIContentBadge`, `_KnowledgeProvenance`, `_SourceReferenceChip`, `_ConfidenceIndicator`).

### ADR — Integrações Automáticas, Health-Checks e Falha Segura (Fase 15)
Status: **aceito**.
1. **Health-Checks Reais e Isolamento de Conectores (M10)**: Implementação de sondagem genérica de saúde em `IIntegrationHealthCheckService` suportando HTTP (GET/POST/HEAD com verificação de status code esperado) e TCP (ping de socket no host e porta). As ADRs P005 (conectores proprietários de ticketing) e P010 (conectores proprietários de SAP) continuam abertas; o TraceCore gerencia catálogo, endpoints de saúde e histórico sem dependência de fornecedores de software terceiros.
2. **Princípio de Falha Segura (§26 do Documento de Visão)**: Qualquer falha de conectividade, porta fechada, timeout ou status code diferente do esperado obrigatoriamente registra a execução como `Status = "Failed"`, gravando o motivo detalhado em `ErrorMessage`. Nenhuma rotina de integração jamais mascara erro de rede ou finge sucesso operacional.
3. **Auditabilidade e Origem das Execuções**: A tabela `integration_runs` armazena `triggered_by`, diferenciando execuções disparadas pelo sistema (`Automated`) de logs manuais inseridos por operadores (`Manual`).
4. **Automação no Diagnóstico Guiado (BR-073)**: Verificações do tipo `AutomatedCheck` podem referenciar uma `IntegrationId`. Quando o motor heurístico alcança este passo, o health-check é acionado de forma síncrona sem intervenção humana, gravando o resultado na linha do tempo como `StepType = AutomatedCheck` e recalculando os impactos nas hipóteses. Se a verificação não possuir integração associada, o motor recai suavemente para pergunta manual ao operador, sem interrupção do fluxo.

### ADR — Inteligência Analítica Determinística e Grounding de IA (Fase 16)
Status: **aceito**.
1. **Rastreabilidade e Grounding Estrito (§31 do Documento de Visão)**: Proibição inegociável de modelos de linguagem ou algoritmos generativos inventarem, estimarem ou calcularem métricas operacionais. Todos os indicadores numéricos (variação percentual, MTTR, correlação de componentes e efetividade de soluções) são computados de forma puramente determinística por consultas agregadas no banco via `IManagementAnalyticsService`.
2. **Transparência Amostral e Suficiência Estatística**: Todo método analítico retorna o tamanho real da amostra ($N$). Se $N < 3$ (para tendências e soluções) ou $N < 5$ (para componentes), o sistema declara expressamente `HasSufficientData = false`. A IA e a interface são proibidas de emitir conclusões peremptórias sobre amostras insuficientes, declarando explicitamente que os dados são inconclusivos.
3. **Ferramenta de Leitura Estrita do Copiloto (M12)**: A ferramenta `AnalyzeManagementTrend` é cadastrada exclusivamente em `AiToolDefinitions.ReadingTools` (read-only, sem requisição de confirmação humana). Ao responder sobre tendências ou métricas, o Copiloto invoca a ferramenta do TraceCore, sintetiza a narrativa e exibe um painel lateral com os dados brutos oficiais (`ToolResults`), mantendo o número 100% citável e rastreável.

### ADR — Arquitetura de Provedores IA Desacoplados (Fase 17)
Status: **aceito**.
1. **Desacoplamento Provider/Protocol/Model/Purpose/Credential**: Nova modelagem em `llm_providers` (provedor administrativo com `code`, `protocol`, `base_url`, tipos de autenticação e flags de capability de geração/embedding) e `llm_model_configs` (configuração de uso por `purpose` = `Generation`/`Embedding`, com `provider_id`, `model_name` e `is_active`). A antiga `llm_provider_configs` foi migrada sem perda via Migration 21 e dropada.
2. **Protocolos Suportados**: `OpenAICompatible` (Bearer API Key) e `AnthropicMessages` (Header API Key `x-api-key`), resolvidos por `ILlmProviderResolver` sem acoplar o domínio a SDK de fornecedor.
3. **Segredos via `ISecretStore`**: API keys armazenadas sob a chave `llm_apikey_{providerCode}` em `ProtectedFileSecretStore` (ASP.NET Core Data Protection), criptografadas em repouso em `App_Data/Secrets/{key}.dat` fora do `wwwroot` e do controle de versão. Existe fallback de configuração `Llm:{providerCode}:ApiKey` (User Secrets/ambiente). A UI só consulta `ExistsAsync` (bool), e nada de segredo é logado nem aparece em auditoria.
4. **Catálogo Dinâmico de Modelos (`ILlmModelCatalog`)**: `LlmModelEntry(ModelId, DisplayName, IsDefault)`; o painel `Settings/LlmProviders` permite buscar modelos disponíveis diretamente na API do provedor (`FetchModelsFromProviderAsync`) e salvá-los com propósito e default, sem deploy.
5. **Portabilidade (NFR-014)**: Trocar LLM/embedding não exige mudança de domínio nem do modelo transacional — apenas cadastro em `llm_providers`/`llm_model_configs` e eventual novo protocolo em `ILlmProviderResolver`.

### ADR — Campos Estruturais do Ecossistema (Fase 04 do "Ajuste Ecossistema")
Status: **aceito**.
1. **Catálogos de Tipos**: Tabelas `component_types` e `integration_types` como catálogos administráveis; `integrations` ganhou colunas de classificação `responsibility`, `hosting_location` e `direction`.
2. **Contexto Técnico de Produtos**: Tabelas `product_technical_profiles` (com `system_type`), `product_technologies`, `product_technical_sources` e `product_external_research_domains`, para pesquisar/classificar domínios e fontes técnicas por sistema.
3. **Execuções Vinculadas a Casos**: `integration_runs` com `run_context` e `case_id`; `diagnostic_steps` e `case_evidences` podem referenciar `integration_run_id`, ligando evidência a uma execução de integração auditada.
4. **Tags de Casos**: Tabela `case_tags` (N:N com `tags`) para sinais de peso menor no motor de casos semelhantes (integração com `CaseRelationService`).
5. **Resolução com Hipóteses**: Tabela `case_resolution_hypotheses` (N:N) ligando hipóteses validadas à resolução, e `knowledge_items.source_case_iteration_id` para permitir nova solução de um mesmo caso apenas quando reaberto e resolvido em nova iteração.


---

# 22 — Glossário

**Caso/Incidente:** ocorrência concreta investigada pela equipe.  
**Sintoma:** manifestação observável do problema.  
**Hipótese:** explicação candidata ainda não confirmada.  
**Evidência:** dado que fortalece, enfraquece ou contextualiza hipótese.  
**Causa raiz:** causa que explica de forma suficiente a origem do incidente, quando confirmada.  
**Tentativa:** ação/teste realizado durante diagnóstico, com resultado registrado.  
**Solução:** conhecimento reutilizável sobre diagnóstico/resolução.  
**Known Issue:** problema conhecido, possivelmente ainda existente, com impacto/versões/workaround documentados.  
**Runbook:** procedimento operacional executável.  
**Handoff:** transferência de responsabilidade/contexto entre pessoas/departamentos.  
**MTTA:** tempo até primeira atuação.  
**MTTR:** tempo até resolução, conforme definição versionada.  
**RAG:** recuperação de conhecimento seguida de geração por LLM usando esse contexto.  
**Embedding:** representação vetorial de conteúdo usada em similaridade semântica.  
**Chunk:** unidade de conteúdo enviada ao índice semântico/LLM.  
**ACL:** regras de autorização sobre conteúdo.  
**Outbox:** padrão para registrar evento assíncrono na mesma transação do dado de negócio.  
**ADR:** Architecture Decision Record; registro de decisão arquitetural.  
**Drill-down:** navegação de um indicador agregado até os registros que o compõem.  
**Groundedness:** grau em que a resposta da IA está sustentada pelas fontes recuperadas.  
**Similaridade:** proximidade entre itens; não é necessariamente probabilidade de causa.  



---

# 23 — Requisitos não funcionais

## NFR-001 — Disponibilidade
A meta numérica deve ser definida após conhecer infraestrutura e criticidade. A arquitetura deve evitar pontos únicos desnecessários e possuir health checks, backup e recuperação testados.

## NFR-002 — Desempenho
Busca, abertura de caso e navegação devem ser percebidas como interativas. Metas p95 serão definidas a partir de baseline de homologação, não por número arbitrário. Queries críticas devem ser instrumentadas.

## NFR-003 — Escalabilidade
A aplicação deve escalar horizontalmente se necessário sem depender de sessão local não compartilhável para estado de negócio. Estado persistente permanece em serviços apropriados.

## NFR-004 — Segurança
OWASP ASVS/boas práticas ASP.NET Core como referência; autenticação forte, autorização server-side, segredo fora do código, TLS e proteção de uploads.

## NFR-005 — Auditabilidade
Eventos críticos devem ser rastreáveis ponta a ponta com correlação.

## NFR-006 — Manutenibilidade
Monólito modular, dependências direcionais, testes, ADRs e migrations. Evitar framework próprio desnecessário.

## NFR-007 — Acessibilidade
Interface web deve buscar conformidade WCAG 2.2 AA nas jornadas principais: navegação por teclado, foco, contraste, rótulos, mensagens de erro e sem dependência exclusiva de cor.

## NFR-008 — Internacionalização
Baseline em pt-BR. Strings de UI não devem ser espalhadas no domínio; preparar mecanismo de recursos caso a empresa necessite outros idiomas.

## NFR-009 — Compatibilidade
Suportar navegadores corporativos homologados e layout responsivo. A plataforma de conhecimento não exige app mobile nativo no MVP.

## NFR-010 — Resiliência
Integrações externas devem usar timeout, retry apenas quando seguro, circuit breaker quando justificado, idempotência e degradação controlada.

## NFR-011 — Consistência
Operações críticas de negócio devem ser transacionais no MySQL. Processamento assíncrono usa outbox e consistência eventual explícita.

## NFR-012 — Privacidade
Coletar apenas dados necessários para finalidade operacional/gerencial; políticas de retenção e acesso devem ser configuráveis.

## NFR-013 — Recuperação
Backup sem restore testado não é considerado estratégia válida. Definir RPO/RTO antes de produção.

## NFR-014 — Portabilidade de IA
Troca de LLM/embedding não deve exigir mudança do domínio ou do modelo transacional.

## NFR-015 — Observabilidade
Toda jornada crítica deve produzir traces/métricas/logs suficientes para diagnosticar falhas da própria plataforma.



---

# 24 — UX, navegação e desenho de telas

## 1. Princípio de UX

A plataforma é uma ferramenta de investigação. A UI deve reduzir carga cognitiva e manter contexto. Evitar formulários enormes e dashboards cheios de cards sem hierarquia.

## 2. Navegação principal (sidebar real — `_Layout.cshtml`)

Sidebar lateral retrátil (`tc-sidebar`) com navegação agrupada e itens condicionados a permissões (`User.HasClaim`):

```text
Visão Geral
├── Dashboard                    (/)            — sempre visível
├── Minha Área                   (/Users/Details) — usuário logado
└── Copiloto IA                  (/Copilot/Index) — permissão ia.usar

Operação
├── Casos                        (/Cases/Index)    — caso.visualizar | caso.criar
├── Diagnóstico                  (/Diagnosis/Index) — caso.diagnosticar
└── Pesquisa                     (/Search)         — sempre visível

Conhecimento
├── Soluções                     (/Knowledge/Index) — sempre visível
├── Lições Aprendidas            (/Knowledge/Index?category=lessons)
└── Documentação                 (/Placeholder?module=Documentação) — simulação

Ecossistema  [se catalogo.gerenciar | integracao.gerenciar]
├── Sistemas                     (/Catalog/Products/Index) — catalogo.gerenciar
├── Componentes                  (/Catalog/Components/Index) — catalogo.gerenciar
└── Integrações                  (/Integrations/Index) — integracao.gerenciar

Inteligência  [se analytics.visualizar]
├── Dashboard Geral              (/Analytics/Index)
├── Departamentos                (/Analytics/Departments)
├── Usuários                     (/Analytics/Users)
├── Conhecimento                 (/Analytics/Knowledge)
└── Qualidade & IA               (/ContentQuality/Index)

Gestão
├── Usuários                     (/Users/Index)  — usuario.gerenciar
├── Departamentos                (/Departments/Index) — usuario.gerenciar
├── Clientes                     (/Clients/Index) — cliente.gerenciar
├── Perfis e Permissões          (/Placeholder) — permissao.gerenciar (simulação)
└── Causas Raízes                (/Cases/RootCauses/Index) — caso.encerrar

Administração
├── Auditoria                    (/Audit/Index) — auditoria.visualizar
└── Configurações                (/Settings/Index) — configuracao.gerenciar
```

Topbar: busca global (`/Search?q=`) com atalho Ctrl+K, botão "Novo Caso" (caso.criar) e menu do usuário (Meu Perfil / Sair). Rodapé indica TraceCore v1.0 Enterprise. Itens "Perfis e Permissões" e "Documentação" ainda são placeholders (`/Placeholder`).

## 3. Início

Blocos (Dashboard `/`):
- KPIs de visão geral (casos abertos, resolvidos, MTTR médio/mediana, reincidência);
- séries temporais e componentes mais impactados;
- atalho para "Novo Caso" `(/Cases/Create)`;
- busca global no topo;
- painéis de Inteligência e drill-down para `/Cases/Index`.

## 4. Pesquisa

Tela real: `/Search`. Layout desktop implementado com topo de busca global e filtros laterais combinados (Cliente, Unidade, Tecnologia, entre outros).

Resultado mostra:
- título;
- tipo;
- resumo/snippet;
- compatibilidade;
- status;
- fatores de correspondência (`MatchedFactors` — blocos prioritários para identificadores técnicos exatos);
- uso/sucesso com amostra quando aplicável.

## 5. Caso

Telas reais: `/Cases/Index`, `/Cases/Details`, `/Cases/Create` e `/Cases/RootCauses/Index`. Cabeçalho fixo com número, status, severidade, cliente, produto e owner.

Áreas de detalhe implementadas (visão geral, diagnóstico, timeline, evidências, relacionados, resolução) via partiais corporativas (`_InvestigationTimeline`, `_HypothesisCard`, `_EvidenceCard`, `_DiagnosticStepCard`, `_RelatedCaseCard`). A timeline distingue visualmente observação, teste, hipótese, handoff, ação e resolução, sem depender apenas de cor.

## 6. Diagnóstico

Telas reais: `/Diagnosis/Index` e `/Diagnosis/Flows/Index`. A tela apresenta contexto do caso, hipóteses ranqueadas pelo motor, próxima verificação sugerida, histórico já testado e semelhantes.

Cada verificação possui botões rápidos de resultado e integração opcional de health-check automatizado (BR-073).

## 7. Conhecimento

Telas reais: `/Knowledge/Index` e `/Knowledge/Details`. A visualização do artigo contém status e revisão, aplicabilidade, conteúdo, riscos/rollback destacados, casos que validaram, histórico de versões e ação "usar neste caso" (`KnowledgeUsage`).

## 8. Analytics

Telas reais: `/Analytics/Index`, `/Analytics/Departments`, `/Analytics/Users`, `/Analytics/Knowledge` e `/ContentQuality/Index`. Padrões:
- filtros globais no topo;
- período sempre explícito;
- definição do KPI acessível;
- drill-down por clique para `/Cases/Index`;
- estado "sem dados" diferente de zero;
- exportação conforme permissão.

## 9. Administração

Telas reais agrupadas por domínio:
- Pessoas e acesso: `/Users/Index`, `/Departments/Index`;
- Catálogo técnico: `/Clients/Index`, `/Catalog/Products/Index`, `/Catalog/Components/Index`;
- Integrações: `/Integrations/Index`;
- IA: `/Settings/LlmProviders/Index`, `/ContentQuality/Index`;
- Auditoria: `/Audit/Index`;
- Configurações: `/Settings/Index`;

## 10. Estados da interface

Toda tela assíncrona deve tratar:
- loading;
- vazio;
- erro recuperável;
- sem permissão;
- dado desatualizado/conflito de concorrência;
- indisponibilidade de integração/IA.

## 11. Confirmações

Não pedir confirmação para ações triviais. Exigir confirmação clara para:
- desativação;
- publicação;
- depreciação;
- mudança de permissão;
- encerramento crítico;
- ação irreversível.



---

# 25 — Matriz inicial de perfis e permissões

Perfis são conveniências administrativas. A autorização real deve usar permissões granulares.

| Capacidade | Usuário Técnico | Especialista | Revisor | Gestor | Admin Funcional | Admin |
|---|---:|---:|---:|---:|---:|---:|
| Pesquisar conhecimento | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Criar/editar próprio caso (`caso.criar`, `caso.editar`) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Relacionar casos (`caso.relacionar`) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Diagnosticar caso (`caso.diagnosticar`) | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Reabrir caso (`caso.reabrir`) | - | ✓ | ✓ | ✓ | ✓ | ✓ |
| Criar rascunho de solução | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Revisar solução | - | ✓ opcional | ✓ | ✓ opcional | ✓ | ✓ |
| Publicar solução | - | - | ✓ | ✓ opcional | ✓ | ✓ |
| Ver analytics operacional | escopo | escopo | escopo | ✓ | ✓ | ✓ |
| Ver analytics individual | próprio/limitado | limitado | limitado | ✓ escopo | ✓ | ✓ |
| Gerenciar clientes (`cliente.gerenciar`) | - | - | - | - | ✓ | ✓ |
| Gerenciar catálogo (`catalogo.gerenciar`) | - | escopo | - | escopo | ✓ | ✓ |
| Gerenciar usuários (`usuario.gerenciar`) | - | - | - | limitado | ✓ | ✓ |
| Gerenciar papéis | - | - | - | - | limitado | ✓ |
| Ver auditoria | próprio/limitado | limitado | limitado | escopo | ✓ | ✓ |
| Configurar IA | - | - | - | - | ✓ | ✓ |
| Ver conteúdo sensível | por ACL | por ACL | por ACL | por ACL | por ACL | por ACL |

## Regras

- Papel `Admin` é único e herda dinamicamente todas as permissões do sistema.
- Existe proteção estrita no `UserService` impedindo desativar ou revogar o papel do último usuário `Admin` ativo.
- Permissão de publicação é restrita a revisores e administradores.
- Mudanças de papel/permissão devem auditar o evento completo em `audit_events`.

## Códigos de permissão reais (navegação e guards)

Seeded nas migrations e usados nos guards de páginas/UI:

```text
caso.visualizar · caso.criar · caso.editar · caso.encerrar
caso.diagnosticar · caso.relacionar · caso.reabrir
solucao.criar · solucao.validar · solucao.publicar
analytics.visualizar · analytics.departamento
conhecimento.visualizar
usuario.gerenciar · permissao.gerenciar
auditoria.visualizar
cliente.gerenciar
catalogo.gerenciar
integracao.gerenciar
configuracao.gerenciar
ia.usar
```

Papéis seed: `Usuário Técnico`, `Especialista`, `Revisor`, `Gestor`, `Admin Funcional`, `Admin Segurança`. Usuário admin inicial: `admin@tracecore.local` (credenciais em `DEV_CREDENTIALS.md`).



---

# 26 — Catálogo inicial de KPIs

## KPI-001 — MTTA
**Pergunta:** quanto tempo levamos para iniciar atuação?  
**Fórmula:** `first_response_at - opened_at`.  
**Segmentar:** severidade, cliente, produto, departamento, origem.  
**Cuidados:** casos importados podem ter timestamp externo diferente.

## KPI-002 — MTTR
**Pergunta:** quanto tempo até resolver?  
**Fórmula base:** `resolved_at - opened_at`.  
**Cuidados:** definir política para períodos `AwaitingInfo` antes de comparar departamentos.

## KPI-003 — Reincidência
**Pergunta:** o mesmo problema está voltando?  
**Fórmula:** casos ligados por mesma causa/componente em janela configurada / casos resolvidos elegíveis.  
**Cuidados:** exige boa qualidade de causa/relacionamento.

## KPI-004 — Reutilização de conhecimento
**Pergunta:** quanto do trabalho usa aprendizado anterior?  
**Fórmula:** casos com `knowledge_usage` / casos resolvidos elegíveis.

## KPI-005 — Sucesso observado de solução
**Fórmula:** usos `Worked` / usos classificáveis.  
**Exibir:** sucessos, parciais, falhas e N total.  
**Nunca:** mostrar percentual isolado.

## KPI-006 — Zero Result Rate
**Fórmula:** consultas com 0 resultados / consultas totais.

## KPI-007 — Zero Click Rate
**Fórmula:** consultas com resultados, mas sem abertura de item / consultas com resultados.  
**Interpretação:** pode indicar baixa relevância, mas também consulta exploratória; não concluir sozinho.

## KPI-008 — Handoffs por caso
**Fórmula:** quantidade média/mediana de transferências por caso.  
**Uso:** encontrar roteamento ruim e fronteiras problemáticas.

## KPI-009 — Tempo por etapa
Medir duração por estado/departamento para identificar espera versus investigação ativa.

## KPI-010 — Conhecimento vencido
Itens publicados com `review_due_at < agora` / itens publicados.

## KPI-011 — Cobertura de conhecimento
Percentual de famílias recorrentes de incidentes que possuem solução publicada vinculada. Requer definição de cluster/família.

## KPI-012 — Tempo até primeiro resultado útil
Do início da busca até abertura/marcação de item posteriormente classificado como útil.

## KPI-013 — Passos diagnósticos até resolução
Mediana de passos classificáveis por família de sintoma.

## KPI-014 — Falha pós-release
Casos ligados a versão/release em janela definida. Não afirmar causalidade sem evidência.

## KPI-015 — Adoção do RAG
Interações com RAG, fontes abertas, feedback, uso posterior em caso. Não confundir uso com qualidade.

## Governança

Cada KPI terá:
- ID;
- nome;
- fórmula;
- versão;
- proprietário;
- data de vigência;
- filtros válidos;
- exclusões;
- fonte de dados;
- observações de interpretação.

---

## Fórmulas Consolidadas na Fase 10 (M08 — Analytics & Inteligência Gerencial)

### KPI-002-A — Casos Abertos
- **Fórmula:** `COUNT(DISTINCT cases.id)` onde `cases.status IN ('Open', 'Reopened')` no filtro.
- **Cuidados:** O domínio do TraceCore não utiliza o estado 'Investigating'. Abertos incluem ocorrências novas e reabertas.

### KPI-002-B — Casos Resolvidos
- **Fórmula:** `COUNT(DISTINCT cases.id)` onde `cases.status = 'Resolved'` com iteração atual resolvida.

### KPI-002-C — MTTR Operacional por Iteração
- **Fórmula:** `AVG(TIMESTAMPDIFF(MINUTE, case_iterations.opened_at, case_iterations.closed_at))` sobre iterações com `status = 'Resolved'` e `closed_at IS NOT NULL`.
- **Interpretação:** Mede a velocidade de resolução do ciclo ativo. Não acumula o tempo total de casos multi-iteração (evitando distorcer o tempo médio por reabertura pontual).

### KPI-002-D — Mediana de Resolução
- **Fórmula:** Elemento central da distribuição ordenada de durações em minutos das iterações resolvidas.
- **Implementação:** Compatível com MySQL/MariaDB 10.5+ e In-Memory via projeção de durações escalares e cálculo determinístico central.

### KPI-003-A — Incidentes Recorrentes (Critério Determinístico)
- **Fórmula:** Casos que possuem ao menos um vínculo em `case_relations` com `relation_type IN ('Recurrence', 'CommonCause')`.
- **Exclusão:** Vínculos do tipo `Similar` (computados por correspondência textual) **não** caracterizam recorrência formal.

### KPI-010-A — Governança de Conhecimento
- **Nunca Revisado:** Itens de conhecimento publicados onde `last_reviewed_at IS NULL`.
- **Revisão Vencida:** Itens publicados onde `review_due_at < AGORA()`.
- **Sem Solução Documentada:** Casos com `status = 'Resolved'` onde não existe `knowledge_items.provenance_case_id = cases.id`.

---

## Fórmulas Consolidadas na Fase 16 (Inteligência Analítica Determinística — §31)

### KPI-014-A — Tendência e Variação Pós-Versão de Ajuste
- **Pergunta:** Qual foi o impacto da publicação de uma versão de produto no volume e MTTR dos casos?
- **Fórmula:** 
  - Janela de observação simétrica: $I$ dias antes e $I$ dias depois da `product_versions.released_at` (padrão 90 dias).
  - Variação percentual de volume: $\Delta\% = \frac{N_{depois} - N_{antes}}{N_{antes}} \times 100$ (quando $N_{antes} > 0$).
  - Variação de MTTR: $\Delta\%_{MTTR} = \frac{\text{Mediana}_{depois} - \text{Mediana}_{antes}}{\text{Mediana}_{antes}} \times 100$.
- **Rastreabilidade e Grounding (§31):** O indicador retorna obrigatoriamente $N_{antes}$, $N_{depois}$, $N_{total}$ e `HasSufficientData` (requer $N_{total} \ge 3$). Se $N < 3$, a IA declara expressamente que a amostra é insuficiente para uma inferência estatística, sem inventar percentuais.

### KPI-016 — Associação Factual de Componentes a Sintomas e Falhas
- **Pergunta:** Quais componentes do catálogo técnico concentram a maior proporção de ocorrências de determinado erro ou contexto?
- **Fórmula:** $\text{Proporção}(\text{componente}) = \frac{\text{Casos do Componente}}{\text{Total de Casos Filtrados}} \times 100$.
- **Rastreabilidade e Grounding (§31):** Retorna o ranking determinístico consolidado no banco (`case_components`), com contagem absoluta e percentual arredondado em 1 casa decimal. Requer $N \ge 5$ casos para declarar suficiência amostral.

### KPI-005-A — Comparação de Efetividade de Solução (Mediana de MTTR)
- **Pergunta:** A aplicação desta solução da base de conhecimento reduz o tempo de resolução em relação aos casos similares resolvidos sem ela?
- **Fórmula:**
  - $\text{Mediana Com} = \text{Mediana}(\text{Durações de casos com } \text{knowledge\_usages}(\text{item\_id}))$.
  - $\text{Mediana Sem} = \text{Mediana}(\text{Durações de casos no mesmo escopo técnico sem } \text{knowledge\_usages}(\text{item\_id}))$.
  - $\text{Redução\%} = \frac{\text{Mediana Sem} - \text{Mediana Com}}{\text{Mediana Sem}} \times 100$.
- **Rastreabilidade e Grounding (§31):** Retorna obrigatoriamente $N_{com}$, $N_{sem}$ e escopo técnico considerado. Se $N_{com} < 3$ ou $N_{sem} < 3$, o sistema declara status de suficiência amostral falso (`HasSufficientData = false`), e a IA deve reportar "dados insuficientes" em vez de emitir recomendações definitivas.



---

# 27 — Cenários e fluxos de referência

Estes cenários não são regras rígidas de diagnóstico. Servem para orientar a modelagem do produto e os testes de jornada.

## Cenário A — “Não consigo entrar no sistema”

### Entrada
Cliente informa que um usuário não consegue acessar.

### Contexto inicial desejado
- cliente;
- produto/canal: web, desktop ou mobile;
- ambiente;
- versão, quando aplicável;
- usuário afetado ou escopo;
- texto exato da mensagem;
- horário de ocorrência.

### Primeira redução de incerteza
1. afeta um usuário ou vários?
2. a interface abre?
3. o erro ocorre antes ou depois do envio da credencial?
4. autenticação está saudável?
5. API correspondente responde?
6. o mesmo usuário acessa por outro canal?
7. houve bloqueio/expiração/configuração recente?

### Possíveis domínios envolvidos
- identidade/autenticação;
- rede/infra;
- frontend;
- API;
- banco/serviço de identidade;
- configuração do cliente;
- versão do desktop/mobile.

### O que a plataforma deve mostrar
- casos semelhantes por erro e contexto;
- known issues de versão;
- verificações de baixo risco;
- resultados históricos;
- responsável somente quando evidência apontar um domínio.

## Cenário B — Desktop “não conecta ao servidor”

### Sinais úteis
- host/endpoint configurado;
- resolução DNS;
- porta;
- TLS/certificado;
- proxy/firewall;
- reachability da API;
- versão do desktop;
- configuração local;
- status do serviço.

### Fluxo
Relato → identificar se falha é local ou coletiva → testar endpoint → comparar configuração com cliente saudável → verificar infraestrutura → verificar compatibilidade de versão → relacionar caso/solução → resolver/escalar.

## Cenário C — Web carrega, mas operação falha

Exemplo: tela abre, porém salvar retorna erro.

A plataforma deve separar:
- UI carregada;
- autenticação válida;
- chamada de API específica;
- regra de negócio;
- persistência;
- integração chamada pela operação.

Evidências como HTTP status, correlation ID e timestamp devem permitir atravessar frontend → API → banco/integração usando observabilidade.

## Cenário D — Integração SAP não processa

### Perguntas iniciais
- todas as operações ou tipo específico?
- uma unidade/cliente ou todos?
- fila acumulada?
- SAP acessível?
- autenticação/certificado válido?
- payload rejeitado?
- mudança de schema/configuração?
- retry está ocorrendo?

### Comportamento desejado
A busca deve encontrar casos pelo nome funcional e também por códigos técnicos, sem exigir que o atendente saiba se a falha está no produto, no adapter ou no SAP.

## Cenário E — Lentidão intermitente

A plataforma deve evitar solução genérica “reinicie”.

Coletar:
- período;
- usuários afetados;
- operação;
- latência observada;
- release recente;
- métricas de API;
- slow queries;
- consumo de recurso;
- dependências externas;
- concorrência.

Casos anteriores devem ser comparados por padrão temporal, componente e evidência, não apenas pela palavra “lento”.

## Cenário F — Mesmo problema reaparece meses depois

### Objetivo central do produto
Ao abrir o novo caso, o sistema identifica:
- mesma mensagem/sintoma;
- mesmo componente;
- mesma versão ou família;
- cliente diferente ou igual;
- solução anteriormente usada;
- tentativa que falhou;
- causa raiz anterior;
- ação preventiva que ficou pendente.

O técnico deve conseguir reaproveitar o caminho anterior e registrar se a recorrência confirma a mesma causa ou representa uma nova variação.

## Cenário G — Escalonamento entre departamentos

Antes de transferir, a plataforma gera automaticamente um resumo:

```text
Problema: ...
Cliente/ambiente/versão: ...
Impacto: ...
Sintomas confirmados: ...
Hipóteses descartadas: ...
Hipóteses ainda abertas: ...
Testes já executados: ...
Evidências principais: ...
Casos/soluções semelhantes: ...
Motivo do escalonamento: ...
```

O receptor não deve precisar pedir novamente informações já registradas.

## Cenário H — Solução histórica ficou desatualizada

Novo caso encontra artigo antigo, mas a versão atual é incompatível. O sistema deve:
- penalizar/alertar no ranking;
- mostrar versão validada;
- permitir feedback “não se aplica”; 
- abrir tarefa de revisão se houver recorrência;
- manter artigo antigo acessível para casos históricos.



---

# 99 — Referências técnicas consultadas para a baseline

Data de consulta: 17/09/2026.

## .NET

- Microsoft — Download .NET: https://dotnet.microsoft.com/download
- Microsoft — Política de suporte do .NET: https://dotnet.microsoft.com/platform/support/policy
- Microsoft Learn — ASP.NET Core 10: https://learn.microsoft.com/aspnet/core/?view=aspnetcore-10.0

A baseline usa .NET 10 porque é a versão LTS ativa no momento da documentação.

## MySQL

- MySQL 8.4 Reference Manual: https://dev.mysql.com/doc/refman/8.4/en/
- MySQL Releases: Innovation and LTS: https://dev.mysql.com/doc/refman/8.4/en/mysql-releases.html
- MySQL Community Server 8.4 LTS: https://dev.mysql.com/downloads/mysql/8.4.html

A baseline usa a linha MySQL 8.4 LTS para privilegiar estabilidade. A versão de patch deve ser mantida atualizada conforme política de segurança da empresa.

## Provider .NET/MySQL

- MySQL Connector/NET / EF Core support: https://dev.mysql.com/doc/connector-net/en/connector-net-entityframework-core.html
- Pomelo.EntityFrameworkCore.MySql: https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql

Como a compatibilidade de providers EF pode variar por release, a baseline técnica propõe Dapper + MySqlConnector e deixa EF Core sujeito a spike/ADR. Isso preserva .NET 10 e MySQL sem acoplar o domínio a um provider específico.



---
