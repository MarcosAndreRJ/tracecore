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

