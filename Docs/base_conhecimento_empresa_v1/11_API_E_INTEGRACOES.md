# 11 — API e integrações

## 1. Objetivo

A plataforma deve possuir contratos claros para permitir integração com sistemas de chamados, monitoramento, produtos web/desktop/mobile, SAP, APIs corporativas e futuras automações.

## 2. Convenções REST

Base: `/api/v1`.

Recursos iniciais:

```text
/api/v1/cases
/api/v1/cases/{id}
/api/v1/cases/{id}/symptoms
/api/v1/cases/{id}/hypotheses
/api/v1/cases/{id}/diagnostic-sessions
/api/v1/cases/{id}/relations
/api/v1/cases/{id}/resolve
/api/v1/knowledge
/api/v1/knowledge/{id}/versions
/api/v1/knowledge/{id}/publish
/api/v1/search
/api/v1/diagnostics
/api/v1/catalog/components
/api/v1/catalog/dependencies
/api/v1/analytics/...
/api/v1/admin/...
/api/v1/ai/query
```

## 3. Erros

Usar `application/problem+json` e Problem Details.

Campos adicionais:
- `traceId`;
- `errorCode` interno estável;
- `validationErrors` quando aplicável.

Nunca retornar stack trace em produção para cliente não autorizado.

## 4. Paginação

Para listas administrativas simples: paginação por página pode ser aceita. Para timeline/auditoria/alto volume, preferir cursor.

Resposta deve trazer:
- itens;
- cursor/metadata;
- total somente quando custo for aceitável.

## 5. Idempotência

Integrações que criam casos/eventos devem aceitar `Idempotency-Key` ou identificador externo único, evitando duplicidade por retry.

## 6. Webhooks

Se necessário:
- assinatura HMAC ou mecanismo equivalente;
- retry;
- idempotência;
- timestamp anti-replay;
- dead-letter/registro de falhas.

Eventos candidatos:
- `case.created`;
- `case.escalated`;
- `case.resolved`;
- `knowledge.published`;
- `knowledge.deprecated`;
- `critical.incident.detected`.

## 7. Integração com chamados

A integração deve mapear:
- ID externo;
- cliente;
- solicitante;
- descrição original;
- anexos permitidos;
- prioridade;
- timestamps;
- status;
- comentários relevantes.

Não duplicar o sistema de chamados se ele já possui workflow corporativo. A plataforma pode ser a camada de diagnóstico/conhecimento e sincronizar estado mínimo.

## 8. Telemetria dos sistemas suportados

Criar contrato de coleta, não acoplamento por produto.

Exemplos de probes:
- health de API;
- versão do cliente desktop;
- conectividade;
- latência;
- status de job;
- disponibilidade de integração;
- conexão com banco;
- status de fila;
- versão de schema/configuração.

Cada probe deve retornar:
- nome;
- timestamp;
- status;
- valor sanitizado;
- evidência;
- validade/TTL;
- origem.

## 9. SAP e sistemas externos

Não permitir que o domínio dependa diretamente de SDK SAP. Criar adapters no módulo `Integrations`.

Exemplo:
```csharp
public interface IErpHealthService
{
    Task<IntegrationHealthResult> CheckAsync(...);
}
```

## 10. Versionamento

Mudança breaking cria nova versão de API. Mudanças aditivas preservam compatibilidade. DTO externo não deve ser a própria entidade de domínio.

