namespace InventoryManagement.Api.Common.Exceptions;

public class BusinessRuleException : Exception
{
    public BusinessRuleException(string rule, string? detail = null)
        : base($"Regla de negocio violada: {rule}" + (detail != null ? $" - {detail}" : ""))
    {
        Rule = rule;
        Detail = detail;
    }

    public string Rule { get; }
    public string? Detail { get; }
}