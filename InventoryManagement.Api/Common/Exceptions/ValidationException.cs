namespace InventoryManagement.Api.Common.Exceptions;

public class ValidationException : Exception
{
    public ValidationException(string message, IDictionary<string, string[]>? errors = null)
        : base(message)
    {
        Errors = errors ?? new Dictionary<string, string[]>();
    }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("Errores de validación")
    {
        Errors = errors;
    }

    public IDictionary<string, string[]> Errors { get; }
}