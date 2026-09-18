# 02 — Requisitos funcionais

## 1. Autenticação, usuários e organização

**FR-001** — Autenticar usuário por mecanismo configurado pela empresa.
**FR-002** — Permitir cadastro, ativação, bloqueio e desativação de usuários.
**FR-003** — Gerenciar departamentos, equipes, cargos funcionais, papéis e permissões.
**FR-004** — Permitir escopo de permissão global, por departamento e por domínio funcional.
**FR-005** — Exibir perfil administrativo com vínculos, papéis, contribuições, atividade auditável e sessões recentes conforme permissão.
**FR-006** — Registrar data do último acesso, falhas de login e eventos de segurança sem expor segredo.

## 2. Catálogo técnico

**FR-020** — Cadastrar clientes/unidades.
**FR-021** — Cadastrar produtos/sistemas e módulos.
**FR-022** — Cadastrar versões/releases.
**FR-023** — Cadastrar componentes técnicos e classificar tipo: Web, Desktop, Mobile, API, Banco, Infraestrutura, Rede, Integração, SAP, Serviço, Job, Outro.
**FR-024** — Cadastrar dependências entre componentes e visualizar grafo.
**FR-025** — Cadastrar tecnologias, bancos, protocolos e serviços externos.
**FR-026** — Definir responsáveis e rotas de escalonamento por componente.

## 3. Casos/incidentes

**FR-040** — Criar caso manualmente ou por integração.
**FR-041** — Preservar relato original e permitir resumo normalizado.
**FR-042** — Informar cliente, produto, ambiente, versão, impacto, severidade e escopo, com possibilidade de campos desconhecidos.
**FR-043** — Adicionar sintomas normalizados e texto livre.
**FR-044** — Anexar evidências: imagem, log, arquivo, link, payload sanitizado e observação.
**FR-045** — Criar hipóteses e registrar evidências pró/contra.
**FR-046** — Registrar sequência de passos de diagnóstico e tentativas.
**FR-047** — Registrar escalonamentos e handoffs entre equipes.
**FR-048** — Relacionar casos.
**FR-049** — Encerrar com resolução, validação, causa raiz confirmada ou não confirmada.
**FR-050** — Reabrir preservando histórico.
**FR-051** — Transformar caso resolvido em proposta de solução/conhecimento.

## 4. Base de conhecimento

**FR-060** — Criar artigo/solução estruturada.
**FR-061** — Suportar conteúdo rico em Markdown, blocos de código, tabelas, imagens e anexos.
**FR-062** — Informar sintomas atendidos, aplicabilidade, pré-condições, diagnóstico, solução, validação, riscos e rollback.
**FR-063** — Versionar conteúdo.
**FR-064** — Fluxo de revisão e aprovação.
**FR-065** — Marcar conteúdo obsoleto, substituído ou arquivado.
**FR-066** — Vincular soluções a casos em que foram usadas.
**FR-067** — Registrar resultado da aplicação de uma solução em um caso.
**FR-068** — Exibir taxa observada de sucesso com quantidade de usos e recorte de contexto.
**FR-069** — Permitir comentários técnicos e sugestões de melhoria com moderação.

## 5. Busca e filtros

**FR-080** — Busca global por texto livre.
**FR-081** — Busca por código/mensagem de erro com tratamento de correspondência exata e parcial.
**FR-082** — Busca em casos, soluções, componentes e documentação, respeitando autorização.
**FR-083** — Filtros combináveis por cliente, produto, versão, componente, ambiente, tecnologia, integração, sintoma, causa, status, data, autor, equipe e severidade.
**FR-084** — Ordenar por relevância, recência, reutilização, taxa observada de sucesso e atualização.
**FR-085** — Exibir “por que este resultado apareceu”.
**FR-086** — Salvar consultas/filtros frequentes.
**FR-087** — Registrar buscas sem clique e buscas sem resultado.
**FR-088** — Sugerir casos relacionados durante edição de um caso.

## 6. Diagnóstico guiado

**FR-100** — Iniciar sessão de diagnóstico a partir de caso ou pesquisa avulsa.
**FR-101** — Apresentar perguntas adaptativas.
**FR-102** — Exibir hipóteses candidatas ranqueadas com evidências e contradições.
**FR-103** — Exibir próximos testes sugeridos e risco operacional.
**FR-104** — Registrar resultado do teste sem sair do fluxo.
**FR-105** — Recalcular ordem de hipóteses após nova evidência.
**FR-106** — Recuperar casos/soluções semelhantes durante o diagnóstico.
**FR-107** — Permitir coleta automática por conectores/telemetria quando disponível.
**FR-108** — Sugerir escalonamento quando atingir condição configurada.

## 7. Analytics

**FR-120** — Dashboard executivo.
**FR-121** — Dashboard operacional.
**FR-122** — Dashboard por departamento/equipe.
**FR-123** — Dashboard de clientes/produtos/componentes.
**FR-124** — Dashboard de conhecimento.
**FR-125** — Dashboard de pesquisa.
**FR-126** — Dashboard de IA/RAG quando habilitado.
**FR-127** — Tela de análise de usuário para gestores autorizados.
**FR-128** — Exportação CSV/XLSX/PDF quando autorizada.
**FR-129** — Drill-down de métricas.
**FR-130** — Períodos comparativos e filtros persistentes.

## 8. Auditoria e administração

**FR-140** — Pesquisar trilha de auditoria.
**FR-141** — Filtrar por ator, entidade, ação, período e correlação.
**FR-142** — Visualizar alterações de campos quando permitido.
**FR-143** — Configurar taxonomias, status, severidades e parâmetros não estruturais.
**FR-144** — Configurar revisão periódica de conhecimento.
**FR-145** — Configurar limites de upload, retenção e políticas.
**FR-146** — Exibir saúde dos jobs, indexação e integrações.

## 9. IA/RAG

**FR-160** — Assistente de pesquisa com pergunta em linguagem natural.
**FR-161** — Recuperar fontes internas relevantes antes da geração.
**FR-162** — Responder com citações internas clicáveis.
**FR-163** — Permitir restringir pergunta por produto, cliente, versão, componente e período.
**FR-164** — Gerar resumo de caso preservando fatos e separando inferências.
**FR-165** — Sugerir sintomas/tags a partir do relato, exigindo confirmação quando impactarem classificação.
**FR-166** — Sugerir casos relacionados.
**FR-167** — Sugerir rascunho de artigo ao encerrar caso, sem publicar automaticamente.
**FR-168** — Registrar feedback sobre resposta.
**FR-169** — Registrar modelo, prompt versionado, fontes recuperadas e identificador de correlação para auditoria técnica.

