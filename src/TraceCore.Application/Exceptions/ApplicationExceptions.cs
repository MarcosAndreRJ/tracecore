using System;

namespace TraceCore.Application.Exceptions;

public class BusinessRuleValidationException : Exception
{
    public string RuleId { get; }

    public BusinessRuleValidationException(string ruleId, string message) : base(message)
    {
        RuleId = ruleId;
    }
}

public class EntityNotFoundException : Exception
{
    public EntityNotFoundException(string entityName, object key)
        : base($"{entityName} com identificador '{key}' não foi encontrado.")
    {
    }
}

public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}
