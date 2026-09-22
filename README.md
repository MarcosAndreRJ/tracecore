# TraceCore

**TraceCore** é uma plataforma corporativa de conhecimento, investigação e diagnóstico técnico criada para transformar problemas resolvidos em conhecimento reutilizável.

O sistema centraliza casos, hipóteses, evidências, soluções, contexto técnico e relações entre incidentes para ajudar equipes técnicas a investigar problemas com mais contexto, menos tentativa e erro e maior rastreabilidade.

> **Objetivo:** registrar o que aconteceu, como foi investigado, quais evidências influenciaram cada hipótese, qual solução funcionou e como esse conhecimento pode orientar ocorrências futuras.

---

## Estado do projeto

O desenvolvimento está implementado até a:

**Fase 17 — Arquitetura de Provedores IA Desacoplados**

São **29 migrations FluentMigrator** (`M20260917_01` a `M20260922_29`) e **105 testes de integração aprovados** em `tests/TraceCore.IntegrationTests` (MySQL real via Testcontainers). As fases 0–9 estão entregues; a Fase 10 (Copiloto de diagnóstico completo) está parcial.

Módulos já implementados (M01–M12): Identidade e Acesso, Estrutura Organizacional (Departamentos), Catálogo Técnico, Casos e Incidentes, Base de Conhecimento, Pesquisa/Similaridade, Diagnóstico Guiado, Analytics e Inteligência Gerencial, Auditoria, Integrações (health-checks HTTP/TCP), IA/Preparação Estrutural de Dados e Copiloto Operacional (RAG grounded + tool calling).

Detalhamento conforme implementação em `Docs/MASTER_SPECIFICATION.md` e no pacote `Docs/base_conhecimento_empresa_v1/`.

---

## Funcionalidades implementadas

### Identidade e acesso

- autenticação;
- usuários;
- departamentos;
- perfis;
- permissões granulares;
- administrador do sistema;
- auditoria de operações.

O papel **Admin** representa o administrador raiz do TraceCore.

Departamentos representam contexto e responsabilidade, mas não funcionam como silos de conhecimento por padrão.

---

## Clientes

O TraceCore possui um cadastro operacional de clientes, sem tentar substituir o CRM da empresa.

São suportados:

- cliente;
- identificação externa no CRM;
- unidades;
- status;
- observações operacionais;
- contexto técnico histórico.

O contexto técnico permite relacionar um cliente a:

- produto/sistema;
- versão;
- ambiente;
- unidade.

Isso permite preservar, por exemplo, qual versão o cliente utilizava quando determinado incidente ocorreu.

---

## Catálogo técnico

O catálogo representa o ecossistema suportado pela empresa.

Atualmente estão modelados:

- produtos/sistemas;
- versões;
- ambientes;
- componentes;
- sistemas externos;
- dependências entre componentes;
- departamentos responsáveis.

Exemplo:

```text
Desktop
   ↓
API
   ↓
Serviço de autenticação
   ↓
Banco de dados
   ↓
Sistema externo