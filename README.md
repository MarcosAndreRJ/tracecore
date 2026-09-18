# TraceCore

**TraceCore** é uma plataforma corporativa de conhecimento, investigação e diagnóstico técnico criada para transformar problemas resolvidos em conhecimento reutilizável.

O sistema centraliza casos, hipóteses, evidências, soluções, contexto técnico e relações entre incidentes para ajudar equipes técnicas a investigar problemas com mais contexto, menos tentativa e erro e maior rastreabilidade.

> **Objetivo:** registrar o que aconteceu, como foi investigado, quais evidências influenciaram cada hipótese, qual solução funcionou e como esse conhecimento pode orientar ocorrências futuras.

---

## Estado do projeto

O desenvolvimento está implementado até a:

**Fase 9 — Diagnóstico Guiado**

incluindo a etapa de consolidação:

**Fase 7.A — Saneamento e Consolidação Estrutural**

O TraceCore já possui uma base funcional relevante para continuar a evolução dos módulos operacionais, gerenciais e de inteligência.

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