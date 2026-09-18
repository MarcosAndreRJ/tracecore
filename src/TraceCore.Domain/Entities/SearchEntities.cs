using System;

namespace TraceCore.Domain.Entities;

public class SearchSession
{
    public long Id { get; set; }
    public long? UserId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public string? ContextJson { get; set; }

    public SearchSession() { }

    public SearchSession(long? userId, string? contextJson = null)
    {
        UserId = userId;
        StartedAt = DateTime.UtcNow;
        ContextJson = contextJson;
    }
}

public class SearchQueryRecord
{
    public long Id { get; set; }
    public long SearchSessionId { get; set; }
    public string QueryText { get; set; } = string.Empty;
    public string? FiltersJson { get; set; }
    public int ResultCount { get; set; }
    public int DurationMs { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    public SearchQueryRecord() { }

    public SearchQueryRecord(long searchSessionId, string queryText, string? filtersJson, int resultCount, int durationMs)
    {
        SearchSessionId = searchSessionId;
        QueryText = queryText ?? string.Empty;
        FiltersJson = filtersJson;
        ResultCount = resultCount;
        DurationMs = durationMs;
        ExecutedAt = DateTime.UtcNow;
    }
}

public class SearchResultInteraction
{
    public long Id { get; set; }
    public long SearchQueryId { get; set; }
    public string ResultType { get; set; } = string.Empty; // Case, Solution, System, Component, Error, RootCause
    public long ResultId { get; set; }
    public int Position { get; set; }
    public DateTime? OpenedAt { get; set; }
    public bool? FeedbackUseful { get; set; }

    public SearchResultInteraction() { }

    public SearchResultInteraction(long searchQueryId, string resultType, long resultId, int position)
    {
        SearchQueryId = searchQueryId;
        ResultType = resultType;
        ResultId = resultId;
        Position = position;
    }

    public void MarkOpened(DateTime? openedAt = null)
    {
        OpenedAt = openedAt ?? DateTime.UtcNow;
    }

    public void SetFeedback(bool useful)
    {
        FeedbackUseful = useful;
    }
}
