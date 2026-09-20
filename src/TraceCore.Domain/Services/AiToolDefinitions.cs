using System.Collections.Generic;

namespace TraceCore.Domain.Services;

/// <summary>
/// Fase 14: Definições de ferramentas disponíveis para o Copiloto Operacional.
/// Cada ferramenta corresponde a uma operação do domínio (leitura ou escrita).
/// Ferramentas de LEITURA executam diretamente; ferramentas de ESCRITA geram proposta
/// que exige confirmação humana explícita antes da execução (§12 do documento de visão).
/// </summary>
public static class AiToolDefinitions
{
    /// <summary>
    /// Ferramentas de LEITURA (baixo risco, executam sem confirmação).
    /// </summary>
    public static readonly IReadOnlyList<LlmToolDefinition> ReadingTools = new[]
    {
        new LlmToolDefinition(
            "SearchCases",
            "Busca casos por termo e filtros estruturados (cliente, produto, versão, componente, ambiente, código de erro, status). Use filtros o quanto antes: comece por cliente+produto (Camada 1), amplie para produto em outros clientes (Camada 2), depois componente/versão/ambiente (Camada 3) ou erro exato (Camada 4) quando a camada anterior não trouxer evidência suficiente. Retorna lista resumida — para detalhes completos de um caso específico, use GetCaseDetails.",
            @"{
  ""type"": ""object"",
  ""properties"": {
    ""query"": { ""type"": ""string"", ""description"": ""Termo de busca livre (relato, resumo, mensagem de erro)"" },
    ""clientId"": { ""type"": ""integer"", ""description"": ""Filtrar por ID do cliente (Camada 1)"" },
    ""productId"": { ""type"": ""integer"", ""description"": ""Filtrar por ID do produto/sistema (Camadas 1-2)"" },
    ""productVersionId"": { ""type"": ""integer"", ""description"": ""Filtrar por ID da versão do produto (Camada 3)"" },
    ""componentId"": { ""type"": ""integer"", ""description"": ""Filtrar por ID do componente técnico (Camada 3)"" },
    ""errorCode"": { ""type"": ""string"", ""description"": ""Código de erro exato (ex: HTTP 504, ORA-12541) (Camada 4)"" },
    ""status"": { ""type"": ""array"", ""items"": { ""type"": ""string"" }, ""description"": ""Filtrar por status (ex: Open, Investigating, Resolved). Não se limite a Resolved — casos em andamento também são relevantes."" },
    ""resolvedOnly"": { ""type"": ""boolean"", ""default"": false, ""description"": ""true para considerar apenas casos já resolvidos"" },
    ""page"": { ""type"": ""integer"", ""default"": 1, ""description"": ""Página (1-based)"" },
    ""pageSize"": { ""type"": ""integer"", ""default"": 20, ""description"": ""Itens por página (máximo 20 por chamada)"" }
  },
  ""required"": []
}"
        ),
        new LlmToolDefinition(
            "SearchKnowledge",
            "Busca artigos da base de conhecimento por termo e filtros. Retorna lista resumida.",
            @"{
  ""type"": ""object"",
  ""properties"": {
    ""query"": { ""type"": ""string"", ""description"": ""Termo de busca livre (titulo, conteudo, tags)"" },
    ""categoryId"": { ""type"": ""integer"", ""description"": ""Filtrar por categoria"" },
    ""visibility"": { ""type"": ""array"", ""items"": { ""type"": ""string"" }, ""description"": ""Filtrar por visibilidade (Public, Internal)"" },
    ""page"": { ""type"": ""integer"", ""default"": 1 },
    ""pageSize"": { ""type"": ""integer"", ""default"": 20 }
  },
  ""required"": []
}"
        ),
        new LlmToolDefinition(
            "GetCaseDetails",
            "Obtém detalhes completos de um caso pelo ID (inclui investigação, evidências, hipóteses).",
            @"{
  ""type"": ""object"",
  ""properties"": {
    ""caseId"": { ""type"": ""integer"", ""description"": ""ID do caso"" }
  },
  ""required"": [""caseId""]
}"
        ),
        new LlmToolDefinition(
            "GetKnowledgeDetails",
            "Obtém detalhes completos de um artigo da base de conhecimento pelo ID.",
            @"{
  ""type"": ""object"",
  ""properties"": {
    ""knowledgeId"": { ""type"": ""integer"", ""description"": ""ID do artigo"" }
  },
  ""required"": [""knowledgeId""]
}"
        ),
        new LlmToolDefinition(
            "GetComponentDependencies",
            "Obtém dependências de um componente do catálogo (upstream/downstream).",
            @"{
  ""type"": ""object"",
  ""properties"": {
    ""componentId"": { ""type"": ""integer"", ""description"": ""ID do componente"" },
    ""direction"": { ""type"": ""string"", ""enum"": [""upstream"", ""downstream"", ""both""], ""default"": ""both"", ""description"": ""Direção das dependências"" }
  },
  ""required"": [""componentId""]
}"
        ),
        new LlmToolDefinition(
            "GetProductContext",
            "Obtém o contexto técnico consolidado de um produto/sistema: finalidade, arquitetura, tecnologias, componentes, dependências, integrações e fontes técnicas cadastradas. Use quando o histórico interno não for suficiente e for necessário raciocinar sobre a arquitetura da aplicação para formular hipóteses. Não invente tecnologia/dependência que não apareça no resultado.",
            @"{
  ""type"": ""object"",
  ""properties"": {
    ""productId"": { ""type"": ""integer"", ""description"": ""ID do produto"" },
    ""productVersionId"": { ""type"": ""integer"", ""description"": ""ID da versão do produto, se conhecida"" }
  },
  ""required"": [""productId""]
}"
        ),
        new LlmToolDefinition(
            "GetRelatedCases",
            "Obtém casos semelhantes a um caso específico já existente, com o score determinístico e os fatores de similaridade calculados pelo sistema (produto, componente, versão, erro, texto). Use quando já houver um caso candidato claro e quiser expandir para casos relacionados a ele.",
            @"{
  ""type"": ""object"",
  ""properties"": {
    ""caseId"": { ""type"": ""integer"", ""description"": ""ID do caso de referência"" }
  },
  ""required"": [""caseId""]
}"
        ),
        new LlmToolDefinition(
            "SearchExternalSources",
            "Pesquisa documentação/fontes externas (fora do TraceCore) sobre uma tecnologia, erro ou tópico técnico. Use SOMENTE depois de esgotar histórico interno, conhecimento validado e contexto técnico, e apenas quando agregar valor real (erro desconhecido, tecnologia externa, versão recente, comportamento novo). O backend decide se a pesquisa é permitida conforme a política cadastrada para o produto — você não escolhe a política, só informa productId e o termo de busca. Nunca inclua nome de cliente, credenciais, IPs internos ou outro dado sensível na query.",
            @"{
  ""type"": ""object"",
  ""properties"": {
    ""productId"": { ""type"": ""integer"", ""description"": ""ID do produto — usado para verificar a política de pesquisa externa cadastrada"" },
    ""query"": { ""type"": ""string"", ""description"": ""Termo de busca técnico, sem dados sensíveis ou do cliente (ex.: 'MySQL 8.4 connection timeout after upgrade')"" },
    ""maxResults"": { ""type"": ""integer"", ""default"": 5, ""description"": ""Máximo de resultados (até 5)"" }
  },
  ""required"": [""productId"", ""query""]
}"
        ),
        new LlmToolDefinition(
            "AnalyzeManagementTrend",
            "Consulta indicadores e análises gerenciais determinísticas do TraceCore (tendência antes vs. depois de versão, associação de componentes a causas, efetividade de soluções comparando mediana de MTTR). Retorna números reais e tamanho de amostra calculados pelo sistema.",
            @"{
  ""type"": ""object"",
  ""properties"": {
    ""metricType"": {
      ""type"": ""string"",
      ""enum"": [""TrendAfterVersion"", ""ComponentAssociation"", ""SolutionEffectiveness""],
      ""description"": ""Tipo de métrica solicitada""
    },
    ""productVersionId"": { ""type"": ""integer"", ""description"": ""ID da versão do produto (para TrendAfterVersion)"" },
    ""rootCauseId"": { ""type"": ""integer"", ""description"": ""ID da causa raiz (para TrendAfterVersion ou ComponentAssociation)"" },
    ""componentId"": { ""type"": ""integer"", ""description"": ""ID do componente técnico"" },
    ""errorCode"": { ""type"": ""string"", ""description"": ""Código de erro técnico (ex: ORA-12541, HTTP 500)"" },
    ""knowledgeItemId"": { ""type"": ""integer"", ""description"": ""ID da solução/artigo (para SolutionEffectiveness)"" },
    ""period"": { ""type"": ""string"", ""description"": ""Período de filtro (ex: 30d, 90d, 12m, all)"" }
  },
  ""required"": [""metricType""]
}"
        )
    };

    /// <summary>
    /// Ferramentas de ESCRITA (alto risco, exigem proposta + confirmação humana).
    /// Cada ferramenta de escrita tem seu par "Propose*" (retornado pelo modelo) e "*" (executado após confirmação).
    /// </summary>
    public static readonly IReadOnlyList<LlmToolDefinition> WritingTools = new[]
    {
        new LlmToolDefinition(
            "ProposeCaseCreation",
            "Propõe a criação de um novo caso. O modelo preenche os campos equivalentes a OpenCaseCommand. Requer confirmação humana antes de executar.",
            @"{
  ""type"": ""object"",
  ""properties"": {
    ""clientId"": { ""type"": ""integer"", ""description"": ""ID do cliente (obrigatório)"" },
    ""productId"": { ""type"": ""integer"", ""description"": ""ID do produto (obrigatório)"" },
    ""versionId"": { ""type"": ""integer"", ""description"": ""ID da versão (obrigatório)"" },
    ""environmentId"": { ""type"": ""integer"", ""description"": ""ID do ambiente (obrigatório)"" },
    ""errorCode"": { ""type"": ""string"", ""description"": ""Código de erro relatado (ex: HTTP 502)"" },
    ""title"": { ""type"": ""string"", ""description"": ""Título resumido do caso"" },
    ""description"": { ""type"": ""string"", ""description"": ""Relato original detalhado do cliente"" },
    ""priority"": { ""type"": ""string"", ""enum"": [""Critica"", ""Alta"", ""Media"", ""Baixa""], ""default"": ""Media"", ""description"": ""Prioridade inicial"" },
    ""tags"": { ""type"": ""array"", ""items"": { ""type"": ""string"" }, ""description"": ""Tags opcionais"" }
  },
  ""required"": [""clientId"", ""productId"", ""versionId"", ""environmentId"", ""title"", ""description""]
}"
        ),
        new LlmToolDefinition(
            "ProposeAddEvidence",
            "Propõe adicionar uma evidência estruturada a um caso (Fase 7.A). Requer confirmação humana.",
            @"{
  ""type"": ""object"",
  ""properties"": {
    ""caseId"": { ""type"": ""integer"", ""description"": ""ID do caso"" },
    ""title"": { ""type"": ""string"", ""description"": ""Título da evidência"" },
    ""description"": { ""type"": ""string"", ""description"": ""Descrição detalhada"" },
    ""evidenceType"": { ""type"": ""string"", ""enum"": [""Log"", ""Metric"", ""Trace"", ""Screenshot"", ""Config"", ""Outro""], ""description"": ""Tipo de evidência"" },
    ""source"": { ""type"": ""string"", ""description"": ""Origem (arquivo, URL, sistema)"" },
    ""collectedAt"": { ""type"": ""string"", ""format"": ""date-time"", ""description"": ""Quando foi coletada (ISO 8601)"" },
    ""tags"": { ""type"": ""array"", ""items"": { ""type"": ""string"" } }
  },
  ""required"": [""caseId"", ""title"", ""description"", ""evidenceType"", ""source"", ""collectedAt""]
}"
        ),
        new LlmToolDefinition(
            "ProposeLinkEvidenceToHypothesis",
            "Propõe vincular uma evidência existente a uma hipótese do caso. Requer confirmação humana.",
            @"{
  ""type"": ""object"",
  ""properties"": {
    ""caseId"": { ""type"": ""integer"", ""description"": ""ID do caso"" },
    ""evidenceId"": { ""type"": ""integer"", ""description"": ""ID da evidência"" },
    ""hypothesisId"": { ""type"": ""integer"", ""description"": ""ID da hipótese"" },
    ""relationship"": { ""type"": ""string"", ""enum"": [""Suporta"", ""Refuta"", ""Relacionado""], ""default"": ""Relacionado"", ""description"": ""Tipo de relação"" },
    ""notes"": { ""type"": ""string"", ""description"": ""Observações opcionais"" }
  },
  ""required"": [""caseId"", ""evidenceId"", ""hypothesisId""]
}"
        )
    };

    /// <summary>
    /// Todas as ferramentas disponíveis (leitura + escrita/proposta).
    /// </summary>
    public static readonly IReadOnlyList<LlmToolDefinition> AllTools = ReadingTools.Concat(WritingTools).ToArray();

    /// <summary>
    /// Mapa de nome da ferramenta de proposta → nome da ferramenta de execução correspondente.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> ProposalToExecutionMap = new Dictionary<string, string>
    {
        ["ProposeCaseCreation"] = "CreateCase",
        ["ProposeAddEvidence"] = "AddEvidence",
        ["ProposeLinkEvidenceToHypothesis"] = "LinkEvidenceToHypothesis"
    };
}