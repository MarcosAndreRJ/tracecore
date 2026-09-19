# TraceCore — Visão e Princípios da IA

## 1. Objetivo

Este documento registra a visão oficial para o uso de Inteligência Artificial no TraceCore.

A IA será uma camada assistiva, opcional e desacoplada. Seu papel é acelerar o trabalho humano, aumentar a consistência da informação e ajudar a encontrar caminhos investigativos com mais rapidez.

O TraceCore deve continuar plenamente utilizável quando a IA estiver indisponível, desativada ou quando o usuário preferir executar todo o fluxo manualmente.

Este documento deve ser referenciado pela documentação de arquitetura e pela **Ordem de desenvolvimento do TraceCore** nas fases relacionadas a IA, busca semântica, RAG, copiloto e integrações futuras.

---

## 2. Princípio central

> A IA é um acelerador opcional do TraceCore, nunca um requisito para o funcionamento do sistema.

Todo fluxo essencial deve possuir um caminho manual equivalente, incluindo criação e edição de casos, hipóteses, evidências, diagnóstico, resolução, reabertura, lições aprendidas, conhecimento, pesquisa, catálogo, gestão e auditoria.

A IA pode tornar esses fluxos mais rápidos, consistentes e informados, mas não pode ser a única forma de executá-los.

---

## 3. Papel da IA

A IA será um **copiloto operacional e analítico**, com quatro papéis principais:

1. Assistente operacional.
2. Assistente investigativo.
3. Assistente de consistência.
4. Assistente contextual proativo.

Esses papéis complementam o TraceCore e não substituem suas regras de negócio.

---

## 4. Assistente operacional

A IA poderá receber comandos em linguagem natural e transformá-los em propostas estruturadas de operação.

Exemplo:

> “Adicionar um novo caso. O cliente informou erro HTTP 502 no Portal, começou depois da atualização 8.4 e acontece apenas em produção.”

A LLM poderá interpretar a intenção e montar algo equivalente a:

~~~text
Intent: CreateCase
Client: cliente identificado no contexto
Product: Portal
Version: 8.4
Environment: Produção
ErrorCode: HTTP 502
OriginalReport: ...
~~~

A LLM **não grava diretamente no banco de dados**.

Fluxo correto:

~~~text
Usuário
   ↓
LLM interpreta intenção
   ↓
LLM produz proposta estruturada
   ↓
TraceCore valida permissões
   ↓
TraceCore valida regras de negócio
   ↓
TraceCore executa o comando
   ↓
Persistência normal
   ↓
Auditoria
   ↓
Resultado devolvido à LLM/UI
~~~

A LLM inicia ou orquestra o procedimento. O TraceCore continua sendo responsável por executá-lo.

---

## 5. A LLM nunca grava diretamente no banco

Esta é uma regra arquitetural obrigatória.

A LLM não deverá possuir acesso SQL direto, connection string, credenciais do banco, repository irrestrito ou capacidade de contornar Application Services, autorização e validações.

Toda operação deve passar pelas mesmas APIs, commands, services e regras utilizadas pela interface convencional.

~~~text
LLM
 ↓
Tool / Application Command
 ↓
TraceCore Application
 ↓
Domain Rules
 ↓
Authorization
 ↓
Repository
 ↓
MySQL
~~~

Nunca:

~~~text
LLM
 ↓
SQL
 ↓
MySQL
~~~

---

## 6. Paridade entre fluxo manual e fluxo assistido

Toda ação essencial oferecida pela IA deve possuir caminho manual correspondente.

Exemplo:

### Com IA

> “Registre esta evidência no caso e relacione-a à hipótese de falha no gateway.”

A IA interpreta e prepara a ação.

### Sem IA

O usuário acessa o caso, registra a evidência e escolhe manualmente a relação com a hipótese.

Os dois caminhos chegam às mesmas regras de domínio.

---

## 7. Assistente investigativo

A LLM poderá auxiliar na investigação utilizando, quando autorizado:

- relato atual;
- sintomas;
- evidências;
- hipóteses;
- resultados de testes;
- cliente;
- produto;
- versão;
- ambiente;
- componentes;
- dependências;
- casos históricos;
- casos relacionados;
- causas raiz;
- soluções anteriores;
- conhecimento validado;
- documentação técnica;
- lições aprendidas.

Exemplos de perguntas:

> “Já tivemos algo parecido?”

> “Quais casos anteriores possuem sinais semelhantes?”

> “Existe alguma solução validada aplicável a este contexto?”

> “Que diferenças existem entre este caso e os casos mais próximos?”

> “Quais hipóteses ainda não foram testadas?”

> “Existe evidência que contradiz a hipótese atual?”

> “Que dependências deste componente podem explicar o sintoma?”

A IA deve buscar evidências e fontes antes de formular conclusões.

---

## 8. Pesquisa profunda sobre experiências anteriores

A camada inteligente poderá cruzar similaridade textual, contexto técnico, códigos de erro, cliente, sistema, versão, ambiente, componente, dependências, causa raiz, solução, evidências, sequência de investigação, recorrências e conhecimento validado.

~~~text
Novo caso
 ↓
Busca híbrida
 ↓
Casos históricos
Conhecimento validado
Dependências relacionadas
 ↓
LLM analisa diferenças e coincidências
 ↓
Resposta contextual
~~~

---

## 9. Assistência contextual proativa

A IA não precisa atuar apenas quando o usuário abre explicitamente um chat.

Eventos do TraceCore poderão disparar análise contextual.

Exemplo:

~~~text
CaseCreated
 ↓
Análise contextual assistida
 ↓
Busca casos semelhantes
 ↓
Busca conhecimento aplicável
 ↓
LLM sintetiza o contexto
 ↓
UI apresenta ajuda ao usuário
~~~

Resposta possível:

> Caso TC-2026-001842 criado com sucesso.
>
> Foram encontrados casos anteriores com contexto semelhante e uma solução validada potencialmente aplicável.
>
> Deseja abrir os casos relacionados ou iniciar a verificação recomendada?

O caso já foi criado pelo TraceCore. A análise inteligente vem depois como assistência.

---

## 10. Eventos que podem gerar assistência contextual

Futuramente, a camada de IA poderá ser acionada após eventos como:

- CaseCreated;
- CaseUpdated;
- EvidenceCreated;
- EvidenceLinkedToHypothesis;
- HypothesisCreated;
- DiagnosticStepCompleted;
- CaseReopened;
- ResolutionStarted;
- CaseResolved;
- KnowledgeDraftCreated;
- KnowledgeSubmittedForReview.

Se o serviço de IA estiver indisponível, a operação principal continua normalmente.

---

## 11. Assistente de consistência

A IA deverá ajudar a melhorar a qualidade da informação inserida no TraceCore.

Exemplos:

> “Este caso está sendo encerrado, mas ainda não possui causa raiz definida.”

> “A evidência registrada parece contradizer a hipótese atualmente marcada como Supported.”

> “A solução está sendo publicada sem ambiente de aplicabilidade informado.”

> “Existem conhecimentos muito semelhantes. Vale revisar possível duplicidade.”

> “O caso possui resolução, mas ainda não há lição aprendida ou conhecimento associado.”

A IA sinaliza. Ela não altera silenciosamente os dados.

---

## 12. Proposta, validação e execução

Operações iniciadas pela IA seguem três estágios:

~~~text
1. PROPOSTA
LLM interpreta e estrutura a ação.

2. VALIDAÇÃO
TraceCore verifica:
- permissão;
- campos;
- referências;
- regras de negócio;
- concorrência;
- estado da entidade.

3. EXECUÇÃO
TraceCore executa pelos serviços normais.
~~~

Em operações sensíveis, a UI poderá exigir confirmação humana antes da execução.

---

## 13. Ferramentas da LLM

A futura camada de IA deverá interagir com o TraceCore através de tools/commands explicitamente definidos.

Exemplos conceituais:

~~~text
SearchCases
SearchKnowledge
GetCaseDetails
GetKnowledgeDetails
GetComponentDependencies
ProposeCaseCreation
CreateCase
AddEvidence
LinkEvidenceToHypothesis
CreateHypothesis
RecordDiagnosticStep
ProposeResolution
CreateKnowledgeDraft
~~~

Cada ferramenta precisa possuir schema definido, autorização, validação, tratamento de erros, auditoria e limites de acesso.

---

## 14. Leitura e escrita

A camada de ferramentas deve distinguir claramente operações de leitura e escrita.

Operações de escrita possuem maior risco e devem sempre passar pelas regras de negócio e autorização do TraceCore.

---

## 15. A IA respeita o usuário atual

> Se o usuário não pode acessar determinado conteúdo pela interface normal, a IA também não pode revelar esse conteúdo.

Isso vale para pesquisa, RAG, casos, documentos, conhecimento, clientes e auditoria.

---

## 16. Hierarquia das fontes

A IA deve distinguir a natureza e a confiabilidade das fontes:

1. **Conhecimento validado** — material revisado e publicado.
2. **Evidências e dados estruturados do caso atual** — fatos da investigação corrente.
3. **Casos históricos** — experiência passada real, mas contextual.
4. **Documentos técnicos** — material de referência dependente da origem e versão.
5. **Conteúdo sugerido por IA** — hipótese ou elaboração da própria IA.

Essas fontes não devem ser apresentadas como equivalentes.

---

## 17. Conhecimento validado e sugestão da IA

A IA nunca transforma automaticamente sua própria resposta em conhecimento oficial.

~~~text
IA sugere
 ↓
usuário revisa
 ↓
Knowledge Draft
 ↓
fluxo normal de revisão
 ↓
aprovação humana
 ↓
Published / Validated
~~~

Auto-publicação de conteúdo gerado por IA é proibida.

---

## 18. RAG

RAG será utilizado futuramente para fornecer à LLM contexto recuperado das fontes do TraceCore.

~~~text
Pergunta / evento
 ↓
Detecção de intenção
 ↓
Busca exata
 ↓
Filtros estruturados
 ↓
Busca textual
 ↓
Busca semântica
 ↓
Reranking
 ↓
Montagem de contexto
 ↓
LLM
 ↓
Resposta com fontes
~~~

RAG não substitui MySQL, domínio, repositories, busca determinística ou regras de negócio.

---

## 19. Busca híbrida antes da LLM

Identificadores técnicos exatos devem continuar sendo priorizados antes da busca semântica.

~~~text
Identificador exato
        ↓
Filtros estruturados
        ↓
FULLTEXT / textual
        ↓
Semantic search
        ↓
Reranking
        ↓
LLM
~~~

---

## 20. Similaridade não é certeza

A IA não deve converter score de recuperação em probabilidade diagnóstica.

Em vez de “92% de chance”, preferir explicações como:

> “Este caso possui forte similaridade com três ocorrências anteriores porque compartilha o mesmo erro, produto e ambiente.”

---

## 21. Evidências e hipóteses

A IA deverá respeitar o modelo:

~~~text
Evidence
   ↓
Supports / Contradicts / Inconclusive / Confirms
   ↓
Hypothesis
~~~

Ela pode sugerir relações, detectar contradições e propor testes, mas não alterar essas relações silenciosamente.

---

## 22. Catálogo técnico e dependências

O catálogo técnico deverá participar do contexto quando relevante.

Se um Desktop depende de API, autenticação e banco, a IA pode investigar essas dependências em vez de procurar apenas ocorrências que contenham a palavra “Desktop”.

---

## 23. Modo sem IA

O sistema precisa permanecer funcional sem IA.

Se o provider estiver indisponível, desativado ou bloqueado por política:

- aplicação inicia;
- casos funcionam;
- pesquisa determinística funciona;
- diagnóstico guiado determinístico funciona;
- conhecimento funciona;
- gestão funciona;
- auditoria funciona.

A interface deve informar a indisponibilidade da assistência sem bloquear o fluxo manual.

---

## 24. Desacoplamento de provider

O domínio do TraceCore não deve depender diretamente de um fornecedor específico de LLM.

Criar futuramente uma abstração equivalente a:

~~~text
ILlmGateway
~~~

Possíveis providers podem incluir OpenAI, Azure OpenAI, modelos locais ou outros.

---

## 25. Arquitetura conceitual

~~~text
TraceCore.Application
        ↓
AI Orchestration
        ↓
ILlmGateway
        ↓
Provider Adapter
        ↓
LLM Provider
~~~

O gateway concentra preocupações técnicas de comunicação. A orquestração decide ferramentas, contexto, permissões, confirmação e validação de saída estruturada.

---

## 26. Falha segura

Timeout, provider offline, resposta inválida ou tool call inválida não podem corromper dados, executar operações incompletas, bloquear fluxo manual ou simular sucesso inexistente.

---

## 27. Auditoria da IA

Registrar, quando apropriado:

- usuário que iniciou;
- operação solicitada;
- tool chamada;
- resultado da tool;
- decisão de confirmação;
- fontes usadas;
- provider/modelo técnico quando necessário;
- timestamp.

Não armazenar chain-of-thought privada do modelo.

---

## 28. Explicabilidade

Respostas investigativas devem indicar, quando possível:

- fontes;
- casos relacionados;
- conhecimentos utilizados;
- fatores relevantes;
- diferenças;
- limitações.

---

## 29. A IA pode dizer “não sei”

Quando não houver evidência suficiente, a IA não deve fabricar solução.

> “Não encontrei conhecimento validado nem casos suficientemente semelhantes para indicar uma solução com segurança. Posso ajudar a estruturar as próximas verificações.”

---

## 30. IA e diagnóstico guiado

O motor determinístico continua existindo independentemente da LLM.

~~~text
Motor determinístico
        +
Histórico do TraceCore
        +
RAG
        +
LLM
        ↓
Assistência investigativa
~~~

A LLM complementa o motor, não o substitui.

---

## 31. IA e gestão

A IA poderá permitir consultas gerenciais em linguagem natural, mas os números deverão continuar vindo das métricas reais do TraceCore.

Ela não inventa indicadores.

---

## 32. Ações autônomas

Por padrão, a IA não deve executar autonomamente operações destrutivas ou de governança, como excluir conteúdo, remover usuário, alterar papel/permissão, publicar conhecimento oficial, resolver caso definitivamente ou alterar causa raiz sem o fluxo normal de autorização e confirmação quando pertinente.

---

## 33. Segurança contra prompt injection

Conteúdo recuperado de casos, documentos e conhecimento deve ser tratado como **dados**, não como instruções para a LLM.

A implementação deve separar claramente:

- system instructions;
- user intent;
- retrieved content;
- tool results.

---

## 34. Dados externos e proveniência

Logs, tickets, SAP, telemetria e documentos externos devem manter origem, momento da coleta, contexto e permissões.

---

## 35. Observabilidade

A futura camada de IA deve permitir acompanhar latência, erros, uso, custo/tokens, tools chamadas, taxa de sucesso, respostas sem fonte e falhas de recuperação.

---

## 36. Avaliação

Antes de considerar a IA madura, deverá existir conjunto de avaliação para verificar, entre outros pontos:

- recuperação de caso conhecido;
- solução correta para erro conhecido;
- respeito a permissões;
- diferenciação entre caso histórico e conhecimento validado;
- não invenção de fontes;
- escolha correta de tool;
- respeito à confirmação;
- funcionamento quando a LLM estiver offline.

---

## 37. Evolução em etapas

### Etapa A — Preparação

Classificação, metadados, versionamento, qualidade, proveniência e conteúdo normalizado.

### Etapa B — Recuperação semântica

Embeddings, índice vetorial, busca híbrida e avaliação de recuperação.

### Etapa C — RAG

Montagem de contexto, fontes, respostas fundamentadas e autorização.

### Etapa D — Copiloto de leitura

Perguntas, pesquisa, comparação e investigação.

### Etapa E — Copiloto operacional

Interpretação de intenção, tools, proposta de commands, confirmação e execução pelo TraceCore.

### Etapa F — Assistência contextual proativa

Eventos, análise após alterações e sugestões no fluxo operacional.

---

## 38. Regras inegociáveis

1. **IA é opcional.**
2. **Todo fluxo essencial possui caminho manual.**
3. **LLM não grava diretamente no banco.**
4. **TraceCore executa todas as operações de negócio.**
5. **As mesmas permissões valem com ou sem IA.**
6. **As mesmas regras de domínio valem com ou sem IA.**
7. **MySQL continua sendo a fonte transacional de verdade.**
8. **Conhecimento validado é diferente de caso histórico.**
9. **Sugestão da IA é diferente de conhecimento validado.**
10. **IA não publica conhecimento automaticamente.**
11. **IA deve identificar suas fontes quando fizer análise baseada na base.**
12. **Score de similaridade não é probabilidade diagnóstica.**
13. **Falha da IA não interrompe a operação principal.**
14. **Provider de LLM deve ser desacoplado.**
15. **Ações relevantes envolvendo IA devem ser auditáveis.**
16. **Conteúdo recuperado é dado, não instrução.**
17. **Nenhum usuário recebe pela IA dados que não poderia acessar normalmente.**
18. **A IA deve poder admitir insuficiência de evidências.**

---

## 39. Critério de sucesso

A IA será considerada bem-sucedida quando reduzir o caminho entre problema relatado, contexto compreendido, experiência anterior localizada, hipóteses relevantes, evidências, solução e conhecimento reutilizável sem retirar do TraceCore determinismo, rastreabilidade, governança, segurança, funcionamento manual e controle humano.

---

## 40. Visão final

O TraceCore não deve se tornar **“um chatbot com um banco de dados”**.

A visão é:

> **Uma plataforma operacional e de conhecimento completa por si só, com uma camada inteligente capaz de entender linguagem natural, pesquisar profundamente a experiência acumulada, sugerir caminhos, detectar inconsistências e iniciar procedimentos do próprio TraceCore de forma segura.**

A LLM atua como copiloto.

O TraceCore permanece responsável pelo domínio, pelas regras, pela persistência e pela verdade operacional.
