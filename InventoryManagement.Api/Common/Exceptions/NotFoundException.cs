namespace InventoryManagement.Api.Common.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string resourceName, object resourceKey)
        : base($"{resourceName} con clave '{resourceKey}' no encontrado")
    {
        ResourceName = resourceName;
        ResourceKey = resourceKey;
    }

    public string ResourceName { get; }
    public object ResourceKey { get; }
}