# 24 — UX, navegação e desenho de telas

## 1. Princípio de UX

A plataforma é uma ferramenta de investigação. A UI deve reduzir carga cognitiva e manter contexto. Evitar formulários enormes e dashboards cheios de cards sem hierarquia.

## 2. Navegação principal sugerida

```text
Início
Pesquisar
Casos
Conhecimento
Diagnóstico
Analytics
Catálogo Técnico
Administração  [somente autorizados]
```

## 3. Início

Blocos:
- busca central “Descreva o problema…”;
- abrir novo caso;
- meus casos/em andamento;
- casos críticos da equipe;
- conhecimento a revisar;
- consultas recentes;
- alertas do sistema.

## 4. Pesquisa

Layout desktop:
- topo: caixa de busca;
- esquerda: filtros;
- centro: resultados;
- direita opcional: contexto/preview;
- chips de filtros ativos sempre visíveis;
- botão claro para remover filtros automáticos.

Resultado mostra:
- título;
- tipo;
- resumo/snippet;
- compatibilidade;
- status;
- última validação;
- fatores de correspondência;
- uso/sucesso com amostra quando aplicável.

## 5. Caso

Cabeçalho fixo com número, status, severidade, cliente, produto e owner.

Abas/áreas:
- Visão geral;
- Diagnóstico;
- Timeline;
- Evidências;
- Relacionados;
- Resolução;
- Auditoria (autorizados).

A timeline deve distinguir visualmente observação, teste, hipótese, handoff, ação e resolução, sem depender apenas de cor.

## 6. Diagnóstico

Tela dividida:
- contexto do caso;
- hipóteses priorizadas;
- próxima verificação sugerida;
- histórico do que já foi testado;
- casos/soluções semelhantes.

Cada verificação deve ter botões rápidos de resultado e campo para evidência.

## 7. Conhecimento

Visualização do artigo:
- status e revisão;
- aplicabilidade;
- sumário lateral;
- conteúdo;
- riscos/rollback destacados;
- casos que validaram;
- histórico de versões;
- feedback;
- ação “usar neste caso”.

## 8. Analytics

Padrões:
- filtros globais no topo;
- período sempre explícito;
- definição do KPI acessível;
- drill-down por clique;
- estado “sem dados” diferente de zero;
- exportação conforme permissão.

## 9. Administração

Agrupar por domínio:
- Pessoas e acesso;
- Organização;
- Catálogo técnico;
- Conhecimento;
- Integrações;
- IA;
- Auditoria;
- Configurações.

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

