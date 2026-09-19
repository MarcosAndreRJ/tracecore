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
            "Busca casos por termo, filtros de status/prioridade/cliente/produto, paginação. Retorna lista resumida.",
            @"{
  ""type"": ""object"",
  ""properties"": {
    ""query"": { ""type"": ""string"", ""description"": ""Termo de busca livre (titulo, descricao, codigo erro)"" },
    ""status"": { ""type"": ""array"", ""items"": { ""type"": ""string"" }, ""description"": ""Filtrar por status (ex: Aberto, EmAnalise, Fechado)"" },
    ""priority"": { ""type"": ""array"", ""items"": { ""type"": ""string"" }, ""description"": ""Filtrar por prioridade (ex: Critica, Alta, Media, Baixa)"" },
    ""clientId"": { ""type"": ""integer"", ""description"": ""Filtrar por ID do cliente"" },
    ""productId"": { ""type"": ""integer"", ""description"": ""Filtrar por ID do produto"" },
    ""page"": { ""type"": ""integer"", ""default"": 1, ""description"": ""Página (1-based)"" },
    ""pageSize"": { ""type"": ""integer"", ""default"": 20, ""description"": ""Itens por página"" }
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