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
- hipótese concentrada em componente de outra equipe;
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

