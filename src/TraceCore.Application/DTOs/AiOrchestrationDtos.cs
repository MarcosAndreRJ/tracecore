using System.Collections.Generic;

namespace TraceCore.Application.DTOs;

/// <summary>
/// Fase 14: DTOs para o Copiloto Operacional (tool calling, propostas, execução).
/// </summary>

/// <summary>
/// Resultado de uma chamada de ferramenta de LEITURA (executada diretamente).
/// </summary>
public record ToolCallResult(
    string ToolName,
    bool Success,
    string? ResultJson = null,
    string? ErrorMessage = null
);

/// <summary>
/// Proposta de execução de uma ferramenta de ESCRITA (aguarda confirmação humana).
/// </summary>
public record ToolCallProposal(
    string ProposalToolName,      // ex: "ProposeCaseCreation"
    string ExecutionToolName,     // ex: "CreateCase"
    string ArgumentsJson,         // argumentos originais da proposta
    string HumanReadableSummary,  // resumo legível para o usuário confirmar
    string RiskLevel              // "Alto" | "Médio" | "Baixo"
);

/// <summary>
/// Confirmação/rejeição de uma proposta pelo usuário.
/// </summary>
public record ToolCallConfirmation(
    string ProposalToolName,
    string ArgumentsJson,
    bool Confirmed,
    string? ModifiedArgumentsJson = null  // se o usuário editou os campos antes de confirmar
);

/// <summary>
/// Resultado da execução de uma ferramenta de ESCRITA após confirmação.
/// </summary>
public record ToolExecutionResult(
    string ExecutionToolName,
    bool Success,
    string? ResultJson = null,      // ex: "{ \"caseId\": 123, \"caseNumber\": \"CAS-2026-001\" }"
    string? ErrorMessage = null,
    string? EntityUrl = null        // link para a entidade criada/alterada
);

/// <summary>
/// Resposta completa do serviço de orquestração de IA.
/// </summary>
public record AiOrchestrationResponse(
    string? TextResponse,           // resposta textual final do modelo (após tool calls de leitura)
    IReadOnlyList<ToolCallResult>? ReadingToolResults,
    ToolCallProposal? WriteProposal, // se houver proposta de escrita pendente
    ToolExecutionResult? WriteExecutionResult // se uma proposta foi confirmada e executada
);