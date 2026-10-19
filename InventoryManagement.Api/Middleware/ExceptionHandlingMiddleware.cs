using System.Net;
using System.Text.Json;
using InventoryManagement.Api.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace InventoryManagement.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.TraceIdentifier;
        
        _logger.LogError(exception, 
            "Excepción no controlada | CorrelationId: {CorrelationId} | Path: {Path} | Method: {Method}",
            correlationId, context.Request.Path, context.Request.Method);

        var problemDetails = CreateProblemDetails(context, exception, correlationId);
        
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        await context.Response.WriteAsync(json);
    }

    private static ProblemDetails CreateProblemDetails(HttpContext context, Exception exception, string correlationId)
    {
        return exception switch
        {
            NotFoundException ex => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                Title = "Recurso no encontrado",
                Status = StatusCodes.Status404NotFound,
                Detail = ex.Message,
                Instance = context.Request.Path,
                Extensions = new Dictionary<string, object?>
                {
                    ["correlationId"] = correlationId,
                    ["resourceName"] = ex.ResourceName,
                    ["resourceKey"] = ex.ResourceKey
                }
            },

            BusinessRuleException ex => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Regla de negocio violada",
                Status = StatusCodes.Status409Conflict,
                Detail = ex.Message,
                Instance = context.Request.Path,
                Extensions = new Dictionary<string, object?>
                {
                    ["correlationId"] = correlationId,
                    ["rule"] = ex.Rule,
                    ["detail"] = ex.Detail
                }
            },

            ConcurrencyException ex => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                Title = "Conflicto de concurrencia",
                Status = StatusCodes.Status409Conflict,
                Detail = ex.Message,
                Instance = context.Request.Path,
                Extensions = new Dictionary<string, object?>
                {
                    ["correlationId"] = correlationId,
                    ["resourceName"] = ex.ResourceName,
                    ["resourceKey"] = ex.ResourceKey
                }
            },

            ValidationException ex => new ValidationProblemDetails(ex.Errors)
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Errores de validación",
                Status = StatusCodes.Status400BadRequest,
                Detail = exception.Message,
                Instance = context.Request.Path,
                Extensions = new Dictionary<string, object?>
                {
                    ["correlationId"] = correlationId
                }
            },

            FluentValidation.ValidationException ex => new ValidationProblemDetails(ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Errores de validación",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Uno o más errores de validación",
                Instance = context.Request.Path,
                Extensions = new Dictionary<string, object?>
                {
                    ["correlationId"] = correlationId
                }
            },

            Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                Title = "Conflicto de concurrencia",
                Status = StatusCodes.Status409Conflict,
                Detail = "El registro fue modificado por otro usuario. Recargue e intente de nuevo.",
                Instance = context.Request.Path,
                Extensions = new Dictionary<string, object?>
                {
                    ["correlationId"] = correlationId
                }
            },

            _ => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Error interno del servidor",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "Ocurrió un error inesperado. Por favor intente más tarde.",
                Instance = context.Request.Path,
                Extensions = new Dictionary<string, object?>
                {
                    ["correlationId"] = correlationId
                }
            }
        };
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}