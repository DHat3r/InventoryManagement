namespace InventoryManagement.Api.Common;

/// <summary>
/// Respuesta estándar de la API para un solo recurso
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = new();
    public object? Meta { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null, object? meta = null)
        => new() { Success = true, Data = data, Message = message, Meta = meta };

    public static ApiResponse<T> Fail(List<string> errors, string? message = null)
        => new() { Success = false, Errors = errors, Message = message };

    public static ApiResponse<T> Fail(string error, string? message = null)
        => new() { Success = false, Errors = new List<string> { error }, Message = message };
}

/// <summary>
/// Respuesta estándar para listas paginadas
/// </summary>
public class PagedResult<T>
{
    public List<T> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    public static PagedResult<T> Create(List<T> data, int totalCount, int page, int pageSize)
        => new() { Data = data, TotalCount = totalCount, Page = page, PageSize = pageSize };
}

/// <summary>
/// Metadatos de paginación para headers HTTP
/// </summary>
public class PaginationMeta
{
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
}