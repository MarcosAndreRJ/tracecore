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
- departamento/equipe;
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

