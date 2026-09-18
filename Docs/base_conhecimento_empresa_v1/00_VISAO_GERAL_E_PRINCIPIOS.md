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

1. Identidade, usuários, departamentos, equipes e permissões.
2. Catálogo técnico: clientes, produtos, versões, componentes, integrações e dependências.
3. Casos/incidentes e sessões de diagnóstico.
4. Base de conhecimento e soluções.
5. Pesquisa, filtros e similaridade.
6. Motor de diagnóstico guiado.
7. Analytics, relatórios e gestão.
8. Auditoria, segurança e governança.
9. IA/RAG e assistente técnico.

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

