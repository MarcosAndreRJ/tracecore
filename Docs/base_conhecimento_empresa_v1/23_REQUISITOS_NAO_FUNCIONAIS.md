# 23 — Requisitos não funcionais

## NFR-001 — Disponibilidade
A meta numérica deve ser definida após conhecer infraestrutura e criticidade. A arquitetura deve evitar pontos únicos desnecessários e possuir health checks, backup e recuperação testados.

## NFR-002 — Desempenho
Busca, abertura de caso e navegação devem ser percebidas como interativas. Metas p95 serão definidas a partir de baseline de homologação, não por número arbitrário. Queries críticas devem ser instrumentadas.

## NFR-003 — Escalabilidade
A aplicação deve escalar horizontalmente se necessário sem depender de sessão local não compartilhável para estado de negócio. Estado persistente permanece em serviços apropriados.

## NFR-004 — Segurança
OWASP ASVS/boas práticas ASP.NET Core como referência; autenticação forte, autorização server-side, segredo fora do código, TLS e proteção de uploads.

## NFR-005 — Auditabilidade
Eventos críticos devem ser rastreáveis ponta a ponta com correlação.

## NFR-006 — Manutenibilidade
Monólito modular, dependências direcionais, testes, ADRs e migrations. Evitar framework próprio desnecessário.

## NFR-007 — Acessibilidade
Interface web deve buscar conformidade WCAG 2.2 AA nas jornadas principais: navegação por teclado, foco, contraste, rótulos, mensagens de erro e sem dependência exclusiva de cor.

## NFR-008 — Internacionalização
Baseline em pt-BR. Strings de UI não devem ser espalhadas no domínio; preparar mecanismo de recursos caso a empresa necessite outros idiomas.

## NFR-009 — Compatibilidade
Suportar navegadores corporativos homologados e layout responsivo. A plataforma de conhecimento não exige app mobile nativo no MVP.

## NFR-010 — Resiliência
Integrações externas devem usar timeout, retry apenas quando seguro, circuit breaker quando justificado, idempotência e degradação controlada.

## NFR-011 — Consistência
Operações críticas de negócio devem ser transacionais no MySQL. Processamento assíncrono usa outbox e consistência eventual explícita.

## NFR-012 — Privacidade
Coletar apenas dados necessários para finalidade operacional/gerencial; políticas de retenção e acesso devem ser configuráveis.

## NFR-013 — Recuperação
Backup sem restore testado não é considerado estratégia válida. Definir RPO/RTO antes de produção.

## NFR-014 — Portabilidade de IA
Troca de LLM/embedding não deve exigir mudança do domínio ou do modelo transacional.

## NFR-015 — Observabilidade
Toda jornada crítica deve produzir traces/métricas/logs suficientes para diagnosticar falhas da própria plataforma.

