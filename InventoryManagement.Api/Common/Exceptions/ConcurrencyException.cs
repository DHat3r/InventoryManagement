namespace InventoryManagement.Api.Common.Exceptions;

public class ConcurrencyException : Exception
{
    public ConcurrencyException(string resourceName, object resourceKey)
        : base($"Conflicto de concurrencia: {resourceName} con clave '{resourceKey}' fue modificado por otro usuario")
    {
        ResourceName = resourceName;
        ResourceKey = resourceKey;
    }

    public string ResourceName { get; }
    public object ResourceKey { get; }
}