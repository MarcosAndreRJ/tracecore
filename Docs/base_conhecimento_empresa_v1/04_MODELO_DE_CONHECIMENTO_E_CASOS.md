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

