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

