using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using TraceCore.Domain.Entities;
using TraceCore.Domain.Repositories;

namespace TraceCore.Infrastructure.Persistence.Repositories;

public class MySqlDiagnosticRepository : IDiagnosticRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MySqlDiagnosticRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<DiagnosticSession?> GetOpenSessionByCaseIdAsync(long caseId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id, case_id AS CaseId, case_iteration_id AS CaseIterationId, status AS Status,
                   started_at AS StartedAt, started_by AS StartedBy, ended_at AS EndedAt
            FROM diagnostic_sessions
            WHERE case_id = @caseId AND status = 'Open'
            ORDER BY started_at DESC
            LIMIT 1;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<DiagnosticSession>(sql, new { caseId });
    }

    public async Task<long> CreateSessionAsync(DiagnosticSession session, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO diagnostic_sessions (case_id, case_iteration_id, status, started_at, started_by, ended_at)
            VALUES (@CaseId, @CaseIterationId, @Status, @StartedAt, @StartedBy, @EndedAt);
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, session);
        session.Id = id;
        return id;
    }

    public async Task<DiagnosticSession?> GetSessionByIdAsync(long sessionId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id, case_id AS CaseId, case_iteration_id AS CaseIterationId, status AS Status,
                   started_at AS StartedAt, started_by AS StartedBy, ended_at AS EndedAt
            FROM diagnostic_sessions
            WHERE id = @sessionId;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<DiagnosticSession>(sql, new { sessionId });
    }

    public async Task<long> AddHypothesisAsync(CaseHypothesis hypothesis, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO case_hypotheses (
                case_id, case_iteration_id, component_id, title, description, status,
                source_type, justification, created_at, created_by, updated_at
            ) VALUES (
                @CaseId, @CaseIterationId, @ComponentId, @Title, @Description, @Status,
                @SourceType, @Justification, @CreatedAt, @CreatedBy, @UpdatedAt
            );
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, hypothesis);
        hypothesis.Id = id;
        return id;
    }

    public async Task<CaseHypothesis?> GetHypothesisByIdAsync(long hypothesisId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id, case_id AS CaseId, case_iteration_id AS CaseIterationId, component_id AS ComponentId,
                   title AS Title, description AS Description, status AS Status,
                   source_type AS SourceType, justification AS Justification,
                   created_at AS CreatedAt, created_by AS CreatedBy, updated_at AS UpdatedAt
            FROM case_hypotheses
            WHERE id = @hypothesisId;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<CaseHypothesis>(sql, new { hypothesisId });
    }

    public async Task<IReadOnlyList<CaseHypothesis>> GetHypothesesByCaseIdAsync(long caseId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id, case_id AS CaseId, case_iteration_id AS CaseIterationId, component_id AS ComponentId,
                   title AS Title, description AS Description, status AS Status,
                   source_type AS SourceType, justification AS Justification,
                   created_at AS CreatedAt, created_by AS CreatedBy, updated_at AS UpdatedAt
            FROM case_hypotheses
            WHERE case_id = @caseId
            ORDER BY created_at ASC, id ASC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryAsync<CaseHypothesis>(sql, new { caseId });
        return list.ToList();
    }

    public async Task UpdateHypothesisAsync(CaseHypothesis hypothesis, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE case_hypotheses
            SET status = @Status,
                justification = @Justification,
                updated_at = @UpdatedAt
            WHERE id = @Id;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, hypothesis);
    }

    public async Task<int> GetNextStepSequenceNoAsync(long sessionId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT COALESCE(MAX(sequence_no), 0) + 1
            FROM diagnostic_steps
            WHERE diagnostic_session_id = @sessionId;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.ExecuteScalarAsync<int>(sql, new { sessionId });
    }

    public async Task<long> AddStepAsync(DiagnosticStep step, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO diagnostic_steps (
                diagnostic_session_id, sequence_no, step_type, hypothesis_id,
                title, objective, instruction, input_evidence_summary,
                result_summary, outcome, risk_level, duration_seconds,
                performed_by, performed_at, metadata_json, integration_run_id
            ) VALUES (
                @DiagnosticSessionId, @SequenceNo, @StepType, @HypothesisId,
                @Title, @Objective, @Instruction, @InputEvidenceSummary,
                @ResultSummary, @Outcome, @RiskLevel, @DurationSeconds,
                @PerformedBy, @PerformedAt, @MetadataJson, @IntegrationRunId
            );
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, step);
        step.Id = id;
        return id;
    }

    public async Task<DiagnosticStep?> GetStepByIdAsync(long stepId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id, diagnostic_session_id AS DiagnosticSessionId, sequence_no AS SequenceNo,
                   step_type AS StepType, hypothesis_id AS HypothesisId, title AS Title,
                   objective AS Objective, instruction AS Instruction, input_evidence_summary AS InputEvidenceSummary,
                   result_summary AS ResultSummary, outcome AS Outcome, risk_level AS RiskLevel,
duration_seconds AS DurationSeconds, performed_by AS PerformedBy,
                    performed_at AS PerformedAt, metadata_json AS MetadataJson,
                    integration_run_id AS IntegrationRunId
            FROM diagnostic_steps
            WHERE id = @stepId;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        return await conn.QueryFirstOrDefaultAsync<DiagnosticStep>(sql, new { stepId });
    }

    public async Task<IReadOnlyList<DiagnosticStep>> GetStepsBySessionIdAsync(long sessionId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id, diagnostic_session_id AS DiagnosticSessionId, sequence_no AS SequenceNo,
                   step_type AS StepType, hypothesis_id AS HypothesisId, title AS Title,
                   objective AS Objective, instruction AS Instruction, input_evidence_summary AS InputEvidenceSummary,
                   result_summary AS ResultSummary, outcome AS Outcome, risk_level AS RiskLevel,
duration_seconds AS DurationSeconds, performed_by AS PerformedBy,
                    performed_at AS PerformedAt, metadata_json AS MetadataJson,
                    integration_run_id AS IntegrationRunId
            FROM diagnostic_steps
            WHERE diagnostic_session_id = @sessionId
            ORDER BY sequence_no ASC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryAsync<DiagnosticStep>(sql, new { sessionId });
        return list.ToList();
    }

    public async Task<IReadOnlyList<DiagnosticStep>> GetStepsByCaseIdAsync(long caseId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT s.id AS Id, s.diagnostic_session_id AS DiagnosticSessionId, s.sequence_no AS SequenceNo,
                   s.step_type AS StepType, s.hypothesis_id AS HypothesisId, s.title AS Title,
                   s.objective AS Objective, s.instruction AS Instruction, s.input_evidence_summary AS InputEvidenceSummary,
                   s.result_summary AS ResultSummary, s.outcome AS Outcome, s.risk_level AS RiskLevel,
s.duration_seconds AS DurationSeconds, s.performed_by AS PerformedBy,
                    s.performed_at AS PerformedAt, s.metadata_json AS MetadataJson,
                    s.integration_run_id AS IntegrationRunId
            FROM diagnostic_steps s
            INNER JOIN diagnostic_sessions sess ON s.diagnostic_session_id = sess.id
            WHERE sess.case_id = @caseId
            ORDER BY s.performed_at ASC, s.sequence_no ASC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryAsync<DiagnosticStep>(sql, new { caseId });
        return list.ToList();
    }

    public async Task<IReadOnlyList<DiagnosticStep>> GetStepsByHypothesisIdAsync(long hypothesisId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id, diagnostic_session_id AS DiagnosticSessionId, sequence_no AS SequenceNo,
                   step_type AS StepType, hypothesis_id AS HypothesisId, title AS Title,
                   objective AS Objective, instruction AS Instruction, input_evidence_summary AS InputEvidenceSummary,
                   result_summary AS ResultSummary, outcome AS Outcome, risk_level AS RiskLevel,
duration_seconds AS DurationSeconds, performed_by AS PerformedBy,
                    performed_at AS PerformedAt, metadata_json AS MetadataJson,
                    integration_run_id AS IntegrationRunId
            FROM diagnostic_steps
            WHERE hypothesis_id = @hypothesisId
            ORDER BY performed_at ASC, sequence_no ASC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryAsync<DiagnosticStep>(sql, new { hypothesisId });
        return list.ToList();
    }

    public async Task<long> AddEvidenceAsync(CaseEvidence evidence, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO case_evidences (case_id, case_iteration_id, diagnostic_step_id, evidence_type, description, attachment_id, created_by, created_at, integration_run_id)
            VALUES (@CaseId, @CaseIterationId, @DiagnosticStepId, @EvidenceType, @Description, @AttachmentId, @CreatedBy, @CreatedAt, @IntegrationRunId);
            SELECT LAST_INSERT_ID();";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var id = await conn.ExecuteScalarAsync<long>(sql, evidence);
        evidence.Id = id;
        return id;
    }

    public async Task<CaseEvidence?> GetEvidenceByIdAsync(long evidenceId, CancellationToken ct = default)
    {
        const string sqlEvidence = @"
            SELECT id AS Id, case_id AS CaseId, case_iteration_id AS CaseIterationId, diagnostic_step_id AS DiagnosticStepId,
                   evidence_type AS EvidenceType, description AS Description, attachment_id AS AttachmentId,
                   created_by AS CreatedBy, created_at AS CreatedAt, integration_run_id AS IntegrationRunId
            FROM case_evidences
            WHERE id = @evidenceId
            LIMIT 1;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var evidence = await conn.QueryFirstOrDefaultAsync<CaseEvidence>(sqlEvidence, new { evidenceId });
        if (evidence != null)
        {
            evidence.HypothesisRelations = (await GetHypothesisRelationsByEvidenceIdAsync(evidenceId, ct)).ToList();
        }
        return evidence;
    }

    public async Task<IReadOnlyList<CaseEvidence>> GetEvidencesByCaseIdAsync(long caseId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id, case_id AS CaseId, case_iteration_id AS CaseIterationId, diagnostic_step_id AS DiagnosticStepId,
                   evidence_type AS EvidenceType, description AS Description, attachment_id AS AttachmentId,
                   created_by AS CreatedBy, created_at AS CreatedAt, integration_run_id AS IntegrationRunId
            FROM case_evidences
            WHERE case_id = @caseId
            ORDER BY created_at DESC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = (await conn.QueryAsync<CaseEvidence>(sql, new { caseId })).ToList();

        if (list.Count > 0)
        {
            var evidenceIds = list.Select(e => e.Id).ToList();
            const string sqlRels = @"
                SELECT id AS Id, evidence_id AS EvidenceId, hypothesis_id AS HypothesisId,
                       relation_type AS RelationType, justification AS Justification,
                       created_by AS CreatedBy, created_at AS CreatedAt
                FROM case_hypothesis_evidence
                WHERE evidence_id IN @evidenceIds;";

            var rels = (await conn.QueryAsync<CaseHypothesisEvidence>(sqlRels, new { evidenceIds })).ToList();
            foreach (var ev in list)
            {
                ev.HypothesisRelations = rels.Where(r => r.EvidenceId == ev.Id).ToList();
            }
        }

        return list;
    }

    public async Task<IReadOnlyList<CaseEvidence>> GetEvidencesByHypothesisIdAsync(long hypothesisId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT ce.id AS Id, ce.case_id AS CaseId, ce.case_iteration_id AS CaseIterationId, ce.diagnostic_step_id AS DiagnosticStepId,
                   ce.evidence_type AS EvidenceType, ce.description AS Description, ce.attachment_id AS AttachmentId,
                   ce.created_by AS CreatedBy, ce.created_at AS CreatedAt, ce.integration_run_id AS IntegrationRunId
            FROM case_evidences ce
            INNER JOIN case_hypothesis_evidence che ON ce.id = che.evidence_id
            WHERE che.hypothesis_id = @hypothesisId
            ORDER BY ce.created_at DESC;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = (await conn.QueryAsync<CaseEvidence>(sql, new { hypothesisId })).ToList();

        if (list.Count > 0)
        {
            var evidenceIds = list.Select(e => e.Id).ToList();
            const string sqlRels = @"
                SELECT id AS Id, evidence_id AS EvidenceId, hypothesis_id AS HypothesisId,
                       relation_type AS RelationType, justification AS Justification,
                       created_by AS CreatedBy, created_at AS CreatedAt
                FROM case_hypothesis_evidence
                WHERE evidence_id IN @evidenceIds;";

            var rels = (await conn.QueryAsync<CaseHypothesisEvidence>(sqlRels, new { evidenceIds })).ToList();
            foreach (var ev in list)
            {
                ev.HypothesisRelations = rels.Where(r => r.EvidenceId == ev.Id).ToList();
            }
        }

        return list;
    }

    public async Task AddHypothesisEvidenceRelationAsync(CaseHypothesisEvidence relation, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO case_hypothesis_evidence (evidence_id, hypothesis_id, relation_type, justification, created_by, created_at)
            VALUES (@EvidenceId, @HypothesisId, @RelationType, @Justification, @CreatedBy, @CreatedAt)
            ON DUPLICATE KEY UPDATE relation_type = @RelationType, justification = @Justification;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        await conn.ExecuteAsync(sql, new
        {
            relation.EvidenceId,
            relation.HypothesisId,
            RelationType = relation.RelationType.ToString(),
            relation.Justification,
            relation.CreatedBy,
            relation.CreatedAt
        });
    }

    public async Task<IReadOnlyList<CaseHypothesisEvidence>> GetHypothesisRelationsByEvidenceIdAsync(long evidenceId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id, evidence_id AS EvidenceId, hypothesis_id AS HypothesisId,
                   relation_type AS RelationType, justification AS Justification,
                   created_by AS CreatedBy, created_at AS CreatedAt
            FROM case_hypothesis_evidence
            WHERE evidence_id = @evidenceId;";

        using var conn = await _connectionFactory.CreateConnectionAsync(ct);
        var list = await conn.QueryAsync<CaseHypothesisEvidence>(sql, new { evidenceId });
        return list.ToList();
    }
}
