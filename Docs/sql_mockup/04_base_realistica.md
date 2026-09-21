# TRACECORE — RESET COMPLETO DA BASE + NOVA MASSA REALISTA DE TESTES
# DELPHI + FIREBIRD 5 + SERVIÇOS + APIs + INTEGRAÇÕES + WEB + MOBILE

## OBJETIVO

Executar duas tarefas nesta ordem:

1. ZERAR completamente os dados funcionais da base de DESENVOLVIMENTO;
2. Criar uma nova massa de dados empresarial, coerente, relacionada e realista para testes do TraceCore.

IMPORTANTE:

NÃO quero apenas análise.
NÃO quero apenas documentação.
NÃO quero um gerador C#.
NÃO quero Faker/Bogus.
NÃO quero uma feature permanente para popular dados.

Quero:

- analisar o schema real;
- criar scripts SQL;
- executar os scripts na base de DEVELOPMENT;
- validar os resultados;
- corrigir erros encontrados;
- deixar a aplicação pronta para testes.

---

# PARTE 1 — RESET COMPLETO DA BASE

## 1. REGRA DE SEGURANÇA

Antes de qualquer DELETE:

CONFIRME programaticamente que a base utilizada é a base de DEVELOPMENT.

Se houver qualquer indício de:

- Production;
- Staging;
- banco remoto de produção;
- connection string não reconhecida;

PARE e informe.

Este script é DESTRUTIVO.

---

# 2. O QUE DEVE SER PRESERVADO

Preservar SOMENTE:

## A. Controle de migrations

Toda tabela utilizada pelo FluentMigrator ou mecanismo equivalente para registrar migrations aplicadas.

NUNCA apagar migrations executadas.

## B. Usuário administrador

Preservar o usuário Admin atual.

Mas atenção:

não basta manter apenas a linha da tabela `users`.

Preservar também TODAS as dependências necessárias para que o Admin continue conseguindo:

- autenticar;
- acessar o sistema;
- administrar usuários;
- administrar catálogo;
- administrar IA;
- acessar casos;
- acessar conhecimento;
- utilizar todas as permissões administrativas atuais.

Portanto, preservar também, conforme schema real:

- role Admin;
- permissões necessárias;
- user_roles;
- role_permissions;
- qualquer vínculo de autenticação/autorização obrigatório.

Resolver os IDs REAIS.

NÃO assumir:

```text
AdminId = 1
RoleId = 1
````

Localizar por dados estáveis reais.

---

# 3. TUDO O MAIS DEVE SER ZERADO

Apagar TODOS os dados funcionais existentes, inclusive:

* clientes;
* unidades de clientes;
* produtos/sistemas;
* versões;
* ambientes;
* componentes;
* dependências;
* owners;
* integrações;
* execuções de integrações;
* tecnologias;
* perfis técnicos;
* fontes técnicas;
* domínios permitidos;
* casos;
* sintomas;
* componentes afetados;
* evidências;
* hipóteses;
* sessões diagnósticas;
* passos diagnósticos;
* iterações;
* resoluções;
* relações entre casos;
* root causes;
* knowledge;
* versões de knowledge;
* aplicabilidades;
* validações;
* usos de knowledge;
* conteúdo pesquisável;
* embeddings;
* dados de busca;
* interações de IA;
* fontes de IA;
* feedback de IA;
* providers/configurações de IA;
* credenciais/configurações associadas no banco;
* dados analíticos persistidos;
* toda massa mockup antiga.

Se alguma tabela não puder ser apagada porque é catálogo estrutural realmente obrigatório para o sistema funcionar, analisar antes e justificar.

Mas NÃO preservar dado funcional simplesmente por comodidade.

---

# 4. NÃO APAGAR SCHEMA

NÃO:

* DROP DATABASE;
* DROP TABLE;
* recriar schema do zero;
* apagar migrations;
* editar migrations históricas.

A estrutura permanece.

Somente os DADOS serão resetados.

---

# 5. AUTO_INCREMENT

Depois do reset:

resetar AUTO_INCREMENT das tabelas funcionais quando seguro.

Não fazer isso nas tabelas de migrations.

Para Admin/RBAC preservados, respeitar IDs existentes.

---

# 6. FOREIGN KEYS

Excluir em ordem correta.

Pode usar temporariamente:

```sql
SET FOREIGN_KEY_CHECKS = 0;
```

somente se realmente necessário e dentro de procedimento controlado.

No final:

```sql
SET FOREIGN_KEY_CHECKS = 1;
```

e executar validações de integridade.

---

# 7. SCRIPT DE RESET

Criar:

```text
Docs/sql_mockup/00_reset_development_database.sql
```

ou caminho equivalente consistente com o projeto.

No cabeçalho colocar claramente:

```text
DEVELOPMENT ONLY
DESTRUCTIVE
PRESERVES ADMIN + RBAC + MIGRATIONS
```

---

# PARTE 2 — NOVO UNIVERSO DE TESTES

# 8. CONTEXTO EMPRESARIAL REAL

A massa deve representar uma empresa de software para:

TRANSPORTES E LOGÍSTICA.

O principal produto é um sistema DESKTOP.

---

# 9. PRODUTO PRINCIPAL — DESKTOP

Criar um sistema principal equivalente a:

```text
TMS Desktop
```

Tecnologia:

```text
Delphi
Firebird 5
Cliente-servidor
Windows
```

Arquitetura principal:

```text
Estações Windows
        ↓
Aplicação Delphi Desktop
        ↓
Firebird 5
        ↓
Servidor Windows/Linux conforme cenário
```

O Desktop deve representar o núcleo da operação logística:

* coletas;
* entregas;
* reversas;
* cargas;
* viagens;
* manifestos;
* ocorrências;
* tracking;
* faturamento;
* clientes;
* motoristas;
* veículos;
* documentos;
* integrações;
* processamento operacional.

---

# 10. FIREBIRD 5 COMO COMPONENTE CENTRAL

Cadastrar Firebird 5 adequadamente no contexto técnico.

Explorar casos reais típicos relacionados a:

* conectividade;
* concorrência;
* transações;
* SQL;
* índices;
* performance;
* locks;
* deadlocks;
* arquivos;
* backup;
* permissões;
* rede;
* configuração;
* disponibilidade.

---

# 11. SERVIÇOS DELPHI AUXILIARES

Criar diversos sistemas/serviços de apoio desenvolvidos em Delphi.

Exemplos:

```text
Serviço de Integração SAP
API Tracking
API Operacional
Worker de Ocorrências
Importador de Pedidos
Exportador de Entregas
Processador de Faturamento
Serviço de CTe
Serviço de Sincronização
Agendador de Processamentos
Consumidor de API
Servidor de API
Monitor de Integrações
```

Eles podem ser:

* Windows Services;
* executáveis agendados;
* workers;
* APIs;
* consumidores de API;
* importadores;
* exportadores.

---

# 12. CUSTOMIZAÇÕES POR CLIENTE

Criar entre:

```text
10 e 15 clientes fictícios
```

do ramo de transportes/logística.

Cada cliente pode possuir:

```text
1 a 5 aplicações complementares
```

Exemplo:

```text
Transportadora Atlas

TMS Desktop
Firebird 5

Customizações:
- Atlas Integrador SAP
- Atlas Importador de Pedidos
- Atlas API Tracking
- Atlas Worker de Ocorrências
```

Outro:

```text
Logística Horizonte

TMS Desktop
Firebird 5

Customizações:
- Integração TOTVS
- Serviço CTe
- API Rastreamento
```

Criar nomes fictícios profissionais.

---

# 13. INTEGRAÇÕES

Criar integrações realistas com:

* SAP;
* TOTVS;
* ERPs externos;
* APIs de clientes;
* plataformas de embarcadores;
* serviços fiscais;
* rastreamento;
* geolocalização;
* serviços de documentos;
* APIs REST;
* SOAP quando fizer sentido;
* arquivos CSV/XML/TXT;
* SFTP;
* webhooks.

---

# 14. SOLUÇÃO WEB

Cadastrar solução Web.

Tecnologias:

```text
Angular
Laravel
REST APIs
```

Funções:

* consulta de entregas;
* tracking;
* ocorrências;
* relatórios;
* alterações operacionais permitidas;
* consulta de documentos;
* acompanhamento logístico.

---

# 15. MOBILE

Cadastrar aplicações Flutter.

Cenários:

## App Transportadora

* consultas;
* acompanhamento;
* tracking;
* ocorrências.

## App Motorista

* chegada ao local;
* início de operação;
* confirmação de entrega;
* ocorrência;
* foto;
* comprovante;
* localização;
* sincronização offline/online;
* consumo de APIs.

---

# 16. DEPENDÊNCIAS

Criar grafo realista.

Exemplos:

```text
Flutter
→ API
→ Firebird
```

```text
Angular
→ Laravel
→ API Delphi
→ Firebird
```

```text
SAP
→ API
→ Consumidor Delphi
→ Firebird
```

```text
Desktop Delphi
→ Firebird
```

```text
Worker Delphi
→ API externa
→ Firebird
```

---

# PARTE 3 — MASSA DE CASOS

# 17. QUANTIDADE

Criar NO MÍNIMO:

```text
1.200 CASOS
```

Não apenas 1.000 registros totais.

Quero pelo menos 1.200 ocorrências/casos principais.

Com tabelas-filhas, o conjunto deve resultar em vários milhares de registros.

Meta aproximada:

```text
1.200+ casos
2.500+ sintomas
2.500–4.000 hipóteses
4.000–6.000 passos diagnósticos
2.000+ evidências
900+ resoluções
400+ relações entre casos
150–250 itens de conhecimento
300+ usos de conhecimento
```

Os números podem variar conforme o schema real.

---

# 18. DISTRIBUIÇÃO TEMPORAL

Distribuir os casos pelos últimos:

```text
18 meses
```

Não distribuir uniformemente.

Criar períodos:

* normais;
* aumento de incidentes após release;
* pico após atualização;
* incidentes sazonais;
* clusters recorrentes.

---

# 19. STATUS

Distribuição aproximada:

```text
75% resolvidos
10% em investigação
5% abertos
5% reabertos
5% outros estados válidos
```

Adaptar aos status reais do domínio.

---

# 20. SEVERIDADE

Distribuição aproximada:

```text
Low       15%
Medium    50%
High      28%
Critical   7%
```

Usar somente valores válidos reais.

---

# 21. NÃO CRIAR 1.200 CASOS ÚNICOS

Criar FAMÍLIAS de incidentes recorrentes.

O objetivo é testar:

* similaridade;
* reincidência;
* aprendizado;
* Knowledge;
* Copiloto;
* diagnóstico;
* busca;
* analytics.

Criar aproximadamente:

```text
40 a 60 famílias de problemas
```

Cada família gera múltiplos casos.

---

# PARTE 4 — FAMÍLIAS DELPHI

# 22. ACCESS VIOLATION

Explorar fortemente:

```text
Access violation at address...
Read of address...
Write of address...
```

Causas diferentes:

* objeto não instanciado;
* objeto já liberado;
* ponteiro inválido;
* componente destruído;
* dataset fechado;
* acesso concorrente;
* thread;
* DLL incompatível;
* erro após atualização;
* campo inesperadamente NULL;
* estrutura de dados inválida;
* callback após destruição do objeto.

Variar relatos.

Exemplos:

```text
"Sistema fecha ao clicar em faturar."
```

```text
"Access violation só ocorre em algumas máquinas."
```

```text
"Depois da atualização começou erro ao abrir o manifesto."
```

---

# 23. ERROS SQL

Criar muitos casos de SQL.

Exemplos:

* syntax error;
* column unknown;
* table unknown;
* ambiguous field;
* datatype mismatch;
* conversion error;
* numeric overflow;
* string truncation;
* violation of PRIMARY KEY;
* FOREIGN KEY violation;
* UNIQUE violation;
* NOT NULL;
* aggregate incorreto;
* JOIN errado;
* subquery retornando múltiplas linhas;
* parâmetros inconsistentes;
* SQL incompatível com Firebird 5;
* SQL antigo que mudou após versão;
* consulta dinâmica malformada;
* alias incorreto.

---

# 24. PERFORMANCE SQL

Casos:

* query lenta;
* índice ausente;
* índice inadequado;
* plano ruim;
* SELECT demasiadamente amplo;
* JOIN custoso;
* subquery correlacionada;
* falta de filtro;
* cardinalidade alta;
* consulta repetitiva;
* N+1 originado pela aplicação;
* leitura excessiva.

Soluções:

* índice;
* reescrita;
* filtro;
* alteração de JOIN;
* redução de dataset;
* paginação;
* ajuste de transação;
* revisão de plano.

---

# 25. FIREBIRD — LOCK E DEADLOCK

Criar famílias:

```text
lock conflict on no wait transaction
deadlock
update conflicts
object in use
```

Causas:

* transação longa;
* usuário deixou tela aberta;
* atualização concorrente;
* worker concorrendo com Desktop;
* commit tardio;
* rollback ausente;
* processamento em lote;
* integração atualizando mesmos registros.

---

# 26. TRANSAÇÕES

Casos:

* transação nunca commitada;
* rollback ausente;
* conexão perdida durante transação;
* leitura antiga;
* transação aberta por horas;
* isolamento inadequado;
* concorrência.

---

# 27. CONEXÃO FIREBIRD

Casos:

```text
Unable to complete network request
connection rejected
connection shutdown
unavailable database
```

Causas possíveis:

* serviço Firebird parado;
* rede;
* DNS;
* firewall;
* porta 3050;
* hostname alterado;
* servidor reiniciado;
* VPN;
* arquivo indisponível;
* configuração;
* limite de recursos;
* problema de serviço.

---

# 28. NÃO ATRIBUIR TUDO AO FIREBIRD

Criar casos em que o sintoma aparenta banco mas a causa é:

* rede;
* antivírus;
* Windows;
* versão do executável;
* DLL;
* serviço;
* API;
* SAP;
* regra de negócio;
* arquivo;
* permissão.

Isso é essencial.

---

# 29. DELPHI — DATASETS

Casos:

* dataset fechado;
* campo inexistente;
* FieldByName;
* campo NULL;
* bookmark inválido;
* dataset em edição;
* post falhou;
* refresh incorreto;
* cursor inconsistente.

---

# 30. DELPHI — MEMÓRIA E OBJETOS

Casos:

* memory leak;
* double free;
* invalid pointer;
* AV;
* referência dangling;
* thread acessando objeto destruído.

---

# 31. DLL / BPL / DEPENDÊNCIAS

Casos:

* DLL faltando;
* DLL antiga;
* arquitetura x86/x64;
* versão incompatível;
* arquivo bloqueado pelo antivírus;
* BPL incompatível;
* componente de terceiros.

---

# 32. VERSÃO DO EXECUTÁVEL

Cenários:

* algumas máquinas atualizadas;
* outras não;
* atualização parcial;
* executável em cache;
* atualização falhou;
* versão Desktop incompatível com banco/schema;
* serviço e Desktop em versões divergentes.

---

# PARTE 5 — SERVIÇOS E APIs

# 33. SERVIÇOS WINDOWS

Casos:

* serviço parado;
* serviço travado;
* inicia e para;
* credencial do serviço inválida;
* falta de permissão;
* caminho inválido;
* configuração incorreta;
* porta ocupada;
* timeout;
* serviço executando mas sem processar.

---

# 34. CONSUMIDORES DE API

Casos:

* HTTP 400;
* 401;
* 403;
* 404;
* 409;
* 422;
* 429;
* 500;
* 502;
* 503;
* 504.

Causas:

* token expirado;
* credencial inválida;
* endpoint alterado;
* payload incorreto;
* campo obrigatório;
* timeout;
* rate limit;
* API externa indisponível;
* certificado;
* DNS;
* firewall.

---

# 35. APIs SERVIDORAS

Casos:

* endpoint indisponível;
* timeout;
* exception;
* consulta Firebird lenta;
* autenticação;
* serialização;
* payload inválido;
* concorrência;
* memória;
* serviço parado.

---

# 36. SAP

Criar vários clusters:

* SAP enviou, TMS não recebeu;
* TMS enviou, SAP não confirmou;
* duplicidade;
* documento rejeitado;
* campo obrigatório;
* estrutura JSON/XML mudou;
* timeout;
* autenticação;
* certificado;
* fila acumulada;
* consumidor Delphi parado.

---

# PARTE 6 — WEB

# 37. ANGULAR

Casos:

* tela em branco;
* erro JS;
* chunk antigo em cache;
* versão frontend/backend incompatível;
* CORS;
* timeout;
* API 500;
* erro de autenticação;
* browser cache;
* deploy parcial.

---

# 38. LARAVEL

Casos:

* exception;
* migration incompatível;
* configuração `.env`;
* fila parada;
* cache;
* sessão;
* Redis quando existir;
* banco;
* timeout;
* API externa.

Somente incluir tecnologias realmente cadastradas no produto correspondente.

---

# PARTE 7 — FLUTTER

# 39. MOBILE

Casos:

* app não sincroniza;
* entrega não envia;
* foto não sobe;
* localização indisponível;
* sessão expirada;
* token inválido;
* internet instável;
* operação offline;
* fila local;
* duplicidade após retry;
* versão antiga do app;
* incompatibilidade de API;
* timeout;
* erro 401;
* erro 500.

---

# 40. CADEIAS DE FALHA

Criar casos onde o sintoma aparece em uma camada e a causa em outra.

Exemplo:

```text
Flutter
↓
API timeout
↓
query lenta
↓
Firebird
```

Outro:

```text
Angular
↓
Laravel
↓
API Delphi
↓
Serviço parado
```

Outro:

```text
SAP
↓
Consumidor
↓
Firebird lock
```

---

# PARTE 8 — QUALIDADE DOS RELATOS

# 41. RELATOS DEVEM SER HUMANOS

Não criar só relatos técnicos perfeitos.

Criar exemplos como:

```text
"Não está baixando entrega."
```

```text
"Sistema fecha quando abre o faturamento."
```

```text
"Alguns motoristas conseguem enviar e outros não."
```

```text
"SAP informa que enviou mas não apareceu."
```

```text
"Depois que atualizou ficou muito lento."
```

```text
"Serviço está iniciado mas não processa nada."
```

---

# 42. VARIAÇÃO TEXTUAL

Casos da mesma família devem ter descrições diferentes.

Isso é obrigatório para testar:

* busca textual;
* Copiloto;
* agrupamento;
* similaridade.

---

# 43. AMBIGUIDADE

Criar sintomas iguais com causas diferentes.

Exemplo:

```text
"Timeout"
```

pode ser:

* SQL lento;
* firewall;
* API externa;
* serviço travado;
* DNS;
* SAP;
* rede;
* Firebird.

---

# PARTE 9 — INVESTIGAÇÃO

# 44. HIPÓTESES

Cada caso relevante deve conter:

```text
1 a 5 hipóteses
```

Não necessariamente todas corretas.

Exemplo:

```text
Hipótese 1:
Firebird indisponível

Hipótese 2:
Problema de rede

Hipótese 3:
Serviço parado
```

---

# 45. PASSOS DIAGNÓSTICOS

Criar:

```text
2 a 8 passos
```

nos casos resolvidos.

Exemplos:

```text
Verificar Event Viewer
Testar ping
Testar porta 3050
Validar serviço Firebird
Executar SQL
Verificar plano
Reproduzir erro
Validar versão
Consultar logs
Testar endpoint
Verificar fila
```

---

# 46. TENTATIVAS QUE FALHAM

Nem todo passo deve funcionar.

Registrar ações como:

```text
Reinício do serviço não resolveu
```

```text
Limpeza de cache não alterou comportamento
```

```text
Índice existente estava sendo utilizado
```

Isso é muito importante para memória investigativa.

---

# PARTE 10 — RESOLUÇÕES

# 47. WORKAROUND X DEFINITIVA

Criar ambos.

Exemplo:

```text
Workaround:
reiniciar serviço diariamente
```

Depois:

```text
Definitive:
corrigir conexão não liberada
```

---

# 48. SOLUÇÕES TÍPICAS

Explorar:

* correção Delphi;
* ajuste SQL;
* criação de índice;
* ajuste de transação;
* atualização executável;
* atualização DLL;
* configuração Firebird;
* liberação firewall;
* ajuste DNS;
* reinício serviço;
* atualização token;
* ajuste payload;
* correção API;
* aumento de timeout quando realmente justificado;
* correção de retry;
* limpeza de fila;
* atualização app;
* ajuste de regra de negócio.

---

# 49. NÃO CRIAR SOLUÇÃO MÁGICA

Evitar:

```text
Problema resolvido após ajuste.
```

Descrever:

* causa;
* ação;
* validação;
* resultado.

---

# PARTE 11 — KNOWLEDGE

# 50. CONHECIMENTO

Criar:

```text
150 a 250 KnowledgeItems
```

Não criar um por caso.

Knowledge deve representar conhecimento reutilizável.

Exemplos:

```text
Diagnóstico de Access Violation em módulos Delphi
```

```text
Troubleshooting de Lock Conflict no Firebird
```

```text
Diagnóstico de porta 3050 indisponível
```

```text
Serviço Windows iniciado mas sem processamento
```

```text
Integração SAP com token expirado
```

```text
Flutter com fila offline não sincronizada
```

---

# 51. STATUS DO KNOWLEDGE

Usar:

```text
Draft
Review
Published
Deprecated
Archived
```

conforme domínio real.

Ter quantidade significativa de `Published`.

---

# 52. PROVENIÊNCIA

Quando possível:

Knowledge deve nascer de casos reais da massa.

Relacionar:

```text
Knowledge → Case
```

pela estrutura real.

---

# 53. KNOWLEDGE USAGE

Criar usos reais.

Data de uso deve ser POSTERIOR à publicação.

---

# PARTE 12 — RELAÇÕES ENTRE CASOS

# 54. RELACIONAR CASOS

Criar:

```text
400+ relações
```

usando somente tipos reais suportados.

Priorizar casos da mesma família.

---

# 55. RECORRÊNCIA

Criar clusters como:

```text
Access Violation em faturamento
```

ocorrendo:

* em vários clientes;
* versões diferentes;
* causas iguais;
* ocasionalmente causas diferentes.

---

# 56. CASOS ABERTOS DUPLICADOS

Criar alguns casos onde já existe outro caso semelhante em andamento.

Isso testará a capacidade futura do Copiloto de dizer:

```text
"Já existe uma ocorrência semelhante em investigação."
```

---

# PARTE 13 — COERÊNCIA TEMPORAL

# 57. DATAS

Garantir:

```text
OpenedAt
<
FirstResponseAt
<
Diagnostic steps
<
Resolution
<
ClosedAt
```

quando aplicável.

---

# 58. REABERTURA

Casos reabertos devem usar:

```text
CaseIteration
```

corretamente.

Não sobrescrever a primeira investigação.

---

# PARTE 14 — SEARCH E IA

# 59. SEARCHABLE CONTENT

Depois da carga:

verificar como o projeto atualmente mantém:

```text
searchable_content_entries
```

Se houver processo LOCAL de reindexação:

executar.

NÃO gerar embeddings externos automaticamente.

---

# 60. NÃO CHAMAR LLM

Durante geração da massa:

NÃO chamar:

* DeepSeek;
* OpenAI;
* Anthropic;
* OpenCode;
* qualquer API externa.

Toda massa deve ser criada deterministicamente.

---

# PARTE 15 — SQL

# 61. CRIAR SCRIPTS

Criar:

```text
Docs/sql_mockup/00_reset_development_database.sql
Docs/sql_mockup/01_populate_realistic_test_data.sql
Docs/sql_mockup/02_validate_realistic_test_data.sql
```

Pode substituir os scripts antigos se eles forem exclusivamente massa descartável, mas NÃO sobrescrever sem primeiro analisar o conteúdo atual.

---

# 62. NÃO CRIAR GERADOR C#

PROIBIDO criar:

```text
SyntheticDataGenerator.cs
SeedService.cs
TestDataService.cs
DemoDataService.cs
FakeDataGenerator.cs
```

Nada disso.

---

# 63. DETERMINISMO

Usar:

* CTE;
* temporary tables;
* modulo;
* sequences;
* loops/procedures temporários somente se adequados.

Evitar aleatoriedade não determinística.

Se usar `RAND`, usar seed fixa.

---

# 64. NÃO ASSUMIR IDs

Sempre resolver por:

```text
Code
Name
ExternalReference
```

ou chave funcional apropriada.

---

# 65. CASE NUMBER

Verificar como funciona:

```text
NextCaseNumberAsync
```

A carga NÃO pode quebrar a criação do próximo caso pela aplicação.

Depois da massa:

criar/testar o próximo número esperado.

---

# PARTE 16 — VALIDAÇÃO

# 66. SCRIPT DE VALIDAÇÃO

`02_validate_realistic_test_data.sql` deve apresentar pelo menos:

## Totais

```text
Clients
Products
Components
Dependencies
Integrations
Cases
Symptoms
Hypotheses
DiagnosticSteps
Evidence
Resolutions
CaseRelations
Knowledge
KnowledgeUsage
```

---

# 67. DISTRIBUIÇÃO

Mostrar:

* por cliente;
* por produto;
* por tecnologia;
* por status;
* por severidade;
* por mês;
* por família de problema;
* por causa raiz;
* por tipo de resolução.

---

# 68. DELPHI/FIREBIRD

Mostrar especificamente:

```text
Access Violation
SQL errors
Performance SQL
Lock Conflict
Deadlock
Connection
Transaction
DLL/version issues
```

e quantidade de casos por cluster.

---

# 69. INTEGRIDADE

Validar:

* FK órfãs;
* CaseIteration órfã;
* hypothesis sem case;
* resolution sem case;
* knowledge usage inválido;
* datas impossíveis;
* casos resolvidos sem `ResolvedAt`;
* casos abertos com `ClosedAt`;
* Product/Component incoerente;
* Integration associada a Product inexistente.

Resultado esperado:

```text
0 inconsistências
```

---

# PARTE 17 — EXECUÇÃO

# 70. EXECUTE DE VERDADE

Depois de criar os scripts:

1. executar RESET;
2. validar Admin;
3. executar nova carga;
4. executar validação;
5. corrigir qualquer erro;
6. iniciar TraceCore;
7. verificar aplicação.

NÃO parar dizendo:

```text
"Scripts prontos para execução."
```

Execute-os na base de DEVELOPMENT.

---

# 71. VALIDAR ADMIN APÓS RESET

Antes de popular:

confirmar que o Admin ainda:

* autentica;
* possui role;
* possui permissões.

---

# 72. VALIDAR APLICAÇÃO

Depois da massa, verificar pelo menos:

```text
Dashboard
Cases
Search
Analytics
Knowledge
Catalog
Components
Integrations
Copilot
```

Não precisa testar profundamente cada página, mas garantir que a carga não quebrou a aplicação.

---

# 73. NÃO EXECUTAR CLEANUP

Não limpar novamente depois da carga.

A nova massa deve permanecer disponível para os testes.

---

# 74. RESULTADO ESPERADO

No final, quero uma base contendo um ecossistema coerente de empresa de software para logística, com:

```text
Delphi
Firebird 5
Desktop
Windows Services
APIs Delphi
Consumidores API
SAP
TOTVS
Angular
Laravel
Flutter
```

e pelo menos:

```text
1.200 casos
```

interligados e úteis para:

* busca;
* diagnóstico;
* recorrência;
* Knowledge;
* Analytics;
* Copiloto.

---

# 75. RELATÓRIO FINAL

Ao terminar, informe:

1. quais tabelas foram zeradas;
2. o que foi preservado;
3. confirmação de que Admin permaneceu funcional;
4. quantidade de clientes;
5. quantidade de produtos;
6. quantidade de customizações;
7. quantidade de componentes;
8. quantidade de dependências;
9. quantidade de integrações;
10. quantidade total de casos;
11. distribuição dos casos;
12. quantidade de Access Violations;
13. quantidade de erros SQL;
14. quantidade de incidentes Firebird;
15. quantidade de incidentes API;
16. quantidade de incidentes SAP;
17. quantidade de incidentes Web;
18. quantidade de incidentes Mobile;
19. quantidade de hipóteses;
20. quantidade de passos diagnósticos;
21. quantidade de evidências;
22. quantidade de resoluções;
23. quantidade de relações;
24. quantidade de KnowledgeItems;
25. quantidade de Knowledge Published;
26. quantidade de KnowledgeUsage;
27. resultado das validações de integridade;
28. confirmação de que `NextCaseNumberAsync` continua funcional;
29. resultado do build;
30. resultado dos testes/aplicação.

NÃO responda apenas com plano.

IMPLEMENTE, EXECUTE E VALIDE.
