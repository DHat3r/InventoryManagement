using InventoryManagement.Api.Common;

namespace InventoryManagement.Api.Endpoints;

/// <summary>
/// Respuestas comunes a todos los módulos de endpoints.
/// Se importan con <c>using static</c> para usarlas como antes: <c>ValidationProblem(...)</c>, <c>OkResponse(...)</c>.
/// </summary>
public static class EndpointHelpers
{
    public static IResult ValidationProblem(FluentValidation.Results.ValidationResult result)
        => Results.ValidationProblem(result.ToDictionary());

    public static ApiResponse<T> OkResponse<T>(T data, string? message = null, object? meta = null)
        => ApiResponse<T>.Ok(data, message, meta);
}
