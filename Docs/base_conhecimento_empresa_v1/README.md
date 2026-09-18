# Plataforma Corporativa de Conhecimento, Diagnóstico e Lições Aprendidas

**Versão da documentação:** 1.0  
**Data-base:** 17/09/2026  
**Status:** baseline para início de desenvolvimento  
**Stack mandatória:** .NET/C# end-to-end + MySQL

## Propósito

Esta documentação define uma plataforma interna para transformar problemas resolvidos em conhecimento reaproveitável, orientar a investigação de novos incidentes, reduzir tentativa e erro, encurtar o tempo de resolução e fornecer informações gerenciais confiáveis.

A ideia central é simples:

> **Cada problema resolvido deve tornar o próximo problema semelhante mais fácil, rápido e previsível de resolver.**

A plataforma não deve ser apenas uma wiki. Ela deve registrar o caminho percorrido: sintomas, contexto, hipóteses, verificações, evidências, tentativas que falharam, tentativas que funcionaram, causa raiz, solução, validação e relacionamentos com casos semelhantes.

## Como usar este pacote

A documentação foi separada para servir simultaneamente a quatro públicos:

1. **Produto e gestão:** entendem escopo, regras, indicadores e governança.
2. **Equipe técnica:** recebe arquitetura, modelo de dados, APIs, segurança, observabilidade, testes e DevOps.
3. **Usuários finais:** recebem manuais operacionais e de pesquisa/diagnóstico.
4. **IA desenvolvedora:** recebe ordem de leitura, restrições, critérios de aceite e regras de implementação.

## Ordem de leitura recomendada

1. `00_VISAO_GERAL_E_PRINCIPIOS.md`
2. `01_REGRAS_DE_NEGOCIO.md`
3. `02_REQUISITOS_FUNCIONAIS.md`
4. `03_MODULOS_DO_SISTEMA.md`
5. `04_MODELO_DE_CONHECIMENTO_E_CASOS.md`
6. `05_MOTOR_DE_DIAGNOSTICO.md`
7. `06_PESQUISA_FILTROS_E_SIMILARIDADE.md`
8. `07_ANALYTICS_E_GESTAO.md`
9. `08_RAG_E_IA.md`
10. `09_ARQUITETURA_TECNICA_DOTNET_MYSQL.md`
11. `10_MODELO_DE_DADOS.md`
12. `11_API_E_INTEGRACOES.md`
13. `12_SEGURANCA_AUDITORIA_E_LGPD.md`
14. `13_OBSERVABILIDADE_OPERACAO_E_DEVOPS.md`
15. `14_TESTES_E_QUALIDADE.md`
16. `15_ROADMAP_DE_IMPLEMENTACAO.md`
17. `16_GUIA_DA_IA_DESENVOLVEDORA.md`
18. `17_MANUAL_DO_USUARIO.md`
19. `18_MANUAL_DE_GERENCIAMENTO.md`
20. `19_CRITERIOS_DE_ACEITE_E_RASTREABILIDADE.md`
21. `20_BACKLOG_INICIAL.md`

## Hierarquia de autoridade dos documentos

Em caso de conflito, prevalece a seguinte ordem:

1. Regras de negócio.
2. Critérios de aceite aprovados.
3. Requisitos funcionais.
4. Decisões arquiteturais registradas em ADR.
5. Arquitetura técnica.
6. Modelo de dados/API.
7. Manuais.
8. Exemplos e templates.

Nenhuma IA ou desenvolvedor deve alterar uma regra de negócio apenas para simplificar a implementação.

## Decisões já fechadas

- Código de aplicação: **C#**.
- Plataforma: **.NET 10 LTS / ASP.NET Core 10**.
- Interface web: **ASP.NET Core Razor Pages** (revisão de ADR-0004 em 2026; Blazor Web App foi a baseline original mas não é o que está implementado — ver `21_ADRS_E_DECISOES_ABERTAS.md`).
- Banco principal e fonte de verdade: **MySQL 8.4 LTS ou superior da linha LTS homologada**.
- Arquitetura inicial: **monólito modular**, não microserviços.
- Integrações por APIs bem definidas e contratos versionados.
- IA/RAG entra como camada de apoio; **não substitui a regra de negócio nem o banco transacional**.
- O sistema deve partir do **sintoma**, e não exigir que o usuário saiba qual departamento é responsável.
- Toda ação relevante deve ser auditável.
- Conhecimento sugerido pela IA nunca vira conhecimento oficial automaticamente.

## Decisões conscientemente deixadas abertas

Devem ser resolvidas por ADR ou spike técnico antes da respectiva etapa:

- provedor de identidade corporativa: autenticação local, Entra ID/AD/OIDC ou combinação;
- armazenamento de anexos: filesystem corporativo, S3 compatível, Azure Blob ou equivalente;
- mecanismo vetorial futuro do RAG, caso o MySQL isoladamente não atenda ao volume/latência;
- integração com sistema de chamados existente;
- origem e padrão de telemetria dos produtos suportados;
- retenção exata de logs e trilhas de auditoria por política interna.

## Estrutura do pacote

- `database/`: esquema inicial e notas de persistência.
- `api/`: convenções e esqueleto OpenAPI.
- `diagrams/`: diagramas Mermaid editáveis.
- `templates/`: templates obrigatórios para casos, soluções, post-mortem, ADR e histórias.

## Nota sobre versões técnicas

A documentação fixa .NET 10 LTS como baseline. Em 17/09/2026, .NET 10 é a versão LTS ativa. A implementação deve manter patches de segurança atualizados sem alterar a versão major sem ADR.


## Documentos complementares

- `23_REQUISITOS_NAO_FUNCIONAIS.md` — segurança, desempenho, resiliência, acessibilidade e manutenção.
- `24_UX_E_NAVEGACAO.md` — mapa de navegação e comportamento das telas.
- `25_MATRIZ_PERFIS_PERMISSOES.md` — baseline de perfis e capacidades.
- `26_CATALOGO_DE_KPIS.md` — definições iniciais de indicadores.
- `99_REFERENCIAS_TECNICAS.md` — referências oficiais consultadas.
- `27_CENARIOS_E_FLUXOS_DE_REFERENCIA.md` — jornadas transversais que demonstram como o sistema deve investigar sem partir de departamentos.
