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

