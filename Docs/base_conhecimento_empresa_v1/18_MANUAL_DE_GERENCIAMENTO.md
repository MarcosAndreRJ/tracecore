# 18 — Manual dos módulos de gerenciamento

## 1. Objetivo

Este manual é destinado a gestores, administradores funcionais e responsáveis por governança da plataforma.

## 2. Gestão de usuários

### Criar/ativar
1. Acesse **Gestão > Usuários** (`/Users/Index`).
2. Crie ou sincronize identidade conforme configuração.
3. Vincule departamentos (conceito organizacional oficial).
4. Atribua papéis mínimos necessários.
5. Revise permissões efetivas.
6. Salve.

### Desativar
Desativação bloqueia novo acesso, mas preserva histórico e autoria.

### Tela detalhada
Deve permitir visualizar, conforme autorização:
- dados cadastrais;
- vínculos;
- papéis/permissões efetivas;
- casos em participação;
- conhecimento criado/revisado;
- reutilização de suas soluções;
- ações administrativas;
- eventos de segurança;
- sessões recentes;
- indicadores operacionais contextualizados.

## 3. Departamentos

A estrutura organizacional oficial é o **Departamento** (`/Departments/Index`); a duplicidade `teams`/`user_teams` foi extinta (Migration 10). Cadastrar estrutura e responsáveis. Não usar departamento como único dono de componente quando houver responsabilidade compartilhada (`component_owners` admite papéis Primário, Secundário e Escalonamento).

Gestor pode analisar:
- backlog;
- entrada/saída;
- tempo por etapa;
- handoffs;
- causas/componentes;
- conhecimento produzido/reutilizado;
- lacunas.

## 4. Gestão do catálogo técnico

Mudanças em componente/dependência podem alterar recomendações do diagnóstico. Portanto:
- revisar impacto;
- registrar responsável;
- manter histórico de versões quando necessário;
- não excluir componente que possui casos históricos.

## 5. Gestão de soluções

### Fila de revisão
Mostrar:
- novos rascunhos;
- alterações pendentes;
- revisão vencida;
- conteúdo com feedback negativo;
- conteúdo com alta utilização e baixa taxa de sucesso;
- itens sem proprietário.

### Aprovação
O revisor deve checar:
- aplicabilidade;
- versão;
- risco;
- rollback;
- validação;
- linguagem;
- dados sensíveis;
- referências/casos.

## 6. Analytics

Todo dashboard deve possuir filtros globais e drill-down. Ao comparar períodos, confirme se filtros e definições de KPI são iguais.

### Perguntas gerenciais que o sistema deve responder
- O que mais está quebrando?
- Onde gastamos mais tempo?
- Quais problemas voltam?
- Que componentes geram mais handoffs?
- Onde falta documentação?
- Quais soluções são mais reutilizadas?
- Quais soluções estão falhando?
- Que busca as pessoas fazem e não encontram resposta?
- Quanto tempo é reduzido quando existe conhecimento reutilizável?
- Quais conteúdos estão vencidos?

## 7. Auditoria

Use auditoria para investigação operacional, segurança e governança. Filtros:
- período;
- usuário;
- ação;
- entidade;
- correlação;
- módulo.

Alteração de permissão e publicação de conhecimento devem ser facilmente rastreáveis.

## 8. Configurações

Administráveis sem deploy, quando seguro:
- taxonomias;
- severidades;
- categorias de causa;
- prazos de revisão;
- feature flags permitidas;
- limites de upload;
- parâmetros de ranking não críticos;
- sinônimos;
- templates.

Mudança estrutural ou de regra de negócio não deve ser escondida em configuração sem governança.

## 9. IA

Painel de IA (`/Settings/LlmProviders/Index`) deve permitir:
- habilitar/desabilitar por ambiente;
- selecionar provider/modelo aprovado (protocolos `OpenAICompatible`/`AnthropicMessages`);
- buscar modelos disponíveis na API do provedor (catálogo dinâmico via `ILlmModelCatalog`);
- ver consumo;
- ver falhas;
- ver versão de prompt;
- avaliar qualidade (`/ContentQuality/Index`);
- reindexar conteúdo autorizado;
- controlar feature flags.

Nunca incluir botão “publicar automaticamente tudo que a IA gerar”.

